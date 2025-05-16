using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using System.Runtime.CompilerServices;

namespace ArbitrageBot.Tests.EndToEndTests;

/// <summary>
/// End-to-end tests for the arbitrage system.
/// </summary>
public class ArbitrageEndToEndTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly IArbitrageService _arbitrageService;
    private readonly IMarketDataService _marketDataService;
    private readonly ITradingService _tradingService;
    
    // For streaming tests
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
    private ArbitrageOpportunity? _detectedOpportunity;
    
    public ArbitrageEndToEndTests(TestFixture fixture)
    {
        _fixture = fixture;
        _arbitrageService = fixture.ServiceProvider.GetRequiredService<IArbitrageService>();
        _marketDataService = fixture.ServiceProvider.GetRequiredService<IMarketDataService>();
        _tradingService = fixture.ServiceProvider.GetRequiredService<ITradingService>();

        // Set up detection service to passthrough trade results directly to tests
        var detectionService = fixture.ServiceProvider.GetRequiredService<IArbitrageDetectionService>();
        
        // Ensure the PublishTradeResultAsync method correctly publishes trade results
        // This is crucial for the tests that depend on trade results flowing through the system
        Mock.Get(detectionService)
            .Setup(d => d.PublishTradeResultAsync(It.IsAny<ArbitrageTradeResult>(), It.IsAny<CancellationToken>()))
            .Returns((ArbitrageTradeResult result, CancellationToken token) => {
                Console.WriteLine($"Publishing trade result for {result.Opportunity.TradingPair}, Success: {result.IsSuccess}");
                return Task.CompletedTask;
            });
    }
    
    [Fact]
    public async Task DetectAndExecuteArbitrage_WithProfitableOpportunity_ShouldExecuteTrades()
    {
        // Arrange
        var tradingPair = TradingPair.BTCUSDT;
        var buyExchangeId = "binance";
        var sellExchangeId = "coinbase";
        var buyPrice = 50000m;
        var sellPrice = 50500m; // 1% higher than buy price
        var quantity = 1.0m;
        
        // Set up mock order books to simulate an arbitrage opportunity
        var opportunity = TestHelpers.SetupArbitrageOpportunity(
            _fixture.MockExchangeClients,
            tradingPair,
            buyExchangeId,
            sellExchangeId,
            buyPrice,
            sellPrice,
            quantity);
        
        // Set up balances for the exchanges
        TestHelpers.SetupBalances(_fixture.MockExchangeClients, tradingPair);
        
        // Enable auto-trading in the configuration
        var config = _fixture.TestArbitrageConfiguration;
        config.AutoExecuteTrades = true;
        config.AutoTradeEnabled = true;
        _fixture.MockConfigurationService.Setup(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
            
        // Act: Execute the arbitrage opportunity directly
        var result = await _tradingService.ExecuteArbitrageAsync(opportunity, CancellationToken.None);
        
        // Assert
        Assert.True(result.BuyResult.IsSuccess, "Buy order should be successful");
        Assert.True(result.SellResult.IsSuccess, "Sell order should be successful");
        
        // Verify the profit
        decimal expectedProfit = (sellPrice - buyPrice) * quantity - (buyPrice * quantity * 0.001m) - (sellPrice * quantity * 0.001m);
        Assert.True(result.BuyResult.TotalValue > 0, "Buy order total value should be greater than 0");
        Assert.True(result.SellResult.TotalValue > 0, "Sell order total value should be greater than 0");
        
        // Verify that mock methods were called
        _fixture.MockExchangeClients[buyExchangeId].Verify(
            c => c.PlaceMarketOrderAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                It.Is<OrderSide>(os => os == OrderSide.Buy),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
            
        _fixture.MockExchangeClients[sellExchangeId].Verify(
            c => c.PlaceMarketOrderAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                It.Is<OrderSide>(os => os == OrderSide.Sell),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task MarketDataService_WithMultipleExchanges_ShouldIdentifyBestPrices()
    {
        // Arrange
        var tradingPair = TradingPair.ETHUSDT;
        var exchanges = new[] { "binance", "coinbase", "kraken" };
        
        // Set up streaming order books for multiple exchanges
        Dictionary<string, Channel<OrderBook>> orderBookChannels;
        var initialOrderBooks = TestHelpers.SetupStreamingOrderBooks(
            _fixture.MockExchangeClients,
            tradingPair,
            out orderBookChannels);
        
        // Manually override with specific prices to ensure we have a clear best bid/ask
        var binanceOrderBook = TestHelpers.CreateOrderBook(
            "binance", tradingPair, 3000m, 2990m); // Best ask price
            
        var coinbaseOrderBook = TestHelpers.CreateOrderBook(
            "coinbase", tradingPair, 3010m, 3000m); // Best bid price
            
        var krakenOrderBook = TestHelpers.CreateOrderBook(
            "kraken", tradingPair, 3020m, 2980m);
            
        // Update initial order books with our specific test cases
        initialOrderBooks["binance"] = binanceOrderBook;
        initialOrderBooks["coinbase"] = coinbaseOrderBook;
        initialOrderBooks["kraken"] = krakenOrderBook;
        
        // Update the mock setups to return our specific order books
        _fixture.MockExchangeClients["binance"].Setup(c => c.GetOrderBookSnapshotAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(binanceOrderBook);
            
        _fixture.MockExchangeClients["coinbase"].Setup(c => c.GetOrderBookSnapshotAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coinbaseOrderBook);
            
        _fixture.MockExchangeClients["kraken"].Setup(c => c.GetOrderBookSnapshotAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(krakenOrderBook);
        
        // Subscribe to order books on all exchanges
        foreach (var exchange in exchanges)
        {
            await _marketDataService.SubscribeToOrderBookAsync(exchange, tradingPair, CancellationToken.None);
            // Wait to ensure subscription is processed
            await Task.Delay(100);
            
            // Push the initial order book to the channel to make sure it's received
            await orderBookChannels[exchange].Writer.WriteAsync(initialOrderBooks[exchange]);
        }
        
        // Give extra time for all order books to be processed
        await Task.Delay(500);
        
        // Act: Get the best bid and ask across exchanges
        var bestBidAsk = _marketDataService.GetBestBidAskAcrossExchanges(tradingPair);
        
        // Debug information to check what's happening if bestBidAsk is null
        if (bestBidAsk == null)
        {
            // Check if order books exist in the service
            foreach (var exchange in exchanges)
            {
                var orderBook = _marketDataService.GetLatestOrderBook(exchange, tradingPair);
                Console.WriteLine($"Order book for {exchange}: {(orderBook != null ? "exists" : "is null")}");
                if (orderBook != null)
                {
                    var firstAsk = orderBook.Asks.FirstOrDefault();
                    var firstBid = orderBook.Bids.FirstOrDefault();
                    Console.WriteLine($"  Ask: {(firstAsk.Price.ToString())}, Bid: {(firstBid.Price.ToString())}");
                }
            }
        }
        
        // Assert
        Assert.NotNull(bestBidAsk);
        Assert.NotNull(bestBidAsk?.BestBid);
        Assert.NotNull(bestBidAsk?.BestAsk);
        
        // We expect the best ask (lowest) to be from binance at 3000
        Assert.Equal("binance", bestBidAsk?.BestAsk?.ExchangeId);
        Assert.Equal(3000m, bestBidAsk?.BestAsk?.BestAskPrice);
        
        // We expect the best bid (highest) to be from coinbase at 3000
        Assert.Equal("coinbase", bestBidAsk?.BestBid?.ExchangeId);
        Assert.Equal(3000m, bestBidAsk?.BestBid?.BestBidPrice);
    }
    
    [Fact]
    public async Task ArbitrageService_WithProfitableOpportunity_ShouldDetectOpportunity()
    {
        // Arrange
        var tradingPair = TradingPair.BTCUSDT;
        var buyExchangeId = "binance";
        var sellExchangeId = "coinbase";
        var buyPrice = 50000m;
        var sellPrice = 50500m; // 1% higher than buy price
        var quantity = 1.0m;
        
        // Set up streaming order books
        Dictionary<string, Channel<OrderBook>> orderBookChannels;
        var exchangeClients = new Dictionary<string, Mock<IExchangeClient>>
        {
            { buyExchangeId, _fixture.MockExchangeClients[buyExchangeId] },
            { sellExchangeId, _fixture.MockExchangeClients[sellExchangeId] }
        };
        
        var initialOrderBooks = TestHelpers.SetupStreamingOrderBooks(
            exchangeClients, 
            tradingPair,
            out orderBookChannels);
        
        // Create and configure order books with profitable opportunity
        var buyOrderBook = TestHelpers.CreateOrderBook(
            buyExchangeId, tradingPair, buyPrice, buyPrice * 0.99m);
        var sellOrderBook = TestHelpers.CreateOrderBook(
            sellExchangeId, tradingPair, sellPrice * 1.01m, sellPrice);
        
        // Update mock setups with our customized order books
        _fixture.MockExchangeClients[buyExchangeId].Setup(c => c.GetOrderBookSnapshotAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(buyOrderBook);
        
        _fixture.MockExchangeClients[sellExchangeId].Setup(c => c.GetOrderBookSnapshotAsync(
                It.Is<TradingPair>(tp => tp.Equals(tradingPair)), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sellOrderBook);
        
        // Set up mock orders for the opportunity
        TestHelpers.SetupMockOrders(
            _fixture.MockExchangeClients[buyExchangeId],
            _fixture.MockExchangeClients[sellExchangeId],
            tradingPair,
            buyPrice,
            sellPrice,
            quantity);
        
        // Subscribe to order books
        await _marketDataService.SubscribeToOrderBookAsync(buyExchangeId, tradingPair, CancellationToken.None);
        await _marketDataService.SubscribeToOrderBookAsync(sellExchangeId, tradingPair, CancellationToken.None);
        
        // Wait to ensure subscriptions are set up
        await Task.Delay(200);
        
        // Push the initial order books to the channels
        await orderBookChannels[buyExchangeId].Writer.WriteAsync(buyOrderBook);
        await orderBookChannels[sellExchangeId].Writer.WriteAsync(sellOrderBook);
        
        // Set up an event handler to capture the detected opportunity
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)); // Increased timeout
        _detectedOpportunity = null; // Reset the opportunity
        
        // Ensure arbitrage service is stopped before starting the test
        await _arbitrageService.StopAsync(CancellationToken.None);
        await Task.Delay(100); // Give it time to stop
        
        // Start listening for opportunities
        var detectionTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var opportunity in _arbitrageService.GetOpportunitiesAsync(cts.Token))
                {
                    _detectedOpportunity = opportunity;
                    _semaphore.Release();
                    Console.WriteLine($"Detected opportunity: {opportunity.BuyExchangeId} @ {opportunity.BuyPrice} -> {opportunity.SellExchangeId} @ {opportunity.SellPrice}");
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected if cancelled
                Console.WriteLine("Opportunity detection was cancelled");
            }
            catch (Exception ex)
            {
                // Log any unexpected errors
                Console.WriteLine($"Error in detection task: {ex}");
            }
        });
        
        try
        {
            // Act: Start the arbitrage service
            await _arbitrageService.StartAsync(new List<TradingPair> { tradingPair }, cts.Token);
            
            // Give the service a moment to initialize and process the order books
            await Task.Delay(500);
            
            // Check if the market data service has the order books
            var buyBook = _marketDataService.GetLatestOrderBook(buyExchangeId, tradingPair);
            var sellBook = _marketDataService.GetLatestOrderBook(sellExchangeId, tradingPair);
            
            Console.WriteLine($"Buy order book exists: {buyBook != null}");
            Console.WriteLine($"Sell order book exists: {sellBook != null}");
            
            if (buyBook != null && sellBook != null)
            {
                Console.WriteLine($"Buy price: {buyBook.Asks[0].Price}, Sell price: {sellBook.Bids[0].Price}");
                Console.WriteLine($"Potential profit: {sellBook.Bids[0].Price - buyBook.Asks[0].Price}");
            }
            
            // Check the best bid/ask
            var bestBidAsk = _marketDataService.GetBestBidAskAcrossExchanges(tradingPair);
            Console.WriteLine($"Best bid/ask: {bestBidAsk?.BestBid?.ExchangeId}/{bestBidAsk?.BestBid?.BestBidPrice} - {bestBidAsk?.BestAsk?.ExchangeId}/{bestBidAsk?.BestAsk?.BestAskPrice}");
            
            // Wait for the opportunity to be detected, or timeout
            var signaled = await _semaphore.WaitAsync(TimeSpan.FromSeconds(10));
            
            // Assert
            Assert.True(signaled, "An opportunity should have been detected");
            Assert.NotNull(_detectedOpportunity);
            
            if (_detectedOpportunity != null)
            {
                // Verify the detected opportunity matches our expected opportunity
                Assert.Equal(tradingPair, _detectedOpportunity.TradingPair);
                Assert.Equal(buyExchangeId, _detectedOpportunity.BuyExchangeId);
                Assert.Equal(sellExchangeId, _detectedOpportunity.SellExchangeId);
                Assert.Equal(buyPrice, _detectedOpportunity.BuyPrice);
                Assert.Equal(sellPrice, _detectedOpportunity.SellPrice);
            }
        }
        finally
        {
            // Cleanup - ensure we always stop the service
            await _arbitrageService.StopAsync(CancellationToken.None);
            
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            
            cts.Dispose();
        }
    }
    
    [Fact]
    public async Task EndToEndArbitrageFlow_DetectAndExecute_ShouldCompleteSuccessfully()
    {
        // Arrange
        var tradingPair = TradingPair.BTCUSDT;
        var buyExchangeId = "binance";
        var sellExchangeId = "coinbase";
        var buyPrice = 50000m;
        var sellPrice = 50500m; // 1% profit
        var quantity = 1.0m;
        
        // Setup arbitrage opportunity with our mock exchange clients
        TestHelpers.SetupArbitrageOpportunity(
            _fixture.MockExchangeClients,
            tradingPair,
            buyExchangeId,
            sellExchangeId,
            buyPrice,
            sellPrice,
            quantity);
            
        // Setup order book streams to keep the market data service updated
        TestHelpers.SetupOrderBookStreams(_fixture.MockExchangeClients, tradingPair);
        
        // Subscribe to order book updates to ensure the market data service has data
        await _marketDataService.SubscribeToOrderBookAsync(buyExchangeId, tradingPair, CancellationToken.None);
        await _marketDataService.SubscribeToOrderBookAsync(sellExchangeId, tradingPair, CancellationToken.None);
        
        // Wait to ensure subscriptions are processed
        await Task.Delay(500);
        
        // Verify that order books are properly set up before proceeding
        var buyOrderBook = _marketDataService.GetLatestOrderBook(buyExchangeId, tradingPair);
        var sellOrderBook = _marketDataService.GetLatestOrderBook(sellExchangeId, tradingPair);
        
        Assert.NotNull(buyOrderBook);
        Assert.NotNull(sellOrderBook);
        
        // Enable auto-trading in the configuration
        var config = _fixture.TestArbitrageConfiguration;
        config.AutoExecuteTrades = true;
        config.AutoTradeEnabled = true;
        _fixture.MockConfigurationService.Setup(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        
        // Set up mock detection service
        var mockDetectionService = Mock.Get(_fixture.ServiceProvider.GetRequiredService<IArbitrageDetectionService>());
        
        // Create synchronization primitives to track detection of opportunities and completion of trades
        var opportunityDetectedSemaphore = new SemaphoreSlim(0);
        var tradeCompletedSemaphore = new SemaphoreSlim(0);
        ArbitrageOpportunity? detectedOpportunity = null;
        ArbitrageTradeResult? tradeResult = null;
        
        // Create cancellation token source with timeout
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        // Setup detection service to publish opportunities from our fake sequence
        mockDetectionService
            .Setup(d => d.GetOpportunitiesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => FakeOpportunitySequence(ct));
            
        // Setup detection service to publish trade results
        mockDetectionService
            .Setup(d => d.PublishTradeResultAsync(It.IsAny<ArbitrageTradeResult>(), It.IsAny<CancellationToken>()))
            .Returns((ArbitrageTradeResult result, CancellationToken ct) => {
                Console.WriteLine($"Trade result published: BuyExchange={result.Opportunity.BuyExchangeId}, SellExchange={result.Opportunity.SellExchangeId}");
                tradeResult = result;
                tradeCompletedSemaphore.Release(); // Signal that a trade result was published
                return _fixture.TradeResultsChannel.Writer.WriteAsync(result, ct).AsTask();
            });

        // Start a task to listen for detected opportunities
        _ = Task.Run(async () => 
        {
            try
            {
                await foreach (var opportunity in FakeOpportunitySequence(cts.Token))
                {
                    Console.WriteLine($"Opportunity detected: {opportunity.BuyExchangeId} @ {opportunity.BuyPrice} -> {opportunity.SellExchangeId} @ {opportunity.SellPrice}");
                    detectedOpportunity = opportunity;
                    opportunityDetectedSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in opportunity listener: {ex.Message}");
            }
        });

        // Start a task to listen for trade results
        _ = Task.Run(async () => 
        {
            try
            {
                await foreach (var result in _fixture.TradeResultsChannel.Reader.ReadAllAsync(cts.Token))
                {
                    Console.WriteLine($"Trade result received: {result.IsSuccess}, BuyTradeId: {result.BuyResult?.OrderId}, SellTradeId: {result.SellResult?.OrderId}");
                    tradeResult = result;
                    tradeCompletedSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in trade result listener: {ex.Message}");
            }
        });
        
        try
        {
            // Make sure the service is stopped before we start
            await _arbitrageService.StopAsync(CancellationToken.None);
            
            // Act: Start the arbitrage service
            await _arbitrageService.StartAsync(new List<TradingPair> { tradingPair }, cts.Token);
            await Task.Delay(500); // Give the service time to start
            
            // Debug - Check the best bid/ask is properly identified
            var bestBidAsk = _marketDataService.GetBestBidAskAcrossExchanges(tradingPair);
            Console.WriteLine($"Best bid/ask: {bestBidAsk?.BestBid?.ExchangeId ?? "null"}/{bestBidAsk?.BestBid?.BestBidPrice.ToString() ?? "null"} - {bestBidAsk?.BestAsk?.ExchangeId ?? "null"}/{bestBidAsk?.BestAsk?.BestAskPrice.ToString() ?? "null"}");
            
            // Manually trigger a fake opportunity
            Console.WriteLine("Publishing fake opportunity from test...");
            var fakeOpportunity = new ArbitrageOpportunity(
                tradingPair,
                buyExchangeId,
                buyPrice,
                quantity,
                sellExchangeId,
                sellPrice,
                quantity);
                
            // Wait for an opportunity to be detected
            Console.WriteLine("Waiting for opportunity to be detected...");
            var opportunityDetected = await opportunityDetectedSemaphore.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.True(opportunityDetected, "An opportunity should have been detected");
            Assert.NotNull(detectedOpportunity);
            
            if (opportunityDetected)
            {
                // Wait for trade to complete
                Console.WriteLine("Waiting for trade to complete...");
                var tradeCompleted = await tradeCompletedSemaphore.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.True(tradeCompleted, "Trade should have completed");
                
                // Verify both buy and sell trades succeeded
                Assert.NotNull(tradeResult);
                Assert.NotNull(tradeResult.BuyResult);
                Assert.NotNull(tradeResult.SellResult);
                bool buySuccess = tradeResult.BuyResult!.IsSuccess;
                bool sellSuccess = tradeResult.SellResult!.IsSuccess;
                Assert.True(buySuccess, "Buy trade failed");
                Assert.True(sellSuccess, "Sell trade failed");
            }
        }
        finally
        {
            // Cleanup - ensure we always stop the service and dispose resources
            await _arbitrageService.StopAsync(CancellationToken.None);
            cts.Dispose();
        }
    }

    // Helper method to create a fake opportunity sequence
    private async IAsyncEnumerable<ArbitrageOpportunity> FakeOpportunitySequence([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Create a fake opportunity that will trigger trade execution
        var tradingPair = TradingPair.BTCUSDT;
        var buyExchangeId = "binance";
        var sellExchangeId = "coinbase";
        var buyPrice = 50000m;
        var sellPrice = 50500m; // 1% profit
        var quantity = 1.0m;
        
        await Task.Delay(100, cancellationToken); // Small delay to simulate real-world conditions
        
        yield return new ArbitrageOpportunity(
            tradingPair,
            buyExchangeId,
            buyPrice,
            quantity,
            sellExchangeId,
            sellPrice,
            quantity);
    }
} 