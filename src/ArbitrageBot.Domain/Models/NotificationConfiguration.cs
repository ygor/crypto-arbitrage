namespace ArbitrageBot.Domain.Models;

/// <summary>
/// Represents the configuration for notifications.
/// </summary>
public class NotificationConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether email notifications are enabled.
    /// </summary>
    public bool EmailEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the email configuration.
    /// </summary>
    public EmailConfiguration Email { get; set; } = new EmailConfiguration();
    
    /// <summary>
    /// Gets or sets a value indicating whether SMS notifications are enabled.
    /// </summary>
    public bool SmsEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the SMS configuration.
    /// </summary>
    public SmsConfiguration Sms { get; set; } = new SmsConfiguration();
    
    /// <summary>
    /// Gets or sets a value indicating whether webhook notifications are enabled.
    /// </summary>
    public bool WebhookEnabled { get; set; }
    
    /// <summary>
    /// Gets or sets the webhook configuration.
    /// </summary>
    public WebhookConfiguration Webhook { get; set; } = new WebhookConfiguration();
    
    /// <summary>
    /// Gets or sets the minimum error severity level for notifications.
    /// </summary>
    public ErrorSeverity MinimumErrorSeverityForNotification { get; set; } = ErrorSeverity.Medium;
    
    /// <summary>
    /// Gets or sets a value indicating whether to notify on arbitrage opportunities.
    /// </summary>
    public bool NotifyOnArbitrageOpportunities { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether to notify on completed trades.
    /// </summary>
    public bool NotifyOnCompletedTrades { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether to notify on failed trades.
    /// </summary>
    public bool NotifyOnFailedTrades { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether to send daily statistics.
    /// </summary>
    public bool SendDailyStatistics { get; set; } = true;
}

/// <summary>
/// Represents the configuration for email notifications.
/// </summary>
public class EmailConfiguration
{
    /// <summary>
    /// Gets or sets the SMTP server.
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    public int SmtpPort { get; set; } = 587;
    
    /// <summary>
    /// Gets or sets a value indicating whether to use SSL.
    /// </summary>
    public bool UseSsl { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the from address.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the recipient addresses.
    /// </summary>
    public List<string> ToAddresses { get; set; } = new List<string>();
}

/// <summary>
/// Represents the configuration for SMS notifications.
/// </summary>
public class SmsConfiguration
{
    /// <summary>
    /// Gets or sets the provider (e.g., Twilio, SendGrid).
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the account SID or ID.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the auth token or API key.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the from phone number.
    /// </summary>
    public string FromNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the recipient phone numbers.
    /// </summary>
    public List<string> ToNumbers { get; set; } = new List<string>();
}

/// <summary>
/// Represents the configuration for webhook notifications.
/// </summary>
public class WebhookConfiguration
{
    /// <summary>
    /// Gets or sets the webhook URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets additional HTTP headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
} 