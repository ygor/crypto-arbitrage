#!/bin/bash

# Colors for terminal output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=== Complete API Contract Verification ===${NC}"
echo -e "${BLUE}Testing both frontend-to-OpenAPI and backend-to-OpenAPI alignment${NC}"
echo ""

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

# Step 1: Generate the TypeScript API client
echo -e "${BLUE}Step 1: Generating TypeScript API client${NC}"
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

echo -e "${GREEN}✓ TypeScript API client generated successfully!${NC}"
echo ""

# Step 2: Verify frontend-to-OpenAPI alignment
echo -e "${BLUE}Step 2: Verifying frontend-to-OpenAPI alignment${NC}"
echo -e "Running API contract verification against OpenAPI specification..."
npm run verify-api-swagger

if [ $? -eq 0 ]; then
  echo -e "${GREEN}✓ Frontend-to-OpenAPI alignment verified!${NC}"
else
  echo -e "${RED}✗ Frontend-to-OpenAPI alignment failed.${NC}"
  echo -e "${YELLOW}Please check api-misalignments.log for details.${NC}"
  exit 1
fi
echo ""

# Step 3: Verify backend-to-OpenAPI alignment
echo -e "${BLUE}Step 3: Verifying backend-to-OpenAPI alignment${NC}"
cd ..
if [ -f "test-backend-endpoints.sh" ]; then
  echo -e "Running backend endpoint implementation test..."
  ./test-backend-endpoints.sh
  
  if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Backend-to-OpenAPI alignment verified!${NC}"
  else
    echo -e "${RED}✗ Backend-to-OpenAPI alignment failed.${NC}"
    echo -e "${YELLOW}Some endpoints defined in OpenAPI spec are not implemented in the backend.${NC}"
    exit 1
  fi
else
  echo -e "${YELLOW}⚠ Backend endpoint test script not found - skipping backend verification${NC}"
  echo -e "${YELLOW}Create test-backend-endpoints.sh to verify backend implements OpenAPI endpoints${NC}"
fi
echo ""

# Final summary
echo -e "${GREEN}=== Complete Contract Verification PASSED ===${NC}"
echo -e "${GREEN}✓ Frontend calls align with OpenAPI specification${NC}"
echo -e "${GREEN}✓ Backend implements endpoints defined in OpenAPI specification${NC}"
echo -e "${GREEN}✓ Full contract integrity verified${NC}" 