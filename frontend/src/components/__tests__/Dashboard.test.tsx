import React from 'react';
import { render, screen, waitFor, fireEvent, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
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
  return function MockStatisticsDashboard() {
    return <div data-testid="statistics-dashboard">Statistics Dashboard</div>;
  };
});

jest.mock('../OpportunityView', () => {
  return function MockOpportunityView() {
    return <div data-testid="opportunity-view">Opportunity View</div>;
  };
});

jest.mock('../TradesList', () => {
  return function MockTradesList() {
    return <div data-testid="trades-list">Trades List</div>;
  };
});

jest.mock('../ActivityLog', () => {
  return function MockActivityLog() {
    return <div data-testid="activity-log">Activity Log</div>;
  };
});

jest.mock('../ExchangeStatus', () => {
  return function MockExchangeStatus() {
    return <div data-testid="exchange-status">Exchange Status</div>;
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

  test('renders components and makes API calls', async () => {
    await act(async () => {
      render(<Dashboard />);
    });
    
    // Wait for the data to be fetched
    await waitFor(() => {
      expect(apiService.getArbitrageStatistics).toHaveBeenCalled();
      expect(apiService.getRecentTradeResults).toHaveBeenCalled();
      expect(apiService.getServiceStatus).toHaveBeenCalled();
    });
    
    // Check that components were rendered with correct props
    expect(screen.getByTestId('statistics-dashboard')).toBeInTheDocument();
    expect(screen.getByTestId('opportunity-view')).toBeInTheDocument();
    expect(screen.getByTestId('trades-list')).toBeInTheDocument();
  });

  test('shows paper trading notice when enabled', async () => {
    await act(async () => {
      render(<Dashboard />);
    });
    
    await waitFor(() => {
      expect(screen.queryAllByRole('progressbar').length).toBe(0);
    });
    
    expect(screen.getByText(/Paper Trading Mode Enabled/i)).toBeInTheDocument();
  });

  test('handles API errors gracefully', async () => {
    // Suppress console error for this test
    const originalConsoleError = console.error;
    console.error = jest.fn();
    
    try {
      // Mock API error
      (apiService.getArbitrageStatistics as jest.Mock).mockRejectedValue(new Error('API Error'));
      
      await act(async () => {
        render(<Dashboard />);
      });
      
      await waitFor(() => {
        expect(apiService.getArbitrageStatistics).toHaveBeenCalled();
      });
      
      // Should still render components with default/empty data
      expect(screen.getByTestId('statistics-dashboard')).toBeInTheDocument();
      expect(screen.getByTestId('opportunity-view')).toBeInTheDocument();
      expect(screen.getByTestId('trades-list')).toBeInTheDocument();
    } finally {
      // Restore console.error
      console.error = originalConsoleError;
    }
  });

  test('allows stopping the bot when running', async () => {
    await act(async () => {
      render(<Dashboard />);
    });
    
    await waitFor(() => {
      expect(apiService.getServiceStatus).toHaveBeenCalled();
    });
    
    // Stop button should be visible when bot is running
    const stopButton = screen.getByText(/Stop Bot/i);
    expect(stopButton).toBeInTheDocument();
    
    // Click the button
    await act(async () => {
      fireEvent.click(stopButton);
    });
    
    // Wait for action to complete and check API was called
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
    
    await act(async () => {
      render(<Dashboard />);
    });
    
    await waitFor(() => {
      expect(apiService.getServiceStatus).toHaveBeenCalled();
    });
    
    // Start button should be visible when bot is not running
    const startButton = screen.getByText(/Start Bot/i);
    expect(startButton).toBeInTheDocument();
    
    // Click the button
    await act(async () => {
      fireEvent.click(startButton);
    });
    
    // Check API was called
    await waitFor(() => {
      expect(apiService.startArbitrageService).toHaveBeenCalled();
    });
  });

  test('handles errors when starting/stopping bot', async () => {
    // Suppress console error for this test
    const originalConsoleError = console.error;
    console.error = jest.fn();
    
    try {
      // Mock API error
      (apiService.startArbitrageService as jest.Mock).mockRejectedValue(
        new Error('Failed to start')
      );
      
      // Set bot to not running
      (apiService.getServiceStatus as jest.Mock).mockResolvedValue({ 
        isRunning: false, 
        paperTradingEnabled: true 
      });
      
      await act(async () => {
        render(<Dashboard />);
      });
      
      await waitFor(() => {
        expect(apiService.getServiceStatus).toHaveBeenCalled();
      });
      
      // Click start button
      const startButton = screen.getByText(/Start Bot/i);
      
      await act(async () => {
        fireEvent.click(startButton);
      });
      
      // Wait for error to be displayed
      await waitFor(() => {
        expect(screen.getByText(/Failed to start the arbitrage service/i)).toBeInTheDocument();
      });
    } finally {
      // Restore console.error
      console.error = originalConsoleError;
    }
  });

  it('renders the dashboard with tabs', async () => {
    await act(async () => {
      render(<Dashboard />);
    });
    
    // Check that the main title is rendered
    expect(screen.getByText('Crypto Arbitrage Dashboard')).toBeInTheDocument();
    
    // Check that tabs are rendered
    expect(screen.getByText('Overview')).toBeInTheDocument();
    expect(screen.getByText('Monitoring')).toBeInTheDocument();
    
    // Wait for loading to complete
    await waitFor(() => {
      expect(apiService.getArbitrageStatistics).toHaveBeenCalled();
    });
  });

  it('shows the overview tab content by default', async () => {
    await act(async () => {
      render(<Dashboard />);
    });
    
    // Wait for the loading to finish
    await waitFor(() => {
      expect(apiService.getArbitrageStatistics).toHaveBeenCalled();
    });
    
    // Check that the overview components are rendered
    expect(screen.getByTestId('statistics-dashboard')).toBeInTheDocument();
    expect(screen.getByTestId('opportunity-view')).toBeInTheDocument();
    expect(screen.getByTestId('trades-list')).toBeInTheDocument();
    
    // Check that monitoring components are not visible
    expect(screen.queryByTestId('activity-log')).not.toBeInTheDocument();
    expect(screen.queryByTestId('exchange-status')).not.toBeInTheDocument();
  });

  it('switches to the monitoring tab when clicked', async () => {
    await act(async () => {
      render(<Dashboard />);
    });
    
    // Wait for the loading to finish
    await waitFor(() => {
      expect(apiService.getArbitrageStatistics).toHaveBeenCalled();
    });
    
    // Find and click the monitoring tab
    const monitoringTab = screen.getByText('Monitoring');
    
    await act(async () => {
      fireEvent.click(monitoringTab);
    });
    
    // Check that monitoring components are now visible
    expect(screen.getByTestId('activity-log')).toBeInTheDocument();
    expect(screen.getByTestId('exchange-status')).toBeInTheDocument();
    
    // And overview components are no longer visible
    expect(screen.queryByTestId('statistics-dashboard')).not.toBeInTheDocument();
    expect(screen.queryByTestId('opportunity-view')).not.toBeInTheDocument();
    expect(screen.queryByTestId('trades-list')).not.toBeInTheDocument();
  });

  it('displays paper trading mode notification when enabled', async () => {
    // Ensure service status is properly mocked
    (apiService.getServiceStatus as jest.Mock).mockResolvedValue({
      isRunning: true,
      paperTradingEnabled: true
    });
    
    await act(async () => {
      render(<Dashboard />);
    });
    
    // Wait for loading to complete
    await waitFor(() => {
      expect(apiService.getServiceStatus).toHaveBeenCalled();
    });
    
    // Check for paper trading notification (using a more flexible regex)
    const notification = screen.getByText(/paper trading mode/i);
    expect(notification).toBeInTheDocument();
  });

  it('displays stop button when service is running', async () => {
    // Ensure service status is properly mocked
    (apiService.getServiceStatus as jest.Mock).mockResolvedValue({
      isRunning: true,
      paperTradingEnabled: true
    });
    
    await act(async () => {
      render(<Dashboard />);
    });
    
    // Wait for loading to complete
    await waitFor(() => {
      expect(apiService.getServiceStatus).toHaveBeenCalled();
    });
    
    // Check for stop button with more flexible matcher
    const stopButton = screen.getByRole('button', { name: /stop bot/i });
    expect(stopButton).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /start bot/i })).not.toBeInTheDocument();
  });

  it('stops the service when stop button is clicked', async () => {
    // Ensure service status is properly mocked
    (apiService.getServiceStatus as jest.Mock).mockResolvedValue({
      isRunning: true,
      paperTradingEnabled: true
    });
    
    await act(async () => {
      render(<Dashboard />);
    });
    
    // Wait for loading to complete
    await waitFor(() => {
      expect(apiService.getServiceStatus).toHaveBeenCalled();
    });
    
    // Find and click the stop button with role selector
    const stopButton = screen.getByRole('button', { name: /stop bot/i });
    
    await act(async () => {
      userEvent.click(stopButton);
    });
    
    // Check that the stopArbitrageService API was called
    await waitFor(() => {
      expect(apiService.stopArbitrageService).toHaveBeenCalled();
    });
  });

  it('handles API errors gracefully', async () => {
    // Suppress console error for this test
    const originalConsoleError = console.error;
    console.error = jest.fn();
    
    try {
      // Mock the API calls to fail 
      (apiService.getArbitrageStatistics as jest.Mock).mockRejectedValue(new Error('API Error'));
      (apiService.getRecentTradeResults as jest.Mock).mockResolvedValue([]);
      (apiService.getServiceStatus as jest.Mock).mockResolvedValue({ isRunning: true });
      
      await act(async () => {
        render(<Dashboard />);
      });
      
      // Wait for component to finish rendering after error
      await waitFor(() => {
        // The statistics component should still be rendered even on error
        expect(screen.getByTestId('statistics-dashboard')).toBeInTheDocument();
      });
      
      // Check the mock was called
      expect(apiService.getArbitrageStatistics).toHaveBeenCalled();
      
      // Verify the other dashboard components rendered
      expect(screen.getByTestId('opportunity-view')).toBeInTheDocument();
      expect(screen.getByTestId('trades-list')).toBeInTheDocument();
    } finally {
      // Restore console.error
      console.error = originalConsoleError;
    }
  });
}); 