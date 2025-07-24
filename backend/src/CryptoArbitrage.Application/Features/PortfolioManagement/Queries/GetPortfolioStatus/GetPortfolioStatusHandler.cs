using MediatR;
using CryptoArbitrage.Application.Interfaces;
using CryptoArbitrage.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CryptoArbitrage.Application.Features.PortfolioManagement.Queries.GetPortfolioStatus;

/// <summary>
/// Handler for getting portfolio status and metrics.
/// </summary>
public class GetPortfolioStatusHandler : IRequestHandler<GetPortfolioStatusQuery, GetPortfolioStatusResult>
{
    private readonly IExchangeFactory _exchangeFactory;
    private readonly IPaperTradingService _paperTradingService;
    private readonly IArbitrageRepository _repository;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<GetPortfolioStatusHandler> _logger;

    public GetPortfolioStatusHandler(
        IExchangeFactory exchangeFactory,
        IPaperTradingService paperTradingService,
        IArbitrageRepository repository,
        IConfigurationService configurationService,
        ILogger<GetPortfolioStatusHandler> logger)
    {
        _exchangeFactory = exchangeFactory ?? throw new ArgumentNullException(nameof(exchangeFactory));
        _paperTradingService = paperTradingService ?? throw new ArgumentNullException(nameof(paperTradingService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetPortfolioStatusResult> Handle(GetPortfolioStatusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting portfolio status with options: BalanceDetails={IncludeBalanceDetails}, RiskMetrics={IncludeRiskMetrics}, Performance={IncludePerformanceMetrics}",
            request.IncludeBalanceDetails, request.IncludeRiskMetrics, request.IncludePerformanceMetrics);

        try
        {
            // Get configuration to determine if we're using paper trading
            var config = await _configurationService.GetConfigurationAsync(cancellationToken);
            var usePaperTrading = config?.PaperTradingEnabled ?? false || _paperTradingService.IsPaperTradingEnabled;

            // Get all balances
            IReadOnlyDictionary<string, IReadOnlyCollection<Balance>> allBalances;
            if (usePaperTrading)
            {
                allBalances = await _paperTradingService.GetAllBalancesAsync(cancellationToken);
                _logger.LogDebug("Using paper trading balances");
            }
            else
            {
                allBalances = await GetLiveBalancesAsync(cancellationToken);
                _logger.LogDebug("Using live exchange balances");
            }

            // Calculate portfolio overview
            var overview = CalculatePortfolioOverview(allBalances, request.BaseCurrency);

            // Calculate risk metrics if requested
            PortfolioRiskMetrics? riskMetrics = null;
            if (request.IncludeRiskMetrics)
            {
                riskMetrics = await CalculateRiskMetricsAsync(allBalances, config, cancellationToken);
            }

            // Calculate performance metrics if requested
            PortfolioPerformanceMetrics? performanceMetrics = null;
            if (request.IncludePerformanceMetrics)
            {
                performanceMetrics = await CalculatePerformanceMetricsAsync(cancellationToken);
            }

            // Include balance details if requested
            var balanceDetails = request.IncludeBalanceDetails ? allBalances : null;

            _logger.LogInformation("Portfolio status calculated successfully - Total Value: {TotalValue} {BaseCurrency}, Active Exchanges: {ActiveExchanges}",
                overview.TotalValue, request.BaseCurrency, overview.ActiveExchanges);

            return GetPortfolioStatusResult.Success(overview, balanceDetails, riskMetrics, performanceMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio status");
            return GetPortfolioStatusResult.Failure($"Failed to get portfolio status: {ex.Message}");
        }
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyCollection<Balance>>> GetLiveBalancesAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, IReadOnlyCollection<Balance>>();
        var supportedExchanges = _exchangeFactory.GetSupportedExchanges();

        foreach (var exchangeId in supportedExchanges)
        {
            try
            {
                var client = await _exchangeFactory.CreateExchangeClientAsync(exchangeId);
                if (client != null)
                {
                    var balances = await client.GetBalancesAsync(cancellationToken);
                    result[exchangeId] = balances;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get balances for exchange {ExchangeId}", exchangeId);
                // Continue with other exchanges
            }
        }

        return result;
    }

    private static PortfolioOverview CalculatePortfolioOverview(
        IReadOnlyDictionary<string, IReadOnlyCollection<Balance>> allBalances,
        string baseCurrency)
    {
        int activeExchanges = allBalances.Count;
        var allCurrencies = new HashSet<string>();
        var currencyTotals = new Dictionary<string, decimal>();

        // Aggregate balances across all exchanges
        foreach (var (exchangeId, balances) in allBalances)
        {
            foreach (var balance in balances.Where(b => b.Total > 0))
            {
                allCurrencies.Add(balance.Currency);
                
                if (currencyTotals.ContainsKey(balance.Currency))
                {
                    currencyTotals[balance.Currency] += balance.Total;
                }
                else
                {
                    currencyTotals[balance.Currency] = balance.Total;
                }
            }
        }

        // Calculate total value (simplified - in real implementation, you'd need price conversion)
        decimal totalValue = 0;
        var topAllocations = new List<CurrencyAllocation>();

        foreach (var (currency, amount) in currencyTotals.OrderByDescending(x => x.Value).Take(10))
        {
            // Simplified: assume 1:1 conversion for non-base currencies
            // In real implementation, you'd use market prices for conversion
            decimal valueInBaseCurrency = currency.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase) 
                ? amount 
                : amount * GetEstimatedPrice(currency, baseCurrency);
            
            totalValue += valueInBaseCurrency;

            topAllocations.Add(new CurrencyAllocation
            {
                Currency = currency,
                TotalAmount = amount,
                ValueInBaseCurrency = valueInBaseCurrency,
                PercentageOfPortfolio = 0 // Will be calculated after totalValue is known
            });
        }

        // Calculate percentages
        for (int i = 0; i < topAllocations.Count; i++)
        {
            var allocation = topAllocations[i];
            topAllocations[i] = allocation with 
            { 
                PercentageOfPortfolio = totalValue > 0 
                    ? Math.Round((allocation.ValueInBaseCurrency / totalValue) * 100, 2) 
                    : 0 
            };
        }

        return new PortfolioOverview
        {
            TotalValue = Math.Round(totalValue, 2),
            ActiveExchanges = activeExchanges,
            CurrenciesCount = allCurrencies.Count,
            BaseCurrency = baseCurrency,
            TopAllocations = topAllocations
        };
    }

    private Task<PortfolioRiskMetrics> CalculateRiskMetricsAsync(
        IReadOnlyDictionary<string, IReadOnlyCollection<Balance>> allBalances,
        ArbitrageConfiguration? config,
        CancellationToken cancellationToken)
    {
        var riskProfile = config?.RiskProfile ?? new RiskProfile();
        var warnings = new List<string>();

        // Calculate asset concentration
        var currencyTotals = new Dictionary<string, decimal>();
        decimal totalValue = 0;

                 foreach (var (exchangeId, balances) in allBalances)
         {
             foreach (var balance in balances.Where(b => b.Total > 0))
            {
                var value = balance.Total * GetEstimatedPrice(balance.Currency, "USD");
                currencyTotals[balance.Currency] = currencyTotals.GetValueOrDefault(balance.Currency, 0) + value;
                totalValue += value;
            }
        }

        // Calculate max asset exposure
        var maxAssetExposure = totalValue > 0 
            ? Math.Round((currencyTotals.Values.Max() / totalValue) * 100, 2)
            : 0;

        // Calculate exchange concentration
        var exchangeValues = allBalances.ToDictionary<KeyValuePair<string, IReadOnlyCollection<Balance>>, string, decimal>(
            kv => kv.Key,
            kv => {
                var filteredBalances = kv.Value.Where(b => b.Total > 0);
                return filteredBalances.Sum(b => b.Total * GetEstimatedPrice(b.Currency, "USD"));
            }
        );
        var maxExchangeExposure = totalValue > 0 
            ? Math.Round((exchangeValues.Values.Max() / totalValue) * 100, 2)
            : 0;

        // Check risk thresholds
        if (maxAssetExposure > riskProfile.MaxAssetExposurePercentage)
        {
            warnings.Add($"Asset concentration risk: {maxAssetExposure}% exceeds limit of {riskProfile.MaxAssetExposurePercentage}%");
        }

        if (maxExchangeExposure > 60) // Hardcoded threshold for demo
        {
            warnings.Add($"Exchange concentration risk: {maxExchangeExposure}% on single exchange");
        }

        // Calculate diversification score (simplified)
        var diversificationScore = currencyTotals.Count > 1 
            ? Math.Min(100, currencyTotals.Count * 10) 
            : 0;

        // Calculate overall risk score
        var riskScore = CalculateRiskScore(maxAssetExposure, maxExchangeExposure, diversificationScore);
        var riskLevel = GetRiskLevel(riskScore);

        return Task.FromResult(new PortfolioRiskMetrics
        {
            RiskScore = riskScore,
            RiskLevel = riskLevel,
            MaxAssetExposure = maxAssetExposure,
            DiversificationScore = diversificationScore,
            ExchangeConcentrationRisk = maxExchangeExposure,
            RiskWarnings = warnings
        });
    }

    private async Task<PortfolioPerformanceMetrics> CalculatePerformanceMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get recent trade history
            var recentTrades = await _repository.GetRecentTradesAsync(100);
            
            var successfulTrades = recentTrades.Where(t => t.IsSuccess).ToList();
            var totalTrades = recentTrades.Count;

            var totalProfitLoss = recentTrades.Sum(t => t.ProfitAmount);
            var totalFees = recentTrades.Sum(t => t.Fees);
            var successRate = totalTrades > 0 ? Math.Round((decimal)successfulTrades.Count / totalTrades * 100, 2) : 0;
            var averageTradeProfit = successfulTrades.Any() ? Math.Round(successfulTrades.Average(t => t.ProfitAmount), 4) : 0;

            // Calculate return percentage (simplified - would need initial portfolio value)
            var totalReturnPercentage = 0m; // TODO: Implement based on initial capital

            return new PortfolioPerformanceMetrics
            {
                TotalProfitLoss = Math.Round(totalProfitLoss, 4),
                TotalReturnPercentage = totalReturnPercentage,
                CompletedTrades = totalTrades,
                SuccessRate = successRate,
                AverageTradeProfit = averageTradeProfit,
                TotalFees = Math.Round(totalFees, 4)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate performance metrics");
            return new PortfolioPerformanceMetrics();
        }
    }

    private static decimal GetEstimatedPrice(string currency, string baseCurrency)
    {
        // Simplified price estimation - in real implementation, you'd use market data
        if (currency.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        return currency.ToUpperInvariant() switch
        {
            "BTC" => 50000m,
            "ETH" => 3000m,
            "USDT" or "USDC" or "USD" => 1m,
            "EUR" => 1.1m,
            _ => 1m
        };
    }

    private static decimal CalculateRiskScore(decimal maxAssetExposure, decimal exchangeConcentration, decimal diversificationScore)
    {
        // Simplified risk scoring
        var riskScore = 0m;
        
        // Asset concentration risk (higher exposure = higher risk)
        riskScore += maxAssetExposure * 0.5m;
        
        // Exchange concentration risk
        riskScore += exchangeConcentration * 0.3m;
        
        // Diversification bonus (higher diversification = lower risk)
        riskScore -= diversificationScore * 0.2m;
        
        return Math.Max(0, Math.Min(100, Math.Round(riskScore, 1)));
    }

    private static string GetRiskLevel(decimal riskScore)
    {
        return riskScore switch
        {
            <= 25 => "Low",
            <= 50 => "Medium",
            <= 75 => "High",
            _ => "Critical"
        };
    }
} 