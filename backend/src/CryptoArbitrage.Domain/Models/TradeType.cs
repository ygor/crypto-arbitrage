namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Enum representing different types of trades.
/// </summary>
public enum TradeType
{
    /// <summary>
    /// Unknown trade type.
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Buy trade.
    /// </summary>
    Buy = 1,
    
    /// <summary>
    /// Sell trade.
    /// </summary>
    Sell = 2,
    
    /// <summary>
    /// Deposit.
    /// </summary>
    Deposit = 3,
    
    /// <summary>
    /// Withdrawal.
    /// </summary>
    Withdrawal = 4,
    
    /// <summary>
    /// Fee.
    /// </summary>
    Fee = 5,
    
    /// <summary>
    /// Transfer between accounts.
    /// </summary>
    Transfer = 6
} 