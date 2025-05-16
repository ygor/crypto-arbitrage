using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ArbitrageBot.Domain.Models;

namespace ArbitrageBot.Infrastructure.Exchanges;

/// <summary>
/// Provides optimized utility methods for exchange operations.
/// </summary>
public static class OptimizedExchangeUtils
{
    private static readonly Random _random = new();
    
    /// <summary>
    /// Fast parser for decimal values from Span to avoid string allocations.
    /// </summary>
    /// <param name="span">The character span to parse.</param>
    /// <returns>The parsed decimal value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ParseDecimalFast(ReadOnlySpan<char> span)
    {
        // Use the built-in decimal.Parse which is optimized for spans
        return decimal.Parse(span);
    }
    
    /// <summary>
    /// Tries to parse a decimal value from a JSON element without string allocations.
    /// </summary>
    /// <param name="jsonElement">The JSON element containing a number.</param>
    /// <param name="result">The parsed decimal value.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseDecimalFromJson(System.Text.Json.JsonElement jsonElement, out decimal result)
    {
        // Convert to a JsonNumber and use direct methods to avoid string allocations
        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
        {
            return jsonElement.TryGetDecimal(out result);
        }
        
        // Fallback to string parsing if it's not a direct number
        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return decimal.TryParse(jsonElement.GetString(), out result);
        }
        
        result = 0;
        return false;
    }
    
    /// <summary>
    /// Generates a unique client order ID with minimal allocations.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <returns>A unique client order ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static string GenerateOptimizedClientOrderId(ExchangeId exchangeId)
    {
        // Use ArrayPool to avoid allocations
        var buffer = ArrayPool<char>.Shared.Rent(32);
        try
        {
            // Format: EXCHANGE_PREFIX + TIMESTAMP + RANDOM
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var randomValue = _random.Next(10000, 99999);
            
            // Copy the exchange prefix
            var prefix = GetExchangePrefix(exchangeId);
            prefix.CopyTo(buffer.AsSpan());
            
            // Format timestamp and random values directly into the buffer
            var span = buffer.AsSpan(prefix.Length);
            timestamp.TryFormat(span, out var timestampChars);
            
            randomValue.TryFormat(span.Slice(timestampChars), out var randomChars);
            
            return new string(buffer, 0, prefix.Length + timestampChars + randomChars);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
    
    /// <summary>
    /// Gets the exchange-specific prefix for an order ID.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <returns>The exchange-specific prefix.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> GetExchangePrefix(ExchangeId exchangeId)
    {
        return exchangeId.Value switch
        {
            "binance" => "BIN",
            "coinbase" => "CBP",
            "kraken" => "KRK",
            _ => "EXC"
        };
    }
    
    /// <summary>
    /// Quickly checks if a potential arbitrage opportunity is profitable after fees.
    /// </summary>
    /// <param name="buyPrice">The price to buy at.</param>
    /// <param name="sellPrice">The price to sell at.</param>
    /// <param name="buyFeeRate">The fee rate for buying.</param>
    /// <param name="sellFeeRate">The fee rate for selling.</param>
    /// <returns>True if the opportunity is profitable after fees; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsProfitableAfterFees(decimal buyPrice, decimal sellPrice, decimal buyFeeRate, decimal sellFeeRate)
    {
        // Calculate the effective prices after fees
        decimal effectiveBuyPrice = buyPrice * (1 + buyFeeRate);
        decimal effectiveSellPrice = sellPrice * (1 - sellFeeRate);
        
        // The opportunity is profitable if the sell price (after fees) is higher than the buy price (after fees)
        return effectiveSellPrice > effectiveBuyPrice;
    }
    
    /// <summary>
    /// Quickly calculates the estimated profit for an arbitrage opportunity, accounting for fees.
    /// </summary>
    /// <param name="buyPrice">The price to buy at.</param>
    /// <param name="sellPrice">The price to sell at.</param>
    /// <param name="quantity">The quantity to trade.</param>
    /// <param name="buyFeeRate">The fee rate for buying.</param>
    /// <param name="sellFeeRate">The fee rate for selling.</param>
    /// <returns>The estimated profit, accounting for fees.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static decimal CalculateEstimatedProfit(
        decimal buyPrice, 
        decimal sellPrice, 
        decimal quantity, 
        decimal buyFeeRate, 
        decimal sellFeeRate)
    {
        // Calculate buying cost including fees
        decimal buyCost = quantity * buyPrice * (1 + buyFeeRate);
        
        // Calculate selling revenue after fees
        decimal sellRevenue = quantity * sellPrice * (1 - sellFeeRate);
        
        // Return the net profit
        return sellRevenue - buyCost;
    }
} 