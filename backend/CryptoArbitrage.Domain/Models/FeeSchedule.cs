namespace CryptoArbitrage.Domain.Models;

/// <summary>
/// Represents the fee schedule for an exchange.
/// </summary>
public class FeeSchedule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeeSchedule"/> class.
    /// </summary>
    /// <param name="exchangeId">The exchange ID.</param>
    /// <param name="makerFeeRate">The maker fee rate.</param>
    /// <param name="takerFeeRate">The taker fee rate.</param>
    /// <param name="withdrawalFee">The withdrawal fee.</param>
    public FeeSchedule(string exchangeId, decimal makerFeeRate = 0.001m, decimal takerFeeRate = 0.001m, decimal withdrawalFee = 0)
    {
        ExchangeId = exchangeId;
        MakerFeeRate = makerFeeRate;
        TakerFeeRate = takerFeeRate;
        WithdrawalFee = withdrawalFee;
    }

    /// <summary>
    /// Gets or sets the exchange ID.
    /// </summary>
    public string ExchangeId { get; set; }
    
    /// <summary>
    /// Gets or sets the maker fee rate.
    /// </summary>
    public decimal MakerFeeRate { get; set; }

    /// <summary>
    /// Gets or sets the taker fee rate.
    /// </summary>
    public decimal TakerFeeRate { get; set; }
    
    /// <summary>
    /// Gets or sets the withdrawal fee.
    /// </summary>
    public decimal WithdrawalFee { get; set; }

    /// <summary>
    /// Gets the fee rate for a specific order side and type.
    /// </summary>
    /// <param name="side">The order side.</param>
    /// <param name="type">The order type.</param>
    /// <returns>The applicable fee rate.</returns>
    public decimal GetFeeRate(OrderSide side, OrderType type)
    {
        // Market orders are always taker orders
        if (type == OrderType.Market)
        {
            return TakerFeeRate;
        }

        // For limit orders, assume maker fee but this is a simplification
        // In reality, it depends on whether the order is immediately matched (taker) or not (maker)
        return MakerFeeRate;
    }
} 