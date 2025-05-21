#!/bin/bash

# Directory structure verification
if [ ! -f "nswag.json" ]; then
    echo "Error: nswag.json not found in current directory"
    exit 1
fi

if [ ! -d "Controllers/Generated" ]; then
    echo "Creating Controllers/Generated directory..."
    mkdir -p Controllers/Generated
fi

# Run NSwag to generate controller interfaces
echo "Generating controller interfaces from OpenAPI specification..."
nswag run nswag.json

echo "Controller interfaces generated successfully!" 