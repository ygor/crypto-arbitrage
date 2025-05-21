import React from 'react';
import { render, screen, waitFor, act, waitForElementToBeRemoved, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ExchangeStatus from '../ExchangeStatus';
import * as api from '../../services/api';
import { ExchangeStatus as ExchangeStatusType } from '../../models/types';

// Mock the API module
jest.mock('../../services/api', () => ({
  getExchangeStatus: jest.fn(),
  subscribeToExchangeStatus: jest.fn()
}));

describe('ExchangeStatus Component', () => {
  const mockExchanges: ExchangeStatusType[] = [
    {
      exchangeId: 'binance',
      exchangeName: 'Binance',
      isUp: true,
      lastChecked: new Date().toISOString(),
      responseTimeMs: 150
    },
    {
      exchangeId: 'coinbase',
      exchangeName: 'Coinbase',
      isUp: false,
      lastChecked: new Date().toISOString(),
      responseTimeMs: 250,
      additionalInfo: 'API rate limit reached'
    }
  ];

  // Mock SignalR connection
  const mockSignalRConnection = {
    stop: jest.fn(),
    on: jest.fn(),
    start: jest.fn().mockResolvedValue(undefined)
  };

  beforeEach(() => {
    jest.clearAllMocks();
    
    // Mock the API responses
    (api.getExchangeStatus as jest.Mock).mockResolvedValue(mockExchanges);
    (api.subscribeToExchangeStatus as jest.Mock).mockImplementation((callback) => {
      // Store the callback so we can trigger it in tests if needed
      (mockSignalRConnection as any).callback = callback;
      return Promise.resolve(mockSignalRConnection);
    });
  });

  it('calls the API to fetch exchange status on mount', async () => {
    await act(async () => {
      render(<ExchangeStatus />);
    });
    
    expect(api.getExchangeStatus).toHaveBeenCalled();
  });

  it('displays loading state initially', async () => {
    // Mock the API to return a pending promise that won't resolve during this test
    (api.getExchangeStatus as jest.Mock).mockReturnValue(new Promise(() => {}));
    
    await act(async () => {
      render(<ExchangeStatus />);
    });
    
    // Loading state should be visible immediately after render
    expect(screen.getByTestId('loading-indicator')).toBeInTheDocument();
  });

  it('renders exchange status cards when data is loaded', async () => {
    await act(async () => {
      render(<ExchangeStatus />);
    });
    
    // Check if exchange names are displayed
    expect(screen.getByText('Binance')).toBeInTheDocument();
    expect(screen.getByText('Coinbase')).toBeInTheDocument();
    
    // Check status display
    expect(screen.getAllByText('Online')[0]).toBeInTheDocument();
    expect(screen.getByText('Offline')).toBeInTheDocument();
    
    // Check response time
    expect(screen.getByText('150 ms')).toBeInTheDocument();
    expect(screen.getByText('250 ms')).toBeInTheDocument();
  });

  it('shows error message when API fails', async () => {
    // Suppress console error for this test
    const originalConsoleError = console.error;
    console.error = jest.fn();
    
    try {
      // Mock API failure
      (api.getExchangeStatus as jest.Mock).mockRejectedValue(new Error('API Error'));
      
      await act(async () => {
        render(<ExchangeStatus />);
      });
      
      expect(screen.getByText(/Failed to load exchange status/i)).toBeInTheDocument();
    } finally {
      // Restore console.error
      console.error = originalConsoleError;
    }
  });

  it('refreshes data when refresh button is clicked', async () => {
    // Create mock for API call
    const mockApiCall = jest.fn().mockResolvedValue(mockExchanges);
    
    // Replace the implementation of the API call
    (api.getExchangeStatus as jest.Mock).mockImplementation(() => mockApiCall());
    
    // Render the component and wait for initial data load
    await act(async () => {
      render(<ExchangeStatus />);
    });

    await waitFor(() => expect(screen.queryByTestId('loading-indicator')).not.toBeInTheDocument());
    
    // Clear the mock to reset call count after initial render
    mockApiCall.mockClear();
    
    // Find the refresh button using the RefreshIcon data-testid
    const refreshIcon = screen.getByTestId('RefreshIcon');
    const refreshButton = refreshIcon.closest('button');
    
    // Make sure we found the button
    expect(refreshButton).not.toBeNull();
    
    // Click the refresh button wrapped in act
    await act(async () => {
      if (refreshButton) {
        fireEvent.click(refreshButton);
      }
    });
    
    // Verify the API was called
    expect(mockApiCall).toHaveBeenCalled();
  });

  it('does not show controls when showControls prop is false', async () => {
    await act(async () => {
      render(<ExchangeStatus showControls={false} />);
    });
    
    expect(api.getExchangeStatus).toHaveBeenCalled();
    expect(screen.queryByRole('button', { name: /refresh/i })).not.toBeInTheDocument();
  });

  it('does not setup real-time connection when autoRefresh is false', async () => {
    await act(async () => {
      render(<ExchangeStatus autoRefresh={false} />);
    });
    
    expect(api.getExchangeStatus).toHaveBeenCalled();
    expect(api.subscribeToExchangeStatus).not.toHaveBeenCalled();
  });

  it('sets up subscriptions when autoRefresh is true', async () => {
    await act(async () => {
      render(<ExchangeStatus />);
    });
    
    expect(api.subscribeToExchangeStatus).toHaveBeenCalled();
  });

  it('displays empty state when no exchanges are available', async () => {
    // Mock empty response
    (api.getExchangeStatus as jest.Mock).mockResolvedValue([]);
    
    await act(async () => {
      render(<ExchangeStatus />);
    });
    
    // Check for empty state message
    expect(screen.getByText(/No exchange status information available/i)).toBeInTheDocument();
  });
}); 