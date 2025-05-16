namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the time in force for an order.
/// </summary>
public enum TimeInForce
{
    /// <summary>
    /// Good till canceled - the order remains active until it is filled or canceled.
    /// </summary>
    GoodTillCancel,
    
    /// <summary>
    /// Immediate or cancel - the order must be filled immediately, at least partially, or it is canceled.
    /// </summary>
    ImmediateOrCancel,
    
    /// <summary>
    /// Fill or kill - the order must be filled immediately in its entirety or it is canceled.
    /// </summary>
    FillOrKill,
    
    /// <summary>
    /// Good till date - the order remains active until a specified date/time, after which it is canceled.
    /// </summary>
    GoodTillDate
} 