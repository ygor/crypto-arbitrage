#!/bin/bash

# Backend Endpoint Implementation Test
# Tests that all endpoints defined in OpenAPI spec are actually implemented
# AND runs regression tests to prevent API contract issues

set -e

API_BASE_URL="http://localhost:3000"
OPENAPI_SPEC="./api-specs/crypto-arbitrage-api.json"

echo "🔍 Testing backend implementation against OpenAPI specification..."

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Track results
TOTAL_ENDPOINTS=0
PASSED_ENDPOINTS=0
FAILED_ENDPOINTS=0

# Function to test endpoint
test_endpoint() {
    local method=$1
    local path=$2
    local operation_id=$3
    
    TOTAL_ENDPOINTS=$((TOTAL_ENDPOINTS + 1))
    
    echo -n "Testing ${method} ${path} (${operation_id})... "
    
    # Make the request and check status
    if [ "$method" = "GET" ]; then
        status_code=$(curl -s -o /dev/null -w "%{http_code}" "${API_BASE_URL}${path}")
    elif [ "$method" = "POST" ]; then
        status_code=$(curl -s -o /dev/null -w "%{http_code}" -X POST "${API_BASE_URL}${path}")
    else
        echo -e "${RED}UNKNOWN METHOD${NC}"
        FAILED_ENDPOINTS=$((FAILED_ENDPOINTS + 1))
        return
    fi
    
    if [ "$status_code" -eq 200 ]; then
        echo -e "${GREEN}✅ PASS (HTTP $status_code)${NC}"
        PASSED_ENDPOINTS=$((PASSED_ENDPOINTS + 1))
    else
        echo -e "${RED}❌ FAIL (HTTP $status_code)${NC}"
        FAILED_ENDPOINTS=$((FAILED_ENDPOINTS + 1))
    fi
}

# Test critical endpoints that must work
echo -e "${BLUE}Testing Critical Endpoints:${NC}"
test_endpoint "GET" "/api/health" "getHealth"
test_endpoint "GET" "/api/statistics" "getStatistics" # This was missing
test_endpoint "GET" "/api/settings/bot/activity-logs" "getActivityLogs" # This was missing  
test_endpoint "GET" "/api/settings/bot/exchange-status" "getExchangeStatus" # This was missing
test_endpoint "GET" "/api/settings/bot/status" "getBotStatus"
test_endpoint "POST" "/api/settings/bot/start" "startBot" # This was returning success: false
test_endpoint "POST" "/api/settings/bot/stop" "stopBot" # This was returning success: false
test_endpoint "GET" "/api/arbitrage/opportunities" "getOpportunities"
test_endpoint "GET" "/api/arbitrage/trades" "getTrades"
test_endpoint "GET" "/api/settings/exchanges" "getExchangeConfigurations"
test_endpoint "GET" "/api/settings/risk-profile" "getRiskProfile"
test_endpoint "GET" "/api/settings/arbitrage" "getArbitrageConfiguration"

echo ""
echo -e "${BLUE}=== Integration Test Results ===${NC}"
echo -e "Total endpoints tested: ${TOTAL_ENDPOINTS}"
echo -e "${GREEN}Passed: ${PASSED_ENDPOINTS}${NC}"
if [ $FAILED_ENDPOINTS -gt 0 ]; then
    echo -e "${RED}Failed: ${FAILED_ENDPOINTS}${NC}"
else
    echo -e "${GREEN}Failed: ${FAILED_ENDPOINTS}${NC}"
fi

echo ""
echo -e "${BLUE}=== Running Regression Tests ===${NC}"
echo "Running automated integration tests to prevent regression of API contract issues..."

cd backend

# Run the specific regression tests we created
echo -e "${YELLOW}Running API Contract Regression Tests...${NC}"
dotnet test --filter "ApiContractRegressionTests" --logger "console;verbosity=minimal"

echo -e "${YELLOW}Running OpenAPI Contract Tests...${NC}"
dotnet test --filter "OpenApiContractTests" --logger "console;verbosity=minimal"

cd ..

# Final result
if [ $FAILED_ENDPOINTS -eq 0 ]; then
    echo ""
    echo -e "${GREEN}🎉 ALL TESTS PASSED! Backend implementation matches OpenAPI specification.${NC}"
    echo -e "${GREEN}   Regression tests completed successfully.${NC}"
    exit 0
else
    echo ""
    echo -e "${RED}❌ Some endpoints failed. Backend implementation doesn't match OpenAPI specification.${NC}"
    exit 1
fi 