#!/bin/bash

# WebSocket Connection Diagnostic Tool
# Tests direct WebSocket connections to Coinbase and Kraken

echo "🔍 Testing WebSocket connections to crypto exchanges..."

# Test Coinbase WebSocket connection
echo ""
echo "📡 Testing Coinbase WebSocket connection..."
echo "Connecting to: wss://ws-feed.exchange.coinbase.com"

# Create a temporary Node.js script to test WebSocket connections
cat > /tmp/test_coinbase_ws.js << 'EOF'
const WebSocket = require('ws');

const ws = new WebSocket('wss://ws-feed.exchange.coinbase.com');

ws.on('open', function open() {
  console.log('✅ Connected to Coinbase WebSocket');
  
  // Subscribe to BTC-USDT level2 data
  const subscription = {
    type: 'subscribe',
    product_ids: ['BTC-USDT'],
    channels: ['level2']
  };
  
  ws.send(JSON.stringify(subscription));
  console.log('📤 Sent subscription message for BTC-USDT level2 data');
});

ws.on('message', function message(data) {
  const msg = JSON.parse(data.toString());
  console.log(`📥 Received message type: ${msg.type}`);
  
  if (msg.type === 'subscriptions') {
    console.log('✅ Subscription confirmed:', JSON.stringify(msg, null, 2));
  } else if (msg.type === 'snapshot') {
    console.log('✅ Received order book snapshot with', msg.bids?.length || 0, 'bids and', msg.asks?.length || 0, 'asks');
  } else if (msg.type === 'l2update') {
    console.log('✅ Received order book update with', msg.changes?.length || 0, 'changes');
  } else if (msg.type === 'error') {
    console.log('❌ Received error:', msg.message);
  }
});

ws.on('error', function error(err) {
  console.log('❌ WebSocket error:', err.message);
});

ws.on('close', function close() {
  console.log('🔌 WebSocket connection closed');
});

// Close after 10 seconds
setTimeout(() => {
  ws.close();
  process.exit(0);
}, 10000);
EOF

# Test if Node.js is available
if command -v node >/dev/null 2>&1; then
    echo "Running Coinbase WebSocket test..."
    timeout 15s node /tmp/test_coinbase_ws.js 2>/dev/null || echo "⚠️  Node.js test timed out or failed"
else
    echo "⚠️  Node.js not available for WebSocket testing"
fi

# Test Kraken WebSocket connection
echo ""
echo "📡 Testing Kraken WebSocket connection..."
echo "Connecting to: wss://ws.kraken.com"

cat > /tmp/test_kraken_ws.js << 'EOF'
const WebSocket = require('ws');

const ws = new WebSocket('wss://ws.kraken.com');

ws.on('open', function open() {
  console.log('✅ Connected to Kraken WebSocket');
  
  // Subscribe to BTC/USDT order book data
  const subscription = {
    event: 'subscribe',
    pair: ['XBT/USDT'],
    subscription: {
      name: 'book',
      depth: 25
    }
  };
  
  ws.send(JSON.stringify(subscription));
  console.log('📤 Sent subscription message for XBT/USDT book data');
});

ws.on('message', function message(data) {
  const msg = data.toString();
  
  try {
    const parsed = JSON.parse(msg);
    
    if (parsed.event === 'subscriptionStatus') {
      console.log('✅ Subscription status:', parsed.status, 'for', parsed.channelName);
    } else if (Array.isArray(parsed) && parsed.length >= 2) {
      const channelData = parsed[1];
      if (channelData && (channelData.as || channelData.bs)) {
        console.log('✅ Received order book snapshot with', 
          channelData.as?.length || 0, 'asks and', 
          channelData.bs?.length || 0, 'bids');
      } else if (channelData && (channelData.a || channelData.b)) {
        console.log('✅ Received order book update');
      }
    }
  } catch (e) {
    console.log('📥 Received message:', msg.substring(0, 100) + '...');
  }
});

ws.on('error', function error(err) {
  console.log('❌ WebSocket error:', err.message);
});

ws.on('close', function close() {
  console.log('🔌 WebSocket connection closed');
});

// Close after 10 seconds
setTimeout(() => {
  ws.close();
  process.exit(0);
}, 10000);
EOF

if command -v node >/dev/null 2>&1; then
    echo "Running Kraken WebSocket test..."
    timeout 15s node /tmp/test_kraken_ws.js 2>/dev/null || echo "⚠️  Node.js test timed out or failed"
else
    echo "⚠️  Node.js not available for WebSocket testing"
fi

# Clean up temporary files
rm -f /tmp/test_coinbase_ws.js /tmp/test_kraken_ws.js

echo ""
echo "🏁 WebSocket connection test completed"
echo ""
echo "💡 If connections failed, check:"
echo "   - Internet connectivity"
echo "   - Firewall settings" 
echo "   - Exchange API status"
echo "   - Trading pair symbols (BTC-USDT for Coinbase, XBT/USDT for Kraken)" 