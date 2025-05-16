import React, { useState } from 'react';
import {
  Box,
  Button,
  Divider,
  Paper,
  Slider,
  TextField,
  Typography
} from '@mui/material';
import { RiskProfile } from '../../models/types';

interface RiskProfileFormProps {
  riskProfile: RiskProfile;
  onSave: (riskProfile: RiskProfile) => void;
}

const RiskProfileForm: React.FC<RiskProfileFormProps> = ({ riskProfile, onSave }) => {
  const [formValues, setFormValues] = useState<RiskProfile>({
    ...riskProfile
  });

  const handleSliderChange = (name: keyof RiskProfile) => (
    event: Event,
    newValue: number | number[]
  ) => {
    if (typeof newValue === 'number') {
      setFormValues({
        ...formValues,
        [name]: newValue
      });
    }
  };

  const handleTextFieldChange = (name: keyof RiskProfile) => (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const value = parseFloat(event.target.value);
    if (!isNaN(value)) {
      setFormValues({
        ...formValues,
        [name]: value
      });
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formValues);
  };

  return (
    <Paper elevation={0} sx={{ p: 2 }}>
      <form onSubmit={handleSubmit}>
        <Typography variant="h6" gutterBottom>
          Profit & Risk Settings
        </Typography>
        
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, mb: 4 }}>
          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <Typography gutterBottom>
              Minimum Profit Percentage (%)
            </Typography>
            <Slider
              value={formValues.minimumProfitPercentage}
              onChange={handleSliderChange('minimumProfitPercentage')}
              aria-labelledby="profit-percentage-slider"
              step={0.05}
              marks
              min={0.1}
              max={5}
              valueLabelDisplay="auto"
            />
            <TextField
              value={formValues.minimumProfitPercentage}
              onChange={handleTextFieldChange('minimumProfitPercentage')}
              type="number"
              size="small"
              InputProps={{
                inputProps: {
                  min: 0.1,
                  max: 5,
                  step: 0.05
                }
              }}
              sx={{ width: 100, mt: 1 }}
            />
          </Box>
          
          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <Typography gutterBottom>
              Max Slippage Percentage (%)
            </Typography>
            <Slider
              value={formValues.maxSlippagePercentage}
              onChange={handleSliderChange('maxSlippagePercentage')}
              aria-labelledby="slippage-percentage-slider"
              step={0.05}
              marks
              min={0.1}
              max={5}
              valueLabelDisplay="auto"
            />
            <TextField
              value={formValues.maxSlippagePercentage}
              onChange={handleTextFieldChange('maxSlippagePercentage')}
              type="number"
              size="small"
              InputProps={{
                inputProps: {
                  min: 0.1,
                  max: 5,
                  step: 0.05
                }
              }}
              sx={{ width: 100, mt: 1 }}
            />
          </Box>
        </Box>
        
        <Divider sx={{ mb: 3 }} />
        <Typography variant="h6" gutterBottom>
          Capital Allocation Settings
        </Typography>
        
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, mb: 4 }}>
          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <Typography gutterBottom>
              Max Trade Amount (USDT)
            </Typography>
            <Slider
              value={formValues.maxTradeAmount}
              onChange={handleSliderChange('maxTradeAmount')}
              aria-labelledby="max-trade-amount-slider"
              step={10}
              marks
              min={10}
              max={1000}
              valueLabelDisplay="auto"
            />
            <TextField
              value={formValues.maxTradeAmount}
              onChange={handleTextFieldChange('maxTradeAmount')}
              type="number"
              size="small"
              InputProps={{
                inputProps: {
                  min: 10,
                  max: 1000,
                  step: 10
                }
              }}
              sx={{ width: 100, mt: 1 }}
            />
          </Box>
          
          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <Typography gutterBottom>
              Max Capital Utilization (%)
            </Typography>
            <Slider
              value={formValues.maxCapitalUtilizationPercentage}
              onChange={handleSliderChange('maxCapitalUtilizationPercentage')}
              aria-labelledby="capital-utilization-slider"
              step={5}
              marks
              min={5}
              max={100}
              valueLabelDisplay="auto"
            />
            <TextField
              value={formValues.maxCapitalUtilizationPercentage}
              onChange={handleTextFieldChange('maxCapitalUtilizationPercentage')}
              type="number"
              size="small"
              InputProps={{
                inputProps: {
                  min: 5,
                  max: 100,
                  step: 5
                }
              }}
              sx={{ width: 100, mt: 1 }}
            />
          </Box>
        </Box>
        
        <Divider sx={{ mb: 3 }} />
        <Typography variant="h6" gutterBottom>
          Execution Settings
        </Typography>
        
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, mb: 4 }}>
          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <Typography gutterBottom>
              Max Execution Time (ms)
            </Typography>
            <Slider
              value={formValues.maxExecutionTimeMs}
              onChange={handleSliderChange('maxExecutionTimeMs')}
              aria-labelledby="execution-time-slider"
              step={100}
              marks
              min={500}
              max={10000}
              valueLabelDisplay="auto"
            />
            <TextField
              value={formValues.maxExecutionTimeMs}
              onChange={handleTextFieldChange('maxExecutionTimeMs')}
              type="number"
              size="small"
              InputProps={{
                inputProps: {
                  min: 500,
                  max: 10000,
                  step: 100
                }
              }}
              sx={{ width: 100, mt: 1 }}
            />
          </Box>
          
          <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
            <Typography gutterBottom>
              Max Retry Attempts
            </Typography>
            <Slider
              value={formValues.maxRetryAttempts}
              onChange={handleSliderChange('maxRetryAttempts')}
              aria-labelledby="retry-attempts-slider"
              step={1}
              marks
              min={0}
              max={10}
              valueLabelDisplay="auto"
            />
            <TextField
              value={formValues.maxRetryAttempts}
              onChange={handleTextFieldChange('maxRetryAttempts')}
              type="number"
              size="small"
              InputProps={{
                inputProps: {
                  min: 0,
                  max: 10,
                  step: 1
                }
              }}
              sx={{ width: 100, mt: 1 }}
            />
          </Box>
        </Box>
        
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 3 }}>
          <Button 
            type="submit" 
            variant="contained" 
            color="primary"
            size="large"
          >
            Save Risk Profile
          </Button>
        </Box>
      </form>
    </Paper>
  );
};

export default RiskProfileForm; 