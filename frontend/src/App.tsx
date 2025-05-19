import React from 'react';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Dashboard from './components/Dashboard';
import OpportunityView from './components/OpportunityView';
import TradesList from './components/TradesList';
import StatisticsDashboard from './components/StatisticsDashboard';
import Settings from './components/Settings';
import { getArbitrageStatistics } from './services/api';
import { ArbitrageStatistics } from './models/types';
import { CircularProgress, Box, Alert } from '@mui/material';

// Create a theme
const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
  },
});

// Default empty statistics object to use when API call fails or data is loading
const emptyStatistics: ArbitrageStatistics = {
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

function App() {
  const [statistics, setStatistics] = React.useState<ArbitrageStatistics>(emptyStatistics);
  const [loading, setLoading] = React.useState<boolean>(true);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);
        const stats = await getArbitrageStatistics();
        setStatistics(stats);
      } catch (error) {
        console.error('Error fetching statistics:', error);
        setError('Failed to load statistics data. Please try again later.');
        // Still use empty statistics to prevent errors
        setStatistics(emptyStatistics);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const renderStatistics = () => {
    if (loading) {
      return (
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="300px">
          <CircularProgress />
        </Box>
      );
    }
    
    if (error) {
      return (
        <Box mb={3}>
          <Alert severity="error">{error}</Alert>
          <StatisticsDashboard statistics={statistics} />
        </Box>
      );
    }
    
    return <StatisticsDashboard statistics={statistics} />;
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<Dashboard />} />
            <Route path="dashboard" element={<Dashboard />} />
            <Route path="opportunities" element={<OpportunityView />} />
            <Route path="trades" element={<TradesList trades={[]} />} />
            <Route path="statistics" element={renderStatistics()} />
            <Route path="settings" element={<Settings />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
