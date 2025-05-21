#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}Updating API contract verification to use OpenAPI specification${NC}"

# Check if the API client has been generated
if [ ! -f "src/services/generated/api-client.ts" ]; then
    echo -e "${RED}Error: Generated API client not found${NC}"
    echo "Please run './generateClient.sh' first to generate the API client"
    exit 1
fi

echo -e "${YELLOW}Creating new API verification script...${NC}"

# Create a new verify-api-swagger.js script
cat > scripts/verify-api-swagger.js << 'EOF'
/**
 * API Contract Verification Script - Swagger Version
 * 
 * This script verifies that the frontend API endpoints align with the OpenAPI specification.
 * It uses the OpenAPI specification directly as the source of truth.
 */

const fs = require('fs');
const path = require('path');
const chalk = require('chalk');

// Path to the OpenAPI specification
const OPENAPI_SPEC_PATH = path.resolve(__dirname, '../../api-specs/crypto-arbitrage-api.json');
// Path to the frontend ApiEndpoints file
const API_ENDPOINTS_PATH = path.resolve(__dirname, '../src/services/apiEndpoints.ts');
// Output file for misalignments
const OUTPUT_LOG_FILE = 'api-misalignments.log';
// Output file for results
const OUTPUT_JSON_FILE = 'api-contract-test-results.json';

console.log(chalk.blue('🔍 Verifying API contract alignment using OpenAPI specification...'));

// Load the OpenAPI specification
let openApiSpec;
try {
    const openApiJson = fs.readFileSync(OPENAPI_SPEC_PATH, 'utf8');
    openApiSpec = JSON.parse(openApiJson);
    console.log(chalk.green(`✓ Loaded OpenAPI specification from ${OPENAPI_SPEC_PATH}`));
} catch (error) {
    console.error(chalk.red(`Error loading OpenAPI specification: ${error.message}`));
    process.exit(1);
}

// Extract endpoints from OpenAPI spec
const specEndpoints = [];
for (const path in openApiSpec.paths) {
    specEndpoints.push(path);
}

console.log(`Found ${specEndpoints.length} endpoints in OpenAPI specification`);

// Get frontend endpoints
const getFrontendEndpoints = () => {
    try {
        // Try to load and parse the apiEndpoints.ts file
        const apiEndpointsFile = fs.readFileSync(API_ENDPOINTS_PATH, 'utf8');
        
        // Extract endpoint strings from the file
        const endpoints = [];
        const regex = /['"]\/api\/[^'"]+['"]/g;
        let match;
        
        while ((match = regex.exec(apiEndpointsFile)) !== null) {
            // Remove quotes
            const endpoint = match[0].replace(/['"`]/g, '');
            endpoints.push(endpoint);
        }
        
        return [...new Set(endpoints)]; // Remove duplicates
    } catch (error) {
        console.error(chalk.red(`Error parsing frontend endpoints: ${error.message}`));
        return [];
    }
};

const frontendEndpoints = getFrontendEndpoints();
console.log(`Found ${frontendEndpoints.length} endpoints in frontend code`);

// Compare endpoints
const compareEndpoints = () => {
    const misalignments = [];
    
    // Check if frontend endpoints exist in the spec
    for (const frontendEndpoint of frontendEndpoints) {
        // Normalize endpoint (remove query params, trailing slashes)
        const normalizedEndpoint = frontendEndpoint.split('?')[0].replace(/\/$/, '');
        
        // Find matching endpoint in spec
        const matchingSpecEndpoint = specEndpoints.find(specPath => {
            // Normalize spec path
            const normalizedSpecPath = specPath.split('{')[0].replace(/\/$/, '');
            return normalizedEndpoint === normalizedSpecPath;
        });
        
        if (!matchingSpecEndpoint) {
            misalignments.push({
                endpoint: frontendEndpoint,
                issue: 'Frontend endpoint not found in OpenAPI specification',
                suggestion: 'Add this endpoint to the OpenAPI specification or remove it from frontend'
            });
        }
    }
    
    // Check if spec endpoints exist in frontend
    for (const specEndpoint of specEndpoints) {
        // Skip endpoints with path parameters for simpler comparison
        if (specEndpoint.includes('{')) continue;
        
        // Normalize endpoint
        const normalizedSpecEndpoint = specEndpoint.split('{')[0].replace(/\/$/, '');
        
        // Find matching frontend endpoint
        const matchingFrontendEndpoint = frontendEndpoints.find(frontendPath => {
            const normalizedFrontendPath = frontendPath.split('?')[0].replace(/\/$/, '');
            return normalizedFrontendPath === normalizedSpecEndpoint;
        });
        
        if (!matchingFrontendEndpoint) {
            misalignments.push({
                endpoint: specEndpoint,
                issue: 'OpenAPI specification endpoint not used in frontend',
                suggestion: 'Add this endpoint to the frontend API client or remove it from the specification'
            });
        }
    }
    
    return misalignments;
};

const misalignments = compareEndpoints();

// Output results
if (misalignments.length === 0) {
    console.log(chalk.green('\n✅ All frontend API endpoints are aligned with the OpenAPI specification!'));
    console.log(chalk.green(`   Total endpoints verified: ${frontendEndpoints.length}`));
} else {
    console.log(chalk.red(`\n❌ Found ${misalignments.length} misaligned API endpoints (out of ${frontendEndpoints.length} total):`));
    
    // List misalignments
    misalignments.forEach((m, index) => {
        console.log(chalk.yellow(`   ${index + 1}. ${m.issue}: ${m.endpoint}`));
        console.log(chalk.blue(`      Suggestion: ${m.suggestion}`));
    });
    
    // Write misalignments to log file
    const logContent = `API Contract Misalignments (generated ${new Date().toISOString()})\n\n` +
        misalignments.map(m => {
            return `${m.issue}: ${m.endpoint}\nSuggestion: ${m.suggestion}`;
        }).join('\n\n');
    
    fs.writeFileSync(OUTPUT_LOG_FILE, logContent);
    console.log(chalk.blue(`\nMisalignment details written to ${OUTPUT_LOG_FILE}`));
}

// Save full results to JSON for potential CI/CD integration
const resultsJson = {
    timestamp: new Date().toISOString(),
    totalEndpoints: frontendEndpoints.length,
    misalignedEndpoints: misalignments.length,
    aligned: misalignments.length === 0,
    frontendEndpoints: frontendEndpoints,
    specEndpoints: specEndpoints,
    misalignments: misalignments
};

fs.writeFileSync(OUTPUT_JSON_FILE, JSON.stringify(resultsJson, null, 2));
console.log(chalk.blue(`\nFull verification results saved to ${OUTPUT_JSON_FILE}`));

// Return exit code based on alignment status
process.exit(misalignments.length === 0 ? 0 : 1);
EOF

echo -e "${GREEN}Created new API verification script: scripts/verify-api-swagger.js${NC}"

# Update package.json to include the new script
if grep -q "verify-api-swagger" package.json; then
    echo "Package.json already contains verify-api-swagger script"
else
    sed -i '' 's/"verify-api": "node scripts\/verify-api.js",/"verify-api": "node scripts\/verify-api.js",\n    "verify-api-swagger": "node scripts\/verify-api-swagger.js",/g' package.json
    echo -e "${GREEN}Added verify-api-swagger script to package.json${NC}"
fi

echo -e "${BLUE}Next steps:${NC}"
echo "1. Run './generateClient.sh' to generate the API client"
echo "2. Run 'npm run verify-api-swagger' to verify frontend endpoints against OpenAPI spec"
echo "3. Update 'start.sh' to use the new verification script"

echo -e "${GREEN}Update completed successfully!${NC}" 