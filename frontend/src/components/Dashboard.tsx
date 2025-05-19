import React, { useState, useEffect } from 'react';
import { 
  Container, 
  Typography, 
  Paper, 
  Box, 
  Stack,
  Button,
  CircularProgress,
  Alert
} from '@mui/material';
import StatisticsDashboard from './StatisticsDashboard';
import OpportunityView from './OpportunityView';
import TradesList from './TradesList';
import { 
  getArbitrageStatistics, 
  getRecentTradeResults, 
  getServiceStatus,
  startArbitrageService,
  stopArbitrageService
} from '../services/api';
import { ArbitrageStatistics, TradeResult } from '../models/types';

// Default empty statistics object
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

const Dashboard: React.FC = () => {
  const [statistics, setStatistics] = useState<ArbitrageStatistics>(emptyStatistics);
  const [recentTrades, setRecentTrades] = useState<TradeResult[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [serviceStatus, setServiceStatus] = useState<{ isRunning: boolean, paperTradingEnabled: boolean }>({ 
    isRunning: false, 
    paperTradingEnabled: false 
  });
  const [actionLoading, setActionLoading] = useState<boolean>(false);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 30000); // Refresh every 30 seconds
    
    return () => clearInterval(interval);
  }, []);

  const fetchData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [stats, trades, status] = await Promise.all([
        getArbitrageStatistics().catch(err => {
          console.error('Error fetching statistics:', err);
          return emptyStatistics;
        }),
        getRecentTradeResults(5).catch(err => {
          console.error('Error fetching trades:', err);
          return [];
        }),
        getServiceStatus().catch(err => {
          console.error('Error fetching service status:', err);
          return { isRunning: false, paperTradingEnabled: false };
        })
      ]);
      
      setStatistics(stats);
      // Cast to unknown first then to TradeResult[] to avoid type mismatch
      setRecentTrades(trades as unknown as TradeResult[]);
      setServiceStatus(status);
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
      setError('Failed to load some dashboard data. Showing available information.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleStartService = async () => {
    setActionLoading(true);
    try {
      const response = await startArbitrageService();
      if (response.success) {
        // Service started, update status
        setServiceStatus(prev => ({ ...prev, isRunning: true }));
      }
    } catch (error) {
      console.error('Error starting service:', error);
      setError('Failed to start the arbitrage service.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleStopService = async () => {
    setActionLoading(true);
    try {
      const response = await stopArbitrageService();
      if (response.success) {
        // Service stopped, update status
        setServiceStatus(prev => ({ ...prev, isRunning: false }));
      }
    } catch (error) {
      console.error('Error stopping service:', error);
      setError('Failed to stop the arbitrage service.');
    } finally {
      setActionLoading(false);
    }
  };

  return (
    <Container maxWidth="xl">
      <Box sx={{ mt: 4, mb: 2 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Typography variant="h4" component="h1" gutterBottom>
            Crypto Arbitrage Dashboard
          </Typography>
          <Stack direction="row" spacing={2}>
            {serviceStatus.isRunning ? (
              <Button 
                variant="contained" 
                color="error"
                onClick={handleStopService}
                disabled={actionLoading}
              >
                {actionLoading ? <CircularProgress size={24} /> : 'Stop Bot'}
              </Button>
            ) : (
              <Button 
                variant="contained" 
                color="success"
                onClick={handleStartService}
                disabled={actionLoading}
              >
                {actionLoading ? <CircularProgress size={24} /> : 'Start Bot'}
              </Button>
            )}
          </Stack>
        </Stack>
        
        {serviceStatus.paperTradingEnabled && (
          <Paper elevation={0} sx={{ p: 1, background: '#fff9c4', mb: 2 }}>
            <Typography variant="body2">
              🔔 Paper Trading Mode Enabled: No real trades will be executed.
            </Typography>
          </Paper>
        )}
        
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        
        <Paper elevation={1} sx={{ p: 3, mb: 4 }}>
          <Typography variant="h6" gutterBottom>
            Statistics Overview
          </Typography>
          {isLoading ? (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
              <CircularProgress />
            </Box>
          ) : (
            <StatisticsDashboard statistics={statistics} />
          )}
        </Paper>
        
        <Stack 
          direction={{ xs: 'column', md: 'row' }} 
          spacing={4}
          sx={{ mb: 4 }}
        >
          <Paper elevation={1} sx={{ p: 3, flex: 1 }}>
            <Typography variant="h6" gutterBottom>
              Recent Opportunities
            </Typography>
            {isLoading ? (
              <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
                <CircularProgress />
              </Box>
            ) : (
              <OpportunityView maxOpportunities={5} />
            )}
          </Paper>
          
          <Paper elevation={1} sx={{ p: 3, flex: 1 }}>
            <Typography variant="h6" gutterBottom>
              Recent Trades
            </Typography>
            {isLoading ? (
              <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
                <CircularProgress />
              </Box>
            ) : (
              <TradesList trades={recentTrades} />
            )}
          </Paper>
        </Stack>
      </Box>
    </Container>
  );
};

export default Dashboard; 