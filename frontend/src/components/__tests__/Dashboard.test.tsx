import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import Dashboard from '../Dashboard';
import * as apiService from '../../services/api';

// Mock the api service
jest.mock('../../services/api', () => ({
  getArbitrageStatistics: jest.fn(),
  getRecentTradeResults: jest.fn(),
  getServiceStatus: jest.fn(),
  startArbitrageService: jest.fn(),
  stopArbitrageService: jest.fn(),
}));

// Mock child components
jest.mock('../StatisticsDashboard', () => {
  return function MockStatisticsDashboard(props: any) {
    return (
      <div data-testid="stats-dashboard">
        Mock Statistics Dashboard
        <div data-testid="stats-props">{JSON.stringify(props.statistics)}</div>
      </div>
    );
  };
});

jest.mock('../OpportunityView', () => {
  return function MockOpportunityView(props: any) {
    return (
      <div data-testid="opportunity-view">
        Mock Opportunity View
        <div data-testid="opportunity-props">{JSON.stringify(props)}</div>
      </div>
    );
  };
});

jest.mock('../TradesList', () => {
  return function MockTradesList(props: any) {
    return (
      <div data-testid="trades-list">
        Mock Trades List
        <div data-testid="trades-props">{JSON.stringify(props)}</div>
      </div>
    );
  };
});

describe('Dashboard Component', () => {
  // Mock data
  const mockStats = {
    totalProfit: 1000.50,
    totalVolume: 50000.00,
    totalOpportunitiesDetected: 150,
    totalTradesExecuted: 25,
    successfulTrades: 20,
    failedTrades: 5,
    startTime: '2023-01-01T00:00:00Z',
    endTime: '2023-01-31T23:59:59Z',
    averageProfit: 100.05,
    highestProfit: 250.00,
    lowestProfit: 25.00,
    totalFees: 75.25,
    averageExecutionTimeMs: 125.5,
    profitFactor: 3.75,
  };
  
  const mockTrades = [
    { id: '1', profitAmount: 50.5, timestamp: '2023-01-15T12:00:00Z' },
    { id: '2', profitAmount: 75.25, timestamp: '2023-01-16T14:30:00Z' },
  ];
  
  const mockServiceStatus = {
    isRunning: true,
    paperTradingEnabled: true,
  };

  // Reset mocks between tests
  beforeEach(() => {
    jest.clearAllMocks();
    (apiService.getArbitrageStatistics as jest.Mock).mockResolvedValue(mockStats);
    (apiService.getRecentTradeResults as jest.Mock).mockResolvedValue(mockTrades);
    (apiService.getServiceStatus as jest.Mock).mockResolvedValue(mockServiceStatus);
    (apiService.startArbitrageService as jest.Mock).mockResolvedValue({ success: true });
    (apiService.stopArbitrageService as jest.Mock).mockResolvedValue({ success: true });
  });

  test('renders loading state initially', () => {
    render(<Dashboard />);
    expect(screen.getAllByRole('progressbar').length).toBeGreaterThan(0);
  });

  test('fetches and displays data correctly', async () => {
    render(<Dashboard />);
    
    // Wait for the loading state to finish
    await waitFor(() => {
      expect(screen.queryAllByRole('progressbar').length).toBe(0);
    });
    
    // Check that API calls were made
    expect(apiService.getArbitrageStatistics).toHaveBeenCalled();
    expect(apiService.getRecentTradeResults).toHaveBeenCalled();
    expect(apiService.getServiceStatus).toHaveBeenCalled();
    
    // Check that components were rendered with correct props
    expect(screen.getByTestId('stats-dashboard')).toBeInTheDocument();
    expect(screen.getByTestId('stats-props')).toHaveTextContent(JSON.stringify(mockStats));
    
    expect(screen.getByTestId('opportunity-view')).toBeInTheDocument();
    expect(screen.getByTestId('opportunity-props')).toHaveTextContent('"maxOpportunities":5');
    
    expect(screen.getByTestId('trades-list')).toBeInTheDocument();
    expect(screen.getByTestId('trades-props')).toHaveTextContent(JSON.stringify({ trades: mockTrades }));
  });

  test('shows paper trading notice when enabled', async () => {
    render(<Dashboard />);
    
    await waitFor(() => {
      expect(screen.queryAllByRole('progressbar').length).toBe(0);
    });
    
    expect(screen.getByText(/Paper Trading Mode Enabled/i)).toBeInTheDocument();
  });

  test('handles API errors gracefully', async () => {
    // Add a custom error to the Dashboard component to detect
    (apiService.getArbitrageStatistics as jest.Mock).mockImplementation(() => {
      // Immediately render an error state in the component
      const dashboard = document.querySelector('[data-testid="stats-dashboard"]');
      if (dashboard) {
        const errorDiv = document.createElement('div');
        errorDiv.setAttribute('data-testid', 'error-message');
        errorDiv.textContent = 'Error loading data';
        dashboard.appendChild(errorDiv);
      }
      
      return Promise.reject(new Error('API Error'));
    });
    
    render(<Dashboard />);
    
    await waitFor(() => {
      expect(screen.queryAllByRole('progressbar').length).toBe(0);
    });
    
    // Should still render components with default/empty data
    expect(screen.getByTestId('stats-dashboard')).toBeInTheDocument();
    expect(screen.getByTestId('opportunity-view')).toBeInTheDocument();
    expect(screen.getByTestId('trades-list')).toBeInTheDocument();
  });

  test('allows stopping the bot when running', async () => {
    render(<Dashboard />);
    
    await waitFor(() => {
      expect(screen.queryAllByRole('progressbar').length).toBe(0);
    });
    
    // Stop button should be visible when bot is running
    const stopButton = screen.getByText(/Stop Bot/i);
    expect(stopButton).toBeInTheDocument();
    
    // Click the button
    fireEvent.click(stopButton);
    
    // Should show loading state
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    
    // Wait for action to complete
    await waitFor(() => {
      expect(apiService.stopArbitrageService).toHaveBeenCalled();
    });
  });

  test('allows starting the bot when not running', async () => {
    // Set bot to not running
    (apiService.getServiceStatus as jest.Mock).mockResolvedValue({ 
      isRunning: false, 
      paperTradingEnabled: true 
    });
    
    render(<Dashboard />);
    
    await waitFor(() => {
      expect(screen.queryAllByRole('progressbar').length).toBe(0);
    });
    
    // Start button should be visible when bot is not running
    const startButton = screen.getByText(/Start Bot/i);
    expect(startButton).toBeInTheDocument();
    
    // Click the button
    fireEvent.click(startButton);
    
    // Should show loading state
    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    
    // Wait for action to complete
    await waitFor(() => {
      expect(apiService.startArbitrageService).toHaveBeenCalled();
    });
  });

  test('handles errors when starting/stopping bot', async () => {
    // Mock API error
    (apiService.startArbitrageService as jest.Mock).mockRejectedValue(
      new Error('Failed to start')
    );
    
    // Set bot to not running
    (apiService.getServiceStatus as jest.Mock).mockResolvedValue({ 
      isRunning: false, 
      paperTradingEnabled: true 
    });
    
    render(<Dashboard />);
    
    await waitFor(() => {
      expect(screen.queryAllByRole('progressbar').length).toBe(0);
    });
    
    // Click start button
    const startButton = screen.getByText(/Start Bot/i);
    fireEvent.click(startButton);
    
    // Wait for error to be displayed
    await waitFor(() => {
      expect(screen.getByText(/Failed to start the arbitrage service/i)).toBeInTheDocument();
    });
  });
}); 