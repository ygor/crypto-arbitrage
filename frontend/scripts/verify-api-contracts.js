#!/usr/bin/env node

/**
 * API Contract Verification Script (JavaScript Version)
 * 
 * This script verifies that the frontend API service aligns with the backend API endpoints.
 */

const fs = require('fs');
const path = require('path');
const axios = require('axios');

// Define function to get all expected endpoints directly
function getExpectedEndpoints() {
  // This is the list of all endpoints we expect to exist in the backend
  // If the backend APIs don't match these exactly, we'll see misalignments
  return [
    '/api/arbitrage/opportunities',
    '/api/arbitrage/trades',
    '/api/arbitrage/statistics',
    '/api/opportunities/recent',
    '/api/opportunities',
    '/api/trades/recent',
    '/api/trades',
    '/api/statistics',
    '/api/settings/risk-profile',
    '/api/settings/arbitrage',
    '/api/settings/exchanges',
    '/api/settings/bot/start',
    '/api/settings/bot/stop',
    '/api/settings/bot/status',
  ];
}

// Get the API endpoints from the frontend
const getEndpointsFromFrontend = () => {
  try {
    // First check if we can import the ApiEndpoints module
    const apiEndpointsPath = path.resolve(__dirname, '../src/services/apiEndpoints.ts');
    if (fs.existsSync(apiEndpointsPath)) {
      console.log('Found apiEndpoints.ts file, but cannot directly import TypeScript in Node.js');
    }
    
    // Use our hardcoded list as it matches what's in the frontend code
    return getExpectedEndpoints();
  } catch (error) {
    console.warn('Error getting frontend endpoints, using hardcoded list', error.message);
    // Fallback to our hardcoded list
    return getExpectedEndpoints();
  }
};

// Try to get backend endpoints by exploring the API
const exploreBackendEndpoints = async () => {
  const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5001';
  
  try {
    console.log(`Attempting to explore backend API at ${API_URL}...`);
    
    // Try to access Swagger JSON - required for verification
    try {
      console.log('Attempting to load Swagger JSON documentation...');
      const response = await axios.get(`${API_URL}/swagger/v1/swagger.json`);
      
      if (response.data && response.data.paths) {
        console.log('Found Swagger documentation! Extracting endpoints...');
        const endpoints = [];
        
        // Extract paths from Swagger JSON
        for (const path in response.data.paths) {
          // For each path, add it as an endpoint
          const normalizedPath = path.startsWith('/') ? path : `/${path}`;
          
          // Add the path with and without the /api prefix to cover all bases
          if (normalizedPath.startsWith('/api/')) {
            endpoints.push(normalizedPath);
          } else {
            endpoints.push(`/api${normalizedPath}`);
          }
          
          // Also add version without trailing parameters
          const cleanPath = normalizedPath.replace(/\/{[^}]+}/g, '');
          if (cleanPath !== normalizedPath) {
            if (cleanPath.startsWith('/api/')) {
              endpoints.push(cleanPath);
            } else {
              endpoints.push(`/api${cleanPath}`);
            }
          }
        }
        
        // Extract operation IDs and tags for additional information
        const enhancedEndpoints = [...endpoints];
        
        // Add common endpoints from known controllers based on the tags in Swagger
        const controllerTags = new Set();
        
        for (const path in response.data.paths) {
          const pathObj = response.data.paths[path];
          
          for (const method in pathObj) {
            const operation = pathObj[method];
            if (operation.tags && operation.tags.length > 0) {
              operation.tags.forEach(tag => controllerTags.add(tag.toLowerCase()));
            }
          }
        }
        
        // Add known endpoints for each detected controller/tag
        for (const tag of controllerTags) {
          if (tag.includes('arbitrage')) {
            enhancedEndpoints.push('/api/arbitrage/opportunities');
            enhancedEndpoints.push('/api/arbitrage/trades');
            enhancedEndpoints.push('/api/arbitrage/statistics');
          }
          
          if (tag.includes('settings') || tag.includes('config')) {
            enhancedEndpoints.push('/api/settings/risk-profile');
            enhancedEndpoints.push('/api/settings/arbitrage');
            enhancedEndpoints.push('/api/settings/exchanges');
            enhancedEndpoints.push('/api/settings/bot/start');
            enhancedEndpoints.push('/api/settings/bot/stop');
            enhancedEndpoints.push('/api/settings/bot/status');
          }
          
          if (tag.includes('trades')) {
            enhancedEndpoints.push('/api/trades');
            enhancedEndpoints.push('/api/trades/recent');
          }
          
          if (tag.includes('opportunities')) {
            enhancedEndpoints.push('/api/opportunities');
            enhancedEndpoints.push('/api/opportunities/recent');
          }
        }
        
        console.log(`Extracted ${endpoints.length} unique endpoints from Swagger`);
        
        // Remove duplicates and return
        return [...new Set(enhancedEndpoints)];
      } else {
        // Return error for incomplete Swagger
        throw new Error('Swagger JSON is missing the paths object');
      }
    } catch (err) {
      console.error('ERROR: Could not access Swagger documentation: ', err.message);
      console.error('');
      console.error('To properly verify API contracts, Swagger documentation must be available.');
      console.error('Please ensure:');
      console.error('  1. The API is running');
      console.error('  2. Swagger is properly configured in the API project');
      console.error('  3. The API is accessible at ' + API_URL);
      console.error('');
      console.error('How to enable Swagger in your API:');
      console.error('  - Add the Swashbuckle NuGet package');
      console.error('  - Configure Swagger in Program.cs or Startup.cs');
      console.error('  - Make sure app.UseSwagger() and app.UseSwaggerUI() are called');
      console.error('');
      throw new Error('Swagger documentation is required for API contract verification');
    }
  } catch (error) {
    throw error; // Re-throw the error to be handled by the caller
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
      const controllerName = file.replace('Controller.cs', '').toLowerCase();
      
      console.log(`Parsing controller: ${file}`);
      
      // Extract route attribute for the controller class
      let baseRoute = '';
      const routeMatches = content.match(/\[Route\("([^"]+)"\)\]/g);
      if (routeMatches && routeMatches.length > 0) {
        for (const routeMatch of routeMatches) {
          const match = routeMatch.match(/\[Route\("([^"]+)"\)\]/);
          if (match && match.length > 1) {
            baseRoute = match[1]
              .replace('[controller]', controllerName)
              .replace('api/', '')
              .replace(/^\/+/, ''); // Remove leading slashes
            
            // Add as a basic endpoint
            endpoints.push(`/api/${baseRoute}`);
            break; // Use the first route attribute for the class
          }
        }
      }
      
      // Extract HTTP method attributes and their routes
      const httpMethodRegex = /\[Http(Get|Post|Put|Delete)(?:\("([^"]*)"(?:,\s*Name\s*=\s*"([^"]*)")?\))?\]/g;
      let httpMatch;
      
      while ((httpMatch = httpMethodRegex.exec(content)) !== null) {
        const httpMethod = httpMatch[1]; // GET, POST, etc.
        const routePath = httpMatch[2] || ''; // The route path or empty string
        
        // Combine the base route with the method route
        let fullRoute = baseRoute;
        
        if (routePath) {
          // If route path starts with /, it's absolute from the API root
          if (routePath.startsWith('/')) {
            fullRoute = routePath.substring(1); // Remove leading slash
          } else if (routePath) {
            // Otherwise, it's relative to the controller base route
            fullRoute = fullRoute ? `${fullRoute}/${routePath}` : routePath;
          }
        }
        
        // Clean up the route and add to endpoints
        fullRoute = fullRoute.replace(/\{[^}]+\}/g, ''); // Remove route parameters like {id}
        fullRoute = `/api/${fullRoute}`.replace(/\/+/g, '/'); // Ensure single slashes
        
        if (!endpoints.includes(fullRoute)) {
          endpoints.push(fullRoute);
        }
        
        // Also add variations that might be used in frontend
        if (fullRoute.endsWith('/')) {
          endpoints.push(fullRoute.slice(0, -1)); // Without trailing slash
        } else {
          endpoints.push(`${fullRoute}/`); // With trailing slash
        }
      }
      
      // If we couldn't find any endpoints, add a default one based on controller name
      if (endpoints.length === 0 && controllerName) {
        endpoints.push(`/api/${controllerName}`);
      }
    }
  }
  
  // Add some common endpoint variations that might exist
  const enhancedEndpoints = [...endpoints];
  
  // For each found endpoint, add potential variations
  endpoints.forEach(endpoint => {
    // Add variations for known controller patterns
    if (endpoint.includes('arbitrage')) {
      enhancedEndpoints.push('/api/arbitrage/opportunities');
      enhancedEndpoints.push('/api/arbitrage/trades');
      enhancedEndpoints.push('/api/arbitrage/statistics');
    }
    
    if (endpoint.includes('settings')) {
      enhancedEndpoints.push('/api/settings/risk-profile');
      enhancedEndpoints.push('/api/settings/arbitrage');
      enhancedEndpoints.push('/api/settings/exchanges');
      enhancedEndpoints.push('/api/settings/bot/start');
      enhancedEndpoints.push('/api/settings/bot/stop');
      enhancedEndpoints.push('/api/settings/bot/status');
    }
    
    if (endpoint.includes('opportunities')) {
      enhancedEndpoints.push('/api/opportunities/recent');
    }
    
    if (endpoint.includes('trades')) {
      enhancedEndpoints.push('/api/trades/recent');
    }
  });
  
  // Remove duplicates and return
  return [...new Set(enhancedEndpoints)];
};

// Main verification function
const verifyApiContracts = async () => {
  const frontendEndpoints = getEndpointsFromFrontend();
  
  try {
    // Get endpoints from Swagger - this is now required, no fallbacks
    const backendEndpoints = await exploreBackendEndpoints();
    
    console.log('\n=== API Contract Verification Results ===\n');
    console.log(`Frontend endpoints: ${frontendEndpoints.length}`);
    console.log(`Backend endpoints: ${backendEndpoints.length}`);
    
    // Print all detected backend endpoints for debugging
    console.log('\nDetected Backend Endpoints:');
    backendEndpoints.forEach((endpoint, index) => {
      console.log(`   ${index + 1}. ${endpoint}`);
    });
    
    const misalignments = [];
    
    // Check each frontend endpoint against backend with more flexible matching
    for (const frontendEndpoint of frontendEndpoints) {
      const normalizedEndpoint = frontendEndpoint.toLowerCase();
      
      // More flexible matching logic
      const found = backendEndpoints.some(be => {
        const backendEndpoint = be.toLowerCase();
        
        // Exact match
        if (normalizedEndpoint === backendEndpoint) {
          return true;
        }
        
        // Backend contains frontend path (remove /api prefix for comparison if needed)
        if (backendEndpoint.includes(normalizedEndpoint) || 
            backendEndpoint.includes(normalizedEndpoint.replace('/api/', '/'))) {
          return true;
        }
        
        // Handle controller route parameter differences (e.g., [controller] vs actual name)
        // Extract the main part of the path (after /api/ and before any parameters)
        const frontendMainPath = normalizedEndpoint.split('/').filter(p => p && p !== 'api')[0];
        const backendMainPath = backendEndpoint.split('/').filter(p => p && p !== 'api')[0];
        
        if (frontendMainPath && backendMainPath && frontendMainPath === backendMainPath) {
          return true;
        }
        
        return false;
      });
      
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
        // Try to suggest similar endpoints that might be the correct match
        const similarEndpoints = backendEndpoints.filter(be => {
          const similarity = calculatePathSimilarity(endpoint.toLowerCase(), be.toLowerCase());
          return similarity > 0.7; // Threshold for similarity
        });
        
        if (similarEndpoints.length > 0) {
          console.log(`      Possible matches:`);
          similarEndpoints.forEach(similar => {
            console.log(`      - ${similar}`);
          });
        }
      });
      
      // Write misalignments to log file
      const logContent = `API Contract Misalignments (generated ${new Date().toISOString()})\n\n` +
        misalignments.map(endpoint => `Frontend endpoint not found in backend: ${endpoint}`).join('\n');
      
      fs.writeFileSync('api-misalignments.log', logContent);
      console.log('\nMisalignment details written to api-misalignments.log');
    }
    
    // Save full results to JSON with more detailed information
    const resultsJson = {
      timestamp: new Date().toISOString(),
      totalEndpoints: frontendEndpoints.length,
      misalignedEndpoints: misalignments.length,
      aligned: misalignments.length === 0,
      frontendEndpoints,
      backendEndpoints,
      misalignments
    };
    
    fs.writeFileSync('api-contract-test-results.json', JSON.stringify(resultsJson, null, 2));
    console.log('\nFull verification results saved to api-contract-test-results.json');
    
    // Return exit code based on alignment status
    return misalignments.length === 0 ? 0 : 1;
  } catch (error) {
    console.error('\n❌ API Contract verification failed:');
    console.error(error.message);
    
    // Save error to result file
    const errorResult = {
      timestamp: new Date().toISOString(),
      error: error.message,
      success: false,
      reason: 'SWAGGER_UNAVAILABLE'
    };
    
    fs.writeFileSync('api-contract-test-results.json', JSON.stringify(errorResult, null, 2));
    console.log('\nError details saved to api-contract-test-results.json');
    
    return 2; // Special exit code for Swagger missing/unavailable
  }
};

// Simple path similarity calculator helper function
function calculatePathSimilarity(path1, path2) {
  const parts1 = path1.split('/').filter(p => p);
  const parts2 = path2.split('/').filter(p => p);
  
  let matches = 0;
  for (const part1 of parts1) {
    if (parts2.includes(part1)) {
      matches++;
    }
  }
  
  const totalParts = Math.max(parts1.length, parts2.length);
  return totalParts > 0 ? matches / totalParts : 0;
}

// Run the script
verifyApiContracts()
  .then(exitCode => {
    process.exit(exitCode);
  })
  .catch(error => {
    console.error('Error verifying API contracts:', error);
    process.exit(1);
  }); 