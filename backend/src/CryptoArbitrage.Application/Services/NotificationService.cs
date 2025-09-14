using System.Text;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Services;

/// <summary>
/// Service for sending notifications about arbitrage events.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<NotificationService> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    public NotificationService(
        IConfigurationService configurationService,
        ILogger<NotificationService> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <inheritdoc />
    public async Task NotifyOpportunityDetectedAsync(
        ArbitrageOpportunity opportunity, 
        CancellationToken cancellationToken = default)
    {
        // Get notification configuration
        var config = await _configurationService.GetNotificationConfigurationAsync(cancellationToken);
        
        // Check if this notification is enabled
        if (!config.NotifyOnArbitrageOpportunities)
        {
            return;
        }
        
        // Build message
        var message = new StringBuilder()
            .AppendLine($"Arbitrage Opportunity Detected")
            .AppendLine($"Trading Pair: {opportunity.TradingPair}")
            .AppendLine($"Buy from: {opportunity.BuyExchangeId} at {opportunity.BuyPrice}")
            .AppendLine($"Sell to: {opportunity.SellExchangeId} at {opportunity.SellPrice}")
            .AppendLine($"Spread: {opportunity.SpreadPercentage:F2}%")
            .AppendLine($"Max Quantity: {opportunity.MaxQuantity}")
            .AppendLine($"Estimated Profit: {opportunity.EstimatedProfit} {opportunity.TradingPair.QuoteCurrency}")
            .AppendLine($"ROI: {opportunity.ROIPercentage:F2}%")
            .AppendLine($"Time: {opportunity.Timestamp:yyyy-MM-dd HH:mm:ss.fff}")
            .ToString();
        
        _logger.LogInformation("Sending opportunity notification: {TradingPair}, Spread: {SpreadPercentage}%", 
            opportunity.TradingPair, opportunity.SpreadPercentage);
        
        try
        {
            // Send notifications based on configuration
            await SendNotificationsAsync(message, "Arbitrage Opportunity", config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending opportunity notification");
        }
    }
    
    /// <inheritdoc />
    public async Task NotifyArbitrageCompletedAsync(
        ArbitrageOpportunity opportunity, 
        TradeResult buyResult, 
        TradeResult sellResult, 
        decimal profit, 
        CancellationToken cancellationToken = default)
    {
        // Get notification configuration
        var config = await _configurationService.GetNotificationConfigurationAsync(cancellationToken);
        
        // Check if this notification is enabled
        if (!config.NotifyOnCompletedTrades)
        {
            return;
        }
        
        // Ensure we have valid trade executions
        if (!buyResult.IsSuccess || !sellResult.IsSuccess || 
            buyResult.TradeExecution == null || sellResult.TradeExecution == null)
        {
            _logger.LogWarning("Cannot send completion notification: Missing trade execution details");
            return;
        }
        
        // Get the trade values
        var buyTrade = buyResult.TradeExecution;
        var sellTrade = sellResult.TradeExecution;
        
        // Ensure the price and fee fields have safe defaults if null
        decimal buyPrice = buyTrade.Price;
        decimal sellPrice = sellTrade.Price;
        decimal buyFee = buyTrade.Fee;
        decimal sellFee = sellTrade.Fee;
        decimal quantity = buyTrade.Quantity;
        decimal buyTotal = buyTrade.TotalValue;
        
        // Build message with safe values
        var message = new StringBuilder()
            .AppendLine($"Arbitrage Trade Completed Successfully")
            .AppendLine($"Trading Pair: {opportunity.TradingPair}")
            .AppendLine($"Buy from: {opportunity.BuyExchangeId} at {buyPrice} (Fee: {buyFee})")
            .AppendLine($"Sell to: {opportunity.SellExchangeId} at {sellPrice} (Fee: {sellFee})")
            .AppendLine($"Quantity: {quantity}")
            .AppendLine($"Profit: {profit} {opportunity.TradingPair.QuoteCurrency}")
            .AppendLine($"ROI: {(buyTotal > 0 ? (profit / buyTotal) * 100 : 0):F2}%")
            .AppendLine($"Buy Execution Time: {buyResult.ExecutionTimeMs}ms")
            .AppendLine($"Sell Execution Time: {sellResult.ExecutionTimeMs}ms")
            .AppendLine($"Time: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff}")
            .ToString();
        
        _logger.LogInformation(
            "Sending completed arbitrage notification: {TradingPair}, Profit: {Profit} {Currency}", 
            opportunity.TradingPair, profit, opportunity.TradingPair.QuoteCurrency);
        
        try
        {
            // Send notifications based on configuration
            await SendNotificationsAsync(message, "Arbitrage Completed", config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending completed arbitrage notification");
        }
    }
    
    /// <inheritdoc />
    public async Task NotifyArbitrageFailedAsync(
        ArbitrageOpportunity opportunity, 
        string errorMessage, 
        CancellationToken cancellationToken = default)
    {
        // Get notification configuration
        var config = await _configurationService.GetNotificationConfigurationAsync(cancellationToken);
        
        // Check if this notification is enabled
        if (!config.NotifyOnFailedTrades)
        {
            return;
        }
        
        // Build message
        var message = new StringBuilder()
            .AppendLine($"Arbitrage Trade Failed")
            .AppendLine($"Trading Pair: {opportunity.TradingPair}")
            .AppendLine($"Buy from: {opportunity.BuyExchangeId} at {opportunity.BuyPrice}")
            .AppendLine($"Sell to: {opportunity.SellExchangeId} at {opportunity.SellPrice}")
            .AppendLine($"Error: {errorMessage}")
            .AppendLine($"Time: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff}")
            .ToString();
        
        _logger.LogInformation(
            "Sending failed arbitrage notification: {TradingPair}, Error: {ErrorMessage}", 
            opportunity.TradingPair, errorMessage);
        
        try
        {
            // Send notifications based on configuration
            await SendNotificationsAsync(message, "Arbitrage Failed", config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending failed arbitrage notification");
        }
    }
    
    /// <inheritdoc />
    public async Task NotifySystemErrorAsync(
        Exception error, 
        ErrorSeverity severity, 
        CancellationToken cancellationToken = default)
    {
        // Forward to the overload with more details
        await NotifySystemErrorAsync(
            "System Error", 
            $"{error.Message}\n{error.StackTrace}", 
            severity, 
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task NotifySystemErrorAsync(
        string title,
        string message,
        ErrorSeverity severity,
        CancellationToken cancellationToken = default)
    {
        // Get notification configuration
        var config = await _configurationService.GetNotificationConfigurationAsync(cancellationToken);
        
        // Check if this notification is enabled based on severity
        if ((int)severity < (int)config.MinimumErrorSeverityForNotification)
        {
            return;
        }
        
        // Build message
        var messageBuilder = new StringBuilder()
            .AppendLine($"{title}: {severity}")
            .AppendLine($"Details: {message}")
            .AppendLine($"Time: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff}")
            .ToString();
        
        _logger.LogInformation(
            "Sending system error notification: {Title}, Severity: {Severity}", 
            title, severity);
        
        try
        {
            // Send notifications based on configuration
            await SendNotificationsAsync(messageBuilder, $"{title}: {severity}", config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending system error notification");
        }
    }
    
    /// <inheritdoc />
    public async Task NotifyDailyStatisticsAsync(
        ArbitrageStatistics statistics, 
        CancellationToken cancellationToken = default)
    {
        // Get notification configuration
        var config = await _configurationService.GetNotificationConfigurationAsync(cancellationToken);
        
        // Check if this notification is enabled
        if (!config.SendDailyStatistics)
        {
            return;
        }
        
        // Build message
        var message = new StringBuilder()
            .AppendLine($"Daily Arbitrage Statistics")
            .AppendLine($"Period: {statistics.StartTime:yyyy-MM-dd HH:mm:ss} - {statistics.EndTime:yyyy-MM-dd HH:mm:ss}")
            .AppendLine($"Total Opportunities: {statistics.TotalOpportunitiesCount}")
            .AppendLine($"Total Trades: {statistics.TotalTradesCount}")
            .AppendLine($"Successful Trades: {statistics.SuccessfulTradesCount}")
            .AppendLine($"Failed Trades: {statistics.FailedTradesCount}")
            .AppendLine($"Success Rate: {statistics.SuccessRate:F2}%")
            .AppendLine($"Total Profit: {statistics.TotalProfitAmount:F8}")
            .AppendLine($"Highest Profit: {statistics.HighestProfitAmount:F8}")
            .AppendLine($"Lowest Profit: {statistics.LowestProfit:F8}")
            .AppendLine($"Average Profit: {statistics.AverageProfitAmount:F8}")
            .AppendLine($"Total Volume: {statistics.TotalVolume:F8}")
            .AppendLine($"Total Fees: {statistics.TotalFeesAmount:F8}")
            .AppendLine($"Avg Execution Time: {statistics.AverageExecutionTimeMs:F2}ms")
            .ToString();
        
        _logger.LogInformation("Sending daily statistics notification");
        
        try
        {
            // Send notifications based on configuration
            await SendNotificationsAsync(message, "Daily Statistics", config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending daily statistics notification");
        }
    }
    
    /// <inheritdoc />
    public async Task SendNotificationAsync(
        string title,
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get notification configuration
            var config = await _configurationService.GetNotificationConfigurationAsync(cancellationToken);
            
            // Build full message including details if provided
            var fullMessage = new StringBuilder()
                .AppendLine(message);
            
            if (!string.IsNullOrEmpty(details))
            {
                fullMessage.AppendLine($"Details: {details}");
            }
            
            fullMessage.AppendLine($"Time: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");
            
            _logger.LogInformation("Sending general notification: {Title}", title);
            
            // Send notifications through all configured channels
            await SendNotificationsAsync(fullMessage.ToString(), title, config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending general notification: {Title}", title);
        }
    }
    
    /// <summary>
    /// Sends notifications through all configured channels.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="subject">The message subject.</param>
    /// <param name="config">The notification configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SendNotificationsAsync(
        string message, 
        string subject, 
        NotificationConfiguration config, 
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        
        // Email notification
        if (config.EmailEnabled)
        {
            tasks.Add(SendEmailAsync(message, subject, config.Email, cancellationToken));
        }
        
        // SMS notification
        if (config.SmsEnabled)
        {
            tasks.Add(SendSmsAsync(message, config.Sms, cancellationToken));
        }
        
        // Webhook notification
        if (config.WebhookEnabled)
        {
            tasks.Add(SendWebhookAsync(message, subject, config.Webhook, cancellationToken));
        }
        
        // Wait for all notifications to complete
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// Sends an email notification.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="subject">The email subject.</param>
    /// <param name="config">The email configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task SendEmailAsync(
        string message, 
        string subject, 
        EmailConfiguration config, 
        CancellationToken cancellationToken)
    {
        // This is a placeholder implementation
        // In a real implementation, we would use SmtpClient or a third-party library like MailKit
        _logger.LogInformation("Email notification would be sent: Subject={Subject}, To={Recipients}", 
            subject, string.Join(", ", config.ToAddresses));
        
        // Simulate sending email
        return Task.Delay(100, cancellationToken);
    }
    
    /// <summary>
    /// Sends an SMS notification.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="config">The SMS configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task SendSmsAsync(
        string message, 
        SmsConfiguration config, 
        CancellationToken cancellationToken)
    {
        // This is a placeholder implementation
        // In a real implementation, we would use a third-party SMS provider like Twilio
        _logger.LogInformation("SMS notification would be sent: To={Recipients}", 
            string.Join(", ", config.ToNumbers));
        
        // Simulate sending SMS
        return Task.Delay(100, cancellationToken);
    }
    
    /// <summary>
    /// Sends a webhook notification.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="subject">The notification subject.</param>
    /// <param name="config">The webhook configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task SendWebhookAsync(
        string message, 
        string subject, 
        WebhookConfiguration config, 
        CancellationToken cancellationToken)
    {
        // This is a placeholder implementation
        // In a real implementation, we would use HttpClient to post to the webhook URL
        _logger.LogInformation("Webhook notification would be sent to: {Url}", config.Url);
        
        // Simulate sending webhook
        return Task.Delay(100, cancellationToken);
    }
} 