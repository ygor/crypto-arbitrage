import axios from 'axios';
import * as signalR from '@microsoft/signalr';
import { 
  ArbitrageOpportunity, 
  ArbitrageTradeResult, 
  ArbitrageStatistics, 
  Balance, 
  PriceQuote,
  RiskProfile,
  ArbitrageConfig,
  ExchangeConfig,
  TradeResult
} from '../models/types';

// API base URL
const BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5001/api';
const SIGNALR_URL = process.env.REACT_APP_SIGNALR_URL || 'http://localhost:5001/hubs';

// Configure axios instance
const api = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Setup SignalR connection builder
const buildSignalRConnection = (hubUrl: string): signalR.HubConnection => {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${SIGNALR_URL}${hubUrl}`)
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();
};

// Start a SignalR connection
const startConnection = async (connection: signalR.HubConnection): Promise<signalR.HubConnection> => {
  try {
    await connection.start();
    console.log('SignalR connection established');
    return connection;
  } catch (error) {
    console.error('Error establishing SignalR connection:', error);
    throw error;
  }
};

// API functions

// Get historical arbitrage opportunities
export const getArbitrageOpportunities = async (): Promise<ArbitrageOpportunity[]> => {
  const response = await api.get('/arbitrage/opportunities');
  return response.data;
};

// Get trade results
export const getTradeResults = async (): Promise<TradeResult[]> => {
  const response = await api.get('/arbitrage/trades');
  return response.data;
};

// Subscribe to arbitrage opportunities via SignalR
export const subscribeToArbitrageOpportunities = (
  callback: (opportunity: ArbitrageOpportunity) => void
): Promise<signalR.HubConnection> => {
  const connection = buildSignalRConnection("/arbitrageHub");
  
  connection.on("ArbitrageOpportunityDetected", (opportunity: ArbitrageOpportunity) => {
    callback(opportunity);
  });
  
  return startConnection(connection);
};

// Subscribe to trade results via SignalR
export const subscribeToTradeResults = (
  callback: (tradeResult: TradeResult) => void
): Promise<signalR.HubConnection> => {
  const connection = buildSignalRConnection("/tradeHub");
  
  connection.on("TradeCompleted", (tradeResult: TradeResult) => {
    callback(tradeResult);
  });
  
  return startConnection(connection);
};

// Get arbitrage statistics
export const getArbitrageStatistics = async (): Promise<ArbitrageStatistics> => {
  const response = await api.get('/arbitrage/statistics');
  return response.data;
};

// API Functions for Opportunities
export const getRecentOpportunities = async (limit: number = 20): Promise<ArbitrageOpportunity[]> => {
  const response = await api.get(`/opportunities/recent?limit=${limit}`);
  return response.data;
};

export const getOpportunitiesByTimeRange = async (
  start: string, 
  end: string
): Promise<ArbitrageOpportunity[]> => {
  const response = await api.get(`/opportunities?start=${start}&end=${end}`);
  return response.data;
};

// API Functions for Trade Results
export const getRecentTradeResults = async (limit: number = 20): Promise<ArbitrageTradeResult[]> => {
  const response = await api.get(`/trades/recent?limit=${limit}`);
  return response.data;
};

export const getTradeResultsByTimeRange = async (
  start: string, 
  end: string
): Promise<ArbitrageTradeResult[]> => {
  const response = await api.get(`/trades?start=${start}&end=${end}`);
  return response.data;
};

// API Functions for Statistics
export const getStatistics = async (): Promise<ArbitrageStatistics> => {
  const response = await api.get('/statistics');
  return response.data;
};

export const getStatisticsByTimeRange = async (
  start: string, 
  end: string
): Promise<ArbitrageStatistics> => {
  const response = await api.get(`/statistics?start=${start}&end=${end}`);
  return response.data;
};

// API Functions for Balances
export const getBalances = async (): Promise<Record<string, Balance[]>> => {
  const response = await api.get('/balances');
  return response.data;
};

// API Functions for Configuration
export const getRiskProfile = async (): Promise<RiskProfile> => {
  const response = await api.get('/config/risk-profile');
  return response.data;
};

export const updateRiskProfile = async (riskProfile: RiskProfile): Promise<RiskProfile> => {
  const response = await api.put('/config/risk-profile', riskProfile);
  return response.data;
};

export const getArbitrageConfig = async (): Promise<ArbitrageConfig> => {
  const response = await api.get('/config/arbitrage');
  return response.data;
};

export const updateArbitrageConfig = async (config: ArbitrageConfig): Promise<ArbitrageConfig> => {
  const response = await api.put('/config/arbitrage', config);
  return response.data;
};

export const getExchangeConfigs = async (): Promise<ExchangeConfig[]> => {
  const response = await api.get('/config/exchanges');
  return response.data;
};

export const updateExchangeConfig = async (config: ExchangeConfig): Promise<ExchangeConfig> => {
  const response = await api.put(`/config/exchanges/${config.exchangeId}`, config);
  return response.data;
};

// Control functions
export const startArbitrageService = async (): Promise<{ success: boolean, message: string }> => {
  const response = await api.post('/control/start');
  return response.data;
};

export const stopArbitrageService = async (): Promise<{ success: boolean, message: string }> => {
  const response = await api.post('/control/stop');
  return response.data;
};

export const getServiceStatus = async (): Promise<{ isRunning: boolean, paperTradingEnabled: boolean }> => {
  const response = await api.get('/control/status');
  return response.data;
};

export default api; 