using System;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Configuration settings for notifications
/// </summary>
public class NotificationConfiguration
{
    /// <summary>
    /// Unique identifier for the notification configuration
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Whether email notifications are enabled
    /// </summary>
    public bool EmailEnabled { get; set; }
    
    /// <summary>
    /// Email address for notifications
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether SMS notifications are enabled
    /// </summary>
    public bool SmsEnabled { get; set; }
    
    /// <summary>
    /// Phone number for SMS notifications
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether webhook notifications are enabled
    /// </summary>
    public bool WebhookEnabled { get; set; }
    
    /// <summary>
    /// Webhook URL for notifications
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Minimum profit threshold to trigger notifications
    /// </summary>
    public decimal MinProfitThreshold { get; set; }
    
    /// <summary>
    /// Whether to notify on arbitrage opportunities detected
    /// </summary>
    public bool NotifyOnOpportunityDetected { get; set; }
    
    /// <summary>
    /// Whether to notify on trades executed
    /// </summary>
    public bool NotifyOnTradeExecuted { get; set; }
    
    /// <summary>
    /// Whether to notify on errors
    /// </summary>
    public bool NotifyOnError { get; set; }
    
    // New properties required by the application
    
    /// <summary>
    /// Whether to notify on arbitrage opportunities
    /// </summary>
    public bool NotifyOnArbitrageOpportunities { get; set; } = true;
    
    /// <summary>
    /// Whether to notify on completed trades
    /// </summary>
    public bool NotifyOnCompletedTrades { get; set; } = true;
    
    /// <summary>
    /// Whether to notify on failed trades
    /// </summary>
    public bool NotifyOnFailedTrades { get; set; } = true;
    
    /// <summary>
    /// Minimum error severity for notifications
    /// </summary>
    public ErrorSeverity MinimumErrorSeverityForNotification { get; set; } = ErrorSeverity.Warning;
    
    /// <summary>
    /// Whether to send daily statistics
    /// </summary>
    public bool SendDailyStatistics { get; set; } = false;
    
    /// <summary>
    /// Email configuration details
    /// </summary>
    public EmailConfiguration Email { get; set; } = new EmailConfiguration();
    
    /// <summary>
    /// SMS configuration details
    /// </summary>
    public SmsConfiguration Sms { get; set; } = new SmsConfiguration();
    
    /// <summary>
    /// Webhook configuration details
    /// </summary>
    public WebhookConfiguration Webhook { get; set; } = new WebhookConfiguration();
    
    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
} 