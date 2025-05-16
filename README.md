# Crypto Arbitrage Bot

A .NET-based cryptocurrency arbitrage trading bot that automatically detects and executes profitable arbitrage opportunities across multiple exchanges.

## Project Overview

The Crypto Arbitrage Bot is designed to:

1. Monitor price differences for cryptocurrencies across multiple exchanges
2. Detect profitable arbitrage opportunities
3. Execute trades automatically when opportunities meet criteria
4. Track performance and provide notifications

The system is built with a clean architecture approach, separating concerns into distinct layers:

- **Domain**: Core business entities and logic
- **Application**: Business use cases and orchestration
- **Infrastructure**: External dependencies and technical details
- **Worker**: Background service that runs the arbitrage bot

## Features

- **Multi-Exchange Support**: Works with Binance, Coinbase, Kraken, and more (extensible)
- **Configurable Risk Management**: Fine-tune risk parameters through risk profiles
- **Performance Tracking**: Track arbitrage opportunities, executed trades, and profit/loss
- **Notifications**: Email and webhook notifications for important events
- **Flexible Configuration**: Easy to configure via appsettings.json
- **Paper Trading Mode**: Test strategies without risking real funds

## Getting Started

### Prerequisites

- .NET 7.0 SDK or later
- API access to cryptocurrency exchanges

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/crypto-arbitrage.git
   cd crypto-arbitrage
   ```

2. Build the solution:
   ```
   dotnet build
   ```

3. Update the configuration in `src/ArbitrageBot.Worker/appsettings.json` with your exchange API keys and settings.

### Configuration

Configure the bot by editing `appsettings.json`. Key settings include:

- **Exchange API credentials**: Set up your exchange API keys
- **Trading pairs**: Configure which cryptocurrency pairs to monitor
- **Risk profile**: Set risk parameters like maximum capital per trade
- **Notification settings**: Configure email and webhook notifications

Example configuration:

```json
{
  "ArbitrageBot": {
    "IsEnabled": true,
    "AutoTradeEnabled": false,
    "PaperTradingEnabled": true,
    "MinimumProfitPercentage": 0.5,
    "MaxConcurrentOperations": 3,
    "PollingIntervalMs": 1000,
    "TradingPairs": ["BTC/USDT", "ETH/USDT", "BNB/USDT"],
    "RiskProfile": {
      "Type": "Balanced",
      "MaxCapitalPerTradePercent": 10.0,
      "MinimumProfitPercentage": 0.5
    },
    "Exchanges": {
      "Binance": {
        "IsEnabled": true,
        "ApiKey": "your-api-key",
        "ApiSecret": "your-api-secret"
      },
      "Coinbase": {
        "IsEnabled": true,
        "ApiKey": "your-api-key",
        "ApiSecret": "your-api-secret"
      }
    },
    "Notifications": {
      "Email": {
        "Enabled": true,
        "SmtpServer": "smtp.example.com",
        "SmtpPort": 587,
        "Username": "your-email@example.com",
        "Password": "your-password",
        "FromAddress": "bot@example.com",
        "ToAddresses": ["you@example.com"]
      },
      "Webhook": {
        "Enabled": false,
        "Url": "https://example.com/webhook"
      }
    }
  }
}
```

### Running the Bot

To run the bot:

```
cd src/ArbitrageBot.Worker
dotnet run
```

For production deployment, consider using a service manager like systemd or Docker.

## Paper Trading Mode

The system includes a paper trading mode that allows you to test strategies without risking real funds. Paper trading simulates trades based on real market data but only updates virtual balances.

### Enabling Paper Trading

To enable paper trading mode, update your `appsettings.json`:

```json
{
  "ArbitrageBot": {
    "PaperTradingEnabled": true,
    // Other settings...
  }
}
```

### How Paper Trading Works

When paper trading is enabled:

1. The bot detects arbitrage opportunities using real market data
2. Instead of executing real trades, it simulates the trades against virtual balances
3. All trade results and balance updates are tracked just like real trades
4. You can review the performance without risking actual funds

### Initial Balances

By default, paper trading starts with:
- 10,000 USDT/USD/EUR for each exchange
- 1 BTC/ETH/XRP for each exchange

### Using Paper Trading Service

The `IPaperTradingService` interface provides methods to interact with paper trading:

```csharp
// Get the paper trading service from dependency injection
var paperTradingService = serviceProvider.GetRequiredService<IPaperTradingService>();

// Check if paper trading is enabled
bool isEnabled = paperTradingService.IsPaperTradingEnabled;

// Initialize with custom balances
var initialBalances = new Dictionary<string, Dictionary<string, decimal>>
{
    ["binance"] = new Dictionary<string, decimal>
    {
        ["BTC"] = 2.0m,
        ["USDT"] = 50000.0m
    }
};
await paperTradingService.InitializeAsync(initialBalances);

// Simulate market buy order
var buyResult = await paperTradingService.SimulateMarketBuyOrderAsync(
    "binance", 
    new TradingPair("BTC", "USDT"), 
    0.1m);

// Simulate market sell order
var sellResult = await paperTradingService.SimulateMarketSellOrderAsync(
    "binance", 
    new TradingPair("BTC", "USDT"), 
    0.1m);

// Get balances
var allBalances = await paperTradingService.GetAllBalancesAsync();
var btcBalance = await paperTradingService.GetBalanceAsync("binance", "BTC");

// Get trade history
var tradeHistory = await paperTradingService.GetTradeHistoryAsync();

// Reset paper trading data
await paperTradingService.ResetAsync();
```

Paper trading is ideal for:
- Testing new strategies
- Learning how the system works
- Validating configuration before using real funds
- Backtesting against historical market conditions

## Architecture

The application follows the principles of Clean Architecture:

- **Domain Layer**: Contains business entities and logic (TradingPair, Order, etc.)
- **Application Layer**: Contains application services and interfaces (ArbitrageService, MarketDataService, etc.)
- **Infrastructure Layer**: Contains implementations of interfaces (ExchangeClients, Repositories, etc.)
- **Worker Layer**: Contains the background service that runs the arbitrage bot

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

Trading cryptocurrencies involves significant risk. This software is for educational purposes only. Use at your own risk. 