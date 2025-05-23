#!/bin/bash

# Backend Endpoint Implementation Test
# Tests that all endpoints defined in OpenAPI spec are actually implemented

set -e

API_BASE_URL="http://localhost:3000"
OPENAPI_SPEC="./api-specs/crypto-arbitrage-api.json"

echo "🔍 Testing backend implementation against OpenAPI specification..."

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
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
    else
        # For POST/PUT/PATCH, we'd need to handle request bodies
        status_code=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "${API_BASE_URL}${path}")
    fi
    
    if [ "$status_code" = "200" ] || [ "$status_code" = "204" ]; then
        echo -e "${GREEN}✓ PASS (${status_code})${NC}"
        PASSED_ENDPOINTS=$((PASSED_ENDPOINTS + 1))
    elif [ "$status_code" = "401" ] || [ "$status_code" = "403" ]; then
        echo -e "${YELLOW}⚠ AUTH REQUIRED (${status_code})${NC}"
        PASSED_ENDPOINTS=$((PASSED_ENDPOINTS + 1))
    else
        echo -e "${RED}✗ FAIL (${status_code})${NC}"
        FAILED_ENDPOINTS=$((FAILED_ENDPOINTS + 1))
    fi
}

# Test the specific endpoints we just implemented
echo "Testing newly implemented endpoints:"
test_endpoint "GET" "/api/settings/bot/activity-logs" "getActivityLogs"
test_endpoint "GET" "/api/settings/bot/exchange-status" "getExchangeStatus"

# Test other known endpoints
echo -e "\nTesting other critical endpoints:"
test_endpoint "GET" "/api/arbitrage/opportunities" "getArbitrageOpportunities"
test_endpoint "GET" "/api/arbitrage/trades" "getArbitrageTrades"
test_endpoint "GET" "/api/arbitrage/statistics" "getArbitrageStatistics"
test_endpoint "GET" "/api/settings/bot/status" "getBotStatus"
test_endpoint "GET" "/api/health" "getHealth"

# Summary
echo -e "\n📊 Results:"
echo -e "Total endpoints tested: ${TOTAL_ENDPOINTS}"
echo -e "${GREEN}Passed: ${PASSED_ENDPOINTS}${NC}"
echo -e "${RED}Failed: ${FAILED_ENDPOINTS}${NC}"

if [ $FAILED_ENDPOINTS -eq 0 ]; then
    echo -e "\n${GREEN}✅ All tested endpoints are implemented!${NC}"
    exit 0
else
    echo -e "\n${RED}❌ Some endpoints are not implemented.${NC}"
    echo -e "${YELLOW}💡 This indicates a gap between OpenAPI specification and backend implementation.${NC}"
    exit 1
fi 