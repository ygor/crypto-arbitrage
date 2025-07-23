using AutoMapper;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Blazor.ViewModels;

namespace CryptoArbitrage.Blazor.Mapping;

/// <summary>
/// AutoMapper profile for mapping domain models to Blazor ViewModels.
/// Handles property name changes, nested property flattening, and computed values.
/// </summary>
public class BlazorMappingProfile : Profile
{
    public BlazorMappingProfile()
    {
        ConfigureArbitrageOpportunityMapping();
        ConfigureTradeResultMapping();
    }

    private void ConfigureArbitrageOpportunityMapping()
    {
        CreateMap<ArbitrageOpportunity, ArbitrageOpportunityViewModel>()
            // Direct property mappings
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BuyPrice, opt => opt.MapFrom(src => src.BuyPrice))
            .ForMember(dest => dest.SellPrice, opt => opt.MapFrom(src => src.SellPrice))
            .ForMember(dest => dest.DetectedAt, opt => opt.MapFrom(src => src.DetectedAt))
            
            // Property name changes
            .ForMember(dest => dest.TradingPair, opt => opt.MapFrom(src => src.TradingPair.ToString()))
            .ForMember(dest => dest.BuyExchange, opt => opt.MapFrom(src => src.BuyExchangeId))
            .ForMember(dest => dest.SellExchange, opt => opt.MapFrom(src => src.SellExchangeId))
            .ForMember(dest => dest.PotentialProfit, opt => opt.MapFrom(src => src.EstimatedProfit))
            
            // Status mapping
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            
            // Volume mapping
            .ForMember(dest => dest.Volume, opt => opt.MapFrom(src => src.EffectiveQuantity))
            
            // Profit percentage mapping with fallback to SpreadPercentage if ProfitPercentage is not set
            .ForMember(dest => dest.ProfitPercentage, opt => opt.MapFrom(src => 
                src.ProfitPercentage > 0 ? src.ProfitPercentage : src.SpreadPercentage));
    }

    private void ConfigureTradeResultMapping()
    {
        // Map from TradeResult (what the repository actually returns)
        CreateMap<TradeResult, TradeResultViewModel>()
            // Direct property mappings
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.ErrorMessage, opt => opt.MapFrom(src => src.ErrorMessage))
            
            // Property name changes
            .ForMember(dest => dest.IsSuccessful, opt => opt.MapFrom(src => src.IsSuccess))
            .ForMember(dest => dest.ActualProfit, opt => opt.MapFrom(src => CalculateTradeProfit(src)))
            .ForMember(dest => dest.ProfitPercentage, opt => opt.MapFrom(src => CalculateProfitPercentage(src)))
            
            // Map from TradeResult properties directly
            .ForMember(dest => dest.TradingPair, opt => opt.MapFrom(src => src.TradingPair))
            .ForMember(dest => dest.BuyExchange, opt => opt.MapFrom(src => src.Side == OrderSide.Buy ? src.ExchangeId : "N/A"))
            .ForMember(dest => dest.SellExchange, opt => opt.MapFrom(src => src.Side == OrderSide.Sell ? src.ExchangeId : "N/A"))
            .ForMember(dest => dest.BuyPrice, opt => opt.MapFrom(src => src.Side == OrderSide.Buy ? src.ExecutedPrice : 0))
            .ForMember(dest => dest.SellPrice, opt => opt.MapFrom(src => src.Side == OrderSide.Sell ? src.ExecutedPrice : 0))
            
            // Volume from executed quantity
            .ForMember(dest => dest.Volume, opt => opt.MapFrom(src => src.ExecutedQuantity))
            
            // Status description
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsSuccess ? "Completed" : "Failed"))
            
            // Trade-specific properties
            .ForMember(dest => dest.TotalFees, opt => opt.MapFrom(src => src.Fee))
            .ForMember(dest => dest.ExecutionTimeMs, opt => opt.MapFrom(src => (int)src.ExecutionTimeMs))
            .ForMember(dest => dest.ExecutedAt, opt => opt.MapFrom(src => src.Timestamp))
            
            // Use TradeResult ID
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));
    }

    /// <summary>
    /// Calculates the profit from a single trade result.
    /// This is a simplified calculation for individual trades.
    /// </summary>
    /// <param name="tradeResult">The trade result.</param>
    /// <returns>The calculated profit.</returns>
    private static decimal CalculateTradeProfit(TradeResult tradeResult)
    {
        // For individual trades, profit calculation is complex without market context
        // Return a simple approximation or 0 for now
        return 0m; // This would need proper implementation with market data
    }

    /// <summary>
    /// Calculates the profit percentage from a single trade result.
    /// </summary>
    /// <param name="tradeResult">The trade result.</param>
    /// <returns>The calculated profit percentage.</returns>
    private static decimal CalculateProfitPercentage(TradeResult tradeResult)
    {
        // Similar to profit calculation, this needs market context
        return 0m; // This would need proper implementation with market data
    }

    /// <summary>
    /// Calculates the trade volume from the trade result.
    /// Uses the effective quantity from the opportunity as the primary source.
    /// </summary>
    /// <param name="tradeResult">The trade result.</param>
    /// <returns>The calculated volume.</returns>
    private static decimal CalculateTradeVolume(ArbitrageTradeResult tradeResult)
    {
        // Priority 1: Use EffectiveQuantity from the opportunity if available
        if (tradeResult.Opportunity.EffectiveQuantity > 0)
        {
            return tradeResult.Opportunity.EffectiveQuantity;
        }
        
        // Priority 2: Use the minimum of buy and sell quantities
        var buyQuantity = tradeResult.Opportunity.BuyQuantity;
        var sellQuantity = tradeResult.Opportunity.SellQuantity;
        
        if (buyQuantity > 0 && sellQuantity > 0)
        {
            return Math.Min(buyQuantity, sellQuantity);
        }
        
        // Priority 3: Use quantity from actual trade results if available
        if (tradeResult.BuyResult?.Quantity > 0)
        {
            return tradeResult.BuyResult.Quantity;
        }
        
        if (tradeResult.SellResult?.Quantity > 0)
        {
            return tradeResult.SellResult.Quantity;
        }
        
        // Fallback: Return 0 if no volume information is available
        return 0m;
    }

    /// <summary>
    /// Calculates the total fees from the trade result.
    /// Sums fees from buy and sell operations if available.
    /// </summary>
    /// <param name="tradeResult">The trade result.</param>
    /// <returns>The calculated total fees.</returns>
    private static decimal CalculateTotalFees(ArbitrageTradeResult tradeResult)
    {
        var totalFees = 0m;
        
        if (tradeResult.BuyResult != null)
        {
            totalFees += tradeResult.BuyResult.Fee;
        }
        
        if (tradeResult.SellResult != null)
        {
            totalFees += tradeResult.SellResult.Fee;
        }
        
        return totalFees;
    }

    /// <summary>
    /// Calculates the execution time in milliseconds.
    /// Returns a reasonable default if actual timing data is not available.
    /// </summary>
    /// <param name="tradeResult">The trade result.</param>
    /// <returns>The execution time in milliseconds.</returns>
    private static int CalculateExecutionTime(ArbitrageTradeResult tradeResult)
    {
        // If we have both buy and sell results with timing, use the max execution time
        if (tradeResult.BuyResult?.ExecutionTimeMs > 0 && tradeResult.SellResult?.ExecutionTimeMs > 0)
        {
            return (int)Math.Max(tradeResult.BuyResult.ExecutionTimeMs, tradeResult.SellResult.ExecutionTimeMs);
        }
        
        // Use individual result timing if available
        if (tradeResult.BuyResult?.ExecutionTimeMs > 0)
        {
            return (int)tradeResult.BuyResult.ExecutionTimeMs;
        }
        
        if (tradeResult.SellResult?.ExecutionTimeMs > 0)
        {
            return (int)tradeResult.SellResult.ExecutionTimeMs;
        }
        
        // Fallback: return 0
        return 0;
    }
} 