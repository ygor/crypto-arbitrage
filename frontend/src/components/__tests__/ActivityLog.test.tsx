import React from 'react';
import { render, screen, waitFor, act, waitForElementToBeRemoved, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ActivityLog from '../ActivityLog';
import * as api from '../../services/api';
import { ActivityLogEntry, ActivityType } from '../../models/types';

// Mock the API module
jest.mock('../../services/api', () => ({
  getActivityLogs: jest.fn(),
  subscribeToActivityLogs: jest.fn()
}));

describe('ActivityLog Component', () => {
  const mockLogs: ActivityLogEntry[] = [
    {
      id: '1',
      timestamp: new Date().toISOString(),
      type: ActivityType.Info,
      message: 'Bot started successfully',
      relatedEntityType: 'System'
    },
    {
      id: '2',
      timestamp: new Date().toISOString(),
      type: ActivityType.Warning,
      message: 'API rate limit approaching',
      details: 'Current usage: 85% of limit',
      relatedEntityType: 'Exchange',
      relatedEntityId: 'binance'
    },
    {
      id: '3',
      timestamp: new Date().toISOString(),
      type: ActivityType.Error,
      message: 'Failed to execute trade',
      details: 'Insufficient funds for the transaction',
      relatedEntityType: 'Trade',
      relatedEntityId: 'trade-123'
    },
    {
      id: '4',
      timestamp: new Date().toISOString(),
      type: ActivityType.Success,
      message: 'Arbitrage opportunity executed',
      details: 'Profit: $25.50',
      relatedEntityType: 'Trade',
      relatedEntityId: 'trade-456'
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
    (api.getActivityLogs as jest.Mock).mockResolvedValue(mockLogs);
    (api.subscribeToActivityLogs as jest.Mock).mockImplementation((callback) => {
      // Store the callback so we can trigger it in tests if needed
      (mockSignalRConnection as any).callback = callback;
      return Promise.resolve(mockSignalRConnection);
    });
  });

  it('calls the API to fetch logs on mount', async () => {
    await act(async () => {
      render(<ActivityLog />);
    });
    
    expect(api.getActivityLogs).toHaveBeenCalled();
  });

  it('displays loading state initially', async () => {
    // Mock the API to return a pending promise that won't resolve during this test
    (api.getActivityLogs as jest.Mock).mockReturnValue(new Promise(() => {}));
    
    await act(async () => {
      render(<ActivityLog />);
    });
    
    // Check for loading indicator
    expect(screen.getByTestId('loading-indicator')).toBeInTheDocument();
  });

  it('shows error message when API fails', async () => {
    // Suppress console error for this test
    const originalConsoleError = console.error;
    console.error = jest.fn();
    
    try {
      // Mock API failure
      (api.getActivityLogs as jest.Mock).mockRejectedValue(new Error('API Error'));
      
      await act(async () => {
        render(<ActivityLog />);
      });
      
      expect(screen.getByText(/Failed to load activity logs/i)).toBeInTheDocument();
    } finally {
      // Restore console.error
      console.error = originalConsoleError;
    }
  });

  it('uses maxItems prop to limit number of logs requested', async () => {
    await act(async () => {
      render(<ActivityLog maxItems={20} />);
    });
    
    expect(api.getActivityLogs).toHaveBeenCalledWith(20);
  });

  it('does not setup real-time connection when autoRefresh is false', async () => {
    await act(async () => {
      render(<ActivityLog autoRefresh={false} />);
    });
    
    expect(api.getActivityLogs).toHaveBeenCalled();
    expect(api.subscribeToActivityLogs).not.toHaveBeenCalled();
  });

  it('does not show controls when showControls prop is false', async () => {
    await act(async () => {
      render(<ActivityLog showControls={false} />);
    });
    
    expect(api.getActivityLogs).toHaveBeenCalled();
    expect(screen.queryByPlaceholderText('Search logs...')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /refresh/i })).not.toBeInTheDocument();
  });

  it('sets up subscriptions when autoRefresh is true', async () => {
    await act(async () => {
      render(<ActivityLog />);
    });
    
    expect(api.subscribeToActivityLogs).toHaveBeenCalled();
  });

  it('displays logs after loading', async () => {
    await act(async () => {
      render(<ActivityLog />);
    });
    
    // Check that logs are displayed
    expect(screen.getByText('Bot started successfully')).toBeInTheDocument();
    expect(screen.getByText('API rate limit approaching')).toBeInTheDocument();
    expect(screen.getByText('Failed to execute trade')).toBeInTheDocument();
    expect(screen.getByText('Arbitrage opportunity executed')).toBeInTheDocument();
  });

  it('refreshes data when refresh button is clicked', async () => {
    // Create mock for API call
    const mockApiCall = jest.fn().mockResolvedValue(mockLogs);
    
    // Replace the implementation of the API call
    (api.getActivityLogs as jest.Mock).mockImplementation(() => mockApiCall());
    
    // Render the component and wait for initial data load
    await act(async () => {
      render(<ActivityLog />);
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
}); 