# Crypto Arbitrage

A real-time cryptocurrency arbitrage detection and trading system that monitors multiple exchanges for price differences and executes trades to profit from these differences.

## Project Overview

This project consists of two main components:

1. **Backend (C# / .NET 9)**: A high-performance arbitrage detection and trading engine with a REST API and real-time SignalR hubs.
2. **Frontend (React/TypeScript)**: A web dashboard for monitoring arbitrage opportunities, trade results, and controlling the bot.

## Features

- Real-time arbitrage opportunity detection across multiple cryptocurrency exchanges
- Configurable risk management and trade parameters
- Paper trading mode for testing without real funds
- Dashboard for visualization of opportunities, trades, and statistics
- SignalR-based real-time updates
- Detailed statistics and performance metrics
- Extensible architecture for adding new exchanges

## Project Structure

- `src/CryptoArbitrage.Domain` - Domain models and entities
- `src/CryptoArbitrage.Application` - Application logic, interfaces, and services
- `src/CryptoArbitrage.Infrastructure` - Implementation of interfaces, repositories, exchange clients
- `src/CryptoArbitrage.Api` - REST API and SignalR hubs for frontend communication
- `src/CryptoArbitrage.Worker` - Background worker for running the bot
- `tests/CryptoArbitrage.Tests` - Unit and integration tests
- `frontend/arbitrage-dashboard` - React web dashboard

## Getting Started

### Prerequisites

- .NET 9 SDK
- Node.js (v16+) and npm (for frontend development)
- Docker and Docker Compose (for containerized setup)
- Git

### Option 1: Running with .NET

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/crypto-arbitrage.git
   cd crypto-arbitrage
   ```

2. Make the run script executable:
   ```bash
   chmod +x run.sh
   ```

3. Run the application:
   ```bash
   ./run.sh
   ```

This will:
- Build and start the API project
- Start the Worker service
- Configure required services

### Option 2: Local Development Setup

#### Backend Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/crypto-arbitrage.git
   cd crypto-arbitrage
   ```

2. Build the solution:
   ```bash
   dotnet build
   ```

3. Run the API:
   ```bash
   cd src/CryptoArbitrage.Api
   dotnet run
   ```

The API will be available at `https://localhost:7000` with Swagger documentation at `https://localhost:7000/swagger`.

#### Frontend Setup

1. Navigate to the frontend directory:
   ```bash
   cd frontend/arbitrage-dashboard
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm start
   ```

The dashboard will be available at `http://localhost:3000`.

### Option 3: Docker Containerized Setup

This project includes Docker support for running the entire stack in containers, which is recommended for production or testing environments.

#### Services Included

1. **API Service**: The REST API that provides endpoints for managing the arbitrage system.
2. **Worker Service**: The background worker that monitors exchanges and executes arbitrage opportunities.
3. **MongoDB**: Database for storing arbitrage opportunities, trade results, and configurations.
4. **Redis**: Used for caching and real-time messaging between services.
5. **Frontend**: The React web dashboard.

#### Running with Docker Compose

We've provided a convenient shell script to manage the services. Make it executable first:

```bash
chmod +x start.sh
```

Start all services:
```bash
./start.sh
```

This will build and start all services in the background. You can also use direct Docker Compose commands:

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

#### Accessing the Containerized Services

- **Frontend**: http://localhost:3000
- **API**: http://localhost:5001/api
- **MongoDB**: mongodb://localhost:27017 (username: admin, password: password)
- **Redis**: localhost:6379

## Configuration

The system configuration is managed through `appsettings.json` files:

- A root-level `appsettings.json` contains global configuration shared between services
- Service-specific configurations exist in their respective project directories

### Key Configuration Options

- Trading pairs to monitor
- Risk profile parameters (profit thresholds, position sizes, etc.)
- Exchange API credentials
- Paper trading settings
- Notification preferences

To use the bot with real exchanges, you need to configure API keys for each exchange in the settings section of the dashboard or in the `appsettings.json` file.

### Paper Trading Mode

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

**Note**: For security reasons, never commit your actual API keys to version control. Use environment variables or secrets management for production environments.

## Architecture

The project follows a clean architecture approach with:

- Domain-driven design principles
- CQRS pattern for separating reads and writes
- Repository pattern for data access
- Dependency injection for loose coupling
- SignalR for real-time communication
- React with Material-UI for the frontend

## Troubleshooting

### Common Issues

- **Services fail to start**: Ensure ports 3000, 5001, 27017, and 6379 are not already in use on your system.
- **Connection issues between services**: Make sure all services are running with `docker-compose ps`.
- **API unreachable**: Check if the service is running and view logs with `docker-compose logs api`.
- **No arbitrage opportunities detected**: Verify that the worker service is running and that the configuration is correct.
- **Frontend connectivity issues**: Ensure the API service is running and check browser console for CORS or other connection errors.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

Trading cryptocurrencies involves significant risk. This software is for educational purposes only. Use at your own risk. 