import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import App from './App';
import * as apiService from './services/api';

// Mock the api service
jest.mock('./services/api', () => ({
  getArbitrageStatistics: jest.fn(),
}));

// Mock all child components for cleaner testing
jest.mock('./components/StatisticsDashboard', () => {
  return function MockStatisticsDashboard(props: any) {
    return (
      <div data-testid="stats-dashboard">
        Mock Statistics Dashboard
        <div data-testid="stats-props">{JSON.stringify(props.statistics)}</div>
      </div>
    );
  };
});

// Mock router-related components
jest.mock('react-router-dom', () => ({
  BrowserRouter: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Routes: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Route: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Navigate: () => <div>Navigate Mock</div>,
  Outlet: () => <div>Outlet Mock</div>,
}));

// Mock other components
jest.mock('./components/Layout', () => () => <div>Layout Mock</div>);
jest.mock('./components/Dashboard', () => () => <div>Dashboard Mock</div>);
jest.mock('./components/OpportunityView', () => () => <div>OpportunityView Mock</div>);
jest.mock('./components/TradesList', () => () => <div>TradesList Mock</div>);
jest.mock('./components/Settings', () => () => <div>Settings Mock</div>);

describe('App Component', () => {
  // Reset mocks between tests
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('renders without crashing', () => {
    render(<App />);
    expect(screen.getByText(/Layout Mock/i)).toBeInTheDocument();
  });

  test('fetches statistics data on mount', () => {
    const mockStats = {
      totalProfit: 1000.50,
      successfulTrades: 20,
      failedTrades: 5,
      // ... other required stats
    };
    
    (apiService.getArbitrageStatistics as jest.Mock).mockResolvedValue(mockStats);
    
    render(<App />);
    
    expect(apiService.getArbitrageStatistics).toHaveBeenCalled();
  });

  test('shows loading state while fetching statistics', () => {
    // Don't resolve the promise yet to keep app in loading state
    (apiService.getArbitrageStatistics as jest.Mock).mockImplementation(
      () => new Promise(() => {})
    );
    
    render(<App />);
    
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
  });

  test('passes statistics data to StatisticsDashboard after loading', async () => {
    const mockStats = {
      totalProfit: 1000.50,
      totalVolume: 50000.00,
      totalFees: 75.25,
      averageProfit: 100.05,
      highestProfit: 250.00,
      lowestProfit: 25.00,
      totalOpportunitiesDetected: 150,
      totalTradesExecuted: 25,
      successfulTrades: 20,
      failedTrades: 5,
      averageExecutionTimeMs: 125.5,
      profitFactor: 3.75,
      startTime: '2023-01-01T00:00:00Z',
      endTime: '2023-01-31T23:59:59Z',
    };
    
    (apiService.getArbitrageStatistics as jest.Mock).mockResolvedValue(mockStats);
    
    render(<App />);
    
    // Wait for the loading state to finish
    await waitFor(() => {
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
    });
    
    // Check that StatisticsDashboard was rendered with the correct props
    expect(screen.getByTestId('stats-dashboard')).toBeInTheDocument();
    expect(screen.getByTestId('stats-props')).toHaveTextContent(JSON.stringify(mockStats));
  });

  test('handles API errors gracefully', async () => {
    (apiService.getArbitrageStatistics as jest.Mock).mockRejectedValue(
      new Error('API Error')
    );
    
    render(<App />);
    
    // Wait for the error to be displayed
    await waitFor(() => {
      expect(screen.getByText(/Failed to load statistics data/i)).toBeInTheDocument();
    });
    
    // Make sure the component still renders StatisticsDashboard with default data
    expect(screen.getByTestId('stats-dashboard')).toBeInTheDocument();
  });
});
