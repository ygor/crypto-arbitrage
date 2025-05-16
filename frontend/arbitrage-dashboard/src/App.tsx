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

function App() {
  const [statistics, setStatistics] = React.useState<ArbitrageStatistics | null>(null);
  const [loading, setLoading] = React.useState<boolean>(true);

  React.useEffect(() => {
    const fetchData = async () => {
      try {
        const stats = await getArbitrageStatistics();
        setStatistics(stats);
      } catch (error) {
        console.error('Error fetching statistics:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

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
            <Route
              path="statistics"
              element={
                loading ? (
                  <div>Loading statistics...</div>
                ) : statistics ? (
                  <StatisticsDashboard statistics={statistics} />
                ) : (
                  <div>Failed to load statistics</div>
                )
              }
            />
            <Route path="settings" element={<Settings />} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App;
