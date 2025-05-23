#!/bin/bash

# WebSocket Health Monitor
# Continuously monitors WebSocket connections and alerts on issues

MONITOR_INTERVAL=${1:-30}  # Default 30 seconds
MAX_ITERATIONS=${2:-0}    # Default infinite (0)
LOG_FILE="websocket-health-$(date +%Y%m%d-%H%M%S).log"

echo "🔍 Starting WebSocket Health Monitor..."
echo "   Monitor interval: ${MONITOR_INTERVAL} seconds"
echo "   Log file: ${LOG_FILE}"
if [ "$MAX_ITERATIONS" -gt 0 ]; then
    echo "   Max iterations: ${MAX_ITERATIONS}"
else
    echo "   Running continuously (Ctrl+C to stop)"
fi
echo ""

# Function to log with timestamp
log_with_timestamp() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

# Function to check API endpoint health
check_endpoint() {
    local endpoint=$1
    local timeout=${2:-5}
    
    local status_code=$(curl -s -m "$timeout" -o /dev/null -w "%{http_code}" "$endpoint" 2>/dev/null)
    echo "$status_code"
}

# Function to check opportunity count
get_opportunity_count() {
    local count=$(curl -s -m 5 "http://localhost:5001/api/arbitrage/opportunities?limit=1" 2>/dev/null | jq length 2>/dev/null)
    if [ "$count" = "null" ] || [ -z "$count" ]; then
        echo "0"
    else
        echo "$count"
    fi
}

# Function to analyze logs for WebSocket issues
analyze_recent_logs() {
    local logs=$(curl -s -m 10 "http://localhost:5001/api/settings/bot/activity-logs" 2>/dev/null)
    
    if [ $? -ne 0 ] || [ -z "$logs" ]; then
        echo "ERROR: Could not fetch logs"
        return 1
    fi
    
    # Count different types of errors
    local websocket_errors=$(echo "$logs" | jq -r '.[] | select(.message | contains("WebSocket") and (contains("error") or contains("Error") or contains("failed") or contains("Failed"))) | .message' 2>/dev/null | wc -l)
    local auth_errors=$(echo "$logs" | jq -r '.[] | select(.message | contains("authentication") or contains("signature") or contains("unauthorized")) | .message' 2>/dev/null | wc -l)
    local sub_errors=$(echo "$logs" | jq -r '.[] | select(.message | contains("Failed to subscribe") or contains("subscription")) | .message' 2>/dev/null | wc -l)
    local conn_errors=$(echo "$logs" | jq -r '.[] | select(.message | contains("closed") or contains("disconnected") or contains("reconnect")) | .message' 2>/dev/null | wc -l)
    
    echo "ws_errors:$websocket_errors auth_errors:$auth_errors sub_errors:$sub_errors conn_errors:$conn_errors"
}

# Function to get exchange status
get_exchange_status() {
    local status=$(curl -s -m 5 "http://localhost:5001/api/settings/bot/exchange-status" 2>/dev/null)
    if [ $? -ne 0 ] || [ -z "$status" ]; then
        echo "ERROR"
    else
        echo "$status"
    fi
}

# Initialize tracking variables
iteration=0
last_opportunity_count=0
consecutive_zero_opportunities=0
last_health_status="unknown"
alert_threshold=3  # Alert after 3 consecutive issues

log_with_timestamp "WebSocket Health Monitor started"

# Main monitoring loop
while true; do
    iteration=$((iteration + 1))
    
    # Check if we should stop
    if [ "$MAX_ITERATIONS" -gt 0 ] && [ "$iteration" -gt "$MAX_ITERATIONS" ]; then
        log_with_timestamp "Reached maximum iterations ($MAX_ITERATIONS), stopping monitor"
        break
    fi
    
    echo ""
    echo "🔍 Health Check #$iteration - $(date '+%Y-%m-%d %H:%M:%S')"
    
    # Check basic API health
    api_health=$(check_endpoint "http://localhost:5001/health")
    if [ "$api_health" = "200" ]; then
        echo "   ✅ API Health: OK"
    else
        echo "   ❌ API Health: Failed (HTTP $api_health)"
        log_with_timestamp "ALERT: API health check failed (HTTP $api_health)"
    fi
    
    # Check bot status
    bot_status=$(check_endpoint "http://localhost:5001/api/settings/bot/status")
    if [ "$bot_status" = "200" ]; then
        echo "   ✅ Bot Status: OK"
    else
        echo "   ❌ Bot Status: Failed (HTTP $bot_status)"
        log_with_timestamp "ALERT: Bot status check failed (HTTP $bot_status)"
    fi
    
    # Check opportunity count
    current_opportunities=$(get_opportunity_count)
    echo "   📊 Current opportunities: $current_opportunities"
    
    if [ "$current_opportunities" -eq 0 ]; then
        consecutive_zero_opportunities=$((consecutive_zero_opportunities + 1))
        if [ "$consecutive_zero_opportunities" -ge "$alert_threshold" ]; then
            echo "   ⚠️  No opportunities for $consecutive_zero_opportunities consecutive checks"
            log_with_timestamp "ALERT: No arbitrage opportunities found for $consecutive_zero_opportunities checks"
        fi
    else
        consecutive_zero_opportunities=0
        echo "   ✅ Opportunities detected"
    fi
    
    # Analyze logs for errors
    echo "   🔍 Analyzing recent logs..."
    log_analysis=$(analyze_recent_logs)
    
    if [[ "$log_analysis" == "ERROR:"* ]]; then
        echo "   ❌ Log analysis failed: $log_analysis"
        log_with_timestamp "ALERT: Could not analyze logs - $log_analysis"
    else
        # Parse error counts
        ws_errors=$(echo "$log_analysis" | grep -o 'ws_errors:[0-9]*' | cut -d: -f2)
        auth_errors=$(echo "$log_analysis" | grep -o 'auth_errors:[0-9]*' | cut -d: -f2)
        sub_errors=$(echo "$log_analysis" | grep -o 'sub_errors:[0-9]*' | cut -d: -f2)
        conn_errors=$(echo "$log_analysis" | grep -o 'conn_errors:[0-9]*' | cut -d: -f2)
        
        total_errors=$((ws_errors + auth_errors + sub_errors + conn_errors))
        
        if [ "$total_errors" -gt 0 ]; then
            echo "   ⚠️  Recent errors: WebSocket($ws_errors) Auth($auth_errors) Subscription($sub_errors) Connection($conn_errors)"
            log_with_timestamp "WARNING: Found $total_errors recent errors in logs"
        else
            echo "   ✅ No recent errors in logs"
        fi
    fi
    
    # Check exchange status
    exchange_status=$(get_exchange_status)
    if [[ "$exchange_status" == "ERROR" ]]; then
        echo "   ❌ Exchange status: Failed to fetch"
        log_with_timestamp "ALERT: Could not fetch exchange status"
    else
        echo "   ✅ Exchange status: Retrieved"
    fi
    
    # Overall health assessment
    current_health="healthy"
    
    if [ "$api_health" != "200" ] || [ "$bot_status" != "200" ]; then
        current_health="critical"
    elif [ "$consecutive_zero_opportunities" -ge "$alert_threshold" ] || [ "$total_errors" -gt 2 ]; then
        current_health="degraded"
    fi
    
    # Log health status changes
    if [ "$current_health" != "$last_health_status" ]; then
        log_with_timestamp "Health status changed: $last_health_status -> $current_health"
        
        case "$current_health" in
            "critical")
                log_with_timestamp "CRITICAL: System is experiencing critical issues"
                ;;
            "degraded")
                log_with_timestamp "WARNING: System performance is degraded"
                ;;
            "healthy")
                log_with_timestamp "INFO: System has recovered to healthy state"
                ;;
        esac
    fi
    
    last_health_status="$current_health"
    last_opportunity_count="$current_opportunities"
    
    # Display overall status
    case "$current_health" in
        "healthy")
            echo "   🟢 Overall Health: HEALTHY"
            ;;
        "degraded")
            echo "   🟡 Overall Health: DEGRADED"
            ;;
        "critical")
            echo "   🔴 Overall Health: CRITICAL"
            ;;
    esac
    
    # Sleep before next iteration
    if [ "$MAX_ITERATIONS" -eq 0 ] || [ "$iteration" -lt "$MAX_ITERATIONS" ]; then
        echo "   💤 Sleeping for $MONITOR_INTERVAL seconds..."
        sleep "$MONITOR_INTERVAL"
    fi
done

log_with_timestamp "WebSocket Health Monitor stopped after $iteration iterations"
echo ""
echo "📋 Health monitoring completed. Check log file: $LOG_FILE"
echo ""
echo "💡 To view the log in real-time in another terminal:"
echo "   tail -f $LOG_FILE" 