#!/bin/bash

# MongoDB Setup Script for Crypto Arbitrage System
# This script initializes MongoDB with proper indexes and test data

echo "🚀 Setting up MongoDB for Crypto Arbitrage System..."

# MongoDB connection details
MONGO_HOST=${MONGO_HOST:-"localhost"}
MONGO_PORT=${MONGO_PORT:-"27017"}
MONGO_USER=${MONGO_USER:-"admin"}
MONGO_PASS=${MONGO_PASS:-"password"}
DATABASE_NAME=${DATABASE_NAME:-"CryptoArbitrage"}

# Connection string
MONGO_URI="mongodb://${MONGO_USER}:${MONGO_PASS}@${MONGO_HOST}:${MONGO_PORT}/${DATABASE_NAME}?authSource=admin"

echo "📡 Connecting to MongoDB at ${MONGO_HOST}:${MONGO_PORT}..."

# Wait for MongoDB to be available
echo "⏳ Waiting for MongoDB to be ready..."
timeout=60
counter=0
until mongosh --host "$MONGO_HOST" --port "$MONGO_PORT" --username "$MONGO_USER" --password "$MONGO_PASS" --authenticationDatabase admin --eval "print('MongoDB is ready')" > /dev/null 2>&1; do
    counter=$((counter + 1))
    if [ $counter -gt $timeout ]; then
        echo "❌ Timeout waiting for MongoDB to be ready"
        exit 1
    fi
    echo "   Still waiting... ($counter/$timeout)"
    sleep 1
done

echo "✅ MongoDB is ready!"

# Create database and collections with indexes
echo "🏗️  Setting up database and indexes..."

mongosh "$MONGO_URI" <<EOF
// Switch to the CryptoArbitrage database
use $DATABASE_NAME;

// Create arbitrageOpportunities collection with optimized indexes
print("Creating arbitrageOpportunities collection...");
db.createCollection("arbitrageOpportunities");

// Create indexes for arbitrageOpportunities
print("Creating indexes for arbitrageOpportunities...");
db.arbitrageOpportunities.createIndex({ "detectedAt": -1, "isExecuted": 1 }, { background: true });
db.arbitrageOpportunities.createIndex({ "opportunityId": 1 }, { unique: true, background: true });
db.arbitrageOpportunities.createIndex({ "tradingPair": 1, "detectedAt": -1 }, { background: true });
db.arbitrageOpportunities.createIndex({ "buyExchangeId": 1, "sellExchangeId": 1, "detectedAt": -1 }, { background: true });
db.arbitrageOpportunities.createIndex({ "profitAmount": -1, "detectedAt": -1 }, { background: true });

// TTL index for automatic cleanup (30 days)
db.arbitrageOpportunities.createIndex({ "detectedAt": 1 }, { expireAfterSeconds: 2592000, background: true });

// Create tradeResults collection with optimized indexes
print("Creating tradeResults collection...");
db.createCollection("tradeResults");

// Create indexes for tradeResults
print("Creating indexes for tradeResults...");
db.tradeResults.createIndex({ "timestamp": -1, "isSuccess": 1 }, { background: true });
db.tradeResults.createIndex({ "tradeId": 1 }, { unique: true, background: true });
db.tradeResults.createIndex({ "opportunityId": 1 }, { background: true });
db.tradeResults.createIndex({ "tradingPair": 1, "buyExchangeId": 1, "sellExchangeId": 1, "timestamp": -1 }, { background: true });
db.tradeResults.createIndex({ "profitAmount": -1, "timestamp": -1 }, { background: true });

// TTL index for automatic cleanup (1 year)
db.tradeResults.createIndex({ "timestamp": 1 }, { expireAfterSeconds: 31536000, background: true });

// Create arbitrageStatistics collection with optimized indexes
print("Creating arbitrageStatistics collection...");
db.createCollection("arbitrageStatistics");

// Create indexes for arbitrageStatistics
print("Creating indexes for arbitrageStatistics...");
db.arbitrageStatistics.createIndex({ "date": -1, "periodType": 1 }, { background: true });
db.arbitrageStatistics.createIndex({ "exchangePair": 1, "date": -1 }, { background: true });
db.arbitrageStatistics.createIndex({ "tradingPair": 1, "date": -1 }, { background: true });

// TTL index for automatic cleanup (2 years)
db.arbitrageStatistics.createIndex({ "date": 1 }, { expireAfterSeconds: 63072000, background: true });

// Create systemConfiguration collection
print("Creating systemConfiguration collection...");
db.createCollection("systemConfiguration");

// Insert sample system configuration
print("Inserting sample configuration...");
db.systemConfiguration.insertOne({
    "_id": "system_config",
    "version": "1.0.0",
    "initialized": new Date(),
    "features": {
        "mongoDbMigration": true,
        "realTimeData": true,
        "arbitrageDetection": true
    }
});

// Show collection statistics
print("📊 Database Statistics:");
print("Collections created: " + db.getCollectionNames().length);
print("Database stats:");
printjson(db.stats());

print("✅ MongoDB setup completed successfully!");
EOF

echo "🎯 Creating sample arbitrage opportunities for testing..."

# Create sample data for testing migration and performance
mongosh "$MONGO_URI" <<EOF
use $DATABASE_NAME;

// Insert sample arbitrage opportunities
print("Inserting sample arbitrage opportunities...");
var opportunities = [];
var baseDate = new Date();

for (var i = 0; i < 100; i++) {
    var detectedAt = new Date(baseDate.getTime() - (i * 60000)); // 1 minute intervals
    opportunities.push({
        opportunityId: "test_opp_" + i.toString().padStart(3, '0'),
        tradingPair: ["BTC/USDT", "ETH/USDT", "ETH/BTC"][i % 3],
        buyExchangeId: ["coinbase", "kraken"][i % 2],
        sellExchangeId: ["kraken", "coinbase"][(i + 1) % 2],
        buyPrice: NumberDecimal((30000 + Math.random() * 1000).toFixed(2)),
        sellPrice: NumberDecimal((30050 + Math.random() * 1000).toFixed(2)),
        quantity: NumberDecimal((0.1 + Math.random() * 0.9).toFixed(4)),
        effectiveQuantity: NumberDecimal((0.1 + Math.random() * 0.9).toFixed(4)),
        profitAmount: NumberDecimal((10 + Math.random() * 50).toFixed(2)),
        profitPercentage: NumberDecimal((0.1 + Math.random() * 2).toFixed(3)),
        detectedAt: detectedAt,
        isExecuted: i % 10 === 0, // 10% executed
        executedAt: i % 10 === 0 ? detectedAt : null,
        buyFees: NumberDecimal((1 + Math.random() * 5).toFixed(2)),
        sellFees: NumberDecimal((1 + Math.random() * 5).toFixed(2)),
        status: i % 10 === 0 ? "Executed" : "Detected"
    });
}

db.arbitrageOpportunities.insertMany(opportunities);
print("✅ Inserted " + opportunities.length + " sample opportunities");

// Insert sample trade results
print("Inserting sample trade results...");
var trades = [];
for (var i = 0; i < 20; i++) {
    var timestamp = new Date(baseDate.getTime() - (i * 300000)); // 5 minute intervals
    trades.push({
        tradeId: "test_trade_" + i.toString().padStart(3, '0'),
        opportunityId: "test_opp_" + (i * 5).toString().padStart(3, '0'),
        tradingPair: ["BTC/USDT", "ETH/USDT", "ETH/BTC"][i % 3],
        buyExchangeId: ["coinbase", "kraken"][i % 2],
        sellExchangeId: ["kraken", "coinbase"][(i + 1) % 2],
        buyPrice: NumberDecimal((30000 + Math.random() * 1000).toFixed(2)),
        sellPrice: NumberDecimal((30050 + Math.random() * 1000).toFixed(2)),
        quantity: NumberDecimal((0.1 + Math.random() * 0.9).toFixed(4)),
        profitAmount: NumberDecimal((10 + Math.random() * 50).toFixed(2)),
        totalFees: NumberDecimal((2 + Math.random() * 8).toFixed(2)),
        timestamp: timestamp,
        isSuccess: i % 5 !== 0, // 80% success rate
        executionTimeMs: Math.floor(100 + Math.random() * 400)
    });
}

db.tradeResults.insertMany(trades);
print("✅ Inserted " + trades.length + " sample trades");

// Show final statistics
print("📈 Final Database Statistics:");
print("Opportunities: " + db.arbitrageOpportunities.countDocuments());
print("Trades: " + db.tradeResults.countDocuments());
print("Statistics: " + db.arbitrageStatistics.countDocuments());
print("Configurations: " + db.systemConfiguration.countDocuments());

EOF

echo "🎉 MongoDB setup completed successfully!"
echo ""
echo "📊 Summary:"
echo "   - Database: $DATABASE_NAME"
echo "   - Collections: arbitrageOpportunities, tradeResults, arbitrageStatistics, systemConfiguration"  
echo "   - Indexes: Optimized for time-series queries and performance"
echo "   - TTL Policies: Auto-cleanup configured (30 days, 1 year, 2 years)"
echo "   - Sample Data: 100 opportunities, 20 trades for testing"
echo ""
echo "🔗 Connection URI: $MONGO_URI"
echo "💡 Ready for migration testing and performance validation!" 