import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import App from '../App';
import * as apiService from '../services/api';

// Mock the API service
jest.mock('../services/api', () => ({
  getArbitrageStatistics: jest.fn(),
  getRecentTradeResults: jest.fn(),
  getServiceStatus: jest.fn(),
  startArbitrageService: jest.fn(),
  stopArbitrageService: jest.fn(),
}));

// Mock router to avoid navigation issues in tests
jest.mock('react-router-dom', () => ({
  BrowserRouter: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Routes: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  Route: (props: any) => {
    // Render the "statistics" route component directly for our test
    if (props.path === "statistics") {
      return props.element;
    }
    return null;
  },
  Navigate: () => <div>Navigate</div>,
  Outlet: () => <div>Outlet</div>,
}));

// These components should NOT be mocked for end-to-end testing
// We want to test the real StatisticsDashboard component

// Mock Layout and other unrelated components
jest.mock('../components/Layout', () => ({ children }: { children: React.ReactNode }) => (
  <div data-testid="layout">{children}</div>
));

jest.mock('../components/Dashboard', () => () => <div>Dashboard</div>);
jest.mock('../components/OpportunityView', () => () => <div>OpportunityView</div>);
jest.mock('../components/TradesList', () => () => <div>TradesList</div>);
jest.mock('../components/Settings', () => () => <div>Settings</div>);

// End-to-end test scenarios
describe('StatisticsDashboard End-to-End', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('E2E: Loads statistics and renders them correctly', async () => {
    // Mock complete stats with realistic data
    const mockCompleteStats = {
      startTime: '2023-01-01T00:00:00Z',
      endTime: '2023-01-31T23:59:59Z',
      totalProfit: 1234.56,
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
      profitFactor: 3.75
    };
    
    // Set up API mock
    (apiService.getArbitrageStatistics as jest.Mock).mockResolvedValue(mockCompleteStats);
    
    // Render the app
    await act(async () => {
      render(<App />);
    });
    
    // Verify loading state is shown
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    
    // Wait for data to load
    await waitFor(() => {
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
    });
    
    // Verify key metrics are displayed with expected values
    expect(screen.getByText('$1234.56')).toBeInTheDocument();
    expect(screen.getByText('80.0%')).toBeInTheDocument(); // Success rate
    expect(screen.getByText(/Total Profit/i)).toBeInTheDocument();
    expect(screen.getByText(/Success Rate/i)).toBeInTheDocument();
    
    // Verify dates are displayed
    const startDate = new Date('2023-01-01').toLocaleDateString();
    const endDate = new Date('2023-01-31').toLocaleDateString();
    expect(screen.getByText(new RegExp(`${startDate}.*${endDate}`))).toBeInTheDocument();
  });

  test('E2E: Handles API errors gracefully', async () => {
    // Mock API error
    (apiService.getArbitrageStatistics as jest.Mock).mockRejectedValue(
      new Error('Network error')
    );
    
    // Render the app
    await act(async () => {
      render(<App />);
    });
    
    // Wait for error message
    await waitFor(() => {
      expect(screen.getByText(/Failed to load statistics data/i)).toBeInTheDocument();
    });
    
    // Even with error, we should still see the dashboard with default values
    expect(screen.getByText(/Total Profit/i)).toBeInTheDocument();
    expect(screen.getByText('$0.00')).toBeInTheDocument();
  });

  test('E2E: Handles partial data correctly', async () => {
    // Mock partial stats with missing values
    const mockPartialStats = {
      startTime: '2023-01-01T00:00:00Z',
      endTime: '2023-01-31T23:59:59Z',
      totalProfit: 1234.56,
      // Many values missing
    };
    
    // Set up API mock
    (apiService.getArbitrageStatistics as jest.Mock).mockResolvedValue(mockPartialStats);
    
    // Render the app
    await act(async () => {
      render(<App />);
    });
    
    // Wait for data to load
    await waitFor(() => {
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
    });
    
    // Verify key metrics are displayed with expected values for available data
    expect(screen.getByText('$1234.56')).toBeInTheDocument(); // Available value
    
    // And default values for missing data
    expect(screen.getByText('0.0%')).toBeInTheDocument(); // Default success rate
  });

  test('E2E: Handles null values in statistics', async () => {
    // Mock stats with null values
    const mockNullStats = {
      startTime: '2023-01-01T00:00:00Z',
      endTime: '2023-01-31T23:59:59Z',
      totalProfit: null,
      totalVolume: null,
      totalFees: null,
      averageProfit: null,
      highestProfit: null,
      lowestProfit: null,
      totalOpportunitiesDetected: null,
      totalTradesExecuted: null,
      successfulTrades: null,
      failedTrades: null,
      averageExecutionTimeMs: null,
      profitFactor: null
    };
    
    // Set up API mock
    (apiService.getArbitrageStatistics as jest.Mock).mockResolvedValue(mockNullStats);
    
    // Render the app
    await act(async () => {
      render(<App />);
    });
    
    // Wait for data to load
    await waitFor(() => {
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
    });
    
    // Component should render with default values without crashing
    expect(screen.getByText(/Total Profit/i)).toBeInTheDocument();
    expect(screen.getByText('$0.00')).toBeInTheDocument();
  });
}); 