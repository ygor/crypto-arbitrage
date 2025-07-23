using MediatR;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using CryptoArbitrage.Domain.Models.Events;
using CryptoArbitrage.Application.Features.TradeExecution.Events;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CryptoArbitrage.Application.Features.TradeExecution.Commands.ExecuteTrade;

/// <summary>
/// Handler for executing trades.
/// </summary>
public class ExecuteTradeHandler : IRequestHandler<ExecuteTradeCommand, ExecuteTradeResult>
{
    private readonly IExchangeFactory _exchangeFactory;
    private readonly IConfigurationService _configurationService;
    private readonly IPaperTradingService _paperTradingService;
    private readonly IMediator _mediator;
    private readonly ILogger<ExecuteTradeHandler> _logger;

    public ExecuteTradeHandler(
        IExchangeFactory exchangeFactory,
        IConfigurationService configurationService,
        IPaperTradingService paperTradingService,
        IMediator mediator,
        ILogger<ExecuteTradeHandler> logger)
    {
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _paperTradingService = paperTradingService ?? throw new ArgumentNullException(nameof(paperTradingService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExecuteTradeResult> Handle(ExecuteTradeCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var opportunity = request.Opportunity;
        var executionQuantity = request.Quantity ?? opportunity.EffectiveQuantity;

        _logger.LogInformation(
            "Executing trade for opportunity {OpportunityId}: {TradingPair} | Buy: {BuyExchange} ({BuyPrice}) | Sell: {SellExchange} ({SellPrice})",
            opportunity.Id, opportunity.TradingPair, opportunity.BuyExchangeId, opportunity.BuyPrice, 
            opportunity.SellExchangeId, opportunity.SellPrice);

        CancellationTokenSource? timeoutCts = null;
        try
        {
            // Set timeout if specified
            timeoutCts = request.TimeoutMs.HasValue 
                ? new CancellationTokenSource(request.TimeoutMs.Value)
                : null;
            
            using var combinedCts = timeoutCts != null 
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Check if paper trading is enabled
            var config = await _configurationService.GetConfigurationAsync(combinedCts.Token);
            var arbitrageConfig = config ?? new ArbitrageConfiguration();

            TradeResult tradeResult;

            if (arbitrageConfig.PaperTradingEnabled || _paperTradingService.IsPaperTradingEnabled)
            {
                _logger.LogInformation("Paper trading enabled. Simulating trade for opportunity {OpportunityId}", opportunity.Id);
                tradeResult = await SimulateTradeAsync(opportunity, executionQuantity, combinedCts.Token);
            }
            else
            {
                tradeResult = await ExecuteRealTradeAsync(opportunity, executionQuantity, combinedCts.Token);
            }

            stopwatch.Stop();

            if (tradeResult.IsSuccess)
            {
                // Publish trade executed event
                await _mediator.Publish(new TradeExecutedEvent
                {
                    TradeResult = tradeResult,
                    Opportunity = opportunity,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                }, cancellationToken);

                return ExecuteTradeResult.Success(tradeResult, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                // Publish trade failed event
                await _mediator.Publish(new TradeFailedEvent
                {
                    OpportunityId = opportunity.Id ?? string.Empty,
                    ErrorMessage = tradeResult.ErrorMessage ?? "Trade execution failed",
                    Opportunity = opportunity,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                }, cancellationToken);

                return ExecuteTradeResult.Failure(
                    tradeResult.ErrorMessage ?? "Trade execution failed",
                    opportunity.Id,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        catch (OperationCanceledException) when (timeoutCts?.Token.IsCancellationRequested == true)
        {
            stopwatch.Stop();
            var errorMessage = $"Trade execution timed out after {request.TimeoutMs}ms";
            _logger.LogWarning(errorMessage);

            // Mark opportunity as failed
            opportunity.MarkAsFailed();

            await _mediator.Publish(new TradeFailedEvent
            {
                OpportunityId = opportunity.Id ?? string.Empty,
                ErrorMessage = errorMessage,
                Opportunity = opportunity,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }, cancellationToken);

            return ExecuteTradeResult.Failure(errorMessage, opportunity.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing trade for opportunity {OpportunityId}", opportunity.Id);

            // Mark opportunity as failed
            opportunity.MarkAsFailed();

            var errorMessage = $"Trade execution failed: {ex.Message}";
            await _mediator.Publish(new TradeFailedEvent
            {
                OpportunityId = opportunity.Id ?? string.Empty,
                ErrorMessage = errorMessage,
                Opportunity = opportunity,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }, cancellationToken);

            return ExecuteTradeResult.Failure(errorMessage, opportunity.Id, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            timeoutCts?.Dispose();
        }
    }

    private async Task<TradeResult> ExecuteRealTradeAsync(
        ArbitrageOpportunity opportunity, 
        decimal quantity,
        CancellationToken cancellationToken)
    {
        // Mark opportunity as executing
        opportunity.MarkAsExecuting();

        // Get exchange clients
        var buyExchangeClient = await _exchangeFactory.CreateExchangeClientAsync(opportunity.BuyExchangeId);
        if (buyExchangeClient == null)
        {
            var errorMessage = $"Failed to create exchange client for {opportunity.BuyExchangeId}";
            _logger.LogError(errorMessage);
            opportunity.MarkAsFailed();
            return CreateFailedTradeResult(opportunity, quantity, errorMessage);
        }

        var sellExchangeClient = await _exchangeFactory.CreateExchangeClientAsync(opportunity.SellExchangeId);
        if (sellExchangeClient == null)
        {
            var errorMessage = $"Failed to create exchange client for {opportunity.SellExchangeId}";
            _logger.LogError(errorMessage);
            opportunity.MarkAsFailed();
            return CreateFailedTradeResult(opportunity, quantity, errorMessage);
        }

        // Execute buy order
        var buyOrder = await buyExchangeClient.PlaceMarketOrderAsync(
            opportunity.TradingPair,
            OrderSide.Buy,
            quantity,
            cancellationToken);

        var buyResult = CreateTradeResultFromOrder(buyOrder, OrderSide.Buy);
        if (!buyResult.IsSuccess)
        {
            _logger.LogWarning("Buy order failed for opportunity {OpportunityId}: {ErrorMessage}", 
                opportunity.Id, buyResult.ErrorMessage);
            opportunity.MarkAsFailed();
            return CreateFailedTradeResult(opportunity, quantity, $"Buy order failed: {buyResult.ErrorMessage}");
        }

        // Execute sell order with actual buy quantity
        var sellQuantity = buyOrder.FilledQuantity > 0 ? buyOrder.FilledQuantity : quantity;
        var sellOrder = await sellExchangeClient.PlaceMarketOrderAsync(
            opportunity.TradingPair,
            OrderSide.Sell,
            sellQuantity,
            cancellationToken);

        var sellResult = CreateTradeResultFromOrder(sellOrder, OrderSide.Sell);
        if (!sellResult.IsSuccess)
        {
            _logger.LogWarning("Sell order failed for opportunity {OpportunityId}: {ErrorMessage}", 
                opportunity.Id, sellResult.ErrorMessage);
            opportunity.MarkAsFailed();
            return CreateFailedTradeResult(opportunity, quantity, $"Sell order failed: {sellResult.ErrorMessage}");
        }

        // Calculate profit
        decimal buyTotal = buyResult.ExecutedPrice * buyResult.ExecutedQuantity;
        decimal sellTotal = sellResult.ExecutedPrice * sellResult.ExecutedQuantity;
        decimal buyFees = buyResult.Fee;
        decimal sellFees = sellResult.Fee;
        
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
            Quantity = Math.Min(buyResult.ExecutedQuantity, sellResult.ExecutedQuantity),
            Timestamp = DateTime.UtcNow,
            Status = TradeStatus.Completed,
            ProfitAmount = profitAmount,
            ProfitPercentage = profitPercentage,
            Fees = buyFees + sellFees,
            ExecutionTimeMs = buyResult.ExecutionTimeMs + sellResult.ExecutionTimeMs,
            IsSuccess = true
        };

        // Mark opportunity as executed
        opportunity.MarkAsExecuted();

        _logger.LogInformation(
            "Trade executed successfully for opportunity {OpportunityId}. Profit: {ProfitAmount} ({ProfitPercentage}%)",
            opportunity.Id, profitAmount, profitPercentage);

        return tradeResult;
    }

    private async Task<TradeResult> SimulateTradeAsync(
        ArbitrageOpportunity opportunity, 
        decimal quantity,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulating trade for opportunity {OpportunityId} on {TradingPair}", 
            opportunity.Id, opportunity.TradingPair);

        // Simulate execution time
        await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);

        // Calculate simulated fees (conservative estimates)
        decimal buyFees = Math.Round(opportunity.BuyPrice * quantity * 0.001m, 8);
        decimal sellFees = Math.Round(opportunity.SellPrice * quantity * 0.001m, 8);

        // Calculate profit
        decimal buyTotal = opportunity.BuyPrice * quantity;
        decimal sellTotal = opportunity.SellPrice * quantity;
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
            ExecutionTimeMs = Random.Shared.Next(100, 300),
            IsSuccess = true
        };

        _logger.LogInformation("Simulated trade completed successfully for opportunity {OpportunityId}. Profit: {ProfitAmount} ({ProfitPercentage}%)",
            opportunity.Id, profitAmount, profitPercentage);

        return tradeResult;
    }

    private static TradeResult CreateTradeResultFromOrder(Order order, OrderSide side, long executionTimeMs = 0)
    {
        return new TradeResult
        {
            IsSuccess = order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled,
            OrderId = order.Id,
            Timestamp = order.Timestamp,
            TradingPair = order.TradingPair.ToString(),
            TradeType = side == OrderSide.Buy ? TradeType.Buy : TradeType.Sell,
            RequestedPrice = order.Price,
            ExecutedPrice = order.AverageFillPrice > 0 ? order.AverageFillPrice : order.Price,
            RequestedQuantity = order.Quantity,
            ExecutedQuantity = order.FilledQuantity,
            TotalValue = order.Price * order.Quantity,
            Fee = 0, // Fee information not available in Order
            FeeCurrency = order.TradingPair.QuoteCurrency,
            ErrorMessage = order.Status == OrderStatus.Rejected ? "Order was rejected by the exchange" : null,
            ExecutionTimeMs = executionTimeMs
        };
    }

    private static TradeResult CreateFailedTradeResult(
        ArbitrageOpportunity opportunity, 
        decimal quantity, 
        string errorMessage)
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
            ProfitAmount = 0,
            ProfitPercentage = 0,
            Fees = 0,
            ExecutionTimeMs = 0,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
} 