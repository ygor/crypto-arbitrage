import React, { useState, useEffect, useRef } from 'react';
import { 
  Paper, 
  Typography, 
  Box,
  List,
  ListItem,
  ListItemText,
  Divider,
  IconButton,
  Tooltip,
  Chip,
  CircularProgress,
  Stack,
  TextField,
  InputAdornment,
  Button
} from '@mui/material';
import { 
  Refresh as RefreshIcon, 
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
  Info as InfoIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
  CheckCircle as SuccessIcon,
  FilterList as FilterIcon,
  Clear as ClearIcon,
  Search as SearchIcon
} from '@mui/icons-material';
import { ActivityLogEntry, ActivityType } from '../models/types';
import { getActivityLogs, subscribeToActivityLogs } from '../services/api';
import { HubConnection } from '@microsoft/signalr';

interface ActivityLogProps {
  maxItems?: number;
  autoRefresh?: boolean;
  showControls?: boolean;
  height?: string | number;
}

const ActivityLog: React.FC<ActivityLogProps> = ({ 
  maxItems = 100, 
  autoRefresh = true,
  showControls = true,
  height = 400
}) => {
  const [logs, setLogs] = useState<ActivityLogEntry[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<ActivityLogEntry[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [search, setSearch] = useState<string>('');
  const [filters, setFilters] = useState<ActivityType[]>([]);
  
  const logEndRef = useRef<HTMLDivElement>(null);

  // Initialize and fetch data
  useEffect(() => {
    fetchLogs();
    
    // Set up real-time connection if autoRefresh is enabled
    if (autoRefresh) {
      const setupRealtime = async () => {
        try {
          const signalRConnection = await subscribeToActivityLogs((log) => {
            setLogs(prevLogs => {
              // Add new log to the beginning and limit the array size
              const newLogs = [log, ...prevLogs];
              return newLogs.slice(0, maxItems);
            });
          });
          
          setConnection(signalRConnection);
          console.log('Real-time activity log connection established');
        } catch (err) {
          console.warn('Real-time activity logs not available, using polling fallback:', err);
          // Don't show error to user immediately - just fall back to polling silently
          
          // Fallback to polling every 30 seconds (less frequent than before)
          const interval = setInterval(fetchLogs, 30000);
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
  
  // Apply filters when logs or search/filters change
  useEffect(() => {
    let result = [...logs];
    
    // Apply type filters
    if (filters.length > 0) {
      result = result.filter(log => filters.includes(log.type));
    }
    
    // Apply search
    if (search) {
      const searchLower = search.toLowerCase();
      result = result.filter(log => 
        log.message.toLowerCase().includes(searchLower) || 
        (log.details && log.details.toLowerCase().includes(searchLower))
      );
    }
    
    setFilteredLogs(result);
  }, [logs, search, filters]);
  
  // Scroll to bottom when new logs are added
  useEffect(() => {
    if (logEndRef.current && typeof logEndRef.current.scrollIntoView === 'function') {
      logEndRef.current.scrollIntoView({ behavior: 'smooth' });
    }
  }, [filteredLogs]);

  const fetchLogs = async () => {
    try {
      setLoading(true);
      const data = await getActivityLogs(maxItems);
      setLogs(data);
      setError(null);
    } catch (err) {
      console.error('Error fetching activity logs:', err);
      setError('Failed to load activity logs. Please try again later.');
    } finally {
      setLoading(false);
    }
  };

  const handleRefresh = () => {
    fetchLogs();
  };

  const toggleExpand = (id: string) => {
    setExpanded(prev => ({
      ...prev,
      [id]: !prev[id]
    }));
  };
  
  const handleToggleFilter = (type: ActivityType) => {
    setFilters(prevFilters => {
      if (prevFilters.includes(type)) {
        return prevFilters.filter(t => t !== type);
      } else {
        return [...prevFilters, type];
      }
    });
  };
  
  const handleClearFilters = () => {
    setFilters([]);
    setSearch('');
  };
  
  const getIconForType = (type: ActivityType) => {
    switch(type) {
      case ActivityType.Info:
        return <InfoIcon color="info" />;
      case ActivityType.Warning:
        return <WarningIcon color="warning" />;
      case ActivityType.Error:
        return <ErrorIcon color="error" />;
      case ActivityType.Success:
        return <SuccessIcon color="success" />;
      default:
        return <InfoIcon color="info" />;
    }
  };
  
  const getChipColor = (type: ActivityType) => {
    switch(type) {
      case ActivityType.Info:
        return 'info';
      case ActivityType.Warning:
        return 'warning';
      case ActivityType.Error:
        return 'error';
      case ActivityType.Success:
        return 'success';
      default:
        return 'default';
    }
  };
  
  const formatTimestamp = (timestamp: string | Date) => {
    const date = typeof timestamp === 'string' ? new Date(timestamp) : timestamp;
    return date.toLocaleTimeString() + ' ' + date.toLocaleDateString();
  };

  return (
    <Paper elevation={1} sx={{ p: 2, height: '100%' }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
        <Typography variant="h6">Activity Log</Typography>
        
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
      
      {showControls && (
        <Box mb={2}>
          <Stack direction="row" spacing={1} alignItems="center" mb={1}>
            <TextField
              placeholder="Search logs..."
              size="small"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchIcon fontSize="small" />
                  </InputAdornment>
                ),
                endAdornment: search ? (
                  <InputAdornment position="end">
                    <IconButton size="small" onClick={() => setSearch('')}>
                      <ClearIcon fontSize="small" />
                    </IconButton>
                  </InputAdornment>
                ) : null
              }}
              sx={{ flex: 1 }}
            />
            
            <Button 
              startIcon={<FilterIcon />} 
              variant="outlined" 
              size="small"
              onClick={handleClearFilters}
              disabled={filters.length === 0 && !search}
            >
              Clear
            </Button>
          </Stack>
          
          <Stack direction="row" spacing={1}>
            <Tooltip title="Toggle Info Logs">
              <Chip 
                icon={<InfoIcon />} 
                label="Info" 
                size="small"
                color={filters.includes(ActivityType.Info) ? 'info' : 'default'}
                variant={filters.includes(ActivityType.Info) ? 'filled' : 'outlined'}
                onClick={() => handleToggleFilter(ActivityType.Info)}
                clickable
              />
            </Tooltip>
            <Tooltip title="Toggle Warning Logs">
              <Chip 
                icon={<WarningIcon />} 
                label="Warning" 
                size="small"
                color={filters.includes(ActivityType.Warning) ? 'warning' : 'default'}
                variant={filters.includes(ActivityType.Warning) ? 'filled' : 'outlined'}
                onClick={() => handleToggleFilter(ActivityType.Warning)}
                clickable
              />
            </Tooltip>
            <Tooltip title="Toggle Error Logs">
              <Chip 
                icon={<ErrorIcon />} 
                label="Error" 
                size="small"
                color={filters.includes(ActivityType.Error) ? 'error' : 'default'}
                variant={filters.includes(ActivityType.Error) ? 'filled' : 'outlined'}
                onClick={() => handleToggleFilter(ActivityType.Error)}
                clickable
              />
            </Tooltip>
            <Tooltip title="Toggle Success Logs">
              <Chip 
                icon={<SuccessIcon />} 
                label="Success" 
                size="small"
                color={filters.includes(ActivityType.Success) ? 'success' : 'default'}
                variant={filters.includes(ActivityType.Success) ? 'filled' : 'outlined'}
                onClick={() => handleToggleFilter(ActivityType.Success)}
                clickable
              />
            </Tooltip>
          </Stack>
        </Box>
      )}
      
      {error && (
        <Typography color="error" variant="body2" gutterBottom>
          {error}
        </Typography>
      )}
      
      <Box 
        sx={{
          height: typeof height === 'number' ? `${height}px` : height,
          overflow: 'auto',
          bgcolor: 'background.paper',
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 1,
        }}
      >
        {loading ? (
          <Box display="flex" justifyContent="center" alignItems="center" minHeight="200px">
            <CircularProgress data-testid="loading-indicator" />
            <Typography sx={{ ml: 2 }}>Loading...</Typography>
          </Box>
        ) : filteredLogs.length === 0 ? (
          <Box display="flex" justifyContent="center" alignItems="center" height="100%" p={2}>
            <Typography variant="body2" color="text.secondary">
              No activity logs available{search || filters.length > 0 ? ' for the selected filters' : ''}
            </Typography>
          </Box>
        ) : (
          <List>
            {filteredLogs.map((log, index) => (
              <React.Fragment key={log.id}>
                <ListItem
                  alignItems="flex-start"
                  secondaryAction={
                    log.details ? (
                      <IconButton edge="end" onClick={() => toggleExpand(log.id)}>
                        {expanded[log.id] ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                      </IconButton>
                    ) : null
                  }
                >
                  <Box display="flex" alignItems="flex-start" width="100%">
                    <Box mt={0.5} mr={1}>
                      {getIconForType(log.type)}
                    </Box>
                    <ListItemText
                      primary={
                        <Box display="flex" justifyContent="space-between" alignItems="center">
                          <Typography variant="body2" component="div" fontWeight="medium">
                            {log.message}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatTimestamp(log.timestamp)}
                          </Typography>
                        </Box>
                      }
                      secondaryTypographyProps={{ component: "div" }}
                      secondary={
                        <>
                          <Box mt={1} component="div">
                            <Chip
                              label={log.type}
                              size="small"
                              color={getChipColor(log.type) as any}
                              variant="outlined"
                            />
                            {log.relatedEntityType && (
                              <Chip
                                label={log.relatedEntityType}
                                size="small"
                                sx={{ ml: 1 }}
                                variant="outlined"
                              />
                            )}
                          </Box>
                          
                          {expanded[log.id] && log.details && (
                            <Box mt={1} ml={1} p={1} bgcolor="action.hover" borderRadius={1} component="div">
                              <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap', fontSize: '0.8rem' }}>
                                {log.details}
                              </Typography>
                            </Box>
                          )}
                        </>
                      }
                    />
                  </Box>
                </ListItem>
                {index < filteredLogs.length - 1 && <Divider component="li" />}
              </React.Fragment>
            ))}
            <div ref={logEndRef} />
          </List>
        )}
      </Box>
      
      {!loading && autoRefresh && (
        <Box mt={1} display="flex" justifyContent="flex-end">
          <Typography variant="caption" color="text.secondary">
            {connection ? "Real-time updates active" : "Auto-refreshing every 10 seconds"}
          </Typography>
        </Box>
      )}
    </Paper>
  );
};

export default ActivityLog; 