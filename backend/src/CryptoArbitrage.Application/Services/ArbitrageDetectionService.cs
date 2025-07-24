using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Application.Services;

/// <summary>
/// 🎯 REAL BUSINESS LOGIC: Core arbitrage detection service
/// 
/// This service implements the ACTUAL arbitrage detection functionality
/// that our business behavior tests require.
/// </summary>
public interface IArbitrageDetectionService
{
    Task StartDetectionAsync(IEnumerable<string> exchanges, IEnumerable<string> tradingPairs);
    Task StopDetectionAsync();
    Task<IEnumerable<ArbitrageOpportunity>> ScanForOpportunitiesAsync();
    bool IsRunning { get; }
}

public class ArbitrageDetectionService : IArbitrageDetectionService
{
    private readonly IMarketDataAggregator _marketDataAggregator;
    private readonly IArbitrageRepository _repository;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ArbitrageDetectionService> _logger;
    
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _detectionTask;
    private bool _isRunning = false;

    public bool IsRunning => _isRunning;

    public ArbitrageDetectionService(
        IMarketDataAggregator marketDataAggregator,
        IArbitrageRepository repository,
        IConfigurationService configurationService,
        ILogger<ArbitrageDetectionService> logger)
    {
        _marketDataAggregator = marketDataAggregator ?? throw new ArgumentNullException(nameof(marketDataAggregator));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartDetectionAsync(IEnumerable<string> exchanges, IEnumerable<string> tradingPairs)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Arbitrage detection is already running");
        }

        _logger.LogInformation("Starting arbitrage detection for exchanges: {Exchanges}, pairs: {TradingPairs}", 
            string.Join(", ", exchanges), string.Join(", ", tradingPairs));

        // Start market data monitoring
        await _marketDataAggregator.StartMonitoringAsync(exchanges, tradingPairs);

        // Start background detection task
        _cancellationTokenSource = new CancellationTokenSource();
        _detectionTask = RunDetectionLoopAsync(_cancellationTokenSource.Token, tradingPairs);
        _isRunning = true;

        _logger.LogInformation("Arbitrage detection started successfully");
    }

    public async Task StopDetectionAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping arbitrage detection...");

        // Stop market data monitoring
        await _marketDataAggregator.StopMonitoringAsync();

        // Cancel detection task
        _cancellationTokenSource?.Cancel();
        
        if (_detectionTask != null)
        {
            try
            {
                await _detectionTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        _isRunning = false;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _detectionTask = null;

        _logger.LogInformation("Arbitrage detection stopped");
    }

    public async Task<IEnumerable<ArbitrageOpportunity>> ScanForOpportunitiesAsync()
    {
        var config = await _configurationService.GetConfigurationAsync();
        var opportunities = new List<ArbitrageOpportunity>();

        foreach (var tradingPair in config.TradingPairs)
        {
            var prices = await _marketDataAggregator.GetLatestPricesAsync(tradingPair.ToString());
            var priceList = prices.ToList();

            if (priceList.Count < 2)
            {
                _logger.LogDebug("Insufficient price data for {TradingPair}: only {Count} exchanges", 
                    tradingPair, priceList.Count);
                continue;
            }

            // Find the best arbitrage opportunities
            var detectedOpportunities = FindArbitrageOpportunities(priceList, config.RiskProfile);
            opportunities.AddRange(detectedOpportunities);
        }

        // Save profitable opportunities
        foreach (var opportunity in opportunities)
        {
            await _repository.SaveOpportunityAsync(opportunity);
            _logger.LogInformation("Detected arbitrage opportunity: {Opportunity}", opportunity);
        }

        return opportunities;
    }

    private async Task RunDetectionLoopAsync(CancellationToken cancellationToken, IEnumerable<string> tradingPairs)
    {
        _logger.LogInformation("Starting continuous arbitrage detection loop");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ScanForOpportunitiesAsync();
                
                // Wait 5 seconds before next scan
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during arbitrage detection scan");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }

        _logger.LogInformation("Arbitrage detection loop ended");
    }

    private List<ArbitrageOpportunity> FindArbitrageOpportunities(
        List<PriceQuote> prices, 
        RiskProfile riskProfile)
    {
        var opportunities = new List<ArbitrageOpportunity>();

        // Compare every exchange pair to find arbitrage opportunities
        for (int i = 0; i < prices.Count; i++)
        {
            for (int j = i + 1; j < prices.Count; j++)
            {
                var price1 = prices[i];
                var price2 = prices[j];

                // Check both directions: buy from exchange1, sell to exchange2
                var opportunity1 = CreateOpportunityIfProfitable(price1, price2, riskProfile);
                if (opportunity1 != null)
                {
                    opportunities.Add(opportunity1);
                }

                // Check reverse: buy from exchange2, sell to exchange1  
                var opportunity2 = CreateOpportunityIfProfitable(price2, price1, riskProfile);
                if (opportunity2 != null)
                {
                    opportunities.Add(opportunity2);
                }
            }
        }

        return opportunities;
    }

    private ArbitrageOpportunity? CreateOpportunityIfProfitable(
        PriceQuote buyExchange, 
        PriceQuote sellExchange, 
        RiskProfile riskProfile)
    {
        // Calculate spread: buy at ask price, sell at bid price
        var buyPrice = buyExchange.AskPrice;
        var sellPrice = sellExchange.BidPrice;

        if (sellPrice <= buyPrice)
        {
            return null; // No arbitrage possible
        }

        var spread = sellPrice - buyPrice;
        var spreadPercentage = (spread / buyPrice) * 100m;

        // Check if spread meets minimum profit threshold
        if (spreadPercentage < riskProfile.MinProfitThresholdPercent)
        {
            return null;
        }

        // Calculate estimated profit (assuming 1 unit trade)
        var tradeAmount = Math.Min(buyExchange.AskVolume, sellExchange.BidVolume);
        tradeAmount = Math.Min(tradeAmount, riskProfile.MaxTradeAmount);
        
        var estimatedProfit = spread * tradeAmount;

        // Apply trading fees (assuming 0.1% per trade)
        var tradingFees = (buyPrice + sellPrice) * 0.001m * tradeAmount;
        var netProfit = estimatedProfit - tradingFees;

        if (netProfit <= 0)
        {
            return null; // Not profitable after fees
        }

        return new ArbitrageOpportunity
        {
            Id = Guid.NewGuid().ToString(),
            TradingPair = buyExchange.TradingPair,
            BuyExchangeId = buyExchange.ExchangeId,
            SellExchangeId = sellExchange.ExchangeId,
            BuyPrice = buyPrice,
            SellPrice = sellPrice,
            SpreadPercentage = spreadPercentage,
            EstimatedProfit = netProfit,
            DetectedAt = DateTime.UtcNow,
            Status = ArbitrageOpportunityStatus.Detected,
            MaxTradeAmount = tradeAmount
        };
    }
} 