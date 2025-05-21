namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the status of a trade.
/// </summary>
public enum TradeStatus
{
    /// <summary>
    /// The trade has been created but not yet executed.
    /// </summary>
    Created = 0,

    /// <summary>
    /// The trade is currently being executed.
    /// </summary>
    Executing = 1,

    /// <summary>
    /// The trade has been completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The trade has failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The trade has been cancelled.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// The trade status is unknown or in an unexpected state.
    /// </summary>
    Unknown = 5
} 