#!/bin/bash

# Script to run integration tests with real exchange APIs
# These tests connect to actual exchange endpoints to validate our schemas

set -e

echo "🔗 Running Integration Tests with Real Exchange APIs"
echo "=================================================="

# Check if we're in CI environment
if [ "$CI" = "true" ]; then
    echo "ℹ️  Running in CI environment - external tests will be skipped"
fi

# Navigate to backend directory
cd "$(dirname "$0")"

echo ""
echo "📦 Restoring packages..."
dotnet restore

echo ""
echo "🏗️  Building integration test project..."
dotnet build tests/CryptoArbitrage.IntegrationTests --no-restore

echo ""
echo "🧪 Running integration tests..."
echo ""

# Run integration tests with verbose output
dotnet test tests/CryptoArbitrage.IntegrationTests \
    --no-build \
    --logger "console;verbosity=detailed" \
    --logger "trx;LogFileName=integration-test-results.trx" \
    --results-directory ./TestResults \
    --collect:"XPlat Code Coverage"

echo ""
echo "✅ Integration tests completed!"

if [ "$CI" != "true" ]; then
    echo ""
    echo "📊 Integration Test Summary:"
    echo "   - Tests validate outgoing WebSocket messages against schemas"
    echo "   - Tests connect to real Coinbase and Kraken public endpoints" 
    echo "   - Tests verify incoming messages match our schemas"
    echo ""
    echo "🔍 To run only schema validation tests (no network):"
    echo "   dotnet test tests/CryptoArbitrage.Tests --filter Category=Contract"
    echo ""
    echo "🌐 To run only network tests:"
    echo "   dotnet test tests/CryptoArbitrage.IntegrationTests --filter Category=Integration"
fi 