import React from 'react';
import { 
  Paper, Typography, Box, Card, CardContent,
  Divider, Stack
} from '@mui/material';
import {
  BarChart, Bar, LineChart, Line, XAxis, YAxis, CartesianGrid, 
  Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell
} from 'recharts';
import { ArbitrageStatistics } from '../models/types';

interface StatisticsDashboardProps {
  statistics: ArbitrageStatistics;
}

const COLORS = ['#4caf50', '#f44336', '#2196f3', '#ff9800', '#9c27b0'];

// Default statistics to use if any property is undefined
const defaultStats: ArbitrageStatistics = {
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
  profitFactor: 0,
  startTime: new Date().toISOString(),
  endTime: new Date().toISOString()
};

// Helper function to safely format numbers with toFixed
const safeToFixed = (value: any, decimals: number = 2): string => {
  if (value === undefined || value === null || isNaN(Number(value))) {
    return (0).toFixed(decimals);
  }
  return Number(value).toFixed(decimals);
};

const StatisticsDashboard: React.FC<StatisticsDashboardProps> = ({ statistics }) => {
  // Create a safe version of statistics with defaults for missing properties
  const safeStats = {
    ...defaultStats,
    ...Object.fromEntries(
      // Filter out any undefined or null values from statistics
      Object.entries(statistics || {}).filter(([_, v]) => v !== undefined && v !== null)
    )
  };

  const successRateData = [
    { name: 'Success', value: safeStats.successfulTrades },
    { name: 'Failed', value: safeStats.failedTrades }
  ];

  // Generate data for profit over time chart
  const profitChartData = [
    { name: 'Day 1', profit: (safeStats.totalProfit || 0) * 0.2 },
    { name: 'Day 2', profit: (safeStats.totalProfit || 0) * 0.35 },
    { name: 'Day 3', profit: (safeStats.totalProfit || 0) * 0.5 },
    { name: 'Day 4', profit: (safeStats.totalProfit || 0) * 0.75 },
    { name: 'Day 5', profit: (safeStats.totalProfit || 0) }
  ];

  // Calculate some derived metrics
  const totalTrades = (safeStats.successfulTrades || 0) + (safeStats.failedTrades || 0);
  const successRate = totalTrades > 0 ? ((safeStats.successfulTrades || 0) / totalTrades) * 100 : 0;
  const avgProfitPerTrade = (safeStats.successfulTrades || 0) > 0 ? (safeStats.totalProfit || 0) / (safeStats.successfulTrades || 0) : 0;
  
  return (
    <Box sx={{ flexGrow: 1, mt: 3 }}>
      <Typography variant="h5" gutterBottom>Arbitrage Statistics Dashboard</Typography>
      <Typography variant="subtitle1" gutterBottom color="text.secondary">
        {new Date(safeStats.startTime || new Date()).toLocaleDateString()} - {new Date(safeStats.endTime || new Date()).toLocaleDateString()}
      </Typography>
      
      {/* Key Metrics */}
      <Stack 
        direction={{ xs: 'column', sm: 'row' }} 
        spacing={2} 
        sx={{ mb: 4 }}
        justifyContent="space-between"
      >
        <MetricCard 
          title="Total Profit" 
          value={`$${safeToFixed(safeStats.totalProfit)}`}
          subtitle={`Avg: $${safeToFixed(avgProfitPerTrade)} per trade`}
          color="#4caf50"
        />
        <MetricCard 
          title="Opportunities" 
          value={(safeStats.totalOpportunitiesDetected || 0).toString()}
          subtitle={`${safeStats.totalTradesExecuted || 0} executed`}
          color="#2196f3"
        />
        <MetricCard 
          title="Success Rate" 
          value={`${safeToFixed(successRate, 1)}%`}
          subtitle={`${safeStats.successfulTrades || 0}/${totalTrades} trades`}
          color={successRate > 75 ? '#4caf50' : successRate > 50 ? '#ff9800' : '#f44336'}
        />
        <MetricCard 
          title="Trading Volume" 
          value={`$${safeToFixed(safeStats.totalVolume)}`}
          subtitle={`Fees: $${safeToFixed(safeStats.totalFees)}`}
          color="#9c27b0"
        />
      </Stack>
      
      {/* Charts */}
      <Stack spacing={3}>
        {/* Charts first row */}
        <Stack 
          direction={{ xs: 'column', md: 'row' }} 
          spacing={3}
        >
          {/* Success Rate Pie Chart */}
          <Paper elevation={2} sx={{ p: 2, height: '100%', flex: 1 }}>
            <Typography variant="h6" gutterBottom>Trade Results</Typography>
            <Box sx={{ height: 300 }}>
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    dataKey="value"
                    data={successRateData}
                    cx="50%"
                    cy="50%"
                    outerRadius={80}
                    label={({ name, percent }) => `${name}: ${safeToFixed(percent * 100, 0)}%`}
                  >
                    {successRateData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={index === 0 ? '#4caf50' : '#f44336'} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value) => [`${value} trades`, '']} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </Box>
          </Paper>
          
          {/* Profit Over Time Line Chart */}
          <Paper elevation={2} sx={{ p: 2, flex: 2 }}>
            <Typography variant="h6" gutterBottom>Profit Over Time</Typography>
            <Box sx={{ height: 300 }}>
              <ResponsiveContainer width="100%" height="100%">
                <LineChart
                  data={profitChartData}
                  margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
                >
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" />
                  <YAxis label={{ value: 'Profit ($)', angle: -90, position: 'insideLeft' }} />
                  <Tooltip formatter={(value) => [`$${safeToFixed(Number(value))}`, 'Profit']} />
                  <Legend />
                  <Line type="monotone" dataKey="profit" stroke="#4caf50" activeDot={{ r: 8 }} />
                </LineChart>
              </ResponsiveContainer>
            </Box>
          </Paper>
        </Stack>
        
        {/* Profit Metrics */}
        <Paper elevation={2} sx={{ p: 2 }}>
          <Typography variant="h6" gutterBottom>Profit Metrics</Typography>
          <Box sx={{ height: 300 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart
                data={[
                  { name: 'Average Profit', value: safeStats.averageProfit },
                  { name: 'Highest Profit', value: safeStats.highestProfit },
                  { name: 'Lowest Profit', value: safeStats.lowestProfit }
                ]}
                margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
              >
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis label={{ value: 'Amount ($)', angle: -90, position: 'insideLeft' }} />
                <Tooltip formatter={(value) => [`$${safeToFixed(Number(value))}`, '']} />
                <Legend />
                <Bar dataKey="value" fill="#2196f3">
                  {[
                    <Cell key="cell-0" fill="#2196f3" />,
                    <Cell key="cell-1" fill="#4caf50" />,
                    <Cell key="cell-2" fill={(safeStats.lowestProfit || 0) < 0 ? '#f44336' : '#ff9800'} />
                  ]}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </Box>
        </Paper>
      </Stack>
      
      {/* Additional Statistics */}
      <Paper elevation={2} sx={{ p: 2, mt: 3 }}>
        <Typography variant="h6" gutterBottom>Performance Metrics</Typography>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={2}
          justifyContent="space-between"
          divider={<Divider orientation="vertical" flexItem />}
        >
          <Box sx={{ flex: 1 }}>
            <Box sx={{ py: 1 }}>
              <Typography variant="subtitle2" color="text.secondary">Average Execution Time</Typography>
              <Typography variant="h6">{safeToFixed(safeStats.averageExecutionTimeMs)} ms</Typography>
            </Box>
            <Divider />
            <Box sx={{ py: 1 }}>
              <Typography variant="subtitle2" color="text.secondary">Profit Factor</Typography>
              <Typography variant="h6">{safeToFixed(safeStats.profitFactor)}</Typography>
            </Box>
          </Box>
          
          <Box sx={{ flex: 1 }}>
            <Box sx={{ py: 1 }}>
              <Typography variant="subtitle2" color="text.secondary">Total Opportunities</Typography>
              <Typography variant="h6">{safeStats.totalOpportunitiesDetected}</Typography>
            </Box>
            <Divider />
            <Box sx={{ py: 1 }}>
              <Typography variant="subtitle2" color="text.secondary">Total Trades</Typography>
              <Typography variant="h6">{safeStats.totalTradesExecuted}</Typography>
            </Box>
          </Box>
          
          <Box sx={{ flex: 1 }}>
            <Box sx={{ py: 1 }}>
              <Typography variant="subtitle2" color="text.secondary">Successful Trades</Typography>
              <Typography variant="h6">{safeStats.successfulTrades}</Typography>
            </Box>
            <Divider />
            <Box sx={{ py: 1 }}>
              <Typography variant="subtitle2" color="text.secondary">Failed Trades</Typography>
              <Typography variant="h6">{safeStats.failedTrades}</Typography>
            </Box>
          </Box>
        </Stack>
      </Paper>
    </Box>
  );
};

interface MetricCardProps {
  title: string;
  value: string;
  subtitle?: string;
  color?: string;
}

const MetricCard: React.FC<MetricCardProps> = ({ title, value, subtitle, color = '#000' }) => {
  return (
    <Card elevation={2} sx={{ flex: 1, minWidth: { xs: '100%', sm: 0 } }}>
      <CardContent>
        <Typography variant="subtitle1" color="text.secondary" gutterBottom>
          {title}
        </Typography>
        <Typography variant="h4" sx={{ color }}>
          {value}
        </Typography>
        {subtitle && (
          <Typography variant="body2" color="text.secondary">
            {subtitle}
          </Typography>
        )}
      </CardContent>
    </Card>
  );
};

export default StatisticsDashboard; 