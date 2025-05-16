#!/bin/bash

# Crypto Arbitrage Backend Runner
# This script simplifies running the crypto arbitrage backend services

# Set the color variables
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_help() {
    echo -e "${BLUE}Crypto Arbitrage Backend Runner${NC}"
    echo "-----------------------------"
    echo "Usage: ./run-backend.sh [command]"
    echo ""
    echo "Commands:"
    echo "  start       - Start all services"
    echo "  stop        - Stop all services"
    echo "  restart     - Restart all services"
    echo "  logs        - View logs from all services"
    echo "  status      - Check the status of all services"
    echo "  api-logs    - View logs from the API service only"
    echo "  worker-logs - View logs from the Worker service only"
    echo "  build       - Rebuild the services"
    echo "  help        - Show this help message"
    echo ""
}

check_docker() {
    if ! command -v docker &> /dev/null; then
        echo -e "${YELLOW}Docker not found. Please install Docker to continue.${NC}"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        echo -e "${YELLOW}Docker Compose not found. Please install Docker Compose to continue.${NC}"
        exit 1
    fi
}

start_services() {
    echo -e "${GREEN}Starting Crypto Arbitrage backend services...${NC}"
    docker-compose up -d
    
    echo -e "${GREEN}Services started!${NC}"
    echo -e "${BLUE}API:${NC} http://localhost:5001"
    echo -e "${BLUE}MongoDB:${NC} mongodb://localhost:27017"
    echo -e "${BLUE}Redis:${NC} localhost:6379"
}

stop_services() {
    echo -e "${YELLOW}Stopping Crypto Arbitrage backend services...${NC}"
    docker-compose down
    echo -e "${GREEN}Services stopped.${NC}"
}

restart_services() {
    echo -e "${YELLOW}Restarting Crypto Arbitrage backend services...${NC}"
    docker-compose restart
    echo -e "${GREEN}Services restarted.${NC}"
}

view_logs() {
    echo -e "${BLUE}Viewing logs from all services (press Ctrl+C to exit)...${NC}"
    docker-compose logs -f
}

view_api_logs() {
    echo -e "${BLUE}Viewing logs from the API service (press Ctrl+C to exit)...${NC}"
    docker-compose logs -f api
}

view_worker_logs() {
    echo -e "${BLUE}Viewing logs from the Worker service (press Ctrl+C to exit)...${NC}"
    docker-compose logs -f worker
}

check_status() {
    echo -e "${BLUE}Current status of all services:${NC}"
    docker-compose ps
}

build_services() {
    echo -e "${GREEN}Building Crypto Arbitrage backend services...${NC}"
    docker-compose build
    echo -e "${GREEN}Build complete.${NC}"
}

# Check if Docker is installed
check_docker

# Process command line arguments
case "$1" in
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        restart_services
        ;;
    logs)
        view_logs
        ;;
    api-logs)
        view_api_logs
        ;;
    worker-logs)
        view_worker_logs
        ;;
    status)
        check_status
        ;;
    build)
        build_services
        ;;
    help|--help|-h)
        print_help
        ;;
    *)
        print_help
        exit 1
        ;;
esac

exit 0 