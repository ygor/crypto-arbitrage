using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using Moq;
using System.Runtime.CompilerServices;

namespace ArbitrageBot.Tests.EndToEndTests
{
    /// <summary>
    /// Helper methods for testing.
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Sets up mock order books across exchanges to simulate an arbitrage opportunity.
        /// </summary>
        public static ArbitrageOpportunity SetupArbitrageOpportunity(
            Dictionary<string, Mock<IExchangeClient>> mockClients,
            TradingPair tradingPair,
            string buyExchangeId,
            string sellExchangeId,
            decimal buyPrice,
            decimal sellPrice,
            decimal quantity = 1.0m)
        {
            // Validate exchanges exist
            if (!mockClients.TryGetValue(buyExchangeId, out var buyExchangeMock))
            {
                throw new ArgumentException($"Exchange {buyExchangeId} not found in mock clients");
            }
            
            if (!mockClients.TryGetValue(sellExchangeId, out var sellExchangeMock))
            {
                throw new ArgumentException($"Exchange {sellExchangeId} not found in mock clients");
            }
            
            // Create order books
            var buyOrderBook = CreateOrderBook(
                buyExchangeId, 
                tradingPair, 
                askStartPrice: buyPrice,
                bidStartPrice: buyPrice * 0.99m, // Bid slightly lower 
                quantity: quantity);
            
            var sellOrderBook = CreateOrderBook(
                sellExchangeId, 
                tradingPair,
                askStartPrice: sellPrice * 1.01m, // Ask slightly higher
                bidStartPrice: sellPrice,
                quantity: quantity);
            
            // Set up GetOrderBookSnapshotAsync to return order books
            buyExchangeMock.Setup(c => c.GetOrderBookSnapshotAsync(
                    It.Is<TradingPair>(tp => tp.Equals(tradingPair)), 
                    It.IsAny<int>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(buyOrderBook);
            
            sellExchangeMock.Setup(c => c.GetOrderBookSnapshotAsync(
                    It.Is<TradingPair>(tp => tp.Equals(tradingPair)), 
                    It.IsAny<int>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sellOrderBook);
            
            // Set up mock orders based on the opportunity
            SetupMockOrders(buyExchangeMock, sellExchangeMock, tradingPair, buyPrice, sellPrice, quantity);
            
            // Create and return the simulated arbitrage opportunity
            return new ArbitrageOpportunity(
                tradingPair: tradingPair,
                buyExchangeId: buyExchangeId,
                buyPrice: buyPrice,
                buyQuantity: quantity,
                sellExchangeId: sellExchangeId,
                sellPrice: sellPrice,
                sellQuantity: quantity);
        }
        
        /// <summary>
        /// Creates an order book for testing with specific prices and quantity.
        /// </summary>
        public static OrderBook CreateOrderBook(
            string exchangeId,
            TradingPair tradingPair,
            decimal askStartPrice,
            decimal bidStartPrice,
            decimal quantity = 1.0m,
            int levels = 5)
        {
            var bids = new List<OrderBookEntry>();
            var asks = new List<OrderBookEntry>();
            
            // Create bid entries (highest to lowest)
            for (int i = 0; i < levels; i++)
            {
                decimal price = bidStartPrice * (1 - (i * 0.001m)); // Decrease price by 0.1% for each level
                decimal qty = quantity * (1 + (i * 0.5m)); // Increase quantity for lower prices
                bids.Add(new OrderBookEntry(price, qty));
            }
            
            // Create ask entries (lowest to highest)
            for (int i = 0; i < levels; i++)
            {
                decimal price = askStartPrice * (1 + (i * 0.001m)); // Increase price by 0.1% for each level
                decimal qty = quantity * (1 + (i * 0.5m)); // Increase quantity for higher prices
                asks.Add(new OrderBookEntry(price, qty));
            }
            
            return new OrderBook(
                exchangeId: exchangeId,
                tradingPair: tradingPair,
                timestamp: DateTime.UtcNow,
                bids: bids,
                asks: asks);
        }
        
        /// <summary>
        /// Sets up mock order responses for exchanges involved in the opportunity.
        /// </summary>
        public static void SetupMockOrders(
            Mock<IExchangeClient> buyExchangeMock, 
            Mock<IExchangeClient> sellExchangeMock,
            TradingPair tradingPair,
            decimal buyPrice,
            decimal sellPrice,
            decimal quantity)
        {
            // Setup buy order
            var buyOrder = new Order(
                id: Guid.NewGuid().ToString(),
                exchangeId: buyExchangeMock.Object.ExchangeId,
                tradingPair: tradingPair,
                side: OrderSide.Buy,
                type: OrderType.Market,
                status: OrderStatus.Filled,
                price: buyPrice,
                quantity: quantity,
                timestamp: DateTime.UtcNow)
            {
                FilledQuantity = quantity,
                AverageFillPrice = buyPrice
            };
            
            buyExchangeMock.Setup(c => c.PlaceMarketOrderAsync(
                    It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                    It.Is<OrderSide>(os => os == OrderSide.Buy),
                    It.IsAny<decimal>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(buyOrder);
            
            // Setup sell order
            var sellOrder = new Order(
                id: Guid.NewGuid().ToString(),
                exchangeId: sellExchangeMock.Object.ExchangeId,
                tradingPair: tradingPair,
                side: OrderSide.Sell,
                type: OrderType.Market,
                status: OrderStatus.Filled,
                price: sellPrice,
                quantity: quantity,
                timestamp: DateTime.UtcNow)
            {
                FilledQuantity = quantity,
                AverageFillPrice = sellPrice
            };
            
            sellExchangeMock.Setup(c => c.PlaceMarketOrderAsync(
                    It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                    It.Is<OrderSide>(os => os == OrderSide.Sell),
                    It.IsAny<decimal>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sellOrder);
        }
        
        /// <summary>
        /// Sets up mock exchange clients to provide real-time order book updates through channels.
        /// </summary>
        public static void SetupOrderBookStreams(
            Dictionary<string, Mock<IExchangeClient>> mockClients,
            TradingPair tradingPair)
        {
            foreach (var clientPair in mockClients)
            {
                var exchangeId = clientPair.Key;
                var mockClient = clientPair.Value;
                
                // Create a sample order book for this exchange/trading pair
                var orderBook = CreateOrderBook(
                    exchangeId, 
                    tradingPair,
                    exchangeId == "binance" ? 50000m : 50500m,  // Match test expectations
                    exchangeId == "binance" ? 49950m : 50500m,
                    quantity: 1.0m);
                    
                // For each mock client, set up a streaming implementation
                mockClient.Setup(c => c.GetOrderBookUpdatesAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                        It.IsAny<CancellationToken>()))
                    .Returns((TradingPair tp, CancellationToken token) => SimulateOrderBookStreamAsync(orderBook, token));
                
                mockClient.Setup(c => c.SubscribeToOrderBookAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                
                mockClient.Setup(c => c.UnsubscribeFromOrderBookAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                // Important: Make sure GetOrderBookSnapshotAsync returns a valid order book
                mockClient.Setup(c => c.GetOrderBookSnapshotAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)), 
                        It.IsAny<int>(), 
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(orderBook);
            }
        }
        
        // Helper method to simulate a stream of order book updates
        private static async IAsyncEnumerable<OrderBook> SimulateOrderBookStreamAsync(
            OrderBook initialOrderBook,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Return initial order book first
            yield return initialOrderBook;
            
            // Then return some slightly modified versions
            for (int i = 0; i < 5; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                    
                await Task.Delay(20, cancellationToken);
                
                var updatedOrderBook = CreateUpdatedOrderBook(
                    initialOrderBook,
                    askPriceChange: 0.001m * (i % 2 == 0 ? 1 : -1),  // Small price fluctuations
                    bidPriceChange: 0.001m * (i % 2 == 0 ? -1 : 1));
                    
                yield return updatedOrderBook;
            }
        }
        
        /// <summary>
        /// Sets up mock balances for exchange clients.
        /// </summary>
        public static void SetupBalances(
            Dictionary<string, Mock<IExchangeClient>> mockClients,
            TradingPair tradingPair)
        {
            foreach (var clientPair in mockClients)
            {
                var exchangeId = clientPair.Key;
                var mockClient = clientPair.Value;
                
                // Create balances that give enough funds for trading
                var balances = new List<Balance>
                {
                    new Balance(exchangeId, tradingPair.BaseCurrency, 10, 10),
                    new Balance(exchangeId, tradingPair.QuoteCurrency, 100000, 100000)
                };
                
                mockClient.Setup(c => c.GetBalancesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(balances);
                    
                // No setup for individual balance requests since IExchangeClient doesn't have GetBalanceAsync
            }
        }

        /// <summary>
        /// Sets up mock exchange clients for streaming order book tests.
        /// </summary>
        /// <param name="mockClients">Dictionary of mock clients</param>
        /// <param name="tradingPair">Trading pair to stream</param>
        /// <param name="orderBookChannels">Returns a dictionary of order book channels for each exchange</param>
        /// <returns>Dictionary of initial order books</returns>
        public static Dictionary<string, OrderBook> SetupStreamingOrderBooks(
            Dictionary<string, Mock<IExchangeClient>> mockClients,
            TradingPair tradingPair,
            out Dictionary<string, Channel<OrderBook>> orderBookChannels)
        {
            var initialOrderBooks = new Dictionary<string, OrderBook>();
            orderBookChannels = new Dictionary<string, Channel<OrderBook>>();
            
            foreach (var clientPair in mockClients)
            {
                var exchangeId = clientPair.Key;
                var mockClient = clientPair.Value;
                
                // Create a channel for this exchange
                var channel = Channel.CreateUnbounded<OrderBook>();
                orderBookChannels[exchangeId] = channel;
                
                // Create an initial order book with slightly different prices for each exchange
                var baseAskPrice = exchangeId == "binance" ? 50000m : 50500m;
                var baseBidPrice = exchangeId == "binance" ? 49950m : 50500m;
                
                var orderBook = CreateOrderBook(
                    exchangeId, 
                    tradingPair, 
                    baseAskPrice, 
                    baseBidPrice);
                
                initialOrderBooks[exchangeId] = orderBook;
                
                // Set up the mock client to return the initial order book
                mockClient.Setup(c => c.GetOrderBookSnapshotAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(orderBook);
                
                // Set up the mock client to read from the channel
                mockClient.Setup(c => c.GetOrderBookUpdatesAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                        It.IsAny<CancellationToken>()))
                    .Returns((TradingPair tp, CancellationToken ct) => 
                        channel.Reader.ReadAllAsync(ct));
                
                // Set up subscription methods to do nothing (they're handled at a higher level)
                mockClient.Setup(c => c.SubscribeToOrderBookAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                
                mockClient.Setup(c => c.UnsubscribeFromOrderBookAsync(
                        It.Is<TradingPair>(tp => tp.Equals(tradingPair)),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            }
            
            return initialOrderBooks;
        }
        
        /// <summary>
        /// Creates an updated order book with modified prices for streaming test scenarios.
        /// </summary>
        public static OrderBook CreateUpdatedOrderBook(
            OrderBook baseOrderBook,
            decimal askPriceChange,
            decimal bidPriceChange)
        {
            var newAskPrice = baseOrderBook.Asks.First().Price + askPriceChange;
            var newBidPrice = baseOrderBook.Bids.First().Price + bidPriceChange;
            
            return CreateOrderBook(
                baseOrderBook.ExchangeId,
                baseOrderBook.TradingPair,
                newAskPrice,
                newBidPrice);
        }
    }
} 