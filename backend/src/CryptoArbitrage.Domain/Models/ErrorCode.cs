using System;

namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Enum representing different types of errors that can occur in the arbitrage system.
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// Unknown error.
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Invalid trading pair format.
    /// </summary>
    InvalidTradingPair = 1,
    
    /// <summary>
    /// Failed to scan for arbitrage opportunity.
    /// </summary>
    FailedToScanForOpportunity = 2,
    
    /// <summary>
    /// Failed to retrieve trades.
    /// </summary>
    FailedToRetrieveTrades = 3,
    
    /// <summary>
    /// Failed to save trade result.
    /// </summary>
    FailedToSaveTradeResult = 4,
    
    /// <summary>
    /// Trade execution failed.
    /// </summary>
    TradeExecutionFailed = 5,
    
    /// <summary>
    /// Failed to execute arbitrage opportunity.
    /// </summary>
    FailedToExecuteArbitrage = 6,
    
    /// <summary>
    /// Failed to calculate profit.
    /// </summary>
    FailedToCalculateProfit = 7,
    
    /// <summary>
    /// Failed to calculate fee.
    /// </summary>
    FailedToCalculateFee = 8,
    
    /// <summary>
    /// Failed to save trade.
    /// </summary>
    FailedToSaveTrade = 9,
    
    /// <summary>
    /// Failed to connect to exchange.
    /// </summary>
    ExchangeConnectionFailed = 10,
    
    /// <summary>
    /// Failed to authenticate with exchange.
    /// </summary>
    ExchangeAuthenticationFailed = 11,
    
    /// <summary>
    /// Failed to get order book.
    /// </summary>
    FailedToGetOrderBook = 12,
    
    /// <summary>
    /// Failed to place order.
    /// </summary>
    FailedToPlaceOrder = 13,
    
    /// <summary>
    /// Failed to get balance.
    /// </summary>
    FailedToGetBalance = 14,
    
    /// <summary>
    /// Failed to get trading fee.
    /// </summary>
    FailedToGetTradingFee = 15,
    
    /// <summary>
    /// Failed to get market data.
    /// </summary>
    FailedToGetMarketData = 16,
    
    /// <summary>
    /// Failed to get configuration.
    /// </summary>
    FailedToGetConfiguration = 17,
    
    /// <summary>
    /// Failed to save configuration.
    /// </summary>
    FailedToSaveConfiguration = 18,
    
    /// <summary>
    /// Failed to get risk profile.
    /// </summary>
    FailedToGetRiskProfile = 19,
    
    /// <summary>
    /// Failed to save risk profile.
    /// </summary>
    FailedToSaveRiskProfile = 20,
    
    /// <summary>
    /// Failed to get exchange configuration.
    /// </summary>
    FailedToGetExchangeConfiguration = 21,
    
    /// <summary>
    /// Failed to save exchange configuration.
    /// </summary>
    FailedToSaveExchangeConfiguration = 22,
    
    /// <summary>
    /// Failed to get notification configuration.
    /// </summary>
    FailedToGetNotificationConfiguration = 23,
    
    /// <summary>
    /// Failed to save notification configuration.
    /// </summary>
    FailedToSaveNotificationConfiguration = 24,
    
    /// <summary>
    /// Failed to send notification.
    /// </summary>
    FailedToSendNotification = 25,
    
    /// <summary>
    /// Failed to get statistics.
    /// </summary>
    FailedToGetStatistics = 26,
    
    /// <summary>
    /// Failed to save statistics.
    /// </summary>
    FailedToSaveStatistics = 27,
    
    /// <summary>
    /// Failed to get opportunities.
    /// </summary>
    FailedToGetOpportunities = 28,
    
    /// <summary>
    /// Failed to save opportunity.
    /// </summary>
    FailedToSaveOpportunity = 29,
    
    /// <summary>
    /// Failed to get trade results.
    /// </summary>
    FailedToGetTradeResults = 30,
    
    /// <summary>
    /// Failed to get active trading pairs.
    /// </summary>
    FailedToGetActiveTradingPairs = 31,
    
    /// <summary>
    /// Failed to add trading pair.
    /// </summary>
    FailedToAddTradingPair = 32,
    
    /// <summary>
    /// Failed to remove trading pair.
    /// </summary>
    FailedToRemoveTradingPair = 33,
    
    /// <summary>
    /// Failed to publish trade result.
    /// </summary>
    FailedToPublishTradeResult = 34,
    
    /// <summary>
    /// Failed to get market data snapshot.
    /// </summary>
    FailedToGetMarketDataSnapshot = 35,
    
    /// <summary>
    /// Failed to subscribe to market data.
    /// </summary>
    FailedToSubscribeToMarketData = 36,
    
    /// <summary>
    /// Failed to unsubscribe from market data.
    /// </summary>
    FailedToUnsubscribeFromMarketData = 37,
    
    /// <summary>
    /// Failed to get order book snapshot.
    /// </summary>
    FailedToGetOrderBookSnapshot = 38,
    
    /// <summary>
    /// Failed to subscribe to order book.
    /// </summary>
    FailedToSubscribeToOrderBook = 39,
    
    /// <summary>
    /// Failed to unsubscribe from order book.
    /// </summary>
    FailedToUnsubscribeFromOrderBook = 40,
    
    /// <summary>
    /// Failed to get ticker.
    /// </summary>
    FailedToGetTicker = 41,
    
    /// <summary>
    /// Failed to subscribe to ticker.
    /// </summary>
    FailedToSubscribeToTicker = 42,
    
    /// <summary>
    /// Failed to unsubscribe from ticker.
    /// </summary>
    FailedToUnsubscribeFromTicker = 43,
    
    /// <summary>
    /// Failed to get trades.
    /// </summary>
    FailedToGetTrades = 44,
    
    /// <summary>
    /// Failed to subscribe to trades.
    /// </summary>
    FailedToSubscribeToTrades = 45,
    
    /// <summary>
    /// Failed to unsubscribe from trades.
    /// </summary>
    FailedToUnsubscribeFromTrades = 46,
    
    /// <summary>
    /// Failed to get candles.
    /// </summary>
    FailedToGetCandles = 47,
    
    /// <summary>
    /// Failed to subscribe to candles.
    /// </summary>
    FailedToSubscribeToCandles = 48,
    
    /// <summary>
    /// Failed to unsubscribe from candles.
    /// </summary>
    FailedToUnsubscribeFromCandles = 49,
    
    /// <summary>
    /// Failed to get order.
    /// </summary>
    FailedToGetOrder = 50,
    
    /// <summary>
    /// Failed to get orders.
    /// </summary>
    FailedToGetOrders = 51,
    
    /// <summary>
    /// Failed to cancel order.
    /// </summary>
    FailedToCancelOrder = 52,
    
    /// <summary>
    /// Failed to cancel all orders.
    /// </summary>
    FailedToCancelAllOrders = 53,
    
    /// <summary>
    /// Failed to get order book depth.
    /// </summary>
    FailedToGetOrderBookDepth = 54,
    
    /// <summary>
    /// Failed to get order book depth for trading pair.
    /// </summary>
    FailedToGetOrderBookDepthForTradingPair = 55,
    
    /// <summary>
    /// Failed to get order book depth for exchange.
    /// </summary>
    FailedToGetOrderBookDepthForExchange = 56,
    
    /// <summary>
    /// Failed to get order book depth for trading pair and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForTradingPairAndExchange = 57,
    
    /// <summary>
    /// Failed to get order book depth for trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForTradingPairAndExchanges = 58,
    
    /// <summary>
    /// Failed to get order book depth for trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForTradingPairs = 59,
    
    /// <summary>
    /// Failed to get order book depth for trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForTradingPairsAndExchange = 60,
    
    /// <summary>
    /// Failed to get order book depth for trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForTradingPairsAndExchanges = 61,
    
    /// <summary>
    /// Failed to get order book depth for all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllTradingPairs = 62,
    
    /// <summary>
    /// Failed to get order book depth for all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllTradingPairsAndExchange = 63,
    
    /// <summary>
    /// Failed to get order book depth for all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllTradingPairsAndExchanges = 64,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchanges = 65,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and trading pair.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndTradingPair = 66,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndTradingPairs = 67,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairs = 68,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndExchange = 69,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndExchanges = 70,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchanges = 71,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairs = 72,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchange = 73,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchanges = 74,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchanges = 75,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairs = 76,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchange = 77,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchanges = 78,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchanges = 79,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairs = 80,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchange = 81,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchanges = 82,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchanges = 83,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairs = 84,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchange = 85,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchanges = 86,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchanges = 87,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairs = 88,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchange = 89,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchanges = 90,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchanges = 91,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairs = 92,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchange = 93,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchanges = 94,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchanges = 95,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairs = 96,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchange.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchange = 97,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndExchanges = 98,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchanges = 99,
    
    /// <summary>
    /// Failed to get order book depth for all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs and all exchanges and all trading pairs.
    /// </summary>
    FailedToGetOrderBookDepthForAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairsAndAllExchangesAndAllTradingPairs = 100
} 