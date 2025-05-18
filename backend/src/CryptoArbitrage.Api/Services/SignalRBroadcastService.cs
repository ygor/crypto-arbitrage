using CryptoArbitrage.Api.Hubs;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.AspNetCore.SignalR;

namespace CryptoArbitrage.Api.Services;

/// <summary>
/// Background service that subscribes to arbitrage opportunities and trade results from the 
/// arbitrage service and broadcasts them to connected SignalR clients.
/// </summary>
public class SignalRBroadcastService : BackgroundService
{
    private readonly ILogger<SignalRBroadcastService> _logger;
    private readonly IArbitrageService _arbitrageService;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public SignalRBroadcastService(
        ILogger<SignalRBroadcastService> logger,
        IArbitrageService arbitrageService,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _arbitrageService = arbitrageService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SignalR broadcast service starting");
        
        // Start tasks for broadcasting opportunities and trade results
        await Task.WhenAll(
            BroadcastOpportunitiesAsync(stoppingToken),
            BroadcastTradeResultsAsync(stoppingToken)
        );
        
        _logger.LogInformation("SignalR broadcast service stopped");
    }

    private async Task BroadcastOpportunitiesAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting to broadcast arbitrage opportunities");
            
            // Process opportunities as they are detected
            await foreach (var opportunity in _arbitrageService.GetOpportunitiesAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ArbitrageHub>>();
                    
                    _logger.LogDebug("Broadcasting opportunity: {TradingPair} {BuyExchange}->{SellExchange} Profit: {Profit}%", 
                        opportunity.TradingPair.ToString(),
                        opportunity.BuyExchangeId,
                        opportunity.SellExchangeId,
                        opportunity.SpreadPercentage);
                    
                    await hubContext.Clients.All.SendAsync("ArbitrageOpportunityDetected", opportunity, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting opportunity");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
            _logger.LogInformation("Opportunity broadcasting canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in opportunity broadcasting loop");
        }
    }

    private async Task BroadcastTradeResultsAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting to broadcast trade results");
            
            // Process trade results as they are completed
            await foreach (var tradeResult in _arbitrageService.GetTradeResultsAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TradeHub>>();
                    
                    _logger.LogDebug("Broadcasting trade result: {Success} Profit: {Profit}", 
                        tradeResult.IsSuccess,
                        tradeResult.ProfitAmount);
                    
                    await hubContext.Clients.All.SendAsync("TradeCompleted", tradeResult, stoppingToken);
                    
                    // Also broadcast individual trade results if available
                    if (tradeResult.BuyResult != null)
                    {
                        await hubContext.Clients.All.SendAsync("TradeCompleted", tradeResult.BuyResult, stoppingToken);
                    }
                    
                    if (tradeResult.SellResult != null)
                    {
                        await hubContext.Clients.All.SendAsync("TradeCompleted", tradeResult.SellResult, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting trade result");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
            _logger.LogInformation("Trade result broadcasting canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in trade result broadcasting loop");
        }
    }
} 