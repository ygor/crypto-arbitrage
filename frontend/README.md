# Crypto Arbitrage Frontend

This is the frontend dashboard for the Crypto Arbitrage detection and trading system. It's built with React, TypeScript, and Material-UI.

## Available Scripts

In the project directory, you can run:

### `npm start`

Runs the app in the development mode.\
Open [http://localhost:3000](http://localhost:3000) to view it in the browser.

The page will reload if you make edits.\
You will also see any lint errors in the console.

### `npm test`

Launches the test runner in the interactive watch mode.\
See the section about [running tests](https://facebook.github.io/create-react-app/docs/running-tests) for more information.

### `npm run test:components`

Runs only the component tests located in `src/components/__tests__/`.

### `npm run test:e2e`

Runs only the end-to-end tests located in `src/__tests__/`.

### `npm run test:coverage`

Generates a test coverage report to identify areas that need more testing.

### `npm run build`

Builds the app for production to the `build` folder.\
It correctly bundles React in production mode and optimizes the build for the best performance.

The build is minified and the filenames include the hashes.\
Your app is ready to be deployed!

### `npm run verify-api`

Runs the API contract verification script. This script verifies that the frontend API calls align with the backend API endpoints.

It will:
1. Compare expected backend endpoints with frontend endpoint constants
2. Highlight any misalignments between frontend and backend
3. Exit with a non-zero status code if mismatches are found

This is useful for catching API contract mismatches early in the development process.

## Features

### Dashboard Overview
The main dashboard provides a comprehensive view of the arbitrage system's performance, including:
- Statistics (profit, volume, success rates)
- Recent arbitrage opportunities
- Recent trades

### Monitoring Features
The new monitoring tab offers real-time visibility into the system's operation:

#### Activity Log
- Real-time log of bot activities and system events
- Filterable by event type (Info, Warning, Error, Success)
- Searchable content
- Expandable details for comprehensive information
- Auto-refreshing with SignalR for live updates

#### Exchange Status
- Real-time monitoring of all connected exchanges
- Visual indicators for online/offline status
- Response time metrics
- Last checked timestamps
- Auto-refreshing with SignalR for immediate status changes

## Comprehensive Testing Strategy

We've implemented a multi-layered testing approach to ensure the application's reliability:

1. **Component Tests**: Unit tests for individual React components to verify they render correctly with different prop scenarios.
2. **Integration Tests**: Tests that verify multiple components work together correctly.
3. **End-to-End Tests**: Tests that simulate real user flows across the application.
4. **API Contract Tests**: Tests that verify our frontend API calls align with the backend API endpoints.

Our tests focus on:
- Data handling and error states
- Component rendering with various data scenarios
- User interactions
- API integration
- Edge cases like null/undefined values

For detailed information on our testing approach, see the [Testing Guide](docs/TESTING.md).

## API Contract Testing

We've implemented comprehensive API contract testing to ensure alignment between the frontend and backend:

1. **Centralized Endpoint Definitions**: All API endpoints are defined in `src/services/apiEndpoints.ts`
2. **Unit Tests**: Automated tests in `src/services/api.contract.test.ts` verify correct endpoint usage
3. **Contract Verification**: The `verify-api` script checks alignment with backend controllers

For more details, see the [API Contract Testing README](src/services/README.md).

## Real-time Functionality

The application uses SignalR for real-time updates in several areas:

1. **Arbitrage Opportunities**: Live updates when new opportunities are detected
2. **Trade Results**: Immediate notification when trades are executed
3. **Activity Logs**: Real-time streaming of system activities and events
4. **Exchange Status**: Live monitoring of exchange connectivity and health

This ensures users always have the most up-to-date information without manual refreshing.

## Project Structure

- `src/components/` - React components for the UI
- `src/services/` - API and data-fetching logic
- `src/models/` - TypeScript interfaces and type definitions
- `scripts/` - Utility scripts for development and testing
- `docs/` - Project documentation
- `src/components/__tests__/` - Component tests
- `src/__tests__/` - End-to-end and integration tests

## Environment Variables

The application uses the following environment variables:

- `REACT_APP_API_URL` - URL for the backend API
- `REACT_APP_SIGNALR_URL` - URL for SignalR hubs
