using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoArbitrage.UI.Tests.Infrastructure;

public static class TestInfrastructureSetup
{
	public static void AddDefaultUiTestServices(this IServiceCollection services)
	{
		// Placeholder for shared UI test services if needed
	}
}

public class UiTestMockExchangeClient : IExchangeClient
{
	public UiTestMockExchangeClient(string exchangeId)
	{
		ExchangeId = exchangeId;
	}

	public string ExchangeId { get; }
	public bool IsConnected => true;
	public bool IsAuthenticated => true;
	public bool SupportsStreaming => false;
	public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task AuthenticateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task<FeeSchedule> GetFeeScheduleAsync(CancellationToken cancellationToken = default) => Task.FromResult(new FeeSchedule(ExchangeId, 0.001m, 0.001m));
	public Task<OrderBook> GetOrderBookSnapshotAsync(TradingPair tradingPair, int depth = 10, CancellationToken cancellationToken = default)
	{
		var bids = new List<OrderBookEntry> { new(100m, 1m) };
		var asks = new List<OrderBookEntry> { new(101m, 1m) };
		return Task.FromResult(new OrderBook(ExchangeId, tradingPair, DateTime.UtcNow, bids, asks));
	}
	public IAsyncEnumerable<OrderBook> GetOrderBookUpdatesAsync(TradingPair tradingPair, CancellationToken cancellationToken = default)
	{
		return EmptyUpdates();
	}

	private static async IAsyncEnumerable<OrderBook> EmptyUpdates()
	{
		yield break;
	}
	public Task SubscribeToOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task UnsubscribeFromOrderBookAsync(TradingPair tradingPair, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public Task<IReadOnlyCollection<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Balance>>(new List<Balance>());
	public Task<Order> PlaceMarketOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal quantity, CancellationToken cancellationToken = default)
	{
		var order = new Order(Guid.NewGuid().ToString(), ExchangeId, tradingPair, orderSide, OrderType.Market, OrderStatus.Filled, 100m, quantity, DateTime.UtcNow);
		order.FilledQuantity = quantity;
		order.AverageFillPrice = 100m;
		return Task.FromResult(order);
	}
	public Task<TradeResult> PlaceLimitOrderAsync(TradingPair tradingPair, OrderSide orderSide, decimal price, decimal quantity, OrderType orderType = OrderType.Limit, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(TradeResult.Success(Guid.NewGuid().ToString(), ExchangeId, orderSide, quantity, price, price * quantity, tradingPair.QuoteCurrency, 10));
	}
	public Task<decimal> GetTradingFeeRateAsync(TradingPair tradingPair, CancellationToken cancellationToken = default) => Task.FromResult(0.001m);
}

 