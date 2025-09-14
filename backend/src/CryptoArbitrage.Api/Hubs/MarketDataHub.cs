using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MediatR;
using CryptoArbitrage.Domain.Models;
using System.Collections.Concurrent;

namespace CryptoArbitrage.Api.Hubs;

/// <summary>
/// SignalR hub for real-time market data updates.
/// </summary>
public class MarketDataHub : Hub
{
    private readonly ILogger<MarketDataHub> _logger;
    private readonly IMediator _mediator;
    private static readonly ConcurrentDictionary<string, DateTime> _lastSentByGroup = new();
    private static readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(75);

    public MarketDataHub(
        ILogger<MarketDataHub> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to MarketDataHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnection, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from MarketDataHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes a client to receive real-time price quotes for a specific trading pair.
    /// </summary>
    /// <param name="baseCurrency">The base currency (e.g., BTC)</param>
    /// <param name="quoteCurrency">The quote currency (e.g., USDT)</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SubscribeToPriceQuotes(string baseCurrency, string quoteCurrency)
    {
        try
        {
            var tradingPair = new TradingPair(baseCurrency, quoteCurrency);
            var groupName = $"quotes_{baseCurrency}_{quoteCurrency}";
            
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("Client {ConnectionId} subscribed to price quotes for {TradingPair}", 
                Context.ConnectionId, tradingPair);
            
            // Send current best bid/ask immediately (mock implementation for now)
            // TODO: Replace with MediatR query to get current market data
            var currentBestBidAsk = (BestBid: (PriceQuote?)null, BestAsk: (PriceQuote?)null);
            if (currentBestBidAsk.BestBid != null || currentBestBidAsk.BestAsk != null)
            {
                await Clients.Caller.SendAsync("BestBidAskUpdate", new
                {
                    TradingPair = new { baseCurrency, quoteCurrency },
                    BestBid = currentBestBidAsk.BestBid,
                    BestAsk = currentBestBidAsk.BestAsk,
                    Spread = currentBestBidAsk.BestAsk?.BestAskPrice - currentBestBidAsk.BestBid?.BestBidPrice,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to price quotes for {BaseCurrency}/{QuoteCurrency}", 
                baseCurrency, quoteCurrency);
            await Clients.Caller.SendAsync("Error", $"Failed to subscribe to {baseCurrency}/{quoteCurrency}");
        }
    }

    /// <summary>
    /// Unsubscribes a client from receiving price quotes for a specific trading pair.
    /// </summary>
    /// <param name="baseCurrency">The base currency (e.g., BTC)</param>
    /// <param name="quoteCurrency">The quote currency (e.g., USDT)</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UnsubscribeFromPriceQuotes(string baseCurrency, string quoteCurrency)
    {
        try
        {
            var groupName = $"quotes_{baseCurrency}_{quoteCurrency}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            
            _logger.LogInformation("Client {ConnectionId} unsubscribed from price quotes for {BaseCurrency}/{QuoteCurrency}", 
                Context.ConnectionId, baseCurrency, quoteCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from price quotes for {BaseCurrency}/{QuoteCurrency}", 
                baseCurrency, quoteCurrency);
        }
    }

    /// <summary>
    /// Broadcasts best bid/ask update to all clients subscribed to the trading pair.
    /// This method is called by the server, not by clients.
    /// </summary>
    /// <param name="tradingPair">The trading pair</param>
    /// <param name="bestBid">The best bid quote</param>
    /// <param name="bestAsk">The best ask quote</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendBestBidAskUpdate(TradingPair tradingPair, PriceQuote? bestBid, PriceQuote? bestAsk)
    {
        try
        {
            var groupName = $"quotes_{tradingPair.BaseCurrency}_{tradingPair.QuoteCurrency}";
            var now = DateTime.UtcNow;
            var last = _lastSentByGroup.GetOrAdd(groupName, now.AddSeconds(-1));
            if (now - last < _minInterval)
            {
                return; // coalesce
            }
            _lastSentByGroup[groupName] = now;
            
            var update = new
            {
                TradingPair = new { 
                    BaseCurrency = tradingPair.BaseCurrency, 
                    QuoteCurrency = tradingPair.QuoteCurrency 
                },
                BestBid = bestBid,
                BestAsk = bestAsk,
                Spread = bestAsk?.BestAskPrice - bestBid?.BestBidPrice,
                SpreadPercentage = bestBid?.BestBidPrice > 0 
                    ? ((bestAsk?.BestAskPrice - bestBid?.BestBidPrice) / bestBid?.BestBidPrice) * 100 
                    : 0,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group(groupName).SendAsync("BestBidAskUpdate", update);
            
            _logger.LogDebug("Broadcasted best bid/ask update for {TradingPair} to group {GroupName}", 
                tradingPair, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting best bid/ask update for {TradingPair}", tradingPair);
        }
    }

    /// <summary>
    /// Broadcasts individual price quotes to all clients subscribed to the trading pair.
    /// This method is called by the server, not by clients.
    /// </summary>
    /// <param name="priceQuote">The price quote to broadcast</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendPriceQuoteUpdate(PriceQuote priceQuote)
    {
        try
        {
            var groupName = $"quotes_{priceQuote.TradingPair.BaseCurrency}_{priceQuote.TradingPair.QuoteCurrency}";
            var now = DateTime.UtcNow;
            var last = _lastSentByGroup.GetOrAdd(groupName, now.AddSeconds(-1));
            if (now - last < _minInterval)
            {
                return; // coalesce
            }
            _lastSentByGroup[groupName] = now;
            
            await Clients.Group(groupName).SendAsync("PriceQuoteUpdate", priceQuote);
            
            _logger.LogDebug("Broadcasted price quote update for {ExchangeId} {TradingPair}", 
                priceQuote.ExchangeId, priceQuote.TradingPair);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting price quote update for {ExchangeId} {TradingPair}", 
                priceQuote.ExchangeId, priceQuote.TradingPair);
        }
    }
} 