/**
 * verify-api-swagger.js
 * 
 * This script verifies that frontend API calls align with the OpenAPI specification.
 * It compares the endpoints used in frontend code against those defined in the
 * generated API client from our OpenAPI specification.
 */

const fs = require('fs');
const path = require('path');
const glob = require('glob');

// Output files
const RESULTS_FILE = path.resolve(__dirname, '../api-contract-test-results.json');
const MISALIGNMENTS_FILE = path.resolve(__dirname, '../api-misalignments.log');

// Configuration
const FRONTEND_SRC_DIR = path.resolve(__dirname, '../src');
const API_CLIENT_PATH = path.resolve(__dirname, '../src/services/generated/api-client.ts');

// Clear previous results
if (fs.existsSync(MISALIGNMENTS_FILE)) {
  fs.unlinkSync(MISALIGNMENTS_FILE);
}

console.log('Mode: OpenAPI/Swagger-based verification');

// Check if the generated API client exists
if (!fs.existsSync(API_CLIENT_PATH)) {
  console.error('\x1b[31mERROR: Generated API client not found at ' + API_CLIENT_PATH + '\x1b[0m');
  console.error('\x1b[33mPlease ensure that:');
  console.error('1. The OpenAPI specification file exists');
  console.error('2. The NSwag client generator has been run');
  console.error('3. Run the frontend/generateClient.sh script to generate the client\x1b[0m');
  
  const errorResults = {
    success: false,
    error: 'Generated API client not found',
    frontendEndpoints: [],
    backendEndpoints: [],
    missingEndpoints: [],
    timestamp: new Date().toISOString()
  };
  
  fs.writeFileSync(RESULTS_FILE, JSON.stringify(errorResults, null, 2));
  fs.writeFileSync(MISALIGNMENTS_FILE, 'ERROR: Generated API client not found. Cannot verify API contracts.');
  process.exit(1);
}

// Parse the generated API client to extract all available endpoints
function extractEndpointsFromApiClient() {
  const apiClientContent = fs.readFileSync(API_CLIENT_PATH, 'utf8');
  
  // Extract method names and their HTTP methods/paths
  const methodRegex = /(\w+)\s*\([^)]*\)\s*:\s*Promise<([^>]+)>[^{]*{[^{]*this\.baseUrl\s*\+\s*["']([^"']+)['"]/g;
  
  const backendEndpoints = [];
  let match;
  
  while ((match = methodRegex.exec(apiClientContent)) !== null) {
    const methodName = match[1];
    const returnType = match[2];
    const path = match[3];
    
    // Determine HTTP method based on method name or by looking ahead in the code
    let httpMethod = "GET";
    
    // More sophisticated HTTP method detection by looking at the code
    const methodCodeStart = match.index;
    const nextMethodStart = apiClientContent.indexOf('/**', methodCodeStart + 10);
    const methodCode = nextMethodStart > 0 
      ? apiClientContent.substring(methodCodeStart, nextMethodStart)
      : apiClientContent.substring(methodCodeStart);
    
    if (methodCode.includes('method: "POST"') || methodCode.includes('method:"POST"')) {
      httpMethod = "POST";
    } else if (methodCode.includes('method: "PUT"') || methodCode.includes('method:"PUT"')) {
      httpMethod = "PUT";
    } else if (methodCode.includes('method: "DELETE"') || methodCode.includes('method:"DELETE"')) {
      httpMethod = "DELETE";
    } else if (methodCode.includes('method: "PATCH"') || methodCode.includes('method:"PATCH"')) {
      httpMethod = "PATCH";
    } else if (methodName.startsWith("update") || methodName.startsWith("create") || methodName.startsWith("post")) {
      httpMethod = "POST";
    } else if (methodName.startsWith("delete") || methodName.startsWith("remove")) {
      httpMethod = "DELETE";
    } else if (methodName.startsWith("patch") || methodName.startsWith("modify")) {
      httpMethod = "PATCH";
    } else if (methodName.startsWith("put")) {
      httpMethod = "PUT";
    }
    
    backendEndpoints.push({
      path,
      httpMethod,
      methodName,
      returnType,
      source: 'OpenAPI'
    });
  }
  
  return backendEndpoints;
}

// Find all frontend API calls
function findFrontendApiCalls() {
  const frontendEndpoints = [];
  
  // First, let's get all API endpoint definitions from apiEndpoints.ts
  const apiEndpointsFile = path.resolve(FRONTEND_SRC_DIR, 'services/apiEndpoints.ts');
  let endpointDefinitions = {};
  
  if (fs.existsSync(apiEndpointsFile)) {
    const apiEndpointsContent = fs.readFileSync(apiEndpointsFile, 'utf8');
    
    // Extract endpoint string values
    const endpointRegex = /([A-Z_]+):\s*['"]([^'"]+)['"]/g;
    let match;
    
    while ((match = endpointRegex.exec(apiEndpointsContent)) !== null) {
      const endpointName = match[1];
      const endpointPath = match[2];
      
      if (endpointPath.startsWith('/api/')) {
        endpointDefinitions[endpointName] = endpointPath;
      }
    }
    
    console.log(`Found ${Object.keys(endpointDefinitions).length} API endpoint definitions in apiEndpoints.ts`);
  }
  
  // API call patterns to search for
  const apiPatterns = [
    // Direct API calls using fetch or axios
    /\.(get|post|put|delete|patch)\s*\(\s*['"`]([^'"`]+)['"`]/gi,
    // ApiClient method calls
    /apiClient\.(\w+)\s*\(/gi,
    // Other common patterns
    /url\s*[:=]\s*['"`]([^'"`]+)['"`].*?method\s*[:=]\s*['"`](GET|POST|PUT|DELETE|PATCH)['"`]/gi,
    // Axios calls using api instance with endpoint constants
    /api\.(get|post|put|delete|patch)\s*\(\s*([A-Za-z0-9_.]+)\)/gi,
    // Axios calls using ApiEndpoints constants directly
    /api\.(get|post|put|delete|patch)\s*\(\s*ApiEndpoints\.([A-Z_]+)\)/gi,
    // Client calls using the generated OpenAPI client
    /client\.(\w+)\s*\(/gi
  ];
  
  // Find all TypeScript and JavaScript files
  const files = glob.sync(`${FRONTEND_SRC_DIR}/**/*.{ts,tsx,js,jsx}`, {
    ignore: [
      '**/node_modules/**',
      '**/dist/**',
      '**/build/**',
      '**/generated/**'
    ]
  });
  
  files.forEach(filePath => {
    const content = fs.readFileSync(filePath, 'utf8');
    const relativePath = path.relative(FRONTEND_SRC_DIR, filePath);
    
    // Extract API calls
    apiPatterns.forEach((pattern, patternIndex) => {
      let match;
      
      if (patternIndex === 0) { // Direct API calls
        while ((match = pattern.exec(content)) !== null) {
          const httpMethod = match[1].toUpperCase();
          const endpoint = match[2];
          
          if (endpoint.startsWith('/api/')) {
            frontendEndpoints.push({
              path: endpoint,
              httpMethod,
              source: relativePath,
              line: getLineNumber(content, match.index)
            });
          }
        }
      } else if (patternIndex === 1 || patternIndex === 5) { // ApiClient method calls or client.method() calls
        while ((match = pattern.exec(content)) !== null) {
          const methodName = match[1];
          frontendEndpoints.push({
            methodName,
            source: relativePath,
            line: getLineNumber(content, match.index)
          });
        }
      } else if (patternIndex === 2) { // URL/method pairs
        while ((match = pattern.exec(content)) !== null) {
          const endpoint = match[1];
          const httpMethod = match[2].toUpperCase();
          
          if (endpoint.startsWith('/api/')) {
            frontendEndpoints.push({
              path: endpoint,
              httpMethod,
              source: relativePath,
              line: getLineNumber(content, match.index)
            });
          }
        }
      } else if (patternIndex === 3) { // Axios calls with endpoint variables
        while ((match = pattern.exec(content)) !== null) {
          const httpMethod = match[1].toUpperCase();
          const endpointVar = match[2];
          
          // Try to find the endpoint value
          frontendEndpoints.push({
            endpointVar,
            httpMethod,
            source: relativePath,
            line: getLineNumber(content, match.index)
          });
        }
      } else if (patternIndex === 4) { // Axios calls with ApiEndpoints constants
        while ((match = pattern.exec(content)) !== null) {
          const httpMethod = match[1].toUpperCase();
          const endpointKey = match[2];
          const endpointPath = endpointDefinitions[endpointKey];
          
          if (endpointPath) {
            frontendEndpoints.push({
              path: endpointPath,
              httpMethod,
              source: relativePath,
              line: getLineNumber(content, match.index),
              endpointKey
            });
          }
        }
      }
    });
    
    // Look for API function implementations
    if (relativePath === 'services/api.ts') {
      // Look for client method calls within API functions
      const clientMethodRegex = /export\s+const\s+(\w+)\s*=\s*async.*?return\s+client\.(\w+)\s*\(/gs;
      while ((match = clientMethodRegex.exec(content)) !== null) {
        const functionName = match[1];
        const methodName = match[2];
        
        frontendEndpoints.push({
          methodName,
          source: relativePath,
          line: getLineNumber(content, match.index),
          functionName
        });
      }
      
      // Look for more complex client method usage with variable assignments
      const complexClientMethodRegex = /export\s+const\s+(\w+)\s*=\s*async.*?client\.(\w+)\s*\(/gs;
      while ((match = complexClientMethodRegex.exec(content)) !== null) {
        const functionName = match[1];
        const methodName = match[2];
        
        // Check if already added by the previous regex
        const alreadyAdded = frontendEndpoints.some(
          ep => ep.functionName === functionName && ep.methodName === methodName
        );
        
        if (!alreadyAdded) {
          frontendEndpoints.push({
            methodName,
            source: relativePath,
            line: getLineNumber(content, match.index),
            functionName
          });
        }
      }

      // Find functions that make API calls with axios
      const apiFunctionRegex = /export\s+const\s+(\w+)\s*=\s*async[^{]*{[^}]*api\.(get|post|put|delete|patch)\s*\(\s*([^)]+)/g;
      
      while ((match = apiFunctionRegex.exec(content)) !== null) {
        const functionName = match[1];
        const httpMethod = match[2].toUpperCase();
        const endpointStr = match[3].trim();
        
        let path = null;
        if (endpointStr.startsWith("'") || endpointStr.startsWith('"') || endpointStr.startsWith('`')) {
          // Direct string
          path = endpointStr.substring(1, endpointStr.indexOf(endpointStr[0], 1));
        } else if (endpointStr.startsWith('ApiEndpoints.')) {
          // ApiEndpoints constant
          const endpointKey = endpointStr.substring('ApiEndpoints.'.length);
          path = endpointDefinitions[endpointKey] || `[${endpointKey}]`;
        }
        
        if (path) {
          frontendEndpoints.push({
            path,
            httpMethod,
            source: relativePath,
            line: getLineNumber(content, match.index),
            functionName
          });
        }
      }
    }
  });
  
  return frontendEndpoints;
}

// Get line number from content and position
function getLineNumber(content, position) {
  const lines = content.substring(0, position).split('\n');
  return lines.length;
}

// Verify that all frontend endpoints exist in the backend
function verifyEndpoints(frontendEndpoints, backendEndpoints) {
  const missingEndpoints = [];
  
  frontendEndpoints.forEach(frontendEndpoint => {
    if (frontendEndpoint.methodName) {
      // Check method name
      const matchingBackend = backendEndpoints.find(be => {
        // Direct comparison
        if (be.methodName === frontendEndpoint.methodName) return true;
        
        // Some common naming variations
        const normalizedFeName = frontendEndpoint.methodName.toLowerCase();
        const normalizedBeName = be.methodName.toLowerCase();
        
        // Handle common prefix differences (e.g., getOpportunities vs. getArbitrageOpportunities)
        if (normalizedFeName.endsWith(normalizedBeName) || normalizedBeName.endsWith(normalizedFeName)) return true;
        
        return false;
      });
      
      if (!matchingBackend) {
        missingEndpoints.push({
          ...frontendEndpoint,
          reason: `Method '${frontendEndpoint.methodName}' does not exist in the API client`
        });
      }
    } else if (frontendEndpoint.path) {
      // Check path and method
      // Normalize path by removing any params like /{id} and replacing with regex
      const normalizedFrontendPath = frontendEndpoint.path.replace(/\/\d+/g, '/{id}');
      
      // For template literals with variables, we need a more flexible approach
      if (normalizedFrontendPath.includes('${')) {
        // Extract the base path without the template variables
        const basePathMatch = normalizedFrontendPath.match(/^([^$]*)/);
        if (basePathMatch && basePathMatch[1]) {
          const basePath = basePathMatch[1];
          
          // Check if any backend endpoint starts with this base path
          const matchingBackend = backendEndpoints.find(be => 
            be.path.startsWith(basePath) && 
            (frontendEndpoint.httpMethod === be.httpMethod || !frontendEndpoint.httpMethod)
          );
          
          if (matchingBackend) {
            return; // Found a match, don't add to missingEndpoints
          }
        }
      } else {
        // Normal path comparison
        const matchingBackend = backendEndpoints.find(be => {
          // Convert OpenAPI path params format /{param} to regex
          const bePathRegex = new RegExp(
            '^' + be.path.replace(/\/{([^}]+)}/g, '/[^/]+').replace(/\?$/, '') + '($|\\?)'
          );
          return bePathRegex.test(normalizedFrontendPath) && 
                 (be.httpMethod === frontendEndpoint.httpMethod || !frontendEndpoint.httpMethod);
        });
        
        if (matchingBackend) {
          return; // Found a match, don't add to missingEndpoints
        }
      }
      
      missingEndpoints.push({
        ...frontendEndpoint,
        reason: `Endpoint '${frontendEndpoint.httpMethod} ${frontendEndpoint.path}' does not exist in the API specification`
      });
    }
  });
  
  return missingEndpoints;
}

// Main verification process
try {
  console.log('🔍 Analyzing API client from OpenAPI specification...');
  const backendEndpoints = extractEndpointsFromApiClient();
  console.log(`✓ Found ${backendEndpoints.length} endpoints in the API client`);
  
  console.log('🔍 Searching for API calls in frontend code...');
  const frontendEndpoints = findFrontendApiCalls();
  console.log(`✓ Found ${frontendEndpoints.length} API calls in frontend code`);
  
  console.log('🔍 Verifying alignment between frontend calls and API specification...');
  
  // Check for client usage - if we're using the OpenAPI client, we can skip many checks
  const apiFilePath = path.resolve(FRONTEND_SRC_DIR, 'services/api.ts');
  console.log(`Checking for API client usage in: ${apiFilePath}`);
  const fileExists = fs.existsSync(apiFilePath);
  console.log(`File exists: ${fileExists}`);
  
  let clientImportCheck = false;
  if (fileExists) {
    const apiFileContent = fs.readFileSync(apiFilePath, 'utf8');
    clientImportCheck = apiFileContent.includes('import { Client } from');
    console.log(`Client import found: ${clientImportCheck}`);
  }
  
  const isUsingGeneratedClient = fileExists && clientImportCheck;
  
  let missingEndpoints = [];
  
  if (isUsingGeneratedClient) {
    console.log('✓ Detected usage of OpenAPI generated client - this means frontend is using the API client as source of truth');
    
    // Only check for direct axios/fetch calls that bypass the client
    const directApiCalls = frontendEndpoints.filter(endpoint => 
      endpoint.path && 
      !endpoint.methodName && 
      endpoint.source !== 'services/api.ts' && 
      !endpoint.path.includes('${')
    );
    
    missingEndpoints = verifyEndpoints(directApiCalls, backendEndpoints);
    
    // Add specific warning about bot endpoints
    if (
      !backendEndpoints.some(be => be.path.includes('/api/bot/')) && 
      frontendEndpoints.some(fe => fe.path && fe.path.includes('/api/bot/'))
    ) {
      console.log('\x1b[33m⚠ Note: Using "/api/bot/" endpoints but the OpenAPI spec uses "/api/settings/bot/" endpoints\x1b[0m');
    }
  } else {
    missingEndpoints = verifyEndpoints(frontendEndpoints, backendEndpoints);
  }
  
  // Save results
  const results = {
    success: missingEndpoints.length === 0,
    frontendEndpointCount: frontendEndpoints.length,
    backendEndpointCount: backendEndpoints.length,
    missingEndpointCount: missingEndpoints.length,
    frontendEndpoints,
    backendEndpoints,
    missingEndpoints,
    timestamp: new Date().toISOString()
  };
  
  fs.writeFileSync(RESULTS_FILE, JSON.stringify(results, null, 2));
  
  // Report results
  if (missingEndpoints.length === 0) {
    console.log('\x1b[32m✓ All frontend API calls align with the OpenAPI specification\x1b[0m');
    console.log(`  ✓ Verified ${frontendEndpoints.length} frontend API endpoints against ${backendEndpoints.length} backend endpoints`);
    console.log(`  ✓ Results saved to ${RESULTS_FILE}`);
    process.exit(0);
  } else {
    console.error('\x1b[31m✗ API contract verification failed\x1b[0m');
    console.error(`  ✗ Found ${missingEndpoints.length} frontend API calls that do not align with the OpenAPI specification`);
    
    // Write misalignments to log file
    let misalignmentLog = 'API CONTRACT MISALIGNMENTS\n';
    misalignmentLog += '------------------------\n\n';
    
    missingEndpoints.forEach((endpoint, i) => {
      misalignmentLog += `${i + 1}. ${endpoint.reason}\n`;
      misalignmentLog += `   File: ${endpoint.source}:${endpoint.line}\n`;
      if (endpoint.methodName) {
        misalignmentLog += `   ApiClient method: ${endpoint.methodName}\n`;
      }
      if (endpoint.path) {
        misalignmentLog += `   Endpoint: ${endpoint.httpMethod} ${endpoint.path}\n`;
      }
      misalignmentLog += '\n';
    });
    
    fs.writeFileSync(MISALIGNMENTS_FILE, misalignmentLog);
    
    console.error(`  ✗ Details saved to ${MISALIGNMENTS_FILE}`);
    console.error(`  ✗ Full results saved to ${RESULTS_FILE}`);
    
    process.exit(1);
  }
} catch (error) {
  console.error('\x1b[31mERROR: API contract verification failed\x1b[0m');
  console.error(error);
  
  const errorResults = {
    success: false,
    error: error.message,
    stack: error.stack,
    timestamp: new Date().toISOString()
  };
  
  fs.writeFileSync(RESULTS_FILE, JSON.stringify(errorResults, null, 2));
  fs.writeFileSync(MISALIGNMENTS_FILE, `ERROR: ${error.message}\n\n${error.stack}`);
  
  process.exit(1);
} 