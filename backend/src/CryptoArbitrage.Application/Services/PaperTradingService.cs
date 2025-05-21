using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Application.Services;

/// <summary>
/// Service for paper trading functionality.
/// </summary>
public class PaperTradingService : IPaperTradingService
{
    private readonly ILogger<PaperTradingService> _logger;
    private readonly ConcurrentDictionary<(string ExchangeId, string Currency), decimal> _balances;
    private readonly ConcurrentBag<TradeResult> _tradeHistory;
    private readonly decimal _defaultFeePercentage = 0.001m; // 0.1% fee

    /// <summary>
    /// Gets a value indicating whether paper trading is enabled.
    /// </summary>
    public bool IsPaperTradingEnabled { get; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaperTradingService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public PaperTradingService(ILogger<PaperTradingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _balances = new ConcurrentDictionary<(string, string), decimal>();
        _tradeHistory = new ConcurrentBag<TradeResult>();
    }

    /// <inheritdoc />
    public Task InitializeAsync(Dictionary<string, Dictionary<string, decimal>>? initialBalances = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing paper trading service");
        
        // Clear existing balances
        _balances.Clear();
        _tradeHistory.Clear();

        // Set initial balances
        if (initialBalances != null)
        {
            foreach (var exchangeKvp in initialBalances)
            {
                string exchangeId = exchangeKvp.Key;
                foreach (var currencyKvp in exchangeKvp.Value)
                {
                    string currency = currencyKvp.Key;
                    decimal amount = currencyKvp.Value;
                    
                    var key = (exchangeId, currency);
                    _balances[key] = amount;
                    
                    _logger.LogInformation("Set initial balance for {ExchangeId} {Currency}: {Amount}", 
                        exchangeId, currency, amount);
                }
            }
        }
        else
        {
            // Set default balances
            var defaultBalances = new Dictionary<string, Dictionary<string, decimal>>
            {
                {
                    "Binance", new Dictionary<string, decimal>
                    {
                        { "BTC", 1.0m },
                        { "ETH", 10.0m },
                        { "USDT", 50000.0m }
                    }
                },
                {
                    "Coinbase", new Dictionary<string, decimal>
                    {
                        { "BTC", 1.0m },
                        { "ETH", 10.0m },
                        { "USDT", 50000.0m }
                    }
                },
                {
                    "Kraken", new Dictionary<string, decimal>
                    {
                        { "BTC", 1.0m },
                        { "ETH", 10.0m },
                        { "USDT", 50000.0m }
                    }
                }
            };

            foreach (var exchangeKvp in defaultBalances)
            {
                string exchangeId = exchangeKvp.Key;
                foreach (var currencyKvp in exchangeKvp.Value)
                {
                    string currency = currencyKvp.Key;
                    decimal amount = currencyKvp.Value;
                    
                    var key = (exchangeId, currency);
                    _balances[key] = amount;
                }
            }
        }

        _logger.LogInformation("Paper trading service initialized with {Count} balances", _balances.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<TradeResult> SimulateMarketBuyOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating market buy order on {Exchange}: {Quantity} {BaseCurrency} at market price", 
            exchangeId, quantity, tradingPair.BaseCurrency);
        
        try
        {
            // Check if we have enough quote currency (e.g. USDT)
            var quoteKey = (exchangeId, tradingPair.QuoteCurrency);
            if (!_balances.TryGetValue(quoteKey, out decimal quoteBalance))
            {
                return Task.FromResult(CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Buy", 
                    $"No {tradingPair.QuoteCurrency} balance found for {exchangeId}"));
            }

            // Simulate a market price (for paper trading we'll use a simple approximation)
            decimal pricePerUnit = 0;
            if (tradingPair.BaseCurrency == "BTC" && tradingPair.QuoteCurrency == "USDT")
                pricePerUnit = 50000m; // Example BTC price
            else if (tradingPair.BaseCurrency == "ETH" && tradingPair.QuoteCurrency == "USDT")
                pricePerUnit = 3000m; // Example ETH price
            else
                pricePerUnit = 100m; // Default for other pairs

            // Calculate total cost and fees
            decimal totalCost = quantity * pricePerUnit;
            decimal fee = totalCost * _defaultFeePercentage;
            decimal totalWithFees = totalCost + fee;

            // Check if we have enough balance
            if (quoteBalance < totalWithFees)
            {
                return Task.FromResult(CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Buy", 
                    $"Insufficient {tradingPair.QuoteCurrency} balance: {quoteBalance} < {totalWithFees}"));
            }

            // Update balances
            _balances[quoteKey] = quoteBalance - totalWithFees;
            
            var baseKey = (exchangeId, tradingPair.BaseCurrency);
            _balances.AddOrUpdate(
                baseKey,
                quantity, // Initial value if key doesn't exist
                (key, oldValue) => oldValue + quantity // Update function if key exists
            );

            // Create successful trade result
            var result = new TradeResult
            {
                Id = Guid.NewGuid(),
                OrderId = $"paper-{Guid.NewGuid()}",
                ExchangeId = exchangeId,
                TradingPair = tradingPair.ToString(),
                Side = OrderSide.Buy,
                RequestedPrice = pricePerUnit, 
                ExecutedPrice = pricePerUnit,
                RequestedQuantity = quantity,
                ExecutedQuantity = quantity,
                Timestamp = DateTime.UtcNow,
                Status = TradeStatus.Completed,
                Fees = fee,
                FeeCurrency = tradingPair.QuoteCurrency,
                ExecutionTimeMs = 0, // Simulated trade is instant
                IsSuccess = true
            };
            
            // Add to trade history
            _tradeHistory.Add(result);
            
            _logger.LogInformation("Paper trading buy successful: {BaseCurrency} {Quantity} @ {Price} {QuoteCurrency}", 
                tradingPair.BaseCurrency, quantity, pricePerUnit, tradingPair.QuoteCurrency);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating market buy order");
            return Task.FromResult(CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Buy", ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<TradeResult> SimulateMarketSellOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating market sell order on {Exchange}: {Quantity} {BaseCurrency} at market price", 
            exchangeId, quantity, tradingPair.BaseCurrency);
        
        try
        {
            // Check if we have enough base currency (e.g. BTC)
            var baseKey = (exchangeId, tradingPair.BaseCurrency);
            if (!_balances.TryGetValue(baseKey, out decimal baseBalance))
            {
                return Task.FromResult(CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Sell", 
                    $"No {tradingPair.BaseCurrency} balance found for {exchangeId}"));
            }

            if (baseBalance < quantity)
            {
                return Task.FromResult(CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Sell", 
                    $"Insufficient {tradingPair.BaseCurrency} balance: {baseBalance} < {quantity}"));
            }

            // Simulate a market price (for paper trading we'll use a simple approximation)
            decimal pricePerUnit = 0;
            if (tradingPair.BaseCurrency == "BTC" && tradingPair.QuoteCurrency == "USDT")
                pricePerUnit = 50000m; // Example BTC price
            else if (tradingPair.BaseCurrency == "ETH" && tradingPair.QuoteCurrency == "USDT")
                pricePerUnit = 3000m; // Example ETH price
            else
                pricePerUnit = 100m; // Default for other pairs

            // Calculate total proceeds and fees
            decimal totalProceeds = quantity * pricePerUnit;
            decimal fee = totalProceeds * _defaultFeePercentage;
            decimal netProceeds = totalProceeds - fee;

            // Update balances
            _balances[baseKey] = baseBalance - quantity;
            
            var quoteKey = (exchangeId, tradingPair.QuoteCurrency);
            _balances.AddOrUpdate(
                quoteKey,
                netProceeds, // Initial value if key doesn't exist
                (key, oldValue) => oldValue + netProceeds // Update function if key exists
            );

            // Create successful trade result
            var result = new TradeResult
            {
                Id = Guid.NewGuid(),
                OrderId = $"paper-{Guid.NewGuid()}",
                ExchangeId = exchangeId,
                TradingPair = tradingPair.ToString(),
                Side = OrderSide.Sell,
                RequestedPrice = pricePerUnit,
                ExecutedPrice = pricePerUnit,
                RequestedQuantity = quantity,
                ExecutedQuantity = quantity,
                Timestamp = DateTime.UtcNow,
                Status = TradeStatus.Completed,
                Fees = fee,
                FeeCurrency = tradingPair.QuoteCurrency,
                ExecutionTimeMs = 0, // Simulated trade is instant
                IsSuccess = true
            };
            
            // Add to trade history
            _tradeHistory.Add(result);
            
            _logger.LogInformation("Paper trading sell successful: {BaseCurrency} {Quantity} @ {Price} {QuoteCurrency}", 
                tradingPair.BaseCurrency, quantity, pricePerUnit, tradingPair.QuoteCurrency);
            
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating market sell order");
            return Task.FromResult(CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Sell", ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>> GetAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        var groupedBalances = _balances
            .GroupBy(kvp => kvp.Key.ExchangeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(kvp => new Balance(
                    kvp.Key.ExchangeId,
                    kvp.Key.Currency,
                    kvp.Value, // Total
                    kvp.Value, // Available (paper trading has no locked funds)
                    0         // Reserved
                )).ToList() as IReadOnlyCollection<Balance>
            );

        return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>>(groupedBalances);
    }

    /// <inheritdoc />
    public Task<Balance?> GetBalanceAsync(string exchangeId, string asset, CancellationToken cancellationToken = default)
    {
        var key = (exchangeId, asset);
        if (_balances.TryGetValue(key, out decimal amount))
        {
            return Task.FromResult<Balance?>(new Balance(
                exchangeId,
                asset,
                amount, // Total
                amount, // Available
                0       // Reserved
            ));
        }

        return Task.FromResult<Balance?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<TradeResult>> GetTradeHistoryAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<TradeResult>>(_tradeHistory.ToList());
    }

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resetting paper trading service");
        _balances.Clear();
        _tradeHistory.Clear();
        return Task.CompletedTask;
    }

    private TradeResult CreateFailedTradeResult(string exchangeId, TradingPair tradingPair, decimal quantity, string side, string errorMessage)
    {
        var tradeResult = new TradeResult
        {
            Id = Guid.NewGuid(),
            OrderId = $"paper-failed-{Guid.NewGuid()}",
            ExchangeId = exchangeId,
            TradingPair = tradingPair.ToString(),
            Side = side.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? OrderSide.Buy : OrderSide.Sell,
            RequestedQuantity = quantity,
            ExecutedQuantity = 0,
            Timestamp = DateTime.UtcNow,
            Status = TradeStatus.Failed,
            ErrorMessage = errorMessage,
            IsSuccess = false
        };
        
        _tradeHistory.Add(tradeResult);
        
        _logger.LogWarning("Paper trading {Side} failed: {ErrorMessage}", side, errorMessage);
        
        return tradeResult;
    }
} 