using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoArbitrage.Infrastructure.Services;

/// <summary>
/// Service for simulating trades without actual execution on exchanges.
/// </summary>
public class PaperTradingService : IPaperTradingService
{
    private readonly IConfigurationService _configurationService;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<PaperTradingService> _logger;
    
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, decimal>> _balances = new();
    private readonly ConcurrentBag<TradeResult> _tradeHistory = new();
    
    // Default balance amounts for simulation if none provided
    private const decimal DEFAULT_FIAT_BALANCE = 10000m; // $10,000 USD/USDT/etc
    private const decimal DEFAULT_CRYPTO_BALANCE = 1m;   // 1 BTC/ETH/etc
    
    // Fee structure for simulated trades (default to somewhat conservative estimates)
    private readonly Dictionary<string, decimal> _exchangeFees = new()
    {
        ["binance"] = 0.001m,  // 0.1% fee
        ["coinbase"] = 0.005m, // 0.5% fee
        ["kraken"] = 0.0026m,  // 0.26% fee
        ["default"] = 0.002m   // 0.2% default fee for other exchanges
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PaperTradingService"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="marketDataService">The market data service.</param>
    /// <param name="logger">The logger.</param>
    public PaperTradingService(
        IConfigurationService configurationService,
        IMarketDataService marketDataService,
        ILogger<PaperTradingService> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsPaperTradingEnabled => GetPaperTradingEnabledSetting();

    private bool GetPaperTradingEnabledSetting()
    {
        var config = _configurationService.GetConfigurationAsync().GetAwaiter().GetResult();
        return config?.PaperTradingEnabled ?? false;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(Dictionary<string, Dictionary<string, decimal>>? initialBalances = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing paper trading service");
        
        // Clear any existing data
        _balances.Clear();
        _tradeHistory.Clear();
        
        var config = await _configurationService.GetConfigurationAsync(cancellationToken);
        var tradingPairs = config?.TradingPairs?.ToList() ?? new List<TradingPair>();
        var exchanges = new HashSet<string>() { "binance", "coinbase", "kraken" };
        
        if (initialBalances != null)
        {
            // Use provided balances
            foreach (var (exchange, assets) in initialBalances)
            {
                var exchangeBalances = _balances.GetOrAdd(exchange.ToLowerInvariant(), new ConcurrentDictionary<string, decimal>());
                
                foreach (var (asset, amount) in assets)
                {
                    exchangeBalances[asset.ToUpperInvariant()] = amount;
                }
            }
        }
        else
        {
            // Set up default balances for each exchange
            foreach (var exchange in exchanges)
            {
                var exchangeBalances = _balances.GetOrAdd(exchange, new ConcurrentDictionary<string, decimal>());
                
                // Add default balances for common trading pairs
                foreach (var pair in tradingPairs)
                {
                    // Add base asset (e.g., BTC in BTC/USDT)
                    exchangeBalances.TryAdd(pair.BaseCurrency.ToUpperInvariant(), DEFAULT_CRYPTO_BALANCE);
                    
                    // Add quote asset (e.g., USDT in BTC/USDT)
                    exchangeBalances.TryAdd(pair.QuoteCurrency.ToUpperInvariant(), DEFAULT_FIAT_BALANCE);
                }
                
                // Ensure we have some common assets
                exchangeBalances.TryAdd("BTC", DEFAULT_CRYPTO_BALANCE);
                exchangeBalances.TryAdd("ETH", DEFAULT_CRYPTO_BALANCE);
                exchangeBalances.TryAdd("USDT", DEFAULT_FIAT_BALANCE);
                exchangeBalances.TryAdd("USD", DEFAULT_FIAT_BALANCE);
            }
        }

        _logger.LogInformation("Paper trading service initialized with balances for {ExchangeCount} exchanges", _balances.Count);
    }

    /// <inheritdoc/>
    public async Task<TradeResult> SimulateMarketBuyOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating market buy order on {Exchange}: {Quantity} {BaseCurrency} at market price", 
            exchangeId, quantity, tradingPair.BaseCurrency);
        
        try
        {
            // Normalize exchange ID
            exchangeId = exchangeId.ToLowerInvariant();
            
            // Get latest price from market data service
            var orderBook = _marketDataService.GetLatestOrderBook(exchangeId, tradingPair);
            if (orderBook == null || orderBook.Asks.Count == 0)
            {
                _logger.LogError("Could not retrieve order book or no asks available for {Exchange} {TradingPair}", exchangeId, tradingPair);
                return CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Buy", "Could not retrieve order book or no asks available");
            }
            
            // Calculate the average price we'd get for our quantity (simplified)
            decimal pricePerUnit = orderBook.Asks.First().Price;
            
            // Calculate total cost including fees
            decimal fee = GetExchangeFee(exchangeId);
            decimal totalCost = quantity * pricePerUnit;
            decimal feeCost = totalCost * fee;
            decimal totalCostWithFees = totalCost + feeCost;
            
            _logger.LogInformation("Buy calculation: Price per unit: {Price}, Total cost: {TotalCost}, Fee: {Fee}, Total with fees: {TotalWithFees}", 
                pricePerUnit, totalCost, feeCost, totalCostWithFees);
            
            // Check if we have enough quote asset (e.g., USDT) balance
            if (!_balances.TryGetValue(exchangeId, out var exchangeBalances))
            {
                _logger.LogError("No balances found for exchange {Exchange}", exchangeId);
                return CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Buy", $"No balances found for exchange {exchangeId}");
            }
            
            if (!exchangeBalances.TryGetValue(tradingPair.QuoteCurrency, out decimal quoteBalance) || quoteBalance < totalCostWithFees)
            {
                _logger.LogError("Insufficient {Currency} balance: {Available} < {Required}", 
                    tradingPair.QuoteCurrency, quoteBalance, totalCostWithFees);
                return CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Buy", 
                    $"Insufficient {tradingPair.QuoteCurrency} balance: {quoteBalance} < {totalCostWithFees}");
            }
            
            // Update balances
            exchangeBalances[tradingPair.QuoteCurrency] = quoteBalance - totalCostWithFees;
            
            // Add base asset (e.g., BTC)
            exchangeBalances.AddOrUpdate(
                tradingPair.BaseCurrency,
                quantity, // Initial value if key doesn't exist
                (key, oldValue) => oldValue + quantity // Update function if key exists
            );
            
            _logger.LogInformation("Updated balances - {QuoteCurrency}: {QuoteBalance}, {BaseCurrency}: {BaseBalance}", 
                tradingPair.QuoteCurrency, exchangeBalances[tradingPair.QuoteCurrency],
                tradingPair.BaseCurrency, exchangeBalances[tradingPair.BaseCurrency]);
            
            // Create successful trade result
            var result = new TradeResult
            {
                IsSuccess = true,
                OrderId = $"paper-{Guid.NewGuid()}",
                Timestamp = DateTimeOffset.UtcNow.DateTime,
                TradingPair = tradingPair.ToString(),
                TradeType = TradeType.Buy,
                RequestedPrice = 0, // Market order has no requested price
                ExecutedPrice = pricePerUnit,
                RequestedQuantity = quantity,
                ExecutedQuantity = quantity,
                TotalValue = totalCost,
                Fee = feeCost,
                FeeCurrency = tradingPair.QuoteCurrency,
                ExecutionTimeMs = 0 // Simulated trade is instant
            };
            
            // Add to trade history
            _tradeHistory.Add(result);
            _logger.LogInformation("Paper trading buy successful: {BaseCurrency} {Quantity} @ {Price} {QuoteCurrency}", 
                tradingPair.BaseCurrency, quantity, pricePerUnit, tradingPair.QuoteCurrency);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating market buy order");
            return CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Buy", ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<TradeResult> SimulateMarketSellOrderAsync(string exchangeId, TradingPair tradingPair, decimal quantity, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating market sell order on {Exchange}: {Quantity} {BaseCurrency} at market price", 
            exchangeId, quantity, tradingPair.BaseCurrency);
        
        try
        {
            // Normalize exchange ID
            exchangeId = exchangeId.ToLowerInvariant();
            
            // Get latest price from market data service
            var orderBook = _marketDataService.GetLatestOrderBook(exchangeId, tradingPair);
            if (orderBook == null || orderBook.Bids.Count == 0)
            {
                return CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Sell", "Could not retrieve order book or no bids available");
            }
            
            // Calculate the average price we'd get for our quantity (simplified)
            decimal pricePerUnit = orderBook.Bids.First().Price;
            
            // Check if we have enough base asset (e.g., BTC) balance
            if (!_balances.TryGetValue(exchangeId, out var exchangeBalances))
            {
                return CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Sell", $"No balances found for exchange {exchangeId}");
            }
            
            if (!exchangeBalances.TryGetValue(tradingPair.BaseCurrency, out decimal baseBalance) || baseBalance < quantity)
            {
                return CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Sell", 
                    $"Insufficient {tradingPair.BaseCurrency} balance: {baseBalance} < {quantity}");
            }
            
            // Calculate proceeds and fees
            decimal fee = GetExchangeFee(exchangeId);
            decimal totalProceeds = quantity * pricePerUnit;
            decimal feeCost = totalProceeds * fee;
            decimal netProceeds = totalProceeds - feeCost;
            
            // Update balances
            exchangeBalances[tradingPair.BaseCurrency] = baseBalance - quantity;
            
            // Add quote asset (e.g., USDT)
            exchangeBalances.AddOrUpdate(
                tradingPair.QuoteCurrency,
                netProceeds, // Initial value if key doesn't exist
                (key, oldValue) => oldValue + netProceeds // Update function if key exists
            );
            
            // Create successful trade result
            var result = new TradeResult
            {
                IsSuccess = true,
                OrderId = $"paper-{Guid.NewGuid()}",
                Timestamp = DateTimeOffset.UtcNow.DateTime,
                TradingPair = tradingPair.ToString(),
                TradeType = TradeType.Sell,
                RequestedPrice = 0, // Market order has no requested price
                ExecutedPrice = pricePerUnit,
                RequestedQuantity = quantity,
                ExecutedQuantity = quantity,
                TotalValue = totalProceeds,
                Fee = feeCost,
                FeeCurrency = tradingPair.QuoteCurrency,
                ExecutionTimeMs = 0 // Simulated trade is instant
            };
            
            // Add to trade history
            _tradeHistory.Add(result);
            _logger.LogInformation("Paper trading sell successful: {BaseCurrency} {Quantity} @ {Price} {QuoteCurrency}", 
                tradingPair.BaseCurrency, quantity, pricePerUnit, tradingPair.QuoteCurrency);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating market sell order");
            return CreateFailedTradeResult(exchangeId, tradingPair, quantity, "Sell", ex.Message);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>> GetAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, IReadOnlyCollection<Balance>>();
        
        foreach (var (exchangeId, assets) in _balances)
        {
            var balances = assets.Select(kv => new Balance(
                exchangeId,
                kv.Key,
                kv.Value,   // Total
                kv.Value,   // Available (assuming no locked funds in paper trading)
                0           // Reserved
            )).ToList();
            
            result[exchangeId] = balances;
        }
        
        return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>>(result);
    }

    /// <inheritdoc/>
    public Task<Balance?> GetBalanceAsync(string exchangeId, string asset, CancellationToken cancellationToken = default)
    {
        exchangeId = exchangeId.ToLowerInvariant();
        asset = asset.ToUpperInvariant();
        
        if (_balances.TryGetValue(exchangeId, out var exchangeBalances) && 
            exchangeBalances.TryGetValue(asset, out var amount))
        {
            return Task.FromResult<Balance?>(new Balance(
                exchangeId,
                asset,
                amount,    // Total
                amount,    // Available (assuming no locked funds in paper trading)
                0          // Reserved
            ));
        }
        
        return Task.FromResult<Balance?>(null);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<TradeResult>> GetTradeHistoryAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<TradeResult>>(_tradeHistory.ToList());
    }

    /// <inheritdoc/>
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resetting paper trading data");
        _balances.Clear();
        _tradeHistory.Clear();
        return Task.CompletedTask;
    }
    
    private decimal GetExchangeFee(string exchangeId)
    {
        if (_exchangeFees.TryGetValue(exchangeId, out decimal fee))
        {
            return fee;
        }
        
        return _exchangeFees["default"];
    }
    
    private TradeResult CreateFailedTradeResult(string exchangeId, TradingPair tradingPair, decimal quantity, string side, string error)
    {
        TradeType tradeType = side.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? TradeType.Buy : TradeType.Sell;
        
        var result = new TradeResult
        {
            IsSuccess = false,
            OrderId = $"paper-failed-{Guid.NewGuid()}",
            Timestamp = DateTimeOffset.UtcNow.DateTime,
            TradingPair = tradingPair.ToString(),
            TradeType = tradeType,
            RequestedQuantity = quantity,
            ErrorMessage = error,
            ExecutionTimeMs = 0 // Simulated trade is instant
        };
        
        _tradeHistory.Add(result);
        _logger.LogWarning("Paper trading {Side} failed: {Error}", side, error);
        
        return result;
    }
} 