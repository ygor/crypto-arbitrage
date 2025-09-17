# Environment Variables Setup Guide

This guide explains how to configure environment variables for the Crypto Arbitrage System using `.env` files.

## 🔧 **Quick Setup**

### 1. Copy the Example File
```bash
cd backend
cp .env.example .env
```

### 2. Edit Your .env File
Open `backend/.env` and fill in your actual API credentials:

```bash
# Exchange API Credentials
COINBASE_API_KEY=your_actual_coinbase_api_key
COINBASE_API_SECRET=your_actual_coinbase_api_secret  
COINBASE_PASSPHRASE=your_actual_coinbase_passphrase

KRAKEN_API_KEY=your_actual_kraken_api_key
KRAKEN_API_SECRET=your_actual_kraken_api_secret
```

### 3. Run the Application
```bash
# The .env file will be automatically loaded
dotnet run --project src/CryptoArbitrage.Api
dotnet run --project src/CryptoArbitrage.Worker
```

## 🔒 **Security Best Practices**

### ✅ DO:
- Keep your `.env` file in the `backend/` directory
- Never commit `.env` files to version control (already in `.gitignore`)
- Use different `.env` files for different environments
- Store production secrets in secure secret management systems

### ❌ DON'T:
- Share your `.env` file or API credentials
- Commit `.env` files to Git repositories
- Use production credentials in development

## 📋 **Available Environment Variables**

### **Exchange API Credentials**
| Variable | Required | Description |
|----------|----------|-------------|
| `COINBASE_API_KEY` | No* | Your Coinbase Pro/Advanced Trade API key |
| `COINBASE_API_SECRET` | No* | Your Coinbase Pro/Advanced Trade API secret |
| `COINBASE_PASSPHRASE` | No* | Your Coinbase Pro/Advanced Trade passphrase |
| `KRAKEN_API_KEY` | No* | Your Kraken API key |
| `KRAKEN_API_SECRET` | No* | Your Kraken API secret |

*Required only for live trading. Leave empty for simulation mode.

### **Database Configuration**
| Variable | Default | Description |
|----------|---------|-------------|
| `MONGODB_CONNECTION_STRING` | `mongodb://localhost:27017` | MongoDB connection string |
| `MONGODB_DATABASE_NAME` | `CryptoArbitrage_Dev` | Database name |
| `REDIS_CONNECTION_STRING` | `localhost:6379` | Redis connection string |

### **Application Settings**
| Variable | Default | Description |
|----------|---------|-------------|
| `PAPER_TRADING_ENABLED` | `true` | Enable paper trading mode |
| `AUTO_TRADE_ENABLED` | `false` | Enable automatic trade execution |
| `MINIMUM_PROFIT_PERCENTAGE` | `0.1` | Minimum profit threshold |
| `POLLING_INTERVAL_MS` | `2000` | Price polling interval |
| `LOG_LEVEL` | `Debug` | Logging level |

## 🧪 **Development vs Production**

### **Development (.env)**
```bash
# Development - Safe defaults
PAPER_TRADING_ENABLED=true
AUTO_TRADE_ENABLED=false
USE_STUB_EXCHANGES=false
LOG_LEVEL=Debug
DETAILED_ERRORS=true

# Leave API keys empty for simulation
COINBASE_API_KEY=
COINBASE_API_SECRET=
COINBASE_PASSPHRASE=
KRAKEN_API_KEY=
KRAKEN_API_SECRET=
```

### **Production (.env.production)**
```bash
# Production - Real trading
PAPER_TRADING_ENABLED=false
AUTO_TRADE_ENABLED=true
USE_STUB_EXCHANGES=false
LOG_LEVEL=Information
DETAILED_ERRORS=false

# Real API credentials (keep secure!)
COINBASE_API_KEY=your_production_coinbase_key
COINBASE_API_SECRET=your_production_coinbase_secret
COINBASE_PASSPHRASE=your_production_coinbase_passphrase
KRAKEN_API_KEY=your_production_kraken_key
KRAKEN_API_SECRET=your_production_kraken_secret
```

## 🐳 **Docker Environment**

When using Docker, you can pass environment variables:

```bash
# Using docker-compose with .env file
docker-compose --env-file backend/.env up

# Or set environment variables directly
docker run -e COINBASE_API_KEY=your_key crypto-arbitrage-api
```

## 🔍 **Troubleshooting**

### **Common Issues:**

1. **"No .env file found"** - Make sure `.env` is in the `backend/` directory
2. **"Invalid API credentials"** - Check your API key format and permissions
3. **"Environment variable not loaded"** - Ensure the variable name matches exactly

### **Testing Environment Variables:**
```bash
cd backend
dotnet run --project src/CryptoArbitrage.Api

# Check logs for:
# "Loaded environment variables from .env"
# "Using paper trading mode" (if credentials are empty)
# "Connected to [exchange]" (if credentials are valid)
```

## 📚 **How It Works**

1. **DotNetEnv Package**: Automatically loads `.env` files at application startup
2. **Priority Order**: Environment variables override `.env` file values
3. **Configuration Binding**: Values are bound to your configuration classes
4. **Validation**: Invalid credentials are caught early with clear error messages

## 🎯 **Getting API Credentials**

### **Coinbase Pro/Advanced Trade:**
1. Go to [Coinbase Pro API Settings](https://pro.coinbase.com/profile/api)
2. Create a new API key with appropriate permissions
3. Copy the API Key, Secret, and Passphrase

### **Kraken:**
1. Go to [Kraken API Settings](https://www.kraken.com/u/security/api)  
2. Create a new API key with trading permissions
3. Copy the API Key and Private Key (Secret)

---

**🔐 Remember: Never share your API credentials or commit them to version control!**
