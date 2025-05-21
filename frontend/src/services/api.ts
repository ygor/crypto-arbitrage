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
  TradeResult,
  ActivityLogEntry,
  ExchangeStatus,
  ArbitrageOpportunityStatus,
  TradeStatus,
  ActivityType,
  ArbitrageConfigEvaluationStrategy,
  ArbitrageConfigExecutionStrategy
} from '../models/types';
import { 
  Client, 
  ArbitrageOpportunityStatus as ClientArbitrageOpportunityStatus,
  TradeResultStatus as ClientTradeStatus,
  ActivityLogEntryType as ClientActivityType,
  ArbitrageConfigEvaluationStrategy as ClientEvaluationStrategy,
  ArbitrageConfigExecutionStrategy as ClientExecutionStrategy
} from './generated/api-client';
import { API_BASE_URL, HubEvents } from './apiEndpoints';

// Type converters for enum mappings
const convertClientArbitrageOpportunityStatus = (clientStatus?: ClientArbitrageOpportunityStatus): ArbitrageOpportunityStatus | undefined => {
  if (clientStatus === undefined) return undefined;
  return clientStatus as unknown as ArbitrageOpportunityStatus;
};

const convertClientTradeStatus = (clientStatus?: ClientTradeStatus): TradeStatus | undefined => {
  if (clientStatus === undefined) return undefined;
  return clientStatus as unknown as TradeStatus;
};

const convertClientActivityType = (clientType: ClientActivityType): ActivityType => {
  return clientType as unknown as ActivityType;
};

const convertClientEvaluationStrategy = (clientStrategy?: ClientEvaluationStrategy): ArbitrageConfigEvaluationStrategy | undefined => {
  if (clientStrategy === undefined) return undefined;
  return clientStrategy as unknown as ArbitrageConfigEvaluationStrategy;
};

const convertClientExecutionStrategy = (clientStrategy?: ClientExecutionStrategy): ArbitrageConfigExecutionStrategy | undefined => {
  if (clientStrategy === undefined) return undefined;
  return clientStrategy as unknown as ArbitrageConfigExecutionStrategy;
};

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

// Create client from OpenAPI spec
const client = new Client(BASE_URL);

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
export const getArbitrageOpportunities = async (limit = 100): Promise<ArbitrageOpportunity[]> => {
  const opportunities = await client.getArbitrageOpportunities(limit);
  return opportunities.map(opp => ({
    ...opp,
    status: convertClientArbitrageOpportunityStatus(opp.status)
  })) as ArbitrageOpportunity[];
};

// Get trade results
export const getTradeResults = async (): Promise<TradeResult[]> => {
  const trades = await client.getArbitrageTrades();
  return trades.map(trade => ({
    ...trade,
    status: convertClientTradeStatus(trade.status)
  })) as TradeResult[];
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
  return client.getArbitrageStatistics() as unknown as ArbitrageStatistics;
};

// API Functions for Opportunities
export const getRecentOpportunities = async (limit: number = 20): Promise<ArbitrageOpportunity[]> => {
  const opportunities = await client.getRecentOpportunities(limit);
  return opportunities.map(opp => ({
    ...opp,
    status: convertClientArbitrageOpportunityStatus(opp.status)
  })) as ArbitrageOpportunity[];
};

export const getOpportunitiesByTimeRange = async (
  start: string,
  end: string
): Promise<ArbitrageOpportunity[]> => {
  const opportunities = await client.getOpportunitiesByTimeRange(new Date(start), new Date(end));
  return opportunities.map(opp => ({
    ...opp,
    status: convertClientArbitrageOpportunityStatus(opp.status)
  })) as ArbitrageOpportunity[];
};

// API Functions for Trade Results
export const getRecentTradeResults = async (limit: number = 20): Promise<ArbitrageTradeResult[]> => {
  const trades = await client.getRecentTrades(limit);
  return trades.map(trade => ({
    opportunity: { id: trade.opportunityId } as ArbitrageOpportunity,
    timestamp: trade.timestamp || new Date(),
    isSuccess: trade.status === ClientTradeStatus._2, // Completed
    profitAmount: trade.profitAmount || 0,
    profitPercentage: trade.profitPercentage || 0
  })) as ArbitrageTradeResult[];
};

export const getTradeResultsByTimeRange = async (
  start: string,
  end: string
): Promise<ArbitrageTradeResult[]> => {
  const trades = await client.getTradesByTimeRange(new Date(start), new Date(end));
  return trades.map(trade => ({
    opportunity: { id: trade.opportunityId } as ArbitrageOpportunity,
    timestamp: trade.timestamp || new Date(),
    isSuccess: trade.status === ClientTradeStatus._2, // Completed
    profitAmount: trade.profitAmount || 0,
    profitPercentage: trade.profitPercentage || 0
  })) as ArbitrageTradeResult[];
};

// API Functions for Statistics
export const getStatistics = async (): Promise<ArbitrageStatistics> => {
  return client.getStatistics() as unknown as ArbitrageStatistics;
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
};

// API Functions for Configuration
export const getRiskProfile = async (): Promise<RiskProfile> => {
  return client.getRiskProfile() as unknown as RiskProfile;
};

export const updateRiskProfile = async (riskProfile: RiskProfile): Promise<RiskProfile> => {
  const result = await client.updateRiskProfile(riskProfile as any);
  return client.getRiskProfile() as unknown as RiskProfile;
};

export const getArbitrageConfig = async (): Promise<ArbitrageConfig> => {
  const config = await client.getArbitrageConfig();
  return {
    ...config,
    evaluationStrategy: convertClientEvaluationStrategy(config.evaluationStrategy),
    executionStrategy: convertClientExecutionStrategy(config.executionStrategy)
  } as ArbitrageConfig;
};

export const updateArbitrageConfig = async (config: ArbitrageConfig): Promise<ArbitrageConfig> => {
  const clientConfig = {
    ...config,
    evaluationStrategy: config.evaluationStrategy as unknown as ClientEvaluationStrategy,
    executionStrategy: config.executionStrategy as unknown as ClientExecutionStrategy
  };
  
  await client.updateArbitrageConfig(clientConfig as any);
  
  const updatedConfig = await client.getArbitrageConfig();
  return {
    ...updatedConfig,
    evaluationStrategy: convertClientEvaluationStrategy(updatedConfig.evaluationStrategy),
    executionStrategy: convertClientExecutionStrategy(updatedConfig.executionStrategy)
  } as ArbitrageConfig;
};

export const getExchangeConfigs = async (): Promise<ExchangeConfig[]> => {
  return client.getExchangeConfigurations() as unknown as ExchangeConfig[];
};

export const updateExchangeConfig = async (config: ExchangeConfig): Promise<ExchangeConfig> => {
  await client.updateExchangeConfigurations([config as any]); // The API accepts an array
  const configs = await client.getExchangeConfigurations();
  const updatedConfig = configs.find(c => c.exchangeId === config.exchangeId);
  return updatedConfig as ExchangeConfig || config;
};

export const startArbitrageService = async (): Promise<{ success: boolean, message: string }> => {
  const result = await client.startArbitrageBot();
  return { success: true, message: result.message || 'Bot started successfully' };
};

export const stopArbitrageService = async (): Promise<{ success: boolean, message: string }> => {
  const result = await client.stopArbitrageBot();
  return { success: true, message: result.message || 'Bot stopped successfully' };
};

export const getServiceStatus = async (): Promise<{ isRunning: boolean, paperTradingEnabled: boolean }> => {
  const status = await client.getBotStatus();
  // The API doesn't provide paperTrading mode, add a placeholder
  return { 
    isRunning: status.isRunning || false, 
    paperTradingEnabled: false // This will need to be added to the API spec if it's needed
  };
};

export const getActivityLogs = async (limit: number = 50): Promise<ActivityLogEntry[]> => {
  const logs = await client.getActivityLogs(limit);
  return logs.map(log => ({
    ...log,
    type: convertClientActivityType(log.type)
  })) as ActivityLogEntry[];
};

export const subscribeToActivityLogs = (
  callback: (activityLog: ActivityLogEntry) => void
): Promise<signalR.HubConnection> => {
  const connection = buildSignalRConnection("/activity");
  
  connection.on("ActivityLogReceived", (log: ActivityLogEntry) => {
    callback(log);
  });
  
  return startConnection(connection);
};

export const getExchangeStatus = async (): Promise<ExchangeStatus[]> => {
  return client.getExchangeStatus() as unknown as ExchangeStatus[];
};

export const subscribeToExchangeStatus = (
  callback: (exchangeStatus: ExchangeStatus) => void
): Promise<signalR.HubConnection> => {
  const connection = buildSignalRConnection("/exchanges");
  
  connection.on("ExchangeStatusUpdated", (status: ExchangeStatus) => {
    callback(status);
  });
  
  return startConnection(connection);
}; 