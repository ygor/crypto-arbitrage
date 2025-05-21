namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the type of an order.
/// </summary>
public enum OrderType
{
    /// <summary>
    /// A limit order that executes at a specified price or better.
    /// </summary>
    Limit = 0,

    /// <summary>
    /// A market order that executes at the current market price.
    /// </summary>
    Market = 1,

    /// <summary>
    /// A stop-limit order that becomes a limit order when the stop price is reached.
    /// </summary>
    StopLimit = 2,

    /// <summary>
    /// A stop-market order that becomes a market order when the stop price is reached.
    /// </summary>
    StopMarket = 3,

    /// <summary>
    /// A trailing stop order that adjusts the stop price as the market price moves.
    /// </summary>
    TrailingStop = 4,

    /// <summary>
    /// A fill-or-kill order that must be filled immediately or cancelled.
    /// </summary>
    FillOrKill = 5,

    /// <summary>
    /// An immediate-or-cancel order that must be filled immediately or cancelled.
    /// </summary>
    ImmediateOrCancel = 6,

    /// <summary>
    /// A good-till-cancelled order that remains active until filled or cancelled.
    /// </summary>
    GoodTillCancelled = 7,

    /// <summary>
    /// A good-till-date order that remains active until a specified date.
    /// </summary>
    GoodTillDate = 8,

    /// <summary>
    /// A post-only order that will only be filled as a maker.
    /// </summary>
    PostOnly = 9,

    /// <summary>
    /// An order type that is not supported or unknown.
    /// </summary>
    Unknown = 10
} 