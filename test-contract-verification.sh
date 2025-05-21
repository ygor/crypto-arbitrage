#!/bin/bash

# Colors for terminal output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Navigate to frontend directory
cd frontend

# Install glob dependency if needed
if ! grep -q '"glob"' package.json; then
  echo -e "${YELLOW}Installing required glob dependency...${NC}"
  npm install --save-dev glob
fi

# Check if the OpenAPI specification exists
OPENAPI_SPEC="../api-specs/crypto-arbitrage-api.json"
if [ ! -f "$OPENAPI_SPEC" ]; then
  echo -e "${RED}Error: OpenAPI specification not found at $OPENAPI_SPEC${NC}"
  echo -e "${YELLOW}Please create the OpenAPI specification first.${NC}"
  exit 1
fi

# Generate the TypeScript API client
echo -e "Generating API client from OpenAPI specification..."
echo -e "Generating TypeScript API client from OpenAPI specification..."

# Use .NET 9.0 compatible NSwag tool with full path
NSWAG_TOOL=~/.dotnet/tools/.store/nswag.consolecore/14.0.3/nswag.consolecore/14.0.3/tools/net9.0/any/dotnet-nswag.dll
if [ -f "$NSWAG_TOOL" ]; then
  dotnet "$NSWAG_TOOL" run nswag.json
  if [ $? -ne 0 ]; then
    echo -e "${RED}Error: Failed to generate TypeScript API client${NC}"
    exit 1
  fi
else
  echo -e "${YELLOW}Warning: Using global nswag command which may require .NET 9.0${NC}"
  nswag run nswag.json
  if [ $? -ne 0 ]; then
    echo -e "${YELLOW}Falling back to the generateClient.sh script${NC}"
    chmod +x generateClient.sh && ./generateClient.sh
  fi
fi

echo -e "TypeScript API client generated successfully!"

# Run the verification script
echo -e "Running API contract verification against OpenAPI specification..."
npm run verify-api-swagger

# Check if verification passed
if [ $? -eq 0 ]; then
  echo -e "${GREEN}✓ Contract verification passed!${NC}"
  echo -e "${GREEN}✓ Frontend API calls align with the OpenAPI specification.${NC}"
  exit 0
else
  echo -e "${RED}✗ Contract verification failed.${NC}"
  echo -e "${YELLOW}Please check api-misalignments.log for details.${NC}"
  exit 1
fi 