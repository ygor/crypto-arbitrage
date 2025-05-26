const axios = require('axios');

// Backend API base URL
const API_BASE_URL = 'http://localhost:5001';

// Test all the key endpoints that the frontend should be able to call
const endpointTests = [
  {
    name: 'Health Check',
    endpoint: '/api/health',
    expectedStatus: 200,
    expectedFields: ['healthy', 'status']
  },
  {
    name: 'Arbitrage Statistics', 
    endpoint: '/api/arbitrage/statistics',
    expectedStatus: 200,
    expectedFields: ['detectedOpportunities', 'executedTrades']
  },
  {
    name: 'General Statistics',
    endpoint: '/api/statistics', 
    expectedStatus: 200,
    expectedFields: ['detectedOpportunities', 'executedTrades']
  },
  {
    name: 'Risk Profile',
    endpoint: '/api/settings/risk-profile',
    expectedStatus: 200,
    expectedFields: ['minProfitPercent', 'maxTradeAmount']
  },
  {
    name: 'Arbitrage Configuration',
    endpoint: '/api/settings/arbitrage',
    expectedStatus: 200,
    expectedFields: ['tradingPairs', 'minimumSpreadPercentage']
  },
  {
    name: 'Bot Status',
    endpoint: '/api/settings/bot/status',
    expectedStatus: 200,
    expectedFields: ['isRunning', 'state']
  },
  {
    name: 'Exchange Status',
    endpoint: '/api/settings/bot/exchange-status',
    expectedStatus: 200,
    expectedFields: [], // This returns an array
    expectArray: true
  },
  {
    name: 'Activity Logs',
    endpoint: '/api/settings/bot/activity-logs',
    expectedStatus: 200,
    expectedFields: [], // This returns an array
    expectArray: true
  }
];

async function testEndpoint(test) {
  try {
    const response = await axios.get(`${API_BASE_URL}${test.endpoint}`);
    
    // Check status code
    if (response.status !== test.expectedStatus) {
      return {
        ...test,
        status: 'FAIL',
        error: `Expected status ${test.expectedStatus}, got ${response.status}`
      };
    }
    
    const data = response.data;
    
    // Check if expecting array
    if (test.expectArray) {
      if (!Array.isArray(data)) {
        return {
          ...test,
          status: 'FAIL', 
          error: `Expected array, got ${typeof data}`
        };
      }
      return {
        ...test,
        status: 'PASS',
        dataType: 'array',
        arrayLength: data.length
      };
    }
    
    // Check expected fields
    const missingFields = test.expectedFields.filter(field => !(field in data));
    if (missingFields.length > 0) {
      return {
        ...test,
        status: 'FAIL',
        error: `Missing fields: ${missingFields.join(', ')}`
      };
    }
    
    return {
      ...test,
      status: 'PASS',
      dataType: typeof data,
      sampleFields: Object.keys(data).slice(0, 5)
    };
    
  } catch (error) {
    return {
      ...test,
      status: 'FAIL',
      error: error.message
    };
  }
}

async function runTests() {
  console.log('🔍 Testing Frontend-Backend Integration\n');
  console.log(`Testing against: ${API_BASE_URL}\n`);
  
  const results = [];
  
  for (const test of endpointTests) {
    console.log(`Testing: ${test.name}...`);
    const result = await testEndpoint(test);
    results.push(result);
    
    if (result.status === 'PASS') {
      console.log(`✅ PASS: ${test.name}`);
      if (result.expectArray) {
        console.log(`   → Array with ${result.arrayLength} items`);
      } else {
        console.log(`   → Fields: ${result.sampleFields?.join(', ')}`);
      }
    } else {
      console.log(`❌ FAIL: ${test.name}`);
      console.log(`   → Error: ${result.error}`);
    }
    console.log('');
  }
  
  // Summary
  const passed = results.filter(r => r.status === 'PASS').length;
  const failed = results.filter(r => r.status === 'FAIL').length;
  
  console.log('🏁 SUMMARY:');
  console.log(`✅ Passed: ${passed}`);
  console.log(`❌ Failed: ${failed}`);
  console.log(`📊 Total: ${results.length}`);
  
  if (failed === 0) {
    console.log('\n🎉 All frontend-backend integrations are working correctly!');
  } else {
    console.log('\n⚠️  Some endpoints have issues that need attention.');
  }
  
  return results;
}

// Run the tests
runTests().catch(console.error); 