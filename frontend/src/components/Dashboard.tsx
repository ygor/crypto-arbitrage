import React, { useState, useEffect } from 'react';
import { 
  Container, 
  Typography, 
  Paper, 
  Box, 
  Stack,
  Button,
  CircularProgress
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

const Dashboard: React.FC = () => {
  const [statistics, setStatistics] = useState<ArbitrageStatistics | null>(null);
  const [recentTrades, setRecentTrades] = useState<TradeResult[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
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
    try {
      const [stats, trades, status] = await Promise.all([
        getArbitrageStatistics(),
        getRecentTradeResults(5),
        getServiceStatus()
      ]);
      
      setStatistics(stats);
      // Cast to unknown first then to TradeResult[] to avoid type mismatch
      setRecentTrades(trades as unknown as TradeResult[]);
      setServiceStatus(status);
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleStartService = async () => {
    setActionLoading(true);
    try {
      await startArbitrageService();
      await fetchData();
    } catch (error) {
      console.error('Error starting service:', error);
    } finally {
      setActionLoading(false);
    }
  };

  const handleStopService = async () => {
    setActionLoading(true);
    try {
      await stopArbitrageService();
      await fetchData();
    } catch (error) {
      console.error('Error stopping service:', error);
    } finally {
      setActionLoading(false);
    }
  };

  if (isLoading && !statistics) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" gutterBottom>
          Arbitrage Dashboard
        </Typography>
        <Box>
          <Stack direction="row" spacing={2}>
            <Box sx={{ display: 'flex', alignItems: 'center', mr: 2 }}>
              <Typography variant="body2" sx={{ mr: 1 }}>
                Status:
              </Typography>
              <Typography 
                variant="body1" 
                sx={{ 
                  fontWeight: 'bold',
                  color: serviceStatus.isRunning ? 'success.main' : 'error.main'
                }}
              >
                {serviceStatus.isRunning ? 'RUNNING' : 'STOPPED'}
              </Typography>
            </Box>
            <Box sx={{ display: 'flex', alignItems: 'center', mr: 2 }}>
              <Typography variant="body2" sx={{ mr: 1 }}>
                Mode:
              </Typography>
              <Typography 
                variant="body1" 
                sx={{ 
                  fontWeight: 'bold',
                  color: serviceStatus.paperTradingEnabled ? 'info.main' : 'warning.main'
                }}
              >
                {serviceStatus.paperTradingEnabled ? 'PAPER TRADING' : 'LIVE TRADING'}
              </Typography>
            </Box>
            <Button
              variant="contained"
              color={serviceStatus.isRunning ? "error" : "success"}
              onClick={serviceStatus.isRunning ? handleStopService : handleStartService}
              disabled={actionLoading}
            >
              {actionLoading ? (
                <CircularProgress size={24} color="inherit" />
              ) : (
                serviceStatus.isRunning ? "Stop Service" : "Start Service"
              )}
            </Button>
          </Stack>
        </Box>
      </Box>

      {statistics && (
        <Paper
          sx={{
            p: 2,
            mb: 4,
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <StatisticsDashboard statistics={statistics} />
        </Paper>
      )}

      <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 3 }}>
        <Box sx={{ flex: 7, minWidth: 0 }}>
          <Paper
            sx={{
              p: 2,
              display: 'flex',
              flexDirection: 'column',
              height: 500,
              overflow: 'auto',
              width: '100%'
            }}
          >
            <OpportunityView maxOpportunities={5} />
          </Paper>
        </Box>
        
        <Box sx={{ flex: 5, minWidth: 0 }}>
          <Paper
            sx={{
              p: 2,
              display: 'flex',
              flexDirection: 'column',
              height: 500,
              overflow: 'auto',
              width: '100%'
            }}
          >
            <Typography variant="h6" gutterBottom>
              Recent Trades
            </Typography>
            {recentTrades.length > 0 ? (
              <TradesList trades={recentTrades} />
            ) : (
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '80%' }}>
                <Typography variant="body1" color="textSecondary">
                  No recent trades found
                </Typography>
              </Box>
            )}
          </Paper>
        </Box>
      </Box>
    </Container>
  );
};

export default Dashboard; 