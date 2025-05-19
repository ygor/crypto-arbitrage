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
import { ApiEndpoints } from './apiEndpoints';

// Get environment variables (at runtime from window.ENV or fallback to process.env)
declare global {
  interface Window {
    ENV: {
      apiUrl?: string;
      signalrUrl?: string;
    };
  }
}

// API base URL
const BASE_URL = window.ENV?.apiUrl || process.env.REACT_APP_API_URL || 'http://localhost:5001';
const SIGNALR_URL = window.ENV?.signalrUrl || process.env.REACT_APP_SIGNALR_URL || 'http://localhost:5001/hubs';

// Configure axios instance
const api = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 seconds
});

// Add a request interceptor for authentication
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('auth_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
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
  const response = await api.get(ApiEndpoints.ARBITRAGE_OPPORTUNITIES);
  return response.data;
};

// Get trade results
export const getTradeResults = async (): Promise<TradeResult[]> => {
  const response = await api.get(ApiEndpoints.ARBITRAGE_TRADES);
  return response.data;
};

// Subscribe to arbitrage opportunities via SignalR
export const subscribeToArbitrageOpportunities = (
  callback: (opportunity: ArbitrageOpportunity) => void
): Promise<signalR.HubConnection> => {
  const connection = buildSignalRConnection("/arbitrage");
  
  connection.on("ArbitrageOpportunityDetected", (opportunity: ArbitrageOpportunity) => {
    callback(opportunity);
  });
  
  return startConnection(connection);
};

// Subscribe to trade results via SignalR
export const subscribeToTradeResults = (
  callback: (tradeResult: TradeResult) => void
): Promise<signalR.HubConnection> => {
  const connection = buildSignalRConnection("/trades");
  
  connection.on("TradeCompleted", (tradeResult: TradeResult) => {
    callback(tradeResult);
  });
  
  return startConnection(connection);
};

// Get arbitrage statistics
export const getArbitrageStatistics = async (): Promise<ArbitrageStatistics> => {
  const response = await api.get(ApiEndpoints.ARBITRAGE_STATISTICS);
  return response.data;
};

// API Functions for Opportunities
export const getRecentOpportunities = async (limit: number = 20): Promise<ArbitrageOpportunity[]> => {
  const response = await api.get(`${ApiEndpoints.OPPORTUNITIES_RECENT}?limit=${limit}`);
  return response.data;
};

export const getOpportunitiesByTimeRange = async (
  start: string,
  end: string
): Promise<ArbitrageOpportunity[]> => {
  const response = await api.get(`${ApiEndpoints.OPPORTUNITIES}?start=${start}&end=${end}`);
  return response.data;
};

// API Functions for Trade Results
export const getRecentTradeResults = async (limit: number = 20): Promise<ArbitrageTradeResult[]> => {
  const response = await api.get(`${ApiEndpoints.TRADES_RECENT}?limit=${limit}`);
  return response.data;
};

export const getTradeResultsByTimeRange = async (
  start: string,
  end: string
): Promise<ArbitrageTradeResult[]> => {
  const response = await api.get(`${ApiEndpoints.TRADES}?start=${start}&end=${end}`);
  return response.data;
};

// API Functions for Statistics
export const getStatistics = async (): Promise<ArbitrageStatistics> => {
  const response = await api.get(ApiEndpoints.STATISTICS);
  return response.data;
};

export const getStatisticsByTimeRange = async (
  start: string,
  end: string
): Promise<ArbitrageStatistics> => {
  const response = await api.get(`${ApiEndpoints.STATISTICS}?start=${start}&end=${end}`);
  return response.data;
};

// API Functions for Balances
export const getBalances = async (): Promise<Record<string, Balance[]>> => {
  // TODO: This endpoint doesn't exist in the backend yet.
  // We need to either:
  // 1. Implement it in a new BalancesController
  // 2. Add it to an existing controller like ArbitrageController
  // For now, return empty object to prevent errors
  console.warn('getBalances API endpoint not implemented in backend');
  return {};
  
  // When implemented, uncomment this:
  // const response = await api.get('/balances');
  // return response.data;
};

// API Functions for Configuration
export const getRiskProfile = async (): Promise<RiskProfile> => {
  const response = await api.get(ApiEndpoints.SETTINGS_RISK_PROFILE);
  return response.data;
};

export const updateRiskProfile = async (riskProfile: RiskProfile): Promise<RiskProfile> => {
  const response = await api.put(ApiEndpoints.SETTINGS_RISK_PROFILE, riskProfile);
  return response.data;
};

export const getArbitrageConfig = async (): Promise<ArbitrageConfig> => {
  const response = await api.get(ApiEndpoints.SETTINGS_ARBITRAGE);
  return response.data;
};

export const updateArbitrageConfig = async (config: ArbitrageConfig): Promise<ArbitrageConfig> => {
  const response = await api.put(ApiEndpoints.SETTINGS_ARBITRAGE, config);
  return response.data;
};

export const getExchangeConfigs = async (): Promise<ExchangeConfig[]> => {
  const response = await api.get(ApiEndpoints.SETTINGS_EXCHANGES);
  return response.data;
};

export const updateExchangeConfig = async (config: ExchangeConfig): Promise<ExchangeConfig> => {
  const response = await api.post(ApiEndpoints.SETTINGS_EXCHANGES, [config]);
  return config;
};

// Control functions
export const startArbitrageService = async (): Promise<{ success: boolean, message: string }> => {
  const response = await api.post(ApiEndpoints.BOT_START);
  return response.data;
};

export const stopArbitrageService = async (): Promise<{ success: boolean, message: string }> => {
  const response = await api.post(ApiEndpoints.BOT_STOP);
  return response.data;
};

export const getServiceStatus = async (): Promise<{ isRunning: boolean, paperTradingEnabled: boolean }> => {
  // Get the bot running status
  const statusResponse = await api.get(ApiEndpoints.BOT_STATUS);
  
  // Get the arbitrage configuration to check if paper trading is enabled
  const configResponse = await api.get(ApiEndpoints.SETTINGS_ARBITRAGE);
  
  return {
    isRunning: statusResponse.data.isRunning,
    paperTradingEnabled: configResponse.data.paperTradingEnabled,
  };
};

export default api; 