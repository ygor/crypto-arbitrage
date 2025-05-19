# API Contract Testing Guide

This document explains how to use the API contract testing system in the Crypto Arbitrage project.

## What is API Contract Testing?

API contract testing ensures that the frontend and backend communicate correctly by validating that:

1. The frontend makes requests to the correct API endpoints
2. The requests include the expected parameters and payload formats
3. The backend has implemented all endpoints that the frontend expects

In short, it verifies that both sides of the application "speak the same language."

## Running the Tests

### Option 1: Using the start.sh Script

The `start.sh` script has been enhanced to include API contract testing. You can use it with various options:

```bash
# Run tests and then start containers if tests pass
./start.sh

# Skip tests and just start containers
./start.sh --no-tests

# Only run tests without starting containers
./start.sh --tests-only

# Show help
./start.sh --help
```

### Option 2: Running Tests Directly

You can also run the tests manually:

```bash
# Navigate to the frontend directory
cd frontend

# Run the API endpoint unit tests
npm test -- src/services/api.contract.test.ts

# Run the API contract verification script
npm run verify-api
```

## Types of Tests

### 1. API Endpoint Unit Tests

These tests (in `frontend/src/services/api.contract.test.ts`) verify:
- That each API function calls the correct endpoint
- That parameters are formatted correctly in the URL
- That request payloads match the expected format

### 2. API Contract Verification Script

This script (in `frontend/scripts/verify-api-contracts.ts`):
- Compares expected backend controller endpoints with frontend endpoint constants
- Identifies any misalignments between frontend and backend
- Provides detailed output showing which endpoints are misaligned

## How to Fix Misaligned Contracts

If the tests fail, you'll need to align the API contracts. Here's how:

### When Backend Endpoints Change

1. Update the endpoint constants in `frontend/src/services/apiEndpoints.ts`
2. Update the API service functions in `frontend/src/services/api.ts`
3. Run the tests to verify alignment

### When Frontend Expectations Change

1. Communicate the changes to the backend team
2. Update the controllers in `backend/src/CryptoArbitrage.Api/Controllers/`
3. Run the tests to verify alignment

## Continuous Integration

API contract tests are automatically run in the CI/CD pipeline:
- On pull requests that affect frontend service files or backend controller files
- When manually triggered through GitHub Actions

The workflow is defined in `.github/workflows/api-contract-tests.yml`.

## Benefits of API Contract Testing

- **Catch Issues Early**: Misalignments are detected before deployment
- **Prevent Service Disruptions**: Ensures frontend and backend can communicate correctly
- **Documentation**: Tests serve as living documentation of expected API behavior
- **Confidence**: Enables teams to make changes with confidence that integration will still work 