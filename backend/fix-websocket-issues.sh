#!/bin/bash

# WebSocket Issue Auto-Fix Script
# Automatically diagnoses and attempts to fix common WebSocket connection issues

echo "🔧 WebSocket Issue Auto-Fix Tool"
echo "=================================="
echo ""

# Function to make API calls with error handling
api_call() {
    local method=$1
    local endpoint=$2
    local description=$3
    
    echo "🔄 $description..."
    local response=$(curl -s -X "$method" "http://localhost:5001$endpoint" 2>/dev/null)
    local status_code=$(curl -s -X "$method" -o /dev/null -w "%{http_code}" "http://localhost:5001$endpoint" 2>/dev/null)
    
    if [ "$status_code" = "200" ]; then
        echo "   ✅ Success: $description"
        return 0
    else
        echo "   ❌ Failed: $description (HTTP $status_code)"
        return 1
    fi
}

# Function to check if the API is responsive
check_api_health() {
    echo "🏥 Checking API Health..."
    local health_status=$(curl -s -m 5 "http://localhost:5001/health" 2>/dev/null)
    local status_code=$(curl -s -m 5 -o /dev/null -w "%{http_code}" "http://localhost:5001/health" 2>/dev/null)
    
    if [ "$status_code" = "200" ]; then
        echo "   ✅ API is responsive"
        return 0
    else
        echo "   ❌ API is not responsive (HTTP $status_code)"
        echo "   💡 Please ensure the application is running on port 5001"
        return 1
    fi
}

# Function to get current logs and analyze issues
analyze_current_issues() {
    echo "🔍 Analyzing Current Issues..."
    
    local logs=$(curl -s -m 10 "http://localhost:5001/api/settings/bot/activity-logs" 2>/dev/null)
    if [ $? -ne 0 ] || [ -z "$logs" ]; then
        echo "   ❌ Could not fetch application logs"
        return 1
    fi
    
    # Check for specific issues
    local auth_issues=$(echo "$logs" | jq -r '.[] | select(.message | contains("authentication") or contains("signature") or contains("unauthorized")) | .message' 2>/dev/null | wc -l)
    local sub_issues=$(echo "$logs" | jq -r '.[] | select(.message | contains("Failed to subscribe") or contains("subscription")) | .message' 2>/dev/null | wc -l)
    local conn_issues=$(echo "$logs" | jq -r '.[] | select(.message | contains("closed") or contains("disconnected") or contains("reconnect")) | .message' 2>/dev/null | wc -l)
    local symbol_issues=$(echo "$logs" | jq -r '.[] | select(.message | contains("400") and (contains("BTC-USDT") or contains("ETH-USDT"))) | .message' 2>/dev/null | wc -l)
    
    echo "   📊 Issue Summary:"
    echo "      - Authentication issues: $auth_issues"
    echo "      - Subscription issues: $sub_issues"
    echo "      - Connection issues: $conn_issues"
    echo "      - Symbol/Trading pair issues: $symbol_issues"
    
    # Store issues for later use
    export AUTH_ISSUES=$auth_issues
    export SUB_ISSUES=$sub_issues
    export CONN_ISSUES=$conn_issues
    export SYMBOL_ISSUES=$symbol_issues
    
    return 0
}

# Function to restart WebSocket connections
restart_websocket_connections() {
    echo "🔄 Restarting WebSocket Connections..."
    
    # Stop the bot
    if api_call "POST" "/api/settings/bot/stop" "Stopping bot"; then
        echo "   ⏳ Waiting 3 seconds for connections to close..."
        sleep 3
        
        # Start the bot
        if api_call "POST" "/api/settings/bot/start" "Starting bot"; then
            echo "   ⏳ Waiting 5 seconds for connections to establish..."
            sleep 5
            return 0
        else
            echo "   ❌ Failed to start bot"
            return 1
        fi
    else
        echo "   ❌ Failed to stop bot"
        return 1
    fi
}

# Function to fix trading pair symbol issues
fix_symbol_issues() {
    echo "🔧 Attempting to Fix Trading Pair Symbol Issues..."
    
    # Get current arbitrage configuration
    local config=$(curl -s "http://localhost:5001/api/settings/arbitrage" 2>/dev/null)
    if [ $? -ne 0 ] || [ -z "$config" ]; then
        echo "   ❌ Could not fetch arbitrage configuration"
        return 1
    fi
    
    echo "   📋 Current trading pairs:"
    echo "$config" | jq -r '.tradingPairs[] | "      - \(.baseCurrency)/\(.quoteCurrency)"'
    
    # Check if we have problematic trading pairs for specific exchanges
    local has_btc_usdt=$(echo "$config" | jq -r '.tradingPairs[] | select(.baseCurrency == "BTC" and .quoteCurrency == "USDT") | length' 2>/dev/null)
    local has_eth_usdt=$(echo "$config" | jq -r '.tradingPairs[] | select(.baseCurrency == "ETH" and .quoteCurrency == "USDT") | length' 2>/dev/null)
    
    if [ "$has_btc_usdt" ] || [ "$has_eth_usdt" ]; then
        echo "   💡 Detected USDT pairs - these may have different symbols on different exchanges:"
        echo "      - Coinbase: BTC-USDT, ETH-USDT"
        echo "      - Kraken: XBT/USDT, ETH/USDT"  
        echo "      - Binance: BTCUSDT, ETHUSDT"
        echo "   ⚠️  Manual symbol verification may be needed"
    fi
    
    return 0
}

# Function to test exchange connectivity
test_exchange_connectivity() {
    echo "🌐 Testing Exchange Connectivity..."
    
    # Test Coinbase
    echo "   📡 Testing Coinbase WebSocket..."
    timeout 3 bash -c "</dev/tcp/ws-feed.exchange.coinbase.com/443" && echo "      ✅ Coinbase reachable" || echo "      ❌ Coinbase unreachable"
    
    # Test Kraken
    echo "   📡 Testing Kraken WebSocket..."
    timeout 3 bash -c "</dev/tcp/ws.kraken.com/443" && echo "      ✅ Kraken reachable" || echo "      ❌ Kraken unreachable"
    
    # Test Binance
    echo "   📡 Testing Binance WebSocket..."
    timeout 3 bash -c "</dev/tcp/stream.binance.com/9443" && echo "      ✅ Binance reachable" || echo "      ❌ Binance unreachable"
}

# Function to check and suggest authentication fixes
check_authentication() {
    echo "🔐 Checking Authentication Configuration..."
    
    local exchanges=$(curl -s "http://localhost:5001/api/settings/exchanges" 2>/dev/null)
    if [ $? -ne 0 ] || [ -z "$exchanges" ]; then
        echo "   ❌ Could not fetch exchange configurations"
        return 1
    fi
    
    echo "   📋 Exchange configurations:"
    echo "$exchanges" | jq -r '.[] | "      - \(.exchangeId): Enabled=\(.isEnabled), Has API Key=\(if .apiKey and .apiKey != "" then "Yes" else "No" end)"'
    
    # Check for exchanges with incomplete authentication
    local incomplete_auth=$(echo "$exchanges" | jq -r '.[] | select(.isEnabled == true and (.apiKey == null or .apiKey == "")) | .exchangeId')
    
    if [ -n "$incomplete_auth" ]; then
        echo "   ⚠️  Exchanges with incomplete authentication:"
        echo "$incomplete_auth" | sed 's/^/      - /'
        echo "   💡 For public data feeds, authentication is not required"
        echo "   💡 For trading, you'll need to configure API keys"
    fi
    
    return 0
}

# Function to run quick diagnostic
run_quick_diagnostic() {
    echo "⚡ Running Quick Diagnostic..."
    
    # Check current opportunities
    local opp_count=$(curl -s "http://localhost:5001/api/arbitrage/opportunities?limit=1" 2>/dev/null | jq length 2>/dev/null)
    echo "   📊 Current arbitrage opportunities: ${opp_count:-0}"
    
    # Check bot status
    local bot_status=$(curl -s "http://localhost:5001/api/settings/bot/status" 2>/dev/null)
    if [ $? -eq 0 ] && [ -n "$bot_status" ]; then
        echo "   🤖 Bot status: Available"
    else
        echo "   🤖 Bot status: Unavailable"
    fi
    
    # Check exchange status
    local exchange_status=$(curl -s "http://localhost:5001/api/settings/bot/exchange-status" 2>/dev/null)
    if [ $? -eq 0 ] && [ -n "$exchange_status" ]; then
        echo "   🏪 Exchange status: Available"
    else
        echo "   🏪 Exchange status: Unavailable"
    fi
}

# Main execution
main() {
    # Check if API is responsive
    if ! check_api_health; then
        echo "❌ Cannot proceed - API is not responsive"
        exit 1
    fi
    
    echo ""
    
    # Run initial diagnostic
    run_quick_diagnostic
    echo ""
    
    # Analyze current issues
    if ! analyze_current_issues; then
        echo "❌ Cannot analyze issues - skipping automated fixes"
        exit 1
    fi
    
    echo ""
    
    # Test external connectivity
    test_exchange_connectivity
    echo ""
    
    # Check authentication
    check_authentication
    echo ""
    
    # Determine what fixes to apply
    echo "🔧 Determining Required Fixes..."
    
    fixes_needed=0
    
    if [ "$CONN_ISSUES" -gt 0 ] || [ "$SUB_ISSUES" -gt 0 ]; then
        echo "   🔄 WebSocket restart needed (connection/subscription issues detected)"
        fixes_needed=1
    fi
    
    if [ "$SYMBOL_ISSUES" -gt 0 ]; then
        echo "   🏷️  Trading pair symbol review needed"
        fix_symbol_issues
    fi
    
    if [ "$fixes_needed" -eq 1 ]; then
        echo ""
        echo "🚀 Applying Automatic Fixes..."
        
        if restart_websocket_connections; then
            echo "   ✅ WebSocket connections restarted successfully"
            
            # Wait a moment and run post-fix diagnostic
            echo ""
            echo "🔍 Post-Fix Diagnostic..."
            sleep 3
            run_quick_diagnostic
            
        else
            echo "   ❌ Failed to restart WebSocket connections"
            echo "   💡 Manual intervention may be required"
        fi
    else
        echo "   ✅ No automatic fixes needed"
    fi
    
    echo ""
    echo "📋 Summary & Recommendations:"
    echo "=============================="
    
    if [ "$AUTH_ISSUES" -gt 0 ]; then
        echo "🔐 Authentication Issues Detected:"
        echo "   - Review API key configuration"
        echo "   - Ensure API keys have correct permissions"
        echo "   - For Coinbase, verify passphrase is correct"
    fi
    
    if [ "$SYMBOL_ISSUES" -gt 0 ]; then
        echo "🏷️  Trading Pair Symbol Issues:"
        echo "   - Verify trading pair symbols match exchange APIs"
        echo "   - Check if trading pairs are supported on each exchange"
        echo "   - Review symbol format differences between exchanges"
    fi
    
    echo ""
    echo "🛠️  Additional Tools Available:"
    echo "   - Full diagnostic: ./debug-websocket-connections.sh"
    echo "   - Direct WebSocket test: ./test-websocket-connections.sh"
    echo "   - Continuous monitoring: ./monitor-websocket-health.sh"
    echo ""
    echo "✅ Auto-fix completed!"
}

# Run main function
main "$@" 