using MediatR;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Application.Features.PortfolioManagement.Events;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Commands.UpdateBalance;

/// <summary>
/// Handler for updating portfolio balances from exchanges.
/// </summary>
public class UpdateBalanceHandler : IRequestHandler<UpdateBalanceCommand, UpdateBalanceResult>
{
    private readonly IExchangeFactory _exchangeFactory;
    private readonly IPaperTradingService _paperTradingService;
    private readonly IConfigurationService _configurationService;
    private readonly IMediator _mediator;
    private readonly ILogger<UpdateBalanceHandler> _logger;

    // Cache for balance data with timestamps
    private static readonly Dictionary<string, (IReadOnlyCollection<Balance> Balances, DateTime Timestamp)> _balanceCache = new();
    private static readonly object _cacheLock = new object();

    public UpdateBalanceHandler(
        IExchangeFactory exchangeFactory,
        IPaperTradingService paperTradingService,
        IConfigurationService configurationService,
        IMediator mediator,
        ILogger<UpdateBalanceHandler> logger)
    {
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));
        _paperTradingService = paperTradingService ?? throw new ArgumentNullException(nameof(paperTradingService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateBalanceResult> Handle(UpdateBalanceCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();

        _logger.LogInformation(
            "Updating balances - ExchangeId: {ExchangeId}, Currency: {Currency}, ForceRefresh: {ForceRefresh}",
            request.ExchangeId ?? "All", request.Currency ?? "All", request.ForceRefresh);

        try
        {
            // Check if we should use paper trading
            var config = await _configurationService.GetConfigurationAsync(cancellationToken);
            var usePaperTrading = config?.PaperTradingEnabled ?? false || _paperTradingService.IsPaperTradingEnabled;

            Dictionary<string, IReadOnlyCollection<Balance>> updatedBalances;

            if (usePaperTrading)
            {
                updatedBalances = await UpdatePaperTradingBalancesAsync(request, warnings, cancellationToken);
            }
            else
            {
                updatedBalances = await UpdateLiveBalancesAsync(request, warnings, cancellationToken);
            }

            stopwatch.Stop();

            // Publish balance updated events for each exchange
            foreach (var (exchangeId, balances) in updatedBalances)
            {
                await PublishBalanceUpdatedEvent(exchangeId, balances, request, cancellationToken);
            }

            _logger.LogInformation(
                "Successfully updated balances for {ExchangeCount} exchanges in {ElapsedMs}ms",
                updatedBalances.Count, stopwatch.ElapsedMilliseconds);

            return UpdateBalanceResult.Success(updatedBalances, stopwatch.ElapsedMilliseconds, warnings);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error updating balances");
            return UpdateBalanceResult.Failure($"Failed to update balances: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<Dictionary<string, IReadOnlyCollection<Balance>>> UpdatePaperTradingBalancesAsync(
        UpdateBalanceCommand request,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Using paper trading balances");
        
        var allBalances = await _paperTradingService.GetAllBalancesAsync(cancellationToken);
        var result = new Dictionary<string, IReadOnlyCollection<Balance>>();

        if (!string.IsNullOrEmpty(request.ExchangeId))
        {
            // Update specific exchange
            if (allBalances.TryGetValue(request.ExchangeId, out var exchangeBalances))
            {
                var filteredBalances = FilterBalancesByCurrency(exchangeBalances, request.Currency);
                result[request.ExchangeId] = filteredBalances;
            }
            else
            {
                warnings.Add($"No balances found for exchange {request.ExchangeId} in paper trading");
            }
        }
        else
        {
            // Update all exchanges
            foreach (var (exchangeId, balances) in allBalances)
            {
                var filteredBalances = FilterBalancesByCurrency(balances, request.Currency);
                result[exchangeId] = filteredBalances;
            }
        }

        return result;
    }

    private async Task<Dictionary<string, IReadOnlyCollection<Balance>>> UpdateLiveBalancesAsync(
        UpdateBalanceCommand request,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, IReadOnlyCollection<Balance>>();
        var exchangesToUpdate = GetExchangesToUpdate(request);

        foreach (var exchangeId in exchangesToUpdate)
        {
            try
            {
                // Check cache first
                if (!request.ForceRefresh && ShouldUseCachedData(exchangeId, request.MaxCacheAgeMinutes))
                {
                                         lock (_cacheLock)
                     {
                         if (_balanceCache.TryGetValue(exchangeId, out var cached))
                         {
                             var cachedFilteredBalances = FilterBalancesByCurrency(cached.Balances, request.Currency);
                             result[exchangeId] = cachedFilteredBalances;
                             _logger.LogDebug("Using cached balances for {ExchangeId}", exchangeId);
                             continue;
                         }
                     }
                }

                // Fetch fresh balances
                var exchangeClient = await _exchangeFactory.CreateExchangeClientAsync(exchangeId);
                if (exchangeClient == null)
                {
                    warnings.Add($"Failed to create client for exchange {exchangeId}");
                    continue;
                }

                var balances = await exchangeClient.GetBalancesAsync(cancellationToken);
                var filteredBalances = FilterBalancesByCurrency(balances, request.Currency);
                
                result[exchangeId] = filteredBalances;

                // Update cache
                lock (_cacheLock)
                {
                    _balanceCache[exchangeId] = (balances, DateTime.UtcNow);
                }

                _logger.LogDebug("Fetched fresh balances for {ExchangeId}: {BalanceCount} balances", 
                    exchangeId, balances.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update balances for exchange {ExchangeId}", exchangeId);
                warnings.Add($"Failed to update balances for {exchangeId}: {ex.Message}");
            }
        }

        return result;
    }

    private IReadOnlyList<string> GetExchangesToUpdate(UpdateBalanceCommand request)
    {
        if (!string.IsNullOrEmpty(request.ExchangeId))
        {
            return new[] { request.ExchangeId };
        }

        // Get all supported exchanges
        return _exchangeFactory.GetSupportedExchanges().ToList();
    }

    private static bool ShouldUseCachedData(string exchangeId, int maxCacheAgeMinutes)
    {
        lock (_cacheLock)
        {
            if (!_balanceCache.TryGetValue(exchangeId, out var cached))
                return false;

            var cacheAge = DateTime.UtcNow - cached.Timestamp;
            return cacheAge.TotalMinutes < maxCacheAgeMinutes;
        }
    }

    private static IReadOnlyCollection<Balance> FilterBalancesByCurrency(
        IReadOnlyCollection<Balance> balances, 
        string? currency)
    {
        if (string.IsNullOrEmpty(currency))
            return balances;

        return balances.AsEnumerable().Where(b => b.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task PublishBalanceUpdatedEvent(
        string exchangeId,
        IReadOnlyCollection<Balance> newBalances,
        UpdateBalanceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get previous balances from cache for comparison
            IReadOnlyCollection<Balance>? previousBalances = null;
            lock (_cacheLock)
            {
                if (_balanceCache.TryGetValue(exchangeId, out var cached))
                {
                    previousBalances = cached.Balances;
                }
            }

            // Calculate significant changes
            var significantChanges = CalculateSignificantChanges(previousBalances, newBalances);

            await _mediator.Publish(new BalanceUpdatedEvent
            {
                ExchangeId = exchangeId,
                UpdatedBalances = newBalances,
                PreviousBalances = previousBalances,
                SignificantChanges = significantChanges,
                UpdateTrigger = "Manual",
                WasForcedRefresh = request.ForceRefresh
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish BalanceUpdatedEvent for {ExchangeId}", exchangeId);
        }
    }

    private static List<BalanceChange> CalculateSignificantChanges(
        IReadOnlyCollection<Balance>? previous,
        IReadOnlyCollection<Balance> current)
    {
        var changes = new List<BalanceChange>();

        if (previous == null)
            return changes;

        var previousDict = previous.ToDictionary<Balance, string, decimal>(b => b.Currency, b => b.Total);
        var currentDict = current.ToDictionary<Balance, string, decimal>(b => b.Currency, b => b.Total);

        // Check for changes (threshold: 1% or minimum 0.01)
        foreach (var (currency, currentAmount) in currentDict)
        {
            if (previousDict.TryGetValue(currency, out var previousAmount))
            {
                var changePercent = previousAmount > 0 ? Math.Abs((currentAmount - previousAmount) / previousAmount) * 100 : 0;
                var changeAmount = Math.Abs(currentAmount - previousAmount);

                if (changePercent >= 1.0m || changeAmount >= 0.01m)
                {
                    changes.Add(new BalanceChange
                    {
                        Currency = currency,
                        PreviousAmount = previousAmount,
                        NewAmount = currentAmount
                    });
                }
            }
            else if (currentAmount > 0)
            {
                // New currency
                changes.Add(new BalanceChange
                {
                    Currency = currency,
                    PreviousAmount = 0,
                    NewAmount = currentAmount
                });
            }
        }

        return changes;
    }
} 