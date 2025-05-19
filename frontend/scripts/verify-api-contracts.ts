#!/usr/bin/env ts-node

/**
 * API Contract Verification Script
 * 
 * This script verifies that the frontend API service aligns with the backend API endpoints.
 * It requires ts-node to run directly: npx ts-node scripts/verify-api-contracts.ts
 */

// Add Node.js type declarations
/// <reference types="node" />

import * as fs from 'fs';
import * as path from 'path';
import chalk from 'chalk';
import { ApiEndpoints } from '../src/services/apiEndpoints';
import axios from 'axios';

interface ControllerInfo {
  controllerName: string;
  endpoints: string[];
}

interface ContractVerificationResult {
  endpointPath: string;
  exists: boolean;
  method?: string;
  controllerName?: string;
}

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5001';
const BACKEND_CONTROLLER_PATH = '../backend/src/CryptoArbitrage.Api/Controllers';
const OUTPUT_LOG_FILE = 'api-misalignments.log';
const OUTPUT_JSON_FILE = 'api-contract-test-results.json';

// Flatten the API_ENDPOINTS object to get a list of all endpoint paths
function getFrontendEndpoints(): string[] {
  const endpoints: string[] = [];
  
  // Just get the values from the ApiEndpoints object
  for (const key in ApiEndpoints) {
    if (Object.prototype.hasOwnProperty.call(ApiEndpoints, key)) {
      endpoints.push(ApiEndpoints[key as keyof typeof ApiEndpoints]);
    }
  }
  
  return endpoints;
}

// Parse C# controller files to get backend endpoints
async function getBackendEndpoints(): Promise<ControllerInfo[]> {
  const controllerPath = path.resolve(__dirname, BACKEND_CONTROLLER_PATH);
  
  if (!fs.existsSync(controllerPath)) {
    console.error(chalk.red(`Backend controller path not found: ${controllerPath}`));
    console.log(chalk.yellow('Trying to verify endpoints through API exploration instead...'));
    return await exploreBackendEndpoints();
  }
  
  const controllers: ControllerInfo[] = [];
  const files = fs.readdirSync(controllerPath);
  
  for (const file of files) {
    if (file.endsWith('Controller.cs')) {
      const filePath = path.join(controllerPath, file);
      const content = fs.readFileSync(filePath, 'utf8');
      const controllerName = file.replace('.cs', '');
      
      // Extract route attributes
      const routeMatches = content.match(/\[Route\("([^"]+)"\)\]/g);
      const httpMethodMatches = content.match(/\[(Http(Get|Post|Put|Delete)(?:\("([^"]+)"\))?)\]/g);
      
      const endpoints: string[] = [];
      
      // Get the base route for the controller
      let baseRoute = '';
      if (routeMatches && routeMatches.length > 0) {
        const routeMatch = routeMatches[0].match(/\[Route\("([^"]+)"\)\]/);
        if (routeMatch && routeMatch.length > 1) {
          baseRoute = routeMatch[1];
        }
      }
      
      // Get all method routes
      if (httpMethodMatches) {
        for (const methodMatch of httpMethodMatches) {
          const match = methodMatch.match(/\[Http(Get|Post|Put|Delete)(?:\("([^"]+)"\))?\]/);
          if (match) {
            const method = match[1].toLowerCase();
            const route = match[2] || '';
            
            // Combine the base route with the method route
            let fullRoute = baseRoute;
            if (route) {
              fullRoute = fullRoute ? `${fullRoute}/${route}` : route;
            }
            
            // Normalize the route (remove parameters placeholders)
            fullRoute = `/api/${fullRoute.replace(/\{[^}]+\}/g, '')}`.replace(/\/\//g, '/');
            
            endpoints.push(fullRoute);
          }
        }
      }
      
      controllers.push({
        controllerName,
        endpoints
      });
    }
  }
  
  return controllers;
}

// Alternative method: Explore backend endpoints via API
async function exploreBackendEndpoints(): Promise<ControllerInfo[]> {
  try {
    // Try to access the swagger endpoint to get API info
    const response = await axios.get(`${API_URL}/swagger/v1/swagger.json`);
    
    if (response.data && response.data.paths) {
      const controllers: ControllerInfo[] = [];
      const controllerMap = new Map<string, string[]>();
      
      // Group endpoints by controller
      for (const path in response.data.paths) {
        const pathInfo = response.data.paths[path];
        
        for (const method in pathInfo) {
          const methodInfo = pathInfo[method];
          const tags = methodInfo.tags || [];
          
          // Assume the first tag is the controller name
          if (tags.length > 0) {
            const controllerName = tags[0];
            if (!controllerMap.has(controllerName)) {
              controllerMap.set(controllerName, []);
            }
            
            const endpoints = controllerMap.get(controllerName);
            if (endpoints) {
              endpoints.push(path);
            }
          }
        }
      }
      
      // Convert map to array of ControllerInfo
      controllerMap.forEach((endpoints, controllerName) => {
        controllers.push({
          controllerName,
          endpoints
        });
      });
      
      return controllers;
    }
    
    console.warn(chalk.yellow('Could not parse Swagger JSON. Using empty controllers list.'));
    return [];
  } catch (error) {
    console.warn(chalk.yellow('Could not access Swagger endpoint. Using empty controllers list.'));
    return [];
  }
}

// Check if frontend endpoints exist in backend
async function verifyApiContracts(): Promise<ContractVerificationResult[]> {
  const frontendEndpoints = getFrontendEndpoints();
  const backendControllers = await getBackendEndpoints();
  
  // Flatten backend endpoints
  const backendEndpointsMap = new Map<string, string>();
  for (const controller of backendControllers) {
    for (const endpoint of controller.endpoints) {
      backendEndpointsMap.set(endpoint.toLowerCase(), controller.controllerName);
    }
  }
  
  const results: ContractVerificationResult[] = [];
  
  // Check each frontend endpoint
  for (const frontendEndpoint of frontendEndpoints) {
    const normalizedEndpoint = frontendEndpoint.toLowerCase();
    const exists = backendEndpointsMap.has(normalizedEndpoint);
    
    results.push({
      endpointPath: frontendEndpoint,
      exists,
      controllerName: exists ? backendEndpointsMap.get(normalizedEndpoint) : undefined
    });
  }
  
  return results;
}

// Main function
async function main() {
  console.log(chalk.blue('🔍 Verifying API contract alignment between frontend and backend...'));
  
  const results = await verifyApiContracts();
  const misalignments = results.filter(r => !r.exists);
  
  // Output results
  console.log('\n=== API Contract Verification Results ===\n');
  
  if (misalignments.length === 0) {
    console.log(chalk.green('✅ All frontend API endpoints are aligned with backend controllers!'));
    console.log(chalk.green(`   Total endpoints verified: ${results.length}`));
  } else {
    console.log(chalk.red(`❌ Found ${misalignments.length} misaligned API endpoints (out of ${results.length} total):`));
    
    // List misalignments
    misalignments.forEach((m, index) => {
      console.log(chalk.yellow(`   ${index + 1}. Frontend endpoint not found in backend: ${m.endpointPath}`));
    });
    
    // Write misalignments to log file
    const logContent = `API Contract Misalignments (generated ${new Date().toISOString()})\n\n` +
      misalignments.map(m => `Frontend endpoint not found in backend: ${m.endpointPath}`).join('\n');
    
    fs.writeFileSync(OUTPUT_LOG_FILE, logContent);
    console.log(chalk.blue(`\nMisalignment details written to ${OUTPUT_LOG_FILE}`));
  }
  
  // Save full results to JSON for potential CI/CD integration
  fs.writeFileSync(OUTPUT_JSON_FILE, JSON.stringify({
    timestamp: new Date().toISOString(),
    totalEndpoints: results.length,
    misalignedEndpoints: misalignments.length,
    aligned: misalignments.length === 0,
    results
  }, null, 2));
  
  console.log(chalk.blue(`\nFull verification results saved to ${OUTPUT_JSON_FILE}`));
  
  // Return exit code based on alignment status
  process.exit(misalignments.length === 0 ? 0 : 1);
}

// Run the script
main().catch(error => {
  console.error(chalk.red('Error verifying API contracts:'), error);
  process.exit(1);
}); 