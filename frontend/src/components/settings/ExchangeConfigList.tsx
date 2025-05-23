import React, { useState } from 'react';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Button,
  Chip,
  Divider,
  FormControlLabel,
  IconButton,
  Paper,
  Switch,
  TextField,
  Typography
} from '@mui/material';
import { 
  ExpandMore as ExpandMoreIcon,
  Visibility as VisibilityIcon,
  VisibilityOff as VisibilityOffIcon
} from '@mui/icons-material';
import { ExchangeConfig } from '../../models/types';

interface ExchangeConfigListProps {
  exchangeConfigs: ExchangeConfig[];
  onSave: (config: ExchangeConfig) => void;
}

const ExchangeConfigList: React.FC<ExchangeConfigListProps> = ({ exchangeConfigs, onSave }) => {
  const [expanded, setExpanded] = useState<string | false>(false);
  const [configs, setConfigs] = useState<ExchangeConfig[]>([...exchangeConfigs]);
  const [showSecrets, setShowSecrets] = useState<Record<string, boolean>>({});

  const handleChange = (panel: string) => (event: React.SyntheticEvent, isExpanded: boolean) => {
    setExpanded(isExpanded ? panel : false);
  };

  const handleToggleVisibility = (exchangeId: string) => {
    setShowSecrets(prev => ({
      ...prev,
      [exchangeId]: !prev[exchangeId]
    }));
  };

  const handleInputChange = (index: number, field: keyof ExchangeConfig, value: any) => {
    const newConfigs = [...configs];
    newConfigs[index] = {
      ...newConfigs[index],
      [field]: field === 'maxRequestsPerSecond' ? Number(value) : value
    };
    setConfigs(newConfigs);
  };

  const handleEnabledChange = (index: number, checked: boolean) => {
    const newConfigs = [...configs];
    newConfigs[index] = {
      ...newConfigs[index],
      isEnabled: checked
    };
    setConfigs(newConfigs);
  };

  const handleSaveConfig = (index: number) => {
    onSave(configs[index]);
  };

  const getStatusColor = (isEnabled: boolean) => {
    return isEnabled ? 'success' : 'error';
  };

  return (
    <Paper elevation={0} sx={{ p: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Exchange Configurations</Typography>
      </Box>
      
      <Divider sx={{ mb: 3 }} />
      
      {configs.map((config, index) => (
        <Accordion 
          key={config.exchangeId}
          expanded={expanded === config.exchangeId}
          onChange={handleChange(config.exchangeId)}
          sx={{ mb: 1 }}
        >
          <AccordionSummary
            expandIcon={<ExpandMoreIcon />}
            aria-controls={`${config.exchangeId}-content`}
            id={`${config.exchangeId}-header`}
          >
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', width: '100%', pr: 2 }}>
              <Typography sx={{ width: '40%', flexShrink: 0, textTransform: 'capitalize' }}>
                {config.name || config.exchangeId}
              </Typography>
              <Chip 
                label={config.isEnabled ? 'Enabled' : 'Disabled'}
                color={getStatusColor(config.isEnabled)}
                size="small"
              />
            </Box>
          </AccordionSummary>
          <AccordionDetails>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
              <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={config.isEnabled}
                      onChange={(e) => handleEnabledChange(index, e.target.checked)}
                      color="primary"
                    />
                  }
                  label={`Enable ${config.name || config.exchangeId}`}
                />
              </Box>
              
              <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
                <TextField
                  fullWidth
                  label="Max Requests Per Second"
                  type="number"
                  value={config.maxRequestsPerSecond}
                  onChange={(e) => handleInputChange(index, 'maxRequestsPerSecond', e.target.value)}
                  InputProps={{ inputProps: { min: 1, max: 100 } }}
                />
              </Box>
              
              <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
                <TextField
                  fullWidth
                  label="API Key"
                  value={config.apiKey || ''}
                  onChange={(e) => handleInputChange(index, 'apiKey', e.target.value)}
                  autoComplete="off"
                />
              </Box>
              
              <Box sx={{ width: { xs: '100%', md: 'calc(50% - 12px)' } }}>
                <TextField
                  fullWidth
                  label="API Secret"
                  type={showSecrets[config.exchangeId] ? 'text' : 'password'}
                  value={config.apiSecret || ''}
                  onChange={(e) => handleInputChange(index, 'apiSecret', e.target.value)}
                  autoComplete="off"
                  InputProps={{
                    endAdornment: (
                      <IconButton
                        aria-label="toggle password visibility"
                        onClick={() => handleToggleVisibility(config.exchangeId)}
                        edge="end"
                      >
                        {showSecrets[config.exchangeId] ? <VisibilityOffIcon /> : <VisibilityIcon />}
                      </IconButton>
                    ),
                  }}
                />
              </Box>
              
              <Box sx={{ width: '100%', mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
                <Button 
                  variant="contained" 
                  color="primary"
                  onClick={() => handleSaveConfig(index)}
                >
                  Save {config.name || config.exchangeId} Configuration
                </Button>
              </Box>
            </Box>
          </AccordionDetails>
        </Accordion>
      ))}
    </Paper>
  );
};

export default ExchangeConfigList; 