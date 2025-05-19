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

## API Contract Testing

We've implemented comprehensive API contract testing to ensure alignment between the frontend and backend:

1. **Centralized Endpoint Definitions**: All API endpoints are defined in `src/services/apiEndpoints.ts`
2. **Unit Tests**: Automated tests in `src/services/api.contract.test.ts` verify correct endpoint usage
3. **Contract Verification**: The `verify-api` script checks alignment with backend controllers

For more details, see the [API Contract Testing README](src/services/README.md).

## Project Structure

- `src/components/` - React components for the UI
- `src/services/` - API and data-fetching logic
- `src/models/` - TypeScript interfaces and type definitions
- `scripts/` - Utility scripts for development and testing

## Environment Variables

The application uses the following environment variables:

- `REACT_APP_API_URL` - URL for the backend API
- `REACT_APP_SIGNALR_URL` - URL for SignalR hubs
