#!/bin/bash

echo "🔍 Debugging WebSocket connections in running application..."

# Function to test API endpoints
test_endpoint() {
    local endpoint=$1
    local description=$2
    
    echo "📡 Testing: $description"
    echo "   Endpoint: $endpoint"
    
    local response=$(curl -s "$endpoint")
    local status_code=$(curl -s -o /dev/null -w "%{http_code}" "$endpoint")
    
    if [ "$status_code" = "200" ]; then
        echo "   ✅ Response: $response"
    else
        echo "   ❌ HTTP $status_code: $response"
    fi
    echo ""
}

# Test basic connectivity
test_endpoint "http://localhost:5001/health" "Health Check"

# Test bot status
test_endpoint "http://localhost:5001/api/settings/bot/status" "Bot Status"

# Test arbitrage configuration
echo "📡 Testing: Current Arbitrage Configuration"
echo "   Endpoint: http://localhost:5001/api/settings/arbitrage"
arbitrage_config=$(curl -s "http://localhost:5001/api/settings/arbitrage")
echo "   ✅ Trading Pairs:"
echo "$arbitrage_config" | jq -r '.tradingPairs[] | "      - \(.baseCurrency)/\(.quoteCurrency)"'
echo "   ✅ Enabled Exchanges:"
echo "$arbitrage_config" | jq -r '.enabledExchanges[] | "      - \(.)"'
echo ""

# Test exchange status
test_endpoint "http://localhost:5001/api/settings/bot/exchange-status" "Exchange Status"

# Test current opportunities (should be empty if WebSocket not working)
echo "📡 Testing: Current Opportunities"
echo "   Endpoint: http://localhost:5001/api/arbitrage/opportunities?limit=5"
opportunities=$(curl -s "http://localhost:5001/api/arbitrage/opportunities?limit=5")
opportunity_count=$(echo "$opportunities" | jq length)
echo "   📊 Found $opportunity_count opportunities"
if [ "$opportunity_count" -gt 0 ]; then
    echo "   ✅ Opportunities found (WebSocket likely working)"
    echo "$opportunities" | jq -r '.[] | "      - \(.tradingPair.baseCurrency)/\(.tradingPair.quoteCurrency): \(.buyExchangeId) → \(.sellExchangeId), Spread: \(.spreadPercentage)%"'
else
    echo "   ⚠️  No opportunities found (WebSocket may not be working)"
fi
echo ""

# Test activity logs for connection diagnostics  
echo "📡 Testing: Activity Logs (last 10)"
echo "   Endpoint: http://localhost:5001/api/settings/bot/activity-logs"
activity_logs=$(curl -s "http://localhost:5001/api/settings/bot/activity-logs")
echo "   📊 Recent activity:"
echo "$activity_logs" | jq -r '.[0:10][] | "      [\(.timestamp)] \(.level): \(.message)"'

# Look for specific WebSocket errors in logs
echo ""
echo "🔍 Analyzing logs for WebSocket connection issues..."
websocket_errors=$(echo "$activity_logs" | jq -r '.[] | select(.message | contains("WebSocket") or contains("Failed to subscribe") or contains("authentication") or contains("signature")) | "[\(.timestamp)] \(.level): \(.message)"')
if [ -n "$websocket_errors" ]; then
    echo "   ⚠️  WebSocket-related errors found:"
    echo "$websocket_errors" | head -5
else
    echo "   ✅ No recent WebSocket errors found in logs"
fi
echo ""

# Check if we can connect to exchange WebSockets from our network
echo "🌐 Testing external WebSocket connectivity..."

# Test if we can reach Coinbase WebSocket
echo "📡 Testing Coinbase WebSocket reachability..."
timeout 5 bash -c "</dev/tcp/ws-feed.exchange.coinbase.com/443" && echo "   ✅ Coinbase WebSocket port reachable" || echo "   ❌ Cannot reach Coinbase WebSocket"

# Test if we can reach Kraken WebSocket  
echo "📡 Testing Kraken WebSocket reachability..."
timeout 5 bash -c "</dev/tcp/ws.kraken.com/443" && echo "   ✅ Kraken WebSocket port reachable" || echo "   ❌ Cannot reach Kraken WebSocket"

# Test if we can reach Binance WebSocket
echo "📡 Testing Binance WebSocket reachability..."
timeout 5 bash -c "</dev/tcp/stream.binance.com/9443" && echo "   ✅ Binance WebSocket port reachable" || echo "   ❌ Cannot reach Binance WebSocket"

echo ""
echo "🔧 Advanced Diagnostics..."

# Check for authentication configuration
echo "📡 Testing: Exchange Configurations"
echo "   Endpoint: http://localhost:5001/api/settings/exchanges"
exchange_configs=$(curl -s "http://localhost:5001/api/settings/exchanges")
echo "$exchange_configs" | jq -r '.[] | "   - \(.exchangeId): API Key configured: \(if .apiKey then "✅ Yes" else "❌ No" end), Enabled: \(if .isEnabled then "✅ Yes" else "❌ No" end)"'
echo ""

# Test WebSocket connection stability by monitoring for a short period
echo "🔄 Testing WebSocket Connection Stability (30 seconds)..."
echo "   Monitoring opportunity count changes..."

initial_count=$(curl -s "http://localhost:5001/api/arbitrage/opportunities?limit=1" | jq length)
echo "   Initial opportunity count: $initial_count"

sleep 15
mid_count=$(curl -s "http://localhost:5001/api/arbitrage/opportunities?limit=1" | jq length)
echo "   Mid-check opportunity count: $mid_count"

sleep 15
final_count=$(curl -s "http://localhost:5001/api/arbitrage/opportunities?limit=1" | jq length)
echo "   Final opportunity count: $final_count"

if [ "$initial_count" -eq "$mid_count" ] && [ "$mid_count" -eq "$final_count" ] && [ "$final_count" -eq 0 ]; then
    echo "   ⚠️  No opportunities detected over 30 seconds - WebSocket may be disconnected"
elif [ "$initial_count" -ne "$final_count" ] || [ "$final_count" -gt 0 ]; then
    echo "   ✅ Opportunity count changed or opportunities found - WebSocket likely working"
else
    echo "   ⚠️  Opportunity count stable but zero - check trading pair symbols and market activity"
fi
echo ""

# Check for specific error patterns in recent logs
echo "🔍 Checking for specific connection issues..."
echo "   Endpoint: http://localhost:5001/api/settings/bot/activity-logs"
recent_logs=$(curl -s "http://localhost:5001/api/settings/bot/activity-logs")

# Check for authentication errors
auth_errors=$(echo "$recent_logs" | jq -r '.[] | select(.message | contains("authentication") or contains("signature") or contains("unauthorized")) | .message')
if [ -n "$auth_errors" ]; then
    echo "   🔐 Authentication Issues Found:"
    echo "$auth_errors" | head -3 | sed 's/^/      /'
fi

# Check for subscription errors
sub_errors=$(echo "$recent_logs" | jq -r '.[] | select(.message | contains("Failed to subscribe") or contains("subscription") or contains("symbol")) | .message')
if [ -n "$sub_errors" ]; then
    echo "   📡 Subscription Issues Found:"
    echo "$sub_errors" | head -3 | sed 's/^/      /'
fi

# Check for connection drops
conn_errors=$(echo "$recent_logs" | jq -r '.[] | select(.message | contains("closed") or contains("disconnected") or contains("reconnect")) | .message')
if [ -n "$conn_errors" ]; then
    echo "   🔌 Connection Issues Found:"
    echo "$conn_errors" | head -3 | sed 's/^/      /'
fi

echo ""
echo "🔧 Troubleshooting suggestions:"
echo "   1. If no opportunities found and WebSocket ports are reachable:"
echo "      - Check application logs for WebSocket connection errors"
echo "      - Verify trading pair symbols match exchange APIs"
echo "      - Ensure arbitrage detection service is running"
echo "      - Check if authentication credentials are properly configured"
echo ""
echo "   2. If WebSocket ports are not reachable:"
echo "      - Check internet connectivity" 
echo "      - Check firewall settings"
echo "      - Try connecting from a different network"
echo ""
echo "   3. If authentication errors are found:"
echo "      - Verify API keys are correct and not expired"
echo "      - Check API key permissions (need read access to market data)"
echo "      - Ensure passphrase is correct (for Coinbase)"
echo ""
echo "   4. If subscription errors are found:"
echo "      - Verify trading pair symbols (BTC-USDT vs BTC/USDT vs XBT/USDT)"
echo "      - Check if trading pairs are supported by the exchange"
echo "      - Verify exchange API documentation for correct symbols"
echo ""
echo "   5. To restart WebSocket connections:"
echo "      curl -X POST http://localhost:5001/api/settings/bot/stop"
echo "      sleep 2"
echo "      curl -X POST http://localhost:5001/api/settings/bot/start"
echo ""
echo "   6. To test individual exchange connections:"
echo "      ./test-websocket-connections.sh"
echo ""
echo "   7. To monitor live WebSocket data:"
echo "      tail -f logs/cryptoarbitrage-worker-$(date +%Y%m%d).txt | grep -i websocket" 