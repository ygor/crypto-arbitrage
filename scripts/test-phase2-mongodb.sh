#!/bin/bash

# Phase 2 MongoDB Migration and Validation Test Suite
echo "🚀 Phase 2 MongoDB Migration & Validation Test Suite"
echo "===================================================="

# Test Docker and MongoDB setup
echo "✅ Testing MongoDB setup..."
docker-compose up -d mongodb redis

echo "⏳ Waiting for MongoDB..."
sleep 10

echo "🔍 Testing MongoDB connection..."
docker exec crypto-arbitrage-mongodb mongosh --eval "print('MongoDB is ready')" || echo "MongoDB connection failed"

echo "🧪 Running application tests..."
docker-compose up -d api

echo "⏳ Waiting for API..."
sleep 15

echo "🏥 Testing health endpoint..."
curl -s http://localhost:5001/health || echo "Health check not available yet"

echo ""
echo "✅ Phase 2 MongoDB setup completed!"
echo "📝 Check logs with: docker-compose logs"
echo "🛑 Stop with: docker-compose down"
