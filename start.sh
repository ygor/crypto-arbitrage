#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}Crypto Arbitrage Application Starter${NC}"
echo "=============================================="
echo ""
echo -e "Choose a startup method:"
echo "1) Start with Docker Compose (includes Blazor)"
echo "2) Start Blazor Frontend with .NET (RPC Mode)"
echo "3) Exit"

read -p "Enter your choice (1-3): " choice

case $choice in
  1)
    echo -e "${GREEN}Starting with Docker Compose...${NC}"
    docker-compose up --build
    if [ $? -eq 0 ]; then
      echo -e "${GREEN}Services started successfully.${NC}"
      echo -e "Access the Blazor application at: http://localhost:7001"
      echo -e "API available at: http://localhost:5001/api"
      echo -e "Swagger UI: http://localhost:5001/swagger"
    else
      echo -e "${RED}Failed to start services with Docker Compose.${NC}"
      exit 1
    fi
    ;;
  2)
    echo -e "${GREEN}Starting Blazor Frontend with RPC...${NC}"
    
    # Check if .NET is installed
    if ! command -v dotnet &> /dev/null; then
      echo -e "${RED}Error: .NET SDK is not installed. Please install .NET 9 SDK.${NC}"
      exit 1
    fi

    # Check .NET version
    DOTNET_VERSION=$(dotnet --version)
    if [[ ! $DOTNET_VERSION == 9.* ]]; then
      echo -e "${RED}Warning: This project requires .NET 9. Current version: $DOTNET_VERSION${NC}"
      echo -e "${YELLOW}Please use .NET 9 SDK from https://dotnet.microsoft.com/download/dotnet/9.0${NC}"
      exit 1
    fi

    # Kill any existing processes using ports
    echo -e "${YELLOW}Stopping any existing processes on ports 7001...${NC}"
    lsof -i :7001 | awk 'NR!=1 {print $2}' | xargs kill -9 2>/dev/null || true

    # Create logs directory if it doesn't exist
    mkdir -p logs

    # Start the Blazor service (includes direct RPC access to services)
    echo -e "${GREEN}Starting Blazor Frontend with direct RPC access...${NC}"
    cd backend/src/CryptoArbitrage.Blazor
    dotnet build
    dotnet run --no-build > ../../../logs/blazor.log 2>&1 &
    BLAZOR_PID=$!

    # Wait a moment for Blazor to start
    echo -e "${YELLOW}Waiting for Blazor to start...${NC}"
    sleep 5

    # Start the Worker service
    echo -e "${GREEN}Starting Worker service...${NC}"
    cd ../CryptoArbitrage.Worker
    dotnet build
    dotnet run --no-build > ../../../logs/worker.log 2>&1 &
    WORKER_PID=$!
    cd ../../..

    echo ""
    echo -e "${GREEN}🚀 Blazor application started successfully!${NC}"
    echo "=============================================="
    echo -e "🌐 Blazor Frontend: ${BLUE}http://localhost:7001${NC}"
    echo -e "🔧 Direct RPC: Services injected directly (no HTTP API calls)"
    echo -e "🛡️ Compile-time Safety: Full type checking between frontend and backend"
    echo -e "📋 Logs: Check ./logs/ directory"
    echo ""
    echo -e "${YELLOW}Press Ctrl+C to stop all services...${NC}"

    # Wait for Ctrl+C
    trap "echo -e '${YELLOW}Stopping all services...${NC}'; kill $BLAZOR_PID $WORKER_PID 2>/dev/null || true; echo -e '${GREEN}All services stopped.${NC}'; exit 0" INT
    wait
    ;;
  3)
    echo "Exiting..."
    exit 0
    ;;
  *)
    echo -e "${RED}Invalid choice. Exiting.${NC}"
    exit 1
    ;;
esac 