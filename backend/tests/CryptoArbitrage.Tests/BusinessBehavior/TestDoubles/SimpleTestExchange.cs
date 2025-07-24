using CryptoArbitrage.Domain.Models;
using System;
using System.Collections.Generic;

namespace CryptoArbitrage.Tests.BusinessBehavior.TestDoubles;

/// <summary>
/// Simple test exchange for business behavior testing.
/// Focuses on business outcomes rather than technical interface compliance.
/// </summary>
public class SimpleTestExchange
{
    public string ExchangeId { get; }
    private readonly Dictionary<string, PriceQuote> _priceQuotes = new();
    private readonly Dictionary<string, decimal> _balances = new();
    
    public SimpleTestExchange(string exchangeId)
    {
        ExchangeId = exchangeId;
        
        // Setup realistic initial balances for testing
        _balances["USD"] = 10000m;  
        _balances["BTC"] = 1.0m;    
        _balances["ETH"] = 10.0m;   
        _balances["ADA"] = 1000m;   
    }
    
    public void SetMarketPrice(string tradingPair, decimal bidPrice, decimal askPrice, decimal volume = 5.0m)
    {
        _priceQuotes[tradingPair] = new PriceQuote(
            ExchangeId,
            TradingPair.Parse(tradingPair),
            DateTime.UtcNow,
            bidPrice,
            volume,
            askPrice,
            volume
        );
    }
    
    public void UpdatePrice(string tradingPair, decimal newBidPrice, decimal newAskPrice)
    {
        if (_priceQuotes.TryGetValue(tradingPair, out var existingQuote))
        {
            _priceQuotes[tradingPair] = new PriceQuote(
                ExchangeId,
                existingQuote.TradingPair,
                DateTime.UtcNow,
                newBidPrice,
                existingQuote.BestBidQuantity,
                newAskPrice,
                existingQuote.BestAskQuantity
            );
        }
    }
    
    public PriceQuote? GetPriceQuote(string tradingPair)
    {
        return _priceQuotes.GetValueOrDefault(tradingPair);
    }
    
    public decimal GetBalance(string currency)
    {
        return _balances.GetValueOrDefault(currency, 0m);
    }
    
    public void UpdateBalance(string currency, decimal amount)
    {
        _balances[currency] = amount;
    }
} 