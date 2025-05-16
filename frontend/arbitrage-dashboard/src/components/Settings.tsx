import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  Tabs,
  Tab,
  CircularProgress,
  Container,
  Snackbar,
  Alert
} from '@mui/material';
import { 
  getArbitrageConfig, 
  getRiskProfile, 
  getExchangeConfigs, 
  updateArbitrageConfig, 
  updateRiskProfile, 
  updateExchangeConfig 
} from '../services/api';
import { ArbitrageConfig, ExchangeConfig, RiskProfile } from '../models/types';
import ArbitrageConfigForm from './settings/ArbitrageConfigForm';
import RiskProfileForm from './settings/RiskProfileForm';
import ExchangeConfigList from './settings/ExchangeConfigList';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`settings-tabpanel-${index}`}
      aria-labelledby={`settings-tab-${index}`}
      {...other}
    >
      {value === index && (
        <Box sx={{ p: 3 }}>
          {children}
        </Box>
      )}
    </div>
  );
}

function a11yProps(index: number) {
  return {
    id: `settings-tab-${index}`,
    'aria-controls': `settings-tabpanel-${index}`,
  };
}

const Settings: React.FC = () => {
  const [tabValue, setTabValue] = useState(0);
  const [loading, setLoading] = useState(true);
  const [arbitrageConfig, setArbitrageConfig] = useState<ArbitrageConfig | null>(null);
  const [riskProfile, setRiskProfile] = useState<RiskProfile | null>(null);
  const [exchangeConfigs, setExchangeConfigs] = useState<ExchangeConfig[]>([]);
  const [alertOpen, setAlertOpen] = useState(false);
  const [alertMessage, setAlertMessage] = useState('');
  const [alertSeverity, setAlertSeverity] = useState<'success' | 'error'>('success');

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      try {
        const [configData, riskData, exchangesData] = await Promise.all([
          getArbitrageConfig(),
          getRiskProfile(),
          getExchangeConfigs()
        ]);
        
        setArbitrageConfig(configData);
        setRiskProfile(riskData);
        setExchangeConfigs(exchangesData);
      } catch (error) {
        console.error('Error fetching settings:', error);
        showAlert('Failed to load settings', 'error');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const handleArbitrageConfigSave = async (updatedConfig: ArbitrageConfig) => {
    try {
      const result = await updateArbitrageConfig(updatedConfig);
      setArbitrageConfig(result);
      showAlert('Arbitrage configuration saved successfully', 'success');
    } catch (error) {
      console.error('Error saving arbitrage config:', error);
      showAlert('Failed to save arbitrage configuration', 'error');
    }
  };

  const handleRiskProfileSave = async (updatedProfile: RiskProfile) => {
    try {
      const result = await updateRiskProfile(updatedProfile);
      setRiskProfile(result);
      showAlert('Risk profile saved successfully', 'success');
    } catch (error) {
      console.error('Error saving risk profile:', error);
      showAlert('Failed to save risk profile', 'error');
    }
  };

  const handleExchangeConfigSave = async (updatedConfig: ExchangeConfig) => {
    try {
      const result = await updateExchangeConfig(updatedConfig);
      setExchangeConfigs(prev => 
        prev.map(config => config.exchangeId === result.exchangeId ? result : config)
      );
      showAlert('Exchange configuration saved successfully', 'success');
    } catch (error) {
      console.error('Error saving exchange config:', error);
      showAlert('Failed to save exchange configuration', 'error');
    }
  };

  const showAlert = (message: string, severity: 'success' | 'error') => {
    setAlertMessage(message);
    setAlertSeverity(severity);
    setAlertOpen(true);
  };

  const handleAlertClose = () => {
    setAlertOpen(false);
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
      <Paper sx={{ 
        p: 2, 
        display: 'flex', 
        flexDirection: 'column', 
        mb: 4 
      }}>
        <Typography variant="h4" gutterBottom>
          Settings
        </Typography>

        <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
          <Tabs 
            value={tabValue} 
            onChange={handleTabChange} 
            aria-label="settings tabs"
            variant="scrollable"
            scrollButtons="auto"
          >
            <Tab label="Arbitrage Configuration" {...a11yProps(0)} />
            <Tab label="Risk Profile" {...a11yProps(1)} />
            <Tab label="Exchanges" {...a11yProps(2)} />
          </Tabs>
        </Box>

        <TabPanel value={tabValue} index={0}>
          {arbitrageConfig && (
            <ArbitrageConfigForm 
              config={arbitrageConfig} 
              onSave={handleArbitrageConfigSave} 
            />
          )}
        </TabPanel>

        <TabPanel value={tabValue} index={1}>
          {riskProfile && (
            <RiskProfileForm 
              riskProfile={riskProfile} 
              onSave={handleRiskProfileSave} 
            />
          )}
        </TabPanel>

        <TabPanel value={tabValue} index={2}>
          <ExchangeConfigList 
            exchangeConfigs={exchangeConfigs} 
            onSave={handleExchangeConfigSave} 
          />
        </TabPanel>
      </Paper>

      <Snackbar open={alertOpen} autoHideDuration={6000} onClose={handleAlertClose}>
        <Alert onClose={handleAlertClose} severity={alertSeverity} sx={{ width: '100%' }}>
          {alertMessage}
        </Alert>
      </Snackbar>
    </Container>
  );
};

export default Settings; 