import React, { useState } from 'react';
import {
  Box,
  Button,
  FormControlLabel,
  Stack,
  Switch,
  TextField,
  Typography,
  Divider,
  Paper
} from '@mui/material';
import { ArbitrageConfig, TradingPair } from '../../models/types';

interface ArbitrageConfigFormProps {
  config: ArbitrageConfig;
  onSave: (config: ArbitrageConfig) => void;
}

const ArbitrageConfigForm: React.FC<ArbitrageConfigFormProps> = ({ config, onSave }) => {
  const [formValues, setFormValues] = useState<ArbitrageConfig>({ 
    ...config,
    tradingPairs: config.tradingPairs || []
  });
  const [newBaseCurrency, setNewBaseCurrency] = useState('');
  const [newQuoteCurrency, setNewQuoteCurrency] = useState('');
  
  // Ensure tradingPairs is always an array
  const tradingPairs = formValues.tradingPairs || [];

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, checked, type } = e.target;
    
    setFormValues(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : (type === 'number' ? parseFloat(value) : value)
    }));
  };

  const handleSelectChange = (e: any) => {
    const { name, value } = e.target;
    setFormValues(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleAddTradingPair = () => {
    if (newBaseCurrency && newQuoteCurrency) {
      const newPair = {
        baseCurrency: newBaseCurrency.toUpperCase(),
        quoteCurrency: newQuoteCurrency.toUpperCase()
      };
      
      // Check if pair already exists
      const pairExists = tradingPairs.some(
        pair => pair.baseCurrency === newPair.baseCurrency && pair.quoteCurrency === newPair.quoteCurrency
      );
      
      if (!pairExists) {
        setFormValues(prev => ({
          ...prev,
          tradingPairs: [...tradingPairs, newPair]
        }));
        setNewBaseCurrency('');
        setNewQuoteCurrency('');
      }
    }
  };

  const handleRemoveTradingPair = (index: number) => {
    setFormValues(prev => ({
      ...prev,
      tradingPairs: tradingPairs.filter((_, i) => i !== index)
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formValues);
  };

  return (
    <Paper elevation={0} sx={{ p: 2 }}>
      <form onSubmit={handleSubmit}>
        <Typography variant="h6" gutterBottom>
          Basic Configuration
        </Typography>
        
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <FormControlLabel
              control={
                <Switch
                  checked={formValues.isEnabled}
                  onChange={handleChange}
                  name="isEnabled"
                  color="primary"
                />
              }
              label="Enable Arbitrage Service"
            />
          </Box>

          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <FormControlLabel
              control={
                <Switch
                  checked={formValues.paperTradingEnabled}
                  onChange={handleChange}
                  name="paperTradingEnabled"
                  color="primary"
                />
              }
              label="Enable Paper Trading"
            />
          </Box>

          <Box sx={{ width: { xs: '100%', md: 'calc(33.333% - 16px)' } }}>
            <TextField
              fullWidth
              label="Polling Interval (ms)"
              name="pollingIntervalMs"
              type="number"
              value={formValues.pollingIntervalMs}
              onChange={handleChange}
              InputProps={{ inputProps: { min: 50, max: 10000 } }}
            />
          </Box>

          <Box sx={{ width: { xs: '100%', md: 'calc(33.333% - 16px)' } }}>
            <TextField
              fullWidth
              label="Max Concurrent Executions"
              name="maxConcurrentExecutions"
              type="number"
              value={formValues.maxConcurrentExecutions}
              onChange={handleChange}
              InputProps={{ inputProps: { min: 1, max: 20 } }}
            />
          </Box>

          <Box sx={{ width: { xs: '100%', md: 'calc(33.333% - 16px)' } }}>
            <TextField
              fullWidth
              label="Max Trade Amount"
              name="maxTradeAmount"
              type="number"
              value={formValues.maxTradeAmount}
              onChange={handleChange}
              InputProps={{ inputProps: { min: 0.01, step: 0.01 } }}
            />
          </Box>

          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <TextField
              fullWidth
              label="Minimum Profit Percentage"
              name="minimumProfitPercentage"
              type="number"
              value={formValues.minimumProfitPercentage}
              onChange={handleChange}
              InputProps={{ inputProps: { min: 0.01, step: 0.01 } }}
            />
          </Box>

          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <TextField
              fullWidth
              label="Max Execution Time (ms)"
              name="maxExecutionTimeMs"
              type="number"
              value={formValues.maxExecutionTimeMs}
              onChange={handleChange}
              InputProps={{ inputProps: { min: 100, max: 30000 } }}
            />
          </Box>

          <Box sx={{ width: '100%' }}>
            <FormControlLabel
              control={
                <Switch
                  checked={formValues.autoExecuteTrades}
                  onChange={handleChange}
                  name="autoExecuteTrades"
                  color="primary"
                />
              }
              label="Auto-Execute Trades"
            />
          </Box>
        </Box>
        
        <Divider sx={{ my: 3 }} />
        
        <Typography variant="h6" gutterBottom>
          Trading Pairs
        </Typography>
        
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems="center" sx={{ mb: 2 }}>
          <TextField
            fullWidth
            label="Base Currency"
            placeholder="BTC"
            value={newBaseCurrency}
            onChange={(e) => setNewBaseCurrency(e.target.value)}
          />
          <TextField
            fullWidth
            label="Quote Currency"
            placeholder="USDT"
            value={newQuoteCurrency}
            onChange={(e) => setNewQuoteCurrency(e.target.value)}
          />
          <Button
            variant="outlined"
            onClick={handleAddTradingPair}
            disabled={!newBaseCurrency || !newQuoteCurrency}
            sx={{ height: 56, minWidth: 150 }}
          >
            Add Trading Pair
          </Button>
        </Stack>
        
        <Box sx={{ mt: 2, mb: 3 }}>
          {tradingPairs.length > 0 ? (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
                {tradingPairs.map((pair, index) => (
                  <Box 
                    key={index}
                    sx={{ 
                      display: 'flex', 
                      justifyContent: 'space-between', 
                      alignItems: 'center',
                      p: 1,
                      border: '1px solid',
                      borderColor: 'divider',
                      borderRadius: 1,
                      minWidth: 200
                    }}
                  >
                    <Typography>
                      {pair.baseCurrency}/{pair.quoteCurrency}
                    </Typography>
                    <Button 
                      size="small" 
                      color="error" 
                      onClick={() => handleRemoveTradingPair(index)}
                    >
                      Remove
                    </Button>
                  </Box>
                ))}
              </Box>
            </Paper>
          ) : (
            <Typography color="text.secondary" align="center">
              No trading pairs added
            </Typography>
          )}
        </Box>
        
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 3 }}>
          <Button 
            type="submit" 
            variant="contained" 
            color="primary"
            size="large"
          >
            Save Configuration
          </Button>
        </Box>
      </form>
    </Paper>
  );
};

export default ArbitrageConfigForm; 