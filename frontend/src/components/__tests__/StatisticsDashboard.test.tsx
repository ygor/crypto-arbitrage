import React from 'react';
import { render, screen } from '@testing-library/react';
import StatisticsDashboard from '../StatisticsDashboard';
import { ArbitrageStatistics } from '../../models/types';

// Mock recharts components to avoid SVG rendering issues in tests
jest.mock('recharts', () => {
  const OriginalModule = jest.requireActual('recharts');
  
  return {
    ...OriginalModule,
    ResponsiveContainer: ({ children }: any) => <div>{children}</div>,
    LineChart: ({ children }: any) => <div data-testid="line-chart">{children}</div>,
    BarChart: ({ children }: any) => <div data-testid="bar-chart">{children}</div>,
    PieChart: ({ children }: any) => <div data-testid="pie-chart">{children}</div>,
    Line: () => <div data-testid="line" />,
    Bar: () => <div data-testid="bar" />,
    Pie: () => <div data-testid="pie" />,
    XAxis: () => <div data-testid="x-axis" />,
    YAxis: () => <div data-testid="y-axis" />,
    CartesianGrid: () => <div data-testid="cartesian-grid" />,
    Tooltip: () => <div data-testid="tooltip" />,
    Legend: () => <div data-testid="legend" />,
    Cell: () => <div data-testid="cell" />,
  };
});

// Sample complete statistics data for testing
const mockCompleteStats: ArbitrageStatistics = {
  startTime: '2023-01-01T00:00:00Z',
  endTime: '2023-01-31T23:59:59Z',
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
  profitFactor: 3.75
};

// Sample partial statistics with some missing data
const mockPartialStats: Partial<ArbitrageStatistics> = {
  totalProfit: 500.25,
  totalVolume: 20000.00,
  successfulTrades: 10,
  failedTrades: 2
  // Many fields are missing
};

// Empty/minimal statistics
const mockEmptyStats: Partial<ArbitrageStatistics> = {
  // Only required fields with minimal values
  startTime: new Date().toISOString(),
  endTime: new Date().toISOString(),
  totalProfit: 0,
  totalVolume: 0,
  totalFees: 0,
  averageProfit: 0,
  highestProfit: 0,
  lowestProfit: 0,
  totalOpportunitiesDetected: 0,
  totalTradesExecuted: 0,
  successfulTrades: 0,
  failedTrades: 0,
  averageExecutionTimeMs: 0,
  profitFactor: 0
};

// Extreme values for testing boundary conditions
const mockExtremeStats: ArbitrageStatistics = {
  ...mockCompleteStats,
  totalProfit: 9999999.99,
  averageExecutionTimeMs: 0.001,
  profitFactor: 9999.99
};

describe('StatisticsDashboard Component', () => {
  // Test 1: Component renders with complete data
  test('renders correctly with complete statistics data', () => {
    render(<StatisticsDashboard statistics={mockCompleteStats} />);
    
    // Check for key metrics
    expect(screen.getByText(/Total Profit/i)).toBeInTheDocument();
    expect(screen.getByText('$1000.50')).toBeInTheDocument();
    expect(screen.getByText(/Success Rate/i)).toBeInTheDocument();
    expect(screen.getByText('80.0%')).toBeInTheDocument(); // 20 successful out of 25 = 80%
  });

  // Test 2: Component handles partial data gracefully
  test('renders correctly with partial statistics data', () => {
    // Cast partial stats to full type as the component expects the full type
    render(<StatisticsDashboard statistics={mockPartialStats as ArbitrageStatistics} />);
    
    // Check that it displays the available data
    expect(screen.getByText(/Total Profit/i)).toBeInTheDocument();
    expect(screen.getByText('$500.25')).toBeInTheDocument();
    
    // Check that it gracefully handles missing data with defaults
    // This test would have failed before our fix with TypeError
    expect(screen.getByText('0.00 ms')).toBeInTheDocument(); // Default for averageExecutionTimeMs
  });

  // Test 3: Component handles empty/minimal data
  test('renders correctly with empty statistics data', () => {
    render(<StatisticsDashboard statistics={mockEmptyStats as ArbitrageStatistics} />);
    
    // Verify no errors and defaults are shown
    expect(screen.getByText(/Total Profit/i)).toBeInTheDocument();
    // Use getAllByText since there are multiple elements with $0.00 (total profit, average profit, etc.)
    const zeroAmountElements = screen.getAllByText('$0.00');
    expect(zeroAmountElements.length).toBeGreaterThan(0);
    expect(screen.getByText('0.0%')).toBeInTheDocument(); // Default success rate
  });

  // Test 4: Component handles undefined values without crashing
  test('handles undefined statistics values without errors', () => {
    // Create a minimal valid stats object, then add undefined values
    const undefinedStats = {
      startTime: '2023-01-01T00:00:00Z',
      endTime: '2023-01-31T23:59:59Z',
      totalProfit: undefined,
      totalVolume: 0,
      totalFees: 0,
      averageProfit: 0,
      highestProfit: 0,
      lowestProfit: 0,
      totalOpportunitiesDetected: 0,
      totalTradesExecuted: 0,
      successfulTrades: 0,
      failedTrades: 0,
      averageExecutionTimeMs: undefined,
      profitFactor: 0
    };
    
    // This would have crashed before our fix
    render(<StatisticsDashboard statistics={undefinedStats as any} />);
    
    // Check that it renders without errors
    expect(screen.getByText(/Total Profit/i)).toBeInTheDocument();
    // Check for default values for undefined props
    const zeroAmountElements = screen.getAllByText('$0.00');
    expect(zeroAmountElements.length).toBeGreaterThan(0);
  });

  // Test 5: Component handles extreme values
  test('renders correctly with extreme statistics values', () => {
    render(<StatisticsDashboard statistics={mockExtremeStats} />);
    
    // Check that extreme values are displayed correctly
    expect(screen.getByText('$9999999.99')).toBeInTheDocument();
    expect(screen.getByText('0.00 ms')).toBeInTheDocument();
  });

  // Test 6: Creates snapshot for regression testing
  test('matches snapshot', () => {
    const { asFragment } = render(<StatisticsDashboard statistics={mockCompleteStats} />);
    expect(asFragment()).toMatchSnapshot();
  });
}); 