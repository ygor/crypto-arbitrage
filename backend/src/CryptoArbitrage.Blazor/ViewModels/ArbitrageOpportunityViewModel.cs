using System.ComponentModel.DataAnnotations;

namespace CryptoArbitrage.Blazor.ViewModels;

/// <summary>
/// View model for arbitrage opportunities optimized for Blazor UI binding.
/// Maps from ArbitrageOpportunity domain model with property name fixes and UI-specific formatting.
/// </summary>
public class ArbitrageOpportunityViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the opportunity.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the trading pair symbol (e.g., "BTC/USDT").
    /// Maps from: TradingPair.Symbol
    /// </summary>
    [Display(Name = "Trading Pair")]
    public string TradingPair { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the buy exchange name.
    /// Maps from: BuyExchangeId
    /// </summary>
    [Display(Name = "Buy Exchange")]
    public string BuyExchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the sell exchange name.
    /// Maps from: SellExchangeId
    /// </summary>
    [Display(Name = "Sell Exchange")]
    public string SellExchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the buy price.
    /// Maps from: BuyPrice
    /// </summary>
    [Display(Name = "Buy Price")]
    [DisplayFormat(DataFormatString = "{0:C4}")]
    public decimal BuyPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the sell price.
    /// Maps from: SellPrice
    /// </summary>
    [Display(Name = "Sell Price")]
    [DisplayFormat(DataFormatString = "{0:C4}")]
    public decimal SellPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the profit percentage.
    /// Maps from: ProfitPercentage (or SpreadPercentage if ProfitPercentage is not set)
    /// </summary>
    [Display(Name = "Profit %")]
    [DisplayFormat(DataFormatString = "{0:F2}%")]
    public decimal ProfitPercentage { get; set; }
    
    /// <summary>
    /// Gets or sets the potential profit amount.
    /// Maps from: EstimatedProfit
    /// </summary>
    [Display(Name = "Potential Profit")]
    [DisplayFormat(DataFormatString = "{0:C2}")]
    public decimal PotentialProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the opportunity was detected.
    /// Maps from: DetectedAt
    /// </summary>
    [Display(Name = "Detected At")]
    public DateTime DetectedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the status of the opportunity.
    /// Maps from: Status.ToString()
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the effective volume for the opportunity.
    /// Maps from: EffectiveQuantity
    /// </summary>
    [Display(Name = "Volume")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal Volume { get; set; }
    
    // UI-specific computed properties for consistent formatting
    
    /// <summary>
    /// Gets the formatted buy price with currency symbol.
    /// </summary>
    public string FormattedBuyPrice => $"${BuyPrice:F4}";
    
    /// <summary>
    /// Gets the formatted sell price with currency symbol.
    /// </summary>
    public string FormattedSellPrice => $"${SellPrice:F4}";
    
    /// <summary>
    /// Gets the formatted profit percentage.
    /// </summary>
    public string FormattedProfitPercentage => $"{ProfitPercentage:F2}%";
    
    /// <summary>
    /// Gets the formatted potential profit with currency symbol.
    /// </summary>
    public string FormattedPotentialProfit => $"${PotentialProfit:F2}";
    
    /// <summary>
    /// Gets the formatted detection time (HH:mm:ss).
    /// </summary>
    public string FormattedDetectedAt => DetectedAt.ToString("HH:mm:ss");
    
    /// <summary>
    /// Gets the time elapsed since detection in human-readable format.
    /// </summary>
    public string TimeSinceDetection => FormatTimeSince(DetectedAt);
    
    /// <summary>
    /// Gets the spread amount (sell price - buy price).
    /// </summary>
    public decimal SpreadAmount => SellPrice - BuyPrice;
    
    /// <summary>
    /// Gets the formatted spread amount with currency symbol.
    /// </summary>
    public string FormattedSpreadAmount => $"${SpreadAmount:F4}";
    
    private static string FormatTimeSince(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        return timeSpan.TotalMinutes switch
        {
            < 1 => "Just now",
            < 60 => $"{(int)timeSpan.TotalMinutes}m ago",
            < 1440 => $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m ago",
            _ => $"{(int)timeSpan.TotalDays}d ago"
        };
    }
} 