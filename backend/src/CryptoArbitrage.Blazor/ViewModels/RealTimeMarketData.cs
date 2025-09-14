using CryptoArbitrage.Domain.Models;

namespace CryptoArbitrage.Blazor.ViewModels;

public class RealTimeMarketDataViewModel
{
    public string TradingPair { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public List<ExchangeOrderBookViewModel> Exchanges { get; set; } = new();
    public List<ArbitrageSpreadViewModel> ArbitrageSpreads { get; set; } = new();
}

public class ExchangeOrderBookViewModel
{
    public string ExchangeId { get; set; } = string.Empty;
    public string TradingPair { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsConnected { get; set; }
    public bool IsRealTime { get; set; }
    
    public decimal BestBidPrice { get; set; }
    public decimal BestBidQuantity { get; set; }
    public decimal BestAskPrice { get; set; }
    public decimal BestAskQuantity { get; set; }
    
    public decimal Spread { get; set; }
    public decimal SpreadPercentage { get; set; }
    
    public List<OrderBookEntryViewModel> TopBids { get; set; } = new();
    public List<OrderBookEntryViewModel> TopAsks { get; set; } = new();
    
    public decimal TotalBidVolume { get; set; }
    public decimal TotalAskVolume { get; set; }
}

public class OrderBookEntryViewModel
{
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal Total { get; set; }
    public OrderSide Side { get; set; }
    public double VolumePercentage { get; set; }
    public string PriceChangeClass { get; set; } = string.Empty;
}

public class ArbitrageSpreadViewModel
{
    public string TradingPair { get; set; } = string.Empty;
    public string BuyExchange { get; set; } = string.Empty;
    public string SellExchange { get; set; } = string.Empty;
    
    public decimal BuyPrice { get; set; }
    public decimal SellPrice { get; set; }
    public decimal ProfitPerUnit { get; set; }
    public decimal ProfitPercentage { get; set; }
    
    public decimal MaxTradeQuantity { get; set; }
    public decimal EstimatedProfit { get; set; }
    
    public DateTime DetectedAt { get; set; }
    public bool IsViable { get; set; }
    public string ViabilityReason { get; set; } = string.Empty;
    
    public string ProfitabilityClass { get; set; } = string.Empty;
    public double ProfitabilityScore { get; set; }
}

public class OrderBookUpdateMessage
{
    public string Type { get; set; } = "OrderBook";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ExchangeId { get; set; } = string.Empty;
    public string TradingPair { get; set; } = string.Empty;
    public ExchangeOrderBookViewModel OrderBook { get; set; } = new();
}

public class ArbitrageOpportunityUpdateMessage
{
    public string Type { get; set; } = "ArbitrageOpportunity";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ArbitrageSpreadViewModel Opportunity { get; set; } = new();
} 