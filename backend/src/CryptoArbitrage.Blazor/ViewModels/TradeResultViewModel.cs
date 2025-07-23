using System.ComponentModel.DataAnnotations;

namespace CryptoArbitrage.Blazor.ViewModels;

/// <summary>
/// View model for trade results optimized for Blazor UI binding.
/// Maps from ArbitrageTradeResult domain model with flattened nested properties and property name fixes.
/// </summary>
public class TradeResultViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the trade result.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the date and time when the trade was executed.
    /// Maps from: Timestamp
    /// </summary>
    [Display(Name = "Executed At")]
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Gets or sets whether the trade was successful.
    /// Maps from: IsSuccess
    /// </summary>
    [Display(Name = "Successful")]
    public bool IsSuccessful { get; set; }
    
    /// <summary>
    /// Gets or sets the actual profit from the trade.
    /// Maps from: ProfitAmount
    /// </summary>
    [Display(Name = "Actual Profit")]
    [DisplayFormat(DataFormatString = "{0:C2}")]
    public decimal ActualProfit { get; set; }
    
    /// <summary>
    /// Gets or sets the trading pair symbol.
    /// Maps from: Opportunity.TradingPair.Symbol
    /// </summary>
    [Display(Name = "Trading Pair")]
    public string TradingPair { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the buy exchange name.
    /// Maps from: Opportunity.BuyExchangeId
    /// </summary>
    [Display(Name = "Buy Exchange")]
    public string BuyExchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the sell exchange name.
    /// Maps from: Opportunity.SellExchangeId
    /// </summary>
    [Display(Name = "Sell Exchange")]
    public string SellExchange { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the buy price.
    /// Maps from: Opportunity.BuyPrice
    /// </summary>
    [Display(Name = "Buy Price")]
    [DisplayFormat(DataFormatString = "{0:C4}")]
    public decimal BuyPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the sell price.
    /// Maps from: Opportunity.SellPrice
    /// </summary>
    [Display(Name = "Sell Price")]
    [DisplayFormat(DataFormatString = "{0:C4}")]
    public decimal SellPrice { get; set; }
    
    /// <summary>
    /// Gets or sets the trade volume.
    /// Calculated from: Min(Opportunity.BuyQuantity, Opportunity.SellQuantity) or BuyResult/SellResult quantities
    /// </summary>
    [Display(Name = "Volume")]
    [DisplayFormat(DataFormatString = "{0:F4}")]
    public decimal Volume { get; set; }
    
    /// <summary>
    /// Gets or sets the profit percentage.
    /// Maps from: ProfitPercentage
    /// </summary>
    [Display(Name = "Profit %")]
    [DisplayFormat(DataFormatString = "{0:F2}%")]
    public decimal ProfitPercentage { get; set; }
    
    /// <summary>
    /// Gets or sets the error message if the trade failed.
    /// Maps from: ErrorMessage
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the status description.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the total fees for the trade.
    /// Calculated from buy and sell fees if available.
    /// </summary>
    [Display(Name = "Total Fees")]
    [DisplayFormat(DataFormatString = "{0:C2}")]
    public decimal TotalFees { get; set; }
    
    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    [Display(Name = "Execution Time")]
    public int ExecutionTimeMs { get; set; }
    
    /// <summary>
    /// Gets or sets the execution timestamp.
    /// Maps from: Timestamp (same property)
    /// </summary>
    [Display(Name = "Executed At")]
    public DateTime ExecutedAt { get; set; }
    
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
    /// Gets the formatted actual profit with currency symbol and color indicator.
    /// </summary>
    public string FormattedActualProfit => ActualProfit >= 0 ? $"+${ActualProfit:F2}" : $"-${Math.Abs(ActualProfit):F2}";
    
    /// <summary>
    /// Gets the formatted profit percentage.
    /// </summary>
    public string FormattedProfitPercentage => $"{ProfitPercentage:F2}%";
    
    /// <summary>
    /// Gets the formatted volume.
    /// </summary>
    public string FormattedVolume => $"{Volume:F4}";
    
    /// <summary>
    /// Gets the formatted execution time.
    /// </summary>
    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    
    /// <summary>
    /// Gets the formatted short execution time.
    /// </summary>
    public string FormattedShortTimestamp => Timestamp.ToString("HH:mm:ss");
    
    /// <summary>
    /// Gets the time elapsed since execution in human-readable format.
    /// </summary>
    public string TimeSinceExecution => FormatTimeSince(Timestamp);
    
    /// <summary>
    /// Gets the spread amount (sell price - buy price).
    /// </summary>
    public decimal SpreadAmount => SellPrice - BuyPrice;
    
    /// <summary>
    /// Gets the formatted spread amount with currency symbol.
    /// </summary>
    public string FormattedSpreadAmount => $"${SpreadAmount:F4}";
    
    /// <summary>
    /// Gets the trade result indicator for UI display.
    /// </summary>
    public string ResultIndicator => IsSuccessful ? "Success" : "Failed";
    
    /// <summary>
    /// Gets whether the trade was profitable.
    /// </summary>
    public bool IsProfitable => ActualProfit > 0;
    
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