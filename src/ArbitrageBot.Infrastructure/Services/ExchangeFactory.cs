using ArbitrageBot.Application.Interfaces;
using ArbitrageBot.Domain.Models;
using ArbitrageBot.Infrastructure.Exchanges;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArbitrageBot.Infrastructure.Services;

/// <summary>
/// Factory for creating exchange clients.
/// </summary>
public class ExchangeFactory : IExchangeFactory
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ExchangeFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeFactory"/> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ExchangeFactory(
        IConfigurationService configurationService,
        ILogger<ExchangeFactory> logger,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider)
    {
        _configurationService = configurationService;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
    }
    
    /// <inheritdoc />
    public IExchangeClient CreateClient(string exchangeId)
    {
        _logger.LogInformation("Creating exchange client for {ExchangeId}", exchangeId);
        
        return exchangeId.ToLowerInvariant() switch
        {
            "binance" => new StubExchangeClient(exchangeId, _configurationService, _loggerFactory.CreateLogger<StubExchangeClient>()),
            "coinbase" => _serviceProvider.GetRequiredService<CoinbaseExchangeClient>(),
            "kraken" => _serviceProvider.GetRequiredService<KrakenExchangeClient>(),
            "kucoin" => new StubExchangeClient(exchangeId, _configurationService, _loggerFactory.CreateLogger<StubExchangeClient>()),
            "okx" => new StubExchangeClient(exchangeId, _configurationService, _loggerFactory.CreateLogger<StubExchangeClient>()),
            _ => throw new ArgumentException($"Unsupported exchange: {exchangeId}", nameof(exchangeId))
        };
    }
    
    /// <inheritdoc />
    public IReadOnlyCollection<string> GetSupportedExchanges()
    {
        return new[]
        {
            "binance",
            "coinbase",
            "kraken",
            "kucoin",
            "okx"
        };
    }
} 