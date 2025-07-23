using MediatR;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Commands.UpdateBalance;

/// <summary>
/// Command to update balances from exchanges.
/// </summary>
public record UpdateBalanceCommand : IRequest<UpdateBalanceResult>
{
    /// <summary>
    /// Specific exchange ID to update balances for. If null, updates all exchanges.
    /// </summary>
    public string? ExchangeId { get; init; }

    /// <summary>
    /// Specific currency to update. If null, updates all currencies.
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Whether to force refresh from exchange APIs even if cached data is recent.
    /// </summary>
    public bool ForceRefresh { get; init; } = false;

    /// <summary>
    /// Maximum age of cached data in minutes before forcing refresh.
    /// </summary>
    public int MaxCacheAgeMinutes { get; init; } = 5;
} 