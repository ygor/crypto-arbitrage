namespace ArbitrageBot.Domain.Models;

/// <summary>
/// Represents the type of an order.
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Limit order - executed at the specified price or better.
    /// </summary>
    Limit,
    
    /// <summary>
    /// Market order - executed immediately at the current market price.
    /// </summary>
    Market,
    
    /// <summary>
    /// Immediate-or-Cancel (IOC) order - executed immediately at the specified price or better, 
    /// and any unfilled portion is canceled.
    /// </summary>
    ImmediateOrCancel,
    
    /// <summary>
    /// Fill-or-Kill (FOK) order - executed immediately and completely at the specified price or better,
    /// or not executed at all.
    /// </summary>
    FillOrKill
} 