import React, { useState, useEffect } from 'react';
import { 
  Paper, 
  Typography, 
  Box, 
  Grid, 
  CircularProgress,
  Tooltip,
  IconButton,
  Chip,
  Card,
  CardContent,
  Divider
} from '@mui/material';
import { 
  Refresh as RefreshIcon,
  CheckCircle as CheckCircleIcon, 
  Warning as WarningIcon,
  Error as ErrorIcon,
  AccessTime as TimeIcon
} from '@mui/icons-material';
import { ExchangeStatus as ExchangeStatusType } from '../models/types';
import { getExchangeStatus, subscribeToExchangeStatus } from '../services/api';
import { HubConnection } from '@microsoft/signalr';

interface ExchangeStatusProps {
  autoRefresh?: boolean;
  showControls?: boolean;
}

const ExchangeStatus: React.FC<ExchangeStatusProps> = ({ 
  autoRefresh = true,
  showControls = true 
}) => {
  const [exchanges, setExchanges] = useState<ExchangeStatusType[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [connection, setConnection] = useState<HubConnection | null>(null);

  // Initialize and fetch data
  useEffect(() => {
    fetchExchangeStatus();
    
    // Set up real-time connection if autoRefresh is enabled
    if (autoRefresh) {
      const setupRealtime = async () => {
        try {
          const signalRConnection = await subscribeToExchangeStatus((status) => {
            setExchanges(prevExchanges => {
              // Find and update the exchange status if it exists, otherwise add it
              const index = prevExchanges.findIndex(e => e.exchangeId === status.exchangeId);
              if (index >= 0) {
                const newExchanges = [...prevExchanges];
                newExchanges[index] = status;
                return newExchanges;
              } else {
                return [...prevExchanges, status];
              }
            });
          });
          
          setConnection(signalRConnection);
        } catch (err) {
          console.error('Failed to connect to real-time exchange status:', err);
          setError('Failed to establish real-time connection. Using polling instead.');
          
          // Fallback to polling
          const interval = setInterval(fetchExchangeStatus, 30000);
          return () => clearInterval(interval);
        }
      };
      
      setupRealtime();
    }
    
    return () => {
      // Clean up SignalR connection when component unmounts
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  const fetchExchangeStatus = async () => {
    try {
      setLoading(true);
      const data = await getExchangeStatus();
      setExchanges(data);
      setError(null);
    } catch (err) {
      console.error('Error fetching exchange status:', err);
      setError('Failed to load exchange status. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const handleRefresh = () => {
    fetchExchangeStatus();
  };
  
  const getStatusColor = (isUp: boolean, lastChecked: string | Date) => {
    if (!isUp) {
      return 'error';
    }
    
    // Check if last checked time is more than 5 minutes ago
    const lastCheckedTime = typeof lastChecked === 'string' ? new Date(lastChecked).getTime() : lastChecked.getTime();
    const fiveMinutesAgo = Date.now() - 5 * 60 * 1000;
    
    if (lastCheckedTime < fiveMinutesAgo) {
      return 'warning';
    }
    
    return 'success';
  };
  
  const getStatusIcon = (isUp: boolean, lastChecked: string | Date) => {
    const statusColor = getStatusColor(isUp, lastChecked);
    
    switch (statusColor) {
      case 'success':
        return <CheckCircleIcon color="success" />;
      case 'warning':
        return <WarningIcon color="warning" />;
      case 'error':
        return <ErrorIcon color="error" />;
      default:
        return <TimeIcon color="info" />;
    }
  };
  
  const formatTimestamp = (timestamp: string | Date) => {
    const date = typeof timestamp === 'string' ? new Date(timestamp) : timestamp;
    return date.toLocaleTimeString() + ' ' + date.toLocaleDateString();
  };
  
  const getTimeSince = (timestamp: string | Date) => {
    const lastCheckedTime = typeof timestamp === 'string' ? new Date(timestamp).getTime() : timestamp.getTime();
    const now = Date.now();
    const diffInSeconds = Math.floor((now - lastCheckedTime) / 1000);
    
    if (diffInSeconds < 60) {
      return `${diffInSeconds} sec ago`;
    } else if (diffInSeconds < 3600) {
      return `${Math.floor(diffInSeconds / 60)} min ago`;
    } else if (diffInSeconds < 86400) {
      return `${Math.floor(diffInSeconds / 3600)} hr ago`;
    } else {
      return `${Math.floor(diffInSeconds / 86400)} days ago`;
    }
  };

  return (
    <Paper elevation={1} sx={{ p: 2, height: '100%' }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
        <Typography variant="h6">Exchange Status</Typography>
        
        {showControls && (
          <Box>
            <Tooltip title="Refresh">
              <span>
                <IconButton onClick={handleRefresh} disabled={loading}>
                  <RefreshIcon />
                </IconButton>
              </span>
            </Tooltip>
          </Box>
        )}
      </Box>
      
      {error && (
        <Typography color="error" variant="body2" gutterBottom>
          {error}
        </Typography>
      )}
      
      {loading ? (
        <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
          <CircularProgress data-testid="loading-indicator" />
          <Typography sx={{ ml: 2 }}>Loading...</Typography>
        </Box>
      ) : exchanges.length === 0 ? (
        <Box display="flex" justifyContent="center" alignItems="center" height="200px">
          <Typography variant="body2" color="text.secondary">
            No exchange status information available
          </Typography>
        </Box>
      ) : (
        <Grid container spacing={2}>
          {exchanges.map((exchange) => (
            <Grid item xs={12} sm={6} md={4} key={exchange.exchangeId}>
              <Card variant="outlined">
                <CardContent>
                  <Box display="flex" justifyContent="space-between" alignItems="center">
                    <Typography variant="h6" component="div">
                      {exchange.exchangeName}
                    </Typography>
                    <Tooltip title={exchange.isUp ? 'Online' : 'Offline'}>
                      {getStatusIcon(exchange.isUp, exchange.lastChecked)}
                    </Tooltip>
                  </Box>
                  
                  <Divider sx={{ my: 1.5 }} />
                  
                  <Box mt={1}>
                    <Grid container spacing={1}>
                      <Grid item xs={6}>
                        <Typography variant="body2" color="text.secondary">
                          Status:
                        </Typography>
                      </Grid>
                      <Grid item xs={6}>
                        <Chip 
                          label={exchange.isUp ? 'Online' : 'Offline'} 
                          size="small" 
                          color={exchange.isUp ? 'success' : 'error'}
                          variant="outlined"
                        />
                      </Grid>
                      
                      <Grid item xs={6}>
                        <Typography variant="body2" color="text.secondary">
                          Last Checked:
                        </Typography>
                      </Grid>
                      <Grid item xs={6}>
                        <Tooltip title={formatTimestamp(exchange.lastChecked)}>
                          <Typography variant="body2">
                            {getTimeSince(exchange.lastChecked)}
                          </Typography>
                        </Tooltip>
                      </Grid>
                      
                      <Grid item xs={6}>
                        <Typography variant="body2" color="text.secondary">
                          API Response Time:
                        </Typography>
                      </Grid>
                      <Grid item xs={6}>
                        <Typography variant="body2">
                          {exchange.responseTimeMs} ms
                        </Typography>
                      </Grid>
                    </Grid>
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
      
      {!loading && autoRefresh && (
        <Box mt={2} display="flex" justifyContent="flex-end">
          <Typography variant="caption" color="text.secondary">
            {connection ? "Real-time updates active" : "Auto-refreshing every 30 seconds"}
          </Typography>
        </Box>
      )}
    </Paper>
  );
};

export default ExchangeStatus; 