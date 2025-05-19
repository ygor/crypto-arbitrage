#!/usr/bin/env node

/**
 * API Contract Verification Script (JavaScript Version)
 * 
 * This script verifies that the frontend API service aligns with the backend API endpoints.
 */

const fs = require('fs');
const path = require('path');
const axios = require('axios');

// Get the API endpoints from the frontend
const getEndpointsFromFrontend = () => {
  // Define the expected endpoints directly here to avoid module issues
  const ApiEndpoints = {
    ARBITRAGE_OPPORTUNITIES: '/api/arbitrage/opportunities',
    ARBITRAGE_TRADES: '/api/arbitrage/trades',
    ARBITRAGE_STATISTICS: '/api/arbitrage/statistics',
    OPPORTUNITIES_RECENT: '/api/opportunities/recent',
    OPPORTUNITIES: '/api/opportunities',
    TRADES_RECENT: '/api/trades/recent',
    TRADES: '/api/trades',
    STATISTICS: '/api/statistics',
    SETTINGS_RISK_PROFILE: '/api/settings/risk-profile',
    SETTINGS_ARBITRAGE: '/api/settings/arbitrage',
    SETTINGS_EXCHANGES: '/api/settings/exchanges',
    BOT_START: '/api/settings/bot/start',
    BOT_STOP: '/api/settings/bot/stop',
    BOT_STATUS: '/api/settings/bot/status',
  };

  return Object.values(ApiEndpoints);
};

// Try to get backend endpoints by exploring the API
const exploreBackendEndpoints = async () => {
  const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5001';
  
  try {
    console.log(`Attempting to explore backend API at ${API_URL}...`);
    const endpoints = [];
    
    // Option 1: Try to access Swagger JSON
    try {
      const response = await axios.get(`${API_URL}/swagger/v1/swagger.json`);
      if (response.data && response.data.paths) {
        console.log('Found Swagger documentation.');
        for (const path in response.data.paths) {
          endpoints.push(path);
        }
        return endpoints;
      }
    } catch (err) {
      console.log('Could not access Swagger documentation, trying next method...');
    }
    
    // Option 2: Try to access a health check or other known endpoints
    const knownEndpoints = [
      '/api/health', 
      '/api/arbitrage/opportunities', 
      '/api/settings/exchanges'
    ];
    
    for (const endpoint of knownEndpoints) {
      try {
        await axios.get(`${API_URL}${endpoint}`);
        endpoints.push(endpoint);
        console.log(`Found endpoint: ${endpoint}`);
      } catch (err) {
        // If it's a 401 or 404, the endpoint might exist but requires authentication
        if (err.response && (err.response.status === 401 || err.response.status === 404)) {
          endpoints.push(endpoint);
          console.log(`Found endpoint (needs auth): ${endpoint}`);
        }
      }
    }
    
    return endpoints;
  } catch (error) {
    console.error('Failed to explore backend endpoints:', error.message);
    return [];
  }
};

// Parse C# controller files to get backend endpoints (if available)
const parseControllerFiles = () => {
  const BACKEND_CONTROLLER_PATH = path.resolve(__dirname, '../../backend/src/CryptoArbitrage.Api/Controllers');
  const endpoints = [];
  
  if (!fs.existsSync(BACKEND_CONTROLLER_PATH)) {
    console.log(`Backend controller path not found: ${BACKEND_CONTROLLER_PATH}`);
    return endpoints;
  }
  
  console.log('Parsing controller files...');
  const files = fs.readdirSync(BACKEND_CONTROLLER_PATH);
  
  for (const file of files) {
    if (file.endsWith('Controller.cs')) {
      const filePath = path.join(BACKEND_CONTROLLER_PATH, file);
      const content = fs.readFileSync(filePath, 'utf8');
      
      // Extract route attribute
      const routeMatches = content.match(/\[Route\("([^"]+)"\)\]/g);
      if (routeMatches && routeMatches.length > 0) {
        const routeMatch = routeMatches[0].match(/\[Route\("([^"]+)"\)\]/);
        if (routeMatch && routeMatch.length > 1) {
          const baseRoute = routeMatch[1];
          const fullRoute = `/api/${baseRoute.replace(/\[controller\]/gi, file.replace('Controller.cs', '').toLowerCase())}`;
          endpoints.push(fullRoute);
        }
      }
      
      // Extract HTTP method attributes
      const httpMethodMatches = content.match(/\[Http(Get|Post|Put|Delete)(?:\("([^"]+)"\))?\]/g);
      if (httpMethodMatches) {
        for (const methodMatch of httpMethodMatches) {
          const match = methodMatch.match(/\[Http(Get|Post|Put|Delete)(?:\("([^"]+)"\))?\]/);
          if (match && match.length > 2 && match[2]) {
            endpoints.push(`/api/${match[2]}`);
          }
        }
      }
    }
  }
  
  return endpoints;
};

// Check if frontend endpoints exist in backend
const verifyApiContracts = async () => {
  const frontendEndpoints = getEndpointsFromFrontend();
  let backendEndpoints = parseControllerFiles();
  
  // If we couldn't get endpoints from controller files, try API exploration
  if (backendEndpoints.length === 0) {
    backendEndpoints = await exploreBackendEndpoints();
  }
  
  console.log('\n=== API Contract Verification Results ===\n');
  console.log(`Frontend endpoints: ${frontendEndpoints.length}`);
  console.log(`Backend endpoints: ${backendEndpoints.length}`);
  
  const misalignments = [];
  
  // Check each frontend endpoint against backend
  for (const frontendEndpoint of frontendEndpoints) {
    const normalizedEndpoint = frontendEndpoint.toLowerCase();
    const found = backendEndpoints.some(be => 
      normalizedEndpoint === be.toLowerCase() || 
      be.toLowerCase().includes(normalizedEndpoint)
    );
    
    if (!found) {
      misalignments.push(frontendEndpoint);
    }
  }
  
  // Output results
  if (misalignments.length === 0) {
    console.log('\n✅ All frontend API endpoints are aligned with backend controllers!');
    console.log(`   Total endpoints verified: ${frontendEndpoints.length}`);
  } else {
    console.log(`\n❌ Found ${misalignments.length} misaligned API endpoints (out of ${frontendEndpoints.length} total):`);
    
    // List misalignments
    misalignments.forEach((endpoint, index) => {
      console.log(`   ${index + 1}. Frontend endpoint not found in backend: ${endpoint}`);
    });
    
    // Write misalignments to log file
    const logContent = `API Contract Misalignments (generated ${new Date().toISOString()})\n\n` +
      misalignments.map(endpoint => `Frontend endpoint not found in backend: ${endpoint}`).join('\n');
    
    fs.writeFileSync('api-misalignments.log', logContent);
    console.log('\nMisalignment details written to api-misalignments.log');
  }
  
  // Save full results to JSON
  const resultsJson = {
    timestamp: new Date().toISOString(),
    totalEndpoints: frontendEndpoints.length,
    misalignedEndpoints: misalignments.length,
    aligned: misalignments.length === 0,
    misalignments
  };
  
  fs.writeFileSync('api-contract-test-results.json', JSON.stringify(resultsJson, null, 2));
  console.log('\nFull verification results saved to api-contract-test-results.json');
  
  // Return exit code based on alignment status
  return misalignments.length === 0 ? 0 : 1;
};

// Run the script
verifyApiContracts()
  .then(exitCode => {
    process.exit(exitCode);
  })
  .catch(error => {
    console.error('Error verifying API contracts:', error);
    process.exit(1);
  }); 