using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Domain.Models.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CryptoArbitrage.Application.Services;

/// <summary>
/// Service for executing trades.
/// </summary>
public class TradeExecutionService : ITradeExecutionService
{
    private readonly IExchangeFactory _exchangeFactory;
    private readonly IConfigurationService _configurationService;
    private readonly IPaperTradingService _paperTradingService;
    private readonly ILogger<TradeExecutionService> _logger;
    private readonly ArbitrageConfiguration _arbitrageConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="TradeExecutionService"/> class.
    /// </summary>
    public TradeExecutionService(
        IExchangeFactory exchangeFactory,
        IConfigurationService configurationService,
        IPaperTradingService paperTradingService,
        ILogger<TradeExecutionService> logger)
    {
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _paperTradingService = paperTradingService ?? throw new ArgumentNullException(nameof(paperTradingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _arbitrageConfiguration = new ArbitrageConfiguration();
    }

    /// <inheritdoc />
    public event Func<TradeResult, Task>? OnTradeExecuted;

    /// <inheritdoc />
    public event Func<Domain.Models.Events.ErrorEventArgs, Task>? OnError;

    /// <inheritdoc />
    public async Task<TradeResult?> ExecuteTradeAsync(
        ArbitrageOpportunity opportunity, 
        decimal? quantity = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing trade for opportunity {OpportunityId}", opportunity.Id);

            // Use provided quantity or EffectiveQuantity from opportunity
            var executionQuantity = quantity ?? opportunity.EffectiveQuantity;

            // If paper trading is enabled, just simulate the trade
            if (_arbitrageConfiguration.PaperTradingEnabled)
            {
                _logger.LogInformation("Paper trading enabled. Simulating trade.");
                return await SimulateTradeAsync(opportunity, executionQuantity, cancellationToken);
            }

            // Get exchange clients
            var buyExchangeClient = await _exchangeFactory.CreateExchangeClientAsync(opportunity.BuyExchangeId);
            if (buyExchangeClient == null)
            {
                var errorMessage = $"Failed to create exchange client for {opportunity.BuyExchangeId}";
                _logger.LogError(errorMessage);
                await RaiseErrorEvent(ErrorCode.ExchangeConnectionFailed, errorMessage);
                return CreateFailedTradeResult(opportunity, executionQuantity, errorMessage);
            }

            var sellExchangeClient = await _exchangeFactory.CreateExchangeClientAsync(opportunity.SellExchangeId);
            if (sellExchangeClient == null)
            {
                var errorMessage = $"Failed to create exchange client for {opportunity.SellExchangeId}";
                _logger.LogError(errorMessage);
                await RaiseErrorEvent(ErrorCode.ExchangeConnectionFailed, errorMessage);
                return CreateFailedTradeResult(opportunity, executionQuantity, errorMessage);
            }

            // Mark opportunity as executing
            opportunity.MarkAsExecuting();

            // Execute buy order
            TradeResult buyResult;
            try
            {
                _logger.LogInformation("Placing buy order on {Exchange} for {Quantity} at {Price}",
                    opportunity.BuyExchangeId, executionQuantity, opportunity.BuyPrice);
                    
                buyResult = await buyExchangeClient.PlaceLimitOrderAsync(
                    opportunity.TradingPair,
                    OrderSide.Buy,
                    opportunity.BuyPrice,
                    executionQuantity,
                    OrderType.Limit,
                    cancellationToken);
                    
                if (!buyResult.IsSuccess)
                {
                    var errorMessage = $"Buy order failed: {buyResult.ErrorMessage}";
                    _logger.LogError(errorMessage);
                    await RaiseErrorEvent(ErrorCode.FailedToPlaceOrder, errorMessage);
                    return CreateFailedTradeResult(opportunity, executionQuantity, errorMessage);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Exception during buy order: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                await RaiseErrorEvent(ErrorCode.FailedToPlaceOrder, errorMessage, ex);
                return CreateFailedTradeResult(opportunity, executionQuantity, errorMessage);
            }

            // Execute sell order with the actual quantity received from the buy
            TradeResult sellResult;
            try
            {
                decimal sellQuantity = buyResult.ExecutedQuantity;
                
                _logger.LogInformation("Placing sell order on {Exchange} for {Quantity} at {Price}",
                    opportunity.SellExchangeId, sellQuantity, opportunity.SellPrice);
                    
                sellResult = await sellExchangeClient.PlaceLimitOrderAsync(
                    opportunity.TradingPair,
                    OrderSide.Sell,
                    opportunity.SellPrice,
                    sellQuantity,
                    OrderType.Limit,
                    cancellationToken);
                    
                if (!sellResult.IsSuccess)
                {
                    var errorMessage = $"Sell order failed after successful buy: {sellResult.ErrorMessage}";
                    _logger.LogError(errorMessage);
                    await RaiseErrorEvent(ErrorCode.FailedToPlaceOrder, errorMessage);
                    return CreateFailedTradeResult(opportunity, executionQuantity, errorMessage);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Exception during sell order after successful buy: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                await RaiseErrorEvent(ErrorCode.FailedToPlaceOrder, errorMessage, ex);
                return CreateFailedTradeResult(opportunity, executionQuantity, errorMessage);
            }

            // Calculate profit
            decimal buyTotal = buyResult.ExecutedPrice * buyResult.ExecutedQuantity;
            decimal sellTotal = sellResult.ExecutedPrice * sellResult.ExecutedQuantity;
            decimal buyFees = buyResult.Fees;
            decimal sellFees = sellResult.Fees;
            
            decimal profitAmount = Math.Round(sellTotal - buyTotal - buyFees - sellFees, 8);
            decimal profitPercentage = buyTotal > 0 
                ? Math.Round(profitAmount / buyTotal * 100, 4) 
                : 0;

            // Create successful trade result
            var tradeResult = new TradeResult
            {
                Id = Guid.NewGuid(),
                OpportunityId = opportunity.Id != null ? Guid.Parse(opportunity.Id) : Guid.Empty,
                TradingPair = opportunity.TradingPair.ToString(),
                BuyExchangeId = opportunity.BuyExchangeId,
                SellExchangeId = opportunity.SellExchangeId,
                BuyPrice = buyResult.ExecutedPrice,
                SellPrice = sellResult.ExecutedPrice,
                Quantity = buyResult.ExecutedQuantity,
                Timestamp = DateTime.UtcNow,
                Status = TradeStatus.Completed,
                ProfitAmount = profitAmount,
                ProfitPercentage = profitPercentage,
                Fees = buyFees + sellFees,
                ExecutionTimeMs = buyResult.ExecutionTimeMs + sellResult.ExecutionTimeMs,
                IsSuccess = true,
                BuyResult = ConvertToSubResult(buyResult, OrderSide.Buy),
                SellResult = ConvertToSubResult(sellResult, OrderSide.Sell)
            };

            // Mark opportunity as executed
            opportunity.MarkAsExecuted();

            _logger.LogInformation(
                "Trade executed successfully for opportunity {OpportunityId}. Profit: {ProfitAmount} ({ProfitPercentage}%)",
                opportunity.Id, profitAmount, profitPercentage);

            // Raise trade executed event
            await OnTradeExecuted?.Invoke(tradeResult);

            return tradeResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing trade for opportunity {OpportunityId}", opportunity.Id);
            
            // Raise error event
            await RaiseErrorEvent(ErrorCode.TradeExecutionFailed, $"Failed to execute trade: {ex.Message}", ex);

            // Mark opportunity as failed
            opportunity.MarkAsFailed();
            
            // Create failed trade result
            return CreateFailedTradeResult(opportunity, quantity ?? opportunity.EffectiveQuantity, $"Trade execution failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<TradeResult> SimulateTradeAsync(
        ArbitrageOpportunity opportunity, 
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Simulating trade for opportunity {OpportunityId} on {TradingPair}", 
                opportunity.Id, opportunity.TradingPair);

            // Create a successful trade result for simulation
            var buyResult = new TradeResult
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid().ToString(),
                ExchangeId = opportunity.BuyExchangeId,
                TradingPair = opportunity.TradingPair.ToString(),
                OrderType = OrderType.Limit,
                Side = OrderSide.Buy,
                RequestedPrice = opportunity.BuyPrice,
                ExecutedPrice = opportunity.BuyPrice,
                RequestedQuantity = quantity,
                ExecutedQuantity = quantity,
                Timestamp = DateTime.UtcNow,
                Status = TradeStatus.Completed,
                Fees = Math.Round(opportunity.BuyPrice * quantity * 0.001m, 8),
                ExecutionTimeMs = 100,
                IsSuccess = true
            };

            var sellResult = new TradeResult
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid().ToString(),
                ExchangeId = opportunity.SellExchangeId,
                TradingPair = opportunity.TradingPair.ToString(),
                OrderType = OrderType.Limit,
                Side = OrderSide.Sell,
                RequestedPrice = opportunity.SellPrice,
                ExecutedPrice = opportunity.SellPrice,
                RequestedQuantity = quantity,
                ExecutedQuantity = quantity,
                Timestamp = DateTime.UtcNow,
                Status = TradeStatus.Completed,
                Fees = Math.Round(opportunity.SellPrice * quantity * 0.001m, 8),
                ExecutionTimeMs = 150,
                IsSuccess = true
            };

            // Calculate profit
            decimal buyTotal = opportunity.BuyPrice * quantity;
            decimal sellTotal = opportunity.SellPrice * quantity;
            decimal buyFees = buyResult.Fees;
            decimal sellFees = sellResult.Fees;
            
            decimal profitAmount = Math.Round(sellTotal - buyTotal - buyFees - sellFees, 8);
            decimal profitPercentage = buyTotal > 0 
                ? Math.Round(profitAmount / buyTotal * 100, 4) 
                : 0;

            var tradeResult = new TradeResult
            {
                Id = Guid.NewGuid(),
                OpportunityId = opportunity.Id != null ? Guid.Parse(opportunity.Id) : Guid.Empty,
                TradingPair = opportunity.TradingPair.ToString(),
                BuyExchangeId = opportunity.BuyExchangeId,
                SellExchangeId = opportunity.SellExchangeId,
                BuyPrice = opportunity.BuyPrice,
                SellPrice = opportunity.SellPrice,
                Quantity = quantity,
                Timestamp = DateTime.UtcNow,
                Status = TradeStatus.Completed,
                ProfitAmount = profitAmount,
                ProfitPercentage = profitPercentage,
                Fees = buyFees + sellFees,
                ExecutionTimeMs = 250,
                IsSuccess = true,
                BuyResult = ConvertToSubResult(buyResult, OrderSide.Buy),
                SellResult = ConvertToSubResult(sellResult, OrderSide.Sell)
            };

            _logger.LogInformation("Simulated trade completed successfully for opportunity {OpportunityId}. Profit: {ProfitAmount} ({ProfitPercentage}%)",
                opportunity.Id, profitAmount, profitPercentage);

            // Raise trade executed event
            await OnTradeExecuted?.Invoke(tradeResult);

            return tradeResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating trade for opportunity {OpportunityId}", opportunity.Id);
            
            // Raise error event
            await RaiseErrorEvent(ErrorCode.TradeExecutionFailed, $"Failed to simulate trade: {ex.Message}", ex);
            
            // Return failed result
            return new TradeResult
            {
                Id = Guid.NewGuid(),
                OpportunityId = opportunity.Id != null ? Guid.Parse(opportunity.Id) : Guid.Empty,
                TradingPair = opportunity.TradingPair.ToString(),
                BuyExchangeId = opportunity.BuyExchangeId,
                SellExchangeId = opportunity.SellExchangeId,
                BuyPrice = opportunity.BuyPrice,
                SellPrice = opportunity.SellPrice,
                Quantity = quantity,
                Timestamp = DateTime.UtcNow,
                Status = TradeStatus.Failed,
                ErrorMessage = $"Simulation failed: {ex.Message}",
                IsSuccess = false
            };
        }
    }

    /// <summary>
    /// Raises the error event.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception that caused the error, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RaiseErrorEvent(ErrorCode errorCode, string message, Exception? exception = null)
    {
        var args = new Domain.Models.Events.ErrorEventArgs(errorCode, message, exception);
        _logger.LogError(exception, "{ErrorCode}: {Message}", errorCode, message);
        if (OnError != null)
        {
            await OnError.Invoke(args);
        }
    }

    private TradeResult CreateFailedTradeResult(ArbitrageOpportunity opportunity, decimal quantity, string errorMessage)
    {
        return new TradeResult
        {
            Id = Guid.NewGuid(),
            OpportunityId = opportunity.Id != null ? Guid.Parse(opportunity.Id) : Guid.Empty,
            TradingPair = opportunity.TradingPair.ToString(),
            BuyExchangeId = opportunity.BuyExchangeId,
            SellExchangeId = opportunity.SellExchangeId,
            BuyPrice = opportunity.BuyPrice,
            SellPrice = opportunity.SellPrice,
            Quantity = quantity,
            Timestamp = DateTime.UtcNow,
            Status = TradeStatus.Failed,
            ErrorMessage = errorMessage,
            IsSuccess = false
        };
    }

    private TradeSubResult ConvertToSubResult(TradeResult result, OrderSide side)
    {
        if (result == null)
        {
            return new TradeSubResult
            {
                Side = side,
                Status = OrderStatus.Rejected,
                ErrorMessage = "Null trade result"
            };
        }

        return new TradeSubResult
        {
            OrderId = result.OrderId ?? string.Empty,
            TradingPair = result.TradingPair ?? string.Empty,
            Side = side,
            Quantity = result.RequestedQuantity,
            Price = result.RequestedPrice,
            FilledQuantity = result.ExecutedQuantity,
            AverageFillPrice = result.ExecutedPrice,
            FeeAmount = result.Fees,
            FeeCurrency = result.FeeCurrency ?? string.Empty,
            Status = result.Status == TradeStatus.Completed ? OrderStatus.Filled : OrderStatus.Rejected,
            Timestamp = result.Timestamp,
            ErrorMessage = result.ErrorMessage
        };
    }
} 