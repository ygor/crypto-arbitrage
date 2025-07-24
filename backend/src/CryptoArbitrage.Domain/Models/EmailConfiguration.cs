using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Email configuration settings
/// </summary>
public class EmailConfiguration
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    
    // Additional properties required by application
    public string Host => SmtpServer;
    public int Port => SmtpPort;
    public bool UseSsl { get; set; } = true;
    public List<string> ToAddresses { get; set; } = new List<string>();
} 