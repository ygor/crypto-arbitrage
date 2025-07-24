using MediatR;
using Microsoft.Extensions.Logging;
using CryptoArbitrage.Application.Services;
using CryptoArbitrage.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace CryptoArbitrage.Application.Features.BotControl.Commands.StartArbitrage;

/// <summary>
/// 🎯 UPDATED: Handler with REAL business logic
/// 
/// Now uses the actual ArbitrageDetectionService to perform real arbitrage detection
/// instead of just setting a flag.
/// </summary>
public class StartArbitrageHandler : IRequestHandler<StartArbitrageCommand, StartArbitrageResult>
{
    private readonly IArbitrageDetectionService _arbitrageDetectionService;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<StartArbitrageHandler> _logger;
    private static bool _isRunning = false;

    public StartArbitrageHandler(
        IArbitrageDetectionService arbitrageDetectionService,
        IConfigurationService configurationService,
        ILogger<StartArbitrageHandler> logger)
    {
        _arbitrageDetectionService = arbitrageDetectionService ?? throw new ArgumentNullException(nameof(arbitrageDetectionService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StartArbitrageResult> Handle(StartArbitrageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (_isRunning || _arbitrageDetectionService.IsRunning)
            {
                return new StartArbitrageResult(false, "Arbitrage bot is already running");
            }

            _logger.LogInformation("Starting arbitrage bot with REAL business logic...");

            // 🎯 REAL BUSINESS LOGIC: Get configuration
            var config = await _configurationService.GetConfigurationAsync();
            if (config == null)
            {
                return new StartArbitrageResult(false, "No arbitrage configuration found");
            }

            // 🎯 REAL BUSINESS LOGIC: Start actual arbitrage detection
            var exchanges = config.EnabledExchanges;
            var tradingPairs = config.TradingPairs.Select(tp => tp.ToString()).ToArray();

            await _arbitrageDetectionService.StartDetectionAsync(exchanges, tradingPairs);
            
            _isRunning = true;
            
            _logger.LogInformation("Arbitrage bot started successfully with real detection for {ExchangeCount} exchanges and {PairCount} trading pairs", 
                exchanges.Count(), tradingPairs.Length);
            
            return new StartArbitrageResult(true, 
                $"Arbitrage bot started successfully - monitoring {exchanges.Count()} exchanges for {tradingPairs.Length} trading pairs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start arbitrage bot");
            return new StartArbitrageResult(false, $"Failed to start arbitrage bot: {ex.Message}");
        }
    }
} 