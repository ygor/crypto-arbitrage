# WebSocket Connection Debugging Guide

This guide covers the WebSocket debugging tools available for the crypto arbitrage application and how to troubleshoot common connection issues.

## Available Tools

### 1. `debug-websocket-connections.sh` - Comprehensive Diagnostic Tool

**Purpose**: Complete diagnostic of WebSocket connections in a running application.

**Usage**:
```bash
./debug-websocket-connections.sh
```

**What it does**:
- Tests API health and bot status
- Checks current arbitrage configuration
- Analyzes recent activity logs for WebSocket errors
- Tests external connectivity to exchange WebSocket endpoints
- Provides detailed troubleshooting suggestions
- Monitors connection stability over 30 seconds
- Checks for specific error patterns (authentication, subscription, connection issues)

**Example Output**:
```
🔍 Debugging WebSocket connections in running application...
📡 Testing: Health Check
   ✅ Response: {"status":"healthy"}
📊 Found 5 opportunities
🔍 Analyzing logs for WebSocket connection issues...
   ✅ No recent WebSocket errors found in logs
```

### 2. `test-websocket-connections.sh` - Direct WebSocket Testing

**Purpose**: Tests direct WebSocket connections to crypto exchanges (independent of the application).

**Usage**:
```bash
./test-websocket-connections.sh
```

**What it does**:
- Creates temporary Node.js scripts to test WebSocket connections
- Tests Coinbase and Kraken WebSocket endpoints directly
- Subscribes to order book data and monitors responses
- Validates WebSocket connectivity independent of the application

**Requirements**: Node.js must be installed on the system.

### 3. `fix-websocket-issues.sh` - Automated Issue Resolution

**Purpose**: Automatically diagnoses and attempts to fix common WebSocket connection issues.

**Usage**:
```bash
./fix-websocket-issues.sh
```

**What it does**:
- Checks API health and responsiveness
- Analyzes logs for specific error patterns
- Tests external exchange connectivity
- Validates authentication configuration
- Automatically restarts WebSocket connections if issues are detected
- Provides targeted recommendations based on found issues

**Example Output**:
```
🔧 WebSocket Issue Auto-Fix Tool
==================================
🏥 Checking API Health...
   ✅ API is responsive
🔍 Analyzing Current Issues...
   📊 Issue Summary:
      - Authentication issues: 0
      - Subscription issues: 2
      - Connection issues: 1
🚀 Applying Automatic Fixes...
   ✅ WebSocket connections restarted successfully
```

### 4. `monitor-websocket-health.sh` - Continuous Health Monitoring

**Purpose**: Continuously monitors WebSocket connection health and alerts on issues.

**Usage**:
```bash
# Monitor every 30 seconds indefinitely
./monitor-websocket-health.sh

# Monitor every 60 seconds for 10 iterations
./monitor-websocket-health.sh 60 10
```

**Parameters**:
- `MONITOR_INTERVAL`: Seconds between checks (default: 30)
- `MAX_ITERATIONS`: Number of checks to perform (default: infinite)

**Features**:
- Continuous health monitoring with timestamped logs
- Tracks opportunity count changes over time
- Counts different types of errors in logs
- Provides health status assessment (Healthy/Degraded/Critical)
- Creates log files for historical analysis
- Alerts on consecutive issues

## Common Issues and Solutions

### 1. Authentication Errors

**Symptoms**:
- "Failed to subscribe" errors in logs
- "authentication failed" or "signature" errors
- WebSocket connections drop frequently

**Solutions**:
1. Verify API keys are correct and not expired
2. Check API key permissions (need read access to market data)
3. For Coinbase, ensure passphrase is correct
4. Run: `./fix-websocket-issues.sh` to check authentication config

### 2. Trading Pair Symbol Errors

**Symptoms**:
- HTTP 400 errors with trading pair symbols
- "product not found" errors
- Subscription failures for specific pairs

**Solutions**:
1. Verify trading pair symbols match exchange APIs:
   - Coinbase: `BTC-USDT`, `ETH-USDT`
   - Kraken: `XBT/USDT`, `ETH/USDT`
   - Binance: `BTCUSDT`, `ETHUSDT`
2. Check if trading pairs are supported on each exchange
3. Review the arbitrage configuration endpoint

### 3. Connection Drops

**Symptoms**:
- "WebSocket closed" errors
- "reconnecting" messages in logs
- Zero arbitrage opportunities detected

**Solutions**:
1. Check internet connectivity and firewall settings
2. Test external WebSocket reachability: `./test-websocket-connections.sh`
3. Restart connections: `./fix-websocket-issues.sh`
4. Monitor connection stability: `./monitor-websocket-health.sh`

### 4. No Arbitrage Opportunities

**Symptoms**:
- Zero opportunities consistently returned
- WebSocket appears connected but no data flow

**Solutions**:
1. Check if WebSocket data is actually flowing (monitor logs)
2. Verify trading pair configurations
3. Ensure arbitrage detection service is running
4. Check market activity (low volatility = fewer opportunities)

## Application Endpoints for Debugging

### Health and Status
- `GET /health` - Application health check
- `GET /api/settings/bot/status` - Bot operational status
- `GET /api/settings/bot/exchange-status` - Exchange connection status

### Configuration
- `GET /api/settings/arbitrage` - Current arbitrage configuration
- `GET /api/settings/exchanges` - Exchange configurations

### Data and Logs
- `GET /api/arbitrage/opportunities?limit=5` - Current arbitrage opportunities
- `GET /api/settings/bot/activity-logs` - Recent application logs

### Control
- `POST /api/settings/bot/start` - Start the bot and WebSocket connections
- `POST /api/settings/bot/stop` - Stop the bot and close WebSocket connections

## Log Analysis Commands

### View Live WebSocket Activity
```bash
tail -f logs/cryptoarbitrage-worker-$(date +%Y%m%d).txt | grep -i websocket
```

### Search for Specific Errors
```bash
grep -i "failed to subscribe" logs/cryptoarbitrage-worker-*.txt
grep -i "authentication" logs/cryptoarbitrage-worker-*.txt
grep -i "400" logs/cryptoarbitrage-worker-*.txt
```

### Monitor Opportunity Detection
```bash
watch -n 5 'curl -s "http://localhost:5001/api/arbitrage/opportunities?limit=3" | jq'
```

## Best Practices

### Before Deploying
1. Run full diagnostic: `./debug-websocket-connections.sh`
2. Test direct connections: `./test-websocket-connections.sh`
3. Verify all trading pairs are supported on target exchanges

### During Operation
1. Monitor health continuously: `./monitor-websocket-health.sh`
2. Check logs regularly for new error patterns
3. Restart connections if issues persist: `./fix-websocket-issues.sh`

### When Issues Occur
1. Run auto-fix first: `./fix-websocket-issues.sh`
2. If issues persist, run full diagnostic: `./debug-websocket-connections.sh`
3. Test individual exchange connections: `./test-websocket-connections.sh`
4. Check application logs for detailed error messages

## Technical Improvements Made

### Enhanced Error Handling
- Added symbol format validation for Coinbase (requires BASE-QUOTE format)
- Implemented trading pair validation before subscription
- Better error categorization and logging
- Automatic cleanup on subscription failures

### Improved Diagnostics
- Comprehensive logging of subscription attempts and failures
- External connectivity testing for all major exchanges
- Health status assessment with multiple criteria
- Historical error tracking and analysis

### Automated Recovery
- Automatic WebSocket connection restart on detected issues
- Smart retry mechanisms with exponential backoff
- Graceful handling of connection drops and reconnections
- Configuration validation before attempting connections

## File Structure

```
backend/
├── debug-websocket-connections.sh      # Main diagnostic tool
├── test-websocket-connections.sh       # Direct WebSocket testing
├── fix-websocket-issues.sh            # Automated issue resolution
├── monitor-websocket-health.sh        # Continuous health monitoring
└── WEBSOCKET_DEBUGGING.md             # This documentation
```

All scripts are executable and can be run from the `backend/` directory. They require the application to be running on port 5001 (except for `test-websocket-connections.sh` which tests independently). 