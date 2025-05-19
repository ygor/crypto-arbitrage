# Frontend Testing Guide

This document provides guidance on how we test the Crypto Arbitrage frontend application to ensure reliability and prevent bugs.

## Testing Strategy

Our testing approach consists of several layers:

1. **Component Tests**: Unit tests for individual React components to verify they render correctly with different props.
2. **Integration Tests**: Tests that verify multiple components work together correctly.
3. **End-to-End Tests**: Tests that simulate real user flows across the application.
4. **API Contract Tests**: Tests that verify our frontend API calls align with the backend API endpoints.

## Running Tests

We have several npm scripts to run different types of tests:

```bash
# Run all tests in watch mode (development)
npm test

# Run all tests once (CI)
npm run test:ci

# Run component tests only
npm run test:components

# Run end-to-end tests only
npm run test:e2e

# Run API contract tests
npm run test:api-contracts

# Verify API contracts against backend
npm run verify-api

# Generate test coverage report
npm run test:coverage
```

## Test File Organization

Tests are organized as follows:

```
src/
├── components/
│   ├── __tests__/            # Component tests
│   │   ├── Component.test.tsx
│   │   └── ...
├── __tests__/                # E2E and integration tests
│   ├── Component.e2e.test.tsx
│   └── ...
└── services/
    ├── api.contract.test.ts  # API contract tests
    └── ...
```

## Writing Component Tests

When writing tests for a component, focus on these key aspects:

### 1. Test Component Rendering with Different Props

Test how the component renders with different sets of props, including:
- Complete data
- Partial data
- Empty data
- Invalid/unexpected data

Example:

```tsx
// Test with complete data
test('renders correctly with complete data', () => {
  render(<MyComponent data={completeData} />);
  expect(screen.getByText('Expected Text')).toBeInTheDocument();
});

// Test with partial data
test('renders correctly with partial data', () => {
  render(<MyComponent data={partialData} />);
  expect(screen.getByText('Default Value')).toBeInTheDocument();
});
```

### 2. Test User Interactions

Test how the component responds to user interactions:

```tsx
test('handles button click', () => {
  const onClickMock = jest.fn();
  render(<Button onClick={onClickMock}>Click Me</Button>);
  
  fireEvent.click(screen.getByText('Click Me'));
  expect(onClickMock).toHaveBeenCalledTimes(1);
});
```

### 3. Test Edge Cases

Test scenarios that might cause errors:

```tsx
test('handles undefined values without crashing', () => {
  render(<MyComponent data={undefined} />);
  // Expect no errors and fallback content
  expect(screen.getByText('No Data Available')).toBeInTheDocument();
});
```

## Testing Components that Use API Data

For components that rely on API data:

1. Mock the API services:

```tsx
jest.mock('../../services/api', () => ({
  getArbitrageStatistics: jest.fn(),
}));

// In your test
(apiService.getArbitrageStatistics as jest.Mock).mockResolvedValue(mockData);
```

2. Test loading states:

```tsx
test('shows loading state', () => {
  // Set up API mock that doesn't resolve immediately
  (apiService.getArbitrageStatistics as jest.Mock).mockImplementation(
    () => new Promise(() => {})
  );
  
  render(<MyComponent />);
  expect(screen.getByRole('progressbar')).toBeInTheDocument();
});
```

3. Test error handling:

```tsx
test('handles API errors', async () => {
  (apiService.getArbitrageStatistics as jest.Mock).mockRejectedValue(
    new Error('API Error')
  );
  
  render(<MyComponent />);
  
  await waitFor(() => {
    expect(screen.getByText(/Error loading data/i)).toBeInTheDocument();
  });
});
```

## Testing Components with Charts

When testing components with chart libraries like Recharts:

1. Mock the chart components:

```tsx
jest.mock('recharts', () => {
  const OriginalModule = jest.requireActual('recharts');
  
  return {
    ...OriginalModule,
    ResponsiveContainer: ({ children }: any) => <div>{children}</div>,
    LineChart: ({ children }: any) => <div data-testid="line-chart">{children}</div>,
    // ... mock other chart components
  };
});
```

2. Focus on testing the data being passed to charts rather than the visual output:

```tsx
test('passes correct data to chart', () => {
  render(<ChartComponent data={mockData} />);
  
  // Verify chart is rendered
  expect(screen.getByTestId('line-chart')).toBeInTheDocument();
  
  // If needed, check data transformations by testing the component's output
  expect(screen.getByText('$100.00')).toBeInTheDocument();
});
```

## Best Practices

1. **Test Behavior, Not Implementation**: Focus on what the component does, not how it does it.

2. **Use Descriptive Test Names**: Make test names describe the scenario being tested.

3. **Setup and Cleanup**: Use `beforeEach` and `afterEach` for common test setup and cleanup.

4. **Mock External Dependencies**: Always mock API calls and external services.

5. **Test Error States**: Always test how components handle errors and edge cases.

6. **Use Screen Queries Effectively**: Prefer user-centric queries like `getByRole` and `getByText` over implementation details like `getByTestId`.

7. **Snapshot Testing**: Use snapshots sparingly for UI regression testing.

## Troubleshooting Common Test Issues

### Tests Timing Out

If your tests are timing out, you might be missing an `await` or `waitFor` for asynchronous operations:

```tsx
// Instead of this:
render(<MyComponent />);
expect(screen.getByText('Loaded Data')).toBeInTheDocument(); // May fail if data loads async

// Do this:
render(<MyComponent />);
await waitFor(() => {
  expect(screen.getByText('Loaded Data')).toBeInTheDocument();
});
```

### Mock Not Working

If your mock isn't being applied:

1. Check that the mock is at the top level, not inside a test.
2. Verify the import path exactly matches the one used in the component.
3. Use `jest.clearAllMocks()` in `beforeEach` to reset mock state between tests.

### Element Not Found

If `getByText` or similar queries fail:

1. Use `screen.debug()` to see what's actually in the DOM.
2. Try less specific queries like `queryByText(/partial text/i)`.
3. Check if the element might be inside a portal or custom container.

## Resources

- [React Testing Library Docs](https://testing-library.com/docs/react-testing-library/intro/)
- [Jest Documentation](https://jestjs.io/docs/getting-started)
- [Common Testing Patterns](https://kentcdodds.com/blog/common-mistakes-with-react-testing-library) 