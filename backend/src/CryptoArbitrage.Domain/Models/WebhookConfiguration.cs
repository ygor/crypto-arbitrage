using System.Collections.Generic;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Webhook configuration settings
/// </summary>
public class WebhookConfiguration
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public string ContentType { get; set; } = "application/json";
    public int TimeoutMs { get; set; } = 5000;
    
    // Additional properties required by application
    public string AuthToken { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
} 