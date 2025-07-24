using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// SMS configuration settings
/// </summary>
public class SmsConfiguration
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
    
    // Additional properties required by application
    public List<string> ToNumbers { get; set; } = new List<string>();
    public string AccountId { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
} 