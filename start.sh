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
echo "1) Start with Docker Compose"
echo "2) Start with .NET directly"
echo "3) Exit"

read -p "Enter your choice (1-3): " choice

case $choice in
  1)
    echo -e "${GREEN}Starting with Docker Compose...${NC}"
    docker-compose up --build
    if [ $? -eq 0 ]; then
      echo -e "${GREEN}Services started successfully.${NC}"
      echo -e "Access the application at: http://localhost:3000"
      echo -e "API available at: http://localhost:5001/api"
      echo -e "Swagger UI: http://localhost:5001/swagger"
    else
      echo -e "${RED}Failed to start services with Docker Compose.${NC}"
      exit 1
    fi
    ;;
  2)
    echo -e "${GREEN}Starting with .NET...${NC}"
    
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

    # Kill any existing processes using port 5001
    echo -e "${YELLOW}Stopping any existing processes on port 5001...${NC}"
    lsof -i :5001 | awk 'NR!=1 {print $2}' | xargs kill -9 2>/dev/null || true

    # Create logs directory if it doesn't exist
    mkdir -p logs

    # Start the API service
    echo -e "${GREEN}Starting API service...${NC}"
    cd backend/src/CryptoArbitrage.Api
    dotnet build
    dotnet run --no-build &
    API_PID=$!

    # Wait a moment for the API to start
    echo -e "${YELLOW}Waiting for API to start...${NC}"
    sleep 5

    # Start the Worker service
    echo -e "${GREEN}Starting Worker service...${NC}"
    cd ../CryptoArbitrage.Worker
    dotnet build
    dotnet run --no-build &
    WORKER_PID=$!

    echo ""
    echo -e "${GREEN}Services started:${NC}"
    echo -e "- API: Running on http://localhost:5001"
    echo -e "- Worker: Running in background"
    echo ""
    echo -e "${YELLOW}Press Ctrl+C to stop all services...${NC}"

    # Wait for Ctrl+C
    trap "kill $API_PID $WORKER_PID 2>/dev/null || true; echo -e '${GREEN}Services stopped.${NC}'; exit 0" INT
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