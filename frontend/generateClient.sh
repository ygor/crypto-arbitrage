#!/bin/bash

# Directory structure verification
if [ ! -f "nswag.json" ]; then
    echo "Error: nswag.json not found in current directory"
    exit 1
fi

if [ ! -d "src/services/generated" ]; then
    echo "Creating src/services/generated directory..."
    mkdir -p src/services/generated
fi

# Run NSwag to generate TypeScript client
echo "Generating TypeScript API client from OpenAPI specification..."
nswag run nswag.json

echo "TypeScript API client generated successfully!" 