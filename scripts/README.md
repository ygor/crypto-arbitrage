# Crypto Arbitrage Scripts

This directory contains utility scripts for the Crypto Arbitrage application.

## Exchange Configuration Scripts

### update-coinbase-config.sh

This script helps you create or update the Coinbase exchange configuration with the required credentials, including the passphrase that is required for Coinbase API authentication.

#### Usage:

```bash
./scripts/update-coinbase-config.sh
```

The script will:
1. Allow you to use an existing configuration file or create a new one
2. Prompt you for API Key, Secret, and Passphrase if they're not already set
3. Save the configuration to a file named `coinbase-config.json`
4. Provide instructions on how to apply the configuration to your application

#### When are API credentials needed?

**Public Market Data**: API credentials are **optional** for accessing public market data like order books and ticker prices. The application can connect to public WebSocket feeds without authentication.

**Trading Operations**: API credentials are **required** for any trading operations or accessing private data like account balances.

If you only need to access public market data and do not need to perform trades, you can:
- Leave the API credentials empty in your configuration
- Set "PaperTradingMode" to true to use simulated trading

#### Why is the passphrase needed?

For operations that do require authentication, Coinbase API requires three credentials:
- API Key
- API Secret
- Passphrase

The passphrase must be provided in the `AdditionalAuthParams` section of the exchange configuration. Without it, you'll see an error like:

```
Error connecting to coinbase
System.InvalidOperationException: Coinbase requires a passphrase in addition to API key and secret. Please add 'passphrase' to AdditionalAuthParams in the exchange configuration.
```

#### Alternative Solution

If you prefer not to use this script, you can directly edit your appsettings.json or appsettings.Development.json files:

```json
{
  "CryptoArbitrage": {
    "Exchanges": [
      {
        "ExchangeId": "coinbase",
        "IsEnabled": true,
        "ApiKey": "",
        "ApiSecret": "",
        "AdditionalAuthParams": {
          "passphrase": ""
        },
        "ApiTimeoutMs": 5000,
        "WebSocketReconnectIntervalMs": 1000
      }
    ]
  }
}
```

You can leave the credentials empty for public data access, or fill them in if you need authenticated operations. 