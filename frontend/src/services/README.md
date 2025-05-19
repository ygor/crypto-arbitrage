# API Contract Testing

This directory contains utilities for API communication and tests to ensure that the frontend correctly aligns with the backend API contract.

## Files

- `api.ts` - The main API service that contains all frontend API calls
- `apiEndpoints.ts` - Constants for all API endpoints to ensure consistency
- `api.contract.test.ts` - Unit tests to verify API endpoint alignment

## Why API Contract Testing?

API contracts define the agreement between the frontend and backend. Ensuring that these contracts remain aligned is crucial for application stability. Benefits include:

1. **Preventing Regressions**: When backend endpoints change, tests will fail, alerting developers
2. **Documentation**: The tests serve as documentation of the expected API behavior
3. **Consistency**: Using constants from `apiEndpoints.ts` ensures consistent usage across the application

## How the Tests Work

The API contract tests use Jest and axios-mock-adapter to mock HTTP requests and verify:

1. That each API function calls the correct endpoint
2. That URL parameters are correctly formatted
3. That request payloads match the expected format

## Running the Tests

Run the API contract tests with:

```bash
npm test -- src/services/api.contract.test.ts
```

## Verifying API Alignment with Backend

To verify that frontend API calls align with actual backend controllers, run:

```bash
npm run verify-api
```

This script:
1. Compares expected backend endpoints with frontend endpoint constants
2. Highlights any misalignments between frontend and backend
3. Exits with a non-zero status code if mismatches are found

## Maintaining the API Contract

When making changes to the API:

1. Update the `apiEndpoints.ts` file with any new or changed endpoints
2. Add tests in `api.contract.test.ts` for new API functions
3. Run the tests to verify that everything is aligned
4. Run `npm run verify-api` to check alignment with backend controllers

## Common Issues

- **Missing Endpoint**: If a backend endpoint exists but is not defined in the frontend, add it to `apiEndpoints.ts`
- **Incorrect Parameter Format**: Check that URL parameters and query strings match what the backend expects
- **HTTP Method Mismatch**: Ensure you're using the correct HTTP method (GET, POST, PUT, DELETE) 