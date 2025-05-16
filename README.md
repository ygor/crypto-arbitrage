# Crypto Arbitrage Bot

A real-time cryptocurrency arbitrage detection and trading system that monitors multiple exchanges for price differences and executes trades to profit from these differences.

## Project Overview

This project consists of two main components:

1. **Backend (C# / .NET 7)**: A high-performance arbitrage detection and trading engine with a REST API and real-time SignalR hubs.
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

- `src/ArbitrageBot.Domain` - Domain models and entities
- `src/ArbitrageBot.Application` - Application logic, interfaces, and services
- `src/ArbitrageBot.Infrastructure` - Implementation of interfaces, repositories, exchange clients
- `src/ArbitrageBot.Api` - REST API and SignalR hubs for frontend communication
- `src/ArbitrageBot.Worker` - Background worker for running the bot
- `src/ArbitrageBot.Tests` - Unit and integration tests
- `frontend/arbitrage-dashboard` - React web dashboard

## Getting Started

### Prerequisites

- .NET 7 SDK
- Node.js (v16+) and npm
- Git

### Backend Setup

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
   cd src/ArbitrageBot.Api
   dotnet run
   ```

The API will be available at `https://localhost:7000` with Swagger documentation at `https://localhost:7000/swagger`.

### Frontend Setup

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

## Configuration

To use the bot with real exchanges, you need to configure API keys for each exchange in the settings section of the dashboard or in the `appsettings.json` file.

Default configuration is set to paper trading mode for safety.

## Architecture

The project follows a clean architecture approach with:

- Domain-driven design principles
- CQRS pattern for separating reads and writes
- Repository pattern for data access
- Dependency injection for loose coupling
- SignalR for real-time communication
- React with Material-UI for the frontend

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Disclaimer

Trading cryptocurrencies involves significant risk. This software is for educational purposes only. Use at your own risk. 