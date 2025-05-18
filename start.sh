#!/bin/bash

echo "Starting Crypto Arbitrage application..."
echo "Building and starting containers..."

# Build and start the containers in detached mode
docker-compose up -d --build

echo "Containers started! Services will be available at:"
echo "- Frontend: http://localhost:3000"
echo "- API: http://localhost:5001/api"
echo "- MongoDB: localhost:27017"
echo "- Redis: localhost:6379"

echo ""
echo "To view logs, run: docker-compose logs -f"
echo "To stop all services, run: docker-compose down" 