using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

namespace ArbitrageBot.Tests.EndToEndTests;

/// <summary>
/// Tests focused on the real-time streaming capabilities of the arbitrage system.
/// </summary>
public class StreamingTestsFixed : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IMarketDataService _marketDataService;
    
    public StreamingTestsFixed(TestFixture fixture)
    {
        _fixture = fixture;
        _marketDataService = fixture.ServiceProvider.GetRequiredService<IMarketDataService>();
    }
    
    [Fact]
    public async Task UnsubscribeFromOrderBookAsync_ShouldStopReceivingUpdates()
    {
        // Arrange
        var tradingPair = TradingPair.BTCUSDT;
        var exchangeId = "binance";
        var mockClient = _fixture.MockExchangeClients[exchangeId];
        
        // Create a channel for order book updates
        var orderBookChannel = Channel.CreateUnbounded<OrderBook>();
        
        // Create initial order book
        var initialOrderBook = TestHelpers.CreateOrderBook(
            exchangeId, tradingPair, 50000m, 49950m);
            
        // Set up the mock client
        mockClient.Setup(c => c.GetOrderBookSnapshotAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialOrderBook);
            
        mockClient.Setup(c => c.GetOrderBookUpdatesAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                It.IsAny<CancellationToken>()))
            .Returns((TradingPair tp, CancellationToken ct) => orderBookChannel.Reader.ReadAllAsync(ct));
            
        // Set up UnsubscribeFromOrderBookAsync to complete the channel
        mockClient.Setup(c => c.UnsubscribeFromOrderBookAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => 
            {
                try
                {
                    orderBookChannel.Writer.Complete();
                }
                catch (Exception)
                {
                    // Ignore if already completed
                }
            });
            
        // Subscribe to order book updates
        await _marketDataService.SubscribeToOrderBookAsync(exchangeId, tradingPair, CancellationToken.None);
        
        // Give some time for subscription to take effect
        await Task.Delay(200);
        
        // Push initial order book to the channel
        await orderBookChannel.Writer.WriteAsync(initialOrderBook);
        
        // Wait for the order book to be processed
        await Task.Delay(200);
        
        // Verify we can get the latest order book
        var orderBookBeforeUnsubscribe = _marketDataService.GetLatestOrderBook(exchangeId, tradingPair);
        
        Assert.NotNull(orderBookBeforeUnsubscribe);
        
        // Act: Push an update, then unsubscribe
        var updatedOrderBook = TestHelpers.CreateOrderBook(
            exchangeId, tradingPair, 50100m, 50050m);
        await orderBookChannel.Writer.WriteAsync(updatedOrderBook);
        
        // Wait a bit for the update to be processed
        await Task.Delay(200);
        
        // Check that the updated order book is now the latest
        var orderBookAfterUpdate = _marketDataService.GetLatestOrderBook(exchangeId, tradingPair);
        Assert.NotNull(orderBookAfterUpdate);
        Assert.Equal(50100m, orderBookAfterUpdate!.Asks[0].Price);
        
        // Unsubscribe
        await _marketDataService.UnsubscribeFromOrderBookAsync(exchangeId, tradingPair, CancellationToken.None);
        
        // Wait a bit for unsubscribe to take effect
        await Task.Delay(200);
        
        // Verify the mock was called
        mockClient.Verify(c => c.UnsubscribeFromOrderBookAsync(
            It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
            It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        
        // Get the active exchanges for the trading pair
        var activeExchanges = _marketDataService.GetActiveExchanges(tradingPair);
        
        // Assert that the exchange is no longer active for the trading pair
        Assert.DoesNotContain(exchangeId, activeExchanges);
    }
} 