#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Default settings
RUN_TESTS=true
BUILD_AND_START=true
DEV_MODE=true  # Set to true for development, false for production/CI

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --no-tests)
      RUN_TESTS=false
      shift
      ;;
    --tests-only)
      BUILD_AND_START=false
      shift
      ;;
    --strict)
      DEV_MODE=false  # Strict mode for CI/production, fails on any misalignment
      shift
      ;;
    --help)
      echo "Usage: ./start.sh [OPTIONS]"
      echo ""
      echo "Options:"
      echo "  --no-tests      Skip API contract tests"
      echo "  --tests-only    Only run tests without starting containers"
      echo "  --strict        Strict mode: fail on any API misalignment (default: false in dev mode)"
      echo "  --help          Display this help message"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      echo "Use --help to see available options"
      exit 1
      ;;
  esac
done

echo -e "${BLUE}Crypto Arbitrage Application Starter${NC}"
echo "=============================================="

# Run API contract tests if enabled
if [ "$RUN_TESTS" = true ]; then
  echo -e "\n${YELLOW}Running API Contract Tests...${NC}"
  
  # Check if frontend directory exists and contains package.json
  if [ ! -d "frontend" ] || [ ! -f "frontend/package.json" ]; then
    echo -e "${RED}Error: frontend directory or package.json not found${NC}"
    echo "Tests cannot be run. Continuing with container startup..."
  else
    # Run in a subshell to avoid changing the current directory
    (
      cd frontend
      
      # Check for simple test file
      if [ ! -f "src/services/simple-api-test.js" ]; then
        echo "Simple API test file not found. Skipping tests."
      else
        # Run the simple API endpoint test
        echo "Running API endpoint tests..."
        if node src/services/simple-api-test.js; then
          echo -e "${GREEN}✓ API endpoint tests passed${NC}"
        else
          echo -e "${RED}✗ API endpoint tests failed${NC}"
          if [ "$BUILD_AND_START" = true ]; then
            echo -e "${YELLOW}Continuing with container startup despite test failures...${NC}"
          else
            exit 1
          fi
        fi
      fi
      
      # Check for verification script
      if [ ! -f "scripts/verify-api-contracts.js" ]; then
        echo "API verification script not found. Skipping verification."
      else
        echo "Verifying API contract alignment..."
        if [ -f "package.json" ] && grep -q "verify-api" package.json; then
          npm run verify-api
          VERIFY_EXIT_CODE=$?
          
          # In development mode, we only show warnings for misalignments
          if [ $VERIFY_EXIT_CODE -eq 0 ]; then
            echo -e "${GREEN}✓ API contract verification passed${NC}"
          elif [ "$DEV_MODE" = true ]; then
            echo -e "${YELLOW}⚠️ API contract verification found misalignments (development mode - continuing)${NC}"
          else
            echo -e "${RED}✗ API contract verification failed${NC}"
            if [ "$BUILD_AND_START" = true ]; then
              echo -e "${YELLOW}Continuing with container startup despite verification failures...${NC}"
            else
              exit 1
            fi
          fi
        else
          echo "verify-api script not found in package.json. Skipping verification."
        fi
      fi
    )
  fi
fi

# Build and start containers if enabled
if [ "$BUILD_AND_START" = true ]; then
  echo -e "\n${YELLOW}Building and starting containers...${NC}"
  
  # Build and start the containers in detached mode
  docker-compose up -d --build
  
  # Check if containers started successfully
  if [ $? -eq 0 ]; then
    echo -e "${GREEN}Containers started successfully!${NC}"
    echo -e "\nServices will be available at:"
    echo -e "${BLUE}- Frontend:${NC} http://localhost:3000"
    echo -e "${BLUE}- API:${NC} http://localhost:5001/api"
    echo -e "${BLUE}- MongoDB:${NC} localhost:27017"
    echo -e "${BLUE}- Redis:${NC} localhost:6379"
    
    echo -e "\nUseful commands:"
    echo -e "${BLUE}- View logs:${NC} docker-compose logs -f"
    echo -e "${BLUE}- Stop all services:${NC} docker-compose down"
  else
    echo -e "${RED}Failed to start containers${NC}"
    exit 1
  fi
fi 