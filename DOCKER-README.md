# Docker Setup for Crypto Arbitrage Backend

This document provides instructions for running the Crypto Arbitrage backend services using Docker Compose.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)

## Services Included

1. **API Service**: The REST API that provides endpoints for managing the arbitrage system.
2. **Worker Service**: The background worker that monitors exchanges and executes arbitrage opportunities.
3. **MongoDB**: Database for storing arbitrage opportunities, trade results, and configurations.
4. **Redis**: Used for caching and real-time messaging between services.

## Configuration

The `appsettings.json` file contains the configuration for all services. You can modify this file to change settings such as:

- Trading pairs
- Risk profile parameters
- Exchange API keys (for live trading)
- Notification settings

**Note**: For security reasons, never commit your actual API keys to version control. Use environment variables or secrets management for production environments.

## Running the Services

We've provided a convenient shell script to manage the services. Make it executable first:

```bash
chmod +x run-backend.sh
```

### Basic Commands

Start all services:
```bash
./run-backend.sh start
```

Stop all services:
```bash
./run-backend.sh stop
```

View logs from all services:
```bash
./run-backend.sh logs
```

View logs from specific services:
```bash
./run-backend.sh api-logs    # API logs only
./run-backend.sh worker-logs # Worker logs only
```

Check service status:
```bash
./run-backend.sh status
```

Rebuild the services:
```bash
./run-backend.sh build
```

Get help:
```bash
./run-backend.sh help
```

## Manual Docker Compose Commands

If you prefer to use Docker Compose directly:

```bash
# Start in the background
docker-compose up -d

# Start in the foreground with logs
docker-compose up

# Stop services
docker-compose down

# Rebuild services
docker-compose build

# View logs
docker-compose logs -f
```

## Accessing the Services

- **API**: http://localhost:5001 (HTTP) or https://localhost:5002 (HTTPS)
- **MongoDB**: mongodb://localhost:27017 (username: admin, password: password)
- **Redis**: localhost:6379

## Paper Trading Mode

By default, the system is configured to run in paper trading mode, which means no real trades will be executed. This is ideal for testing and development.

To enable live trading (use with caution):

1. Edit the `appsettings.json` file:
   ```json
   "CryptoArbitrage": {
     "PaperTradingEnabled": false,
     ...
   }
   ```

2. Add your exchange API keys:
   ```json
   "Exchanges": {
     "binance": {
       "ApiKey": "YOUR_API_KEY",
       "ApiSecret": "YOUR_API_SECRET",
       ...
     }
   }
   ```

## Troubleshooting

- **Services fail to start**: Ensure ports 5001, 5002, 27017, and 6379 are not already in use on your system.
- **Connection issues between services**: Make sure all services are running (`./run-backend.sh status`).
- **API unreachable**: Check if the service is running and logs for any errors (`./run-backend.sh api-logs`).
- **No arbitrage opportunities detected**: Verify that the worker service is running (`./run-backend.sh worker-logs`) and that the configuration is correct. 