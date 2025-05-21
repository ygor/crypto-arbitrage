/**
 * API Endpoints
 * 
 * This file contains all the API endpoint URLs used by the frontend.
 * Keeping these in a single file makes it easier to ensure consistency
 * between frontend expectations and backend implementations.
 */

// API Endpoints for the Crypto Arbitrage application
// These endpoints are used for API requests

// Base API URL (should match what's configured in the api-client.ts)
export const API_BASE_URL = 'http://localhost:5001';

export const ApiEndpoints = {
  // Arbitrage Opportunities
  ARBITRAGE_OPPORTUNITIES: `${API_BASE_URL}/api/arbitrage/opportunities`,
  RECENT_OPPORTUNITIES: `${API_BASE_URL}/api/opportunities/recent`,
  OPPORTUNITIES_BY_TIMERANGE: `${API_BASE_URL}/api/opportunities`,

  // Trades
  ARBITRAGE_TRADES: `${API_BASE_URL}/api/arbitrage/trades`,
  RECENT_TRADES: `${API_BASE_URL}/api/trades/recent`,
  TRADES_BY_TIMERANGE: `${API_BASE_URL}/api/trades`,

  // Statistics
  ARBITRAGE_STATISTICS: `${API_BASE_URL}/api/arbitrage/statistics`, 
  STATISTICS: `${API_BASE_URL}/api/statistics`,

  // Settings
  RISK_PROFILE: `${API_BASE_URL}/api/settings/risk-profile`,
  ARBITRAGE_CONFIG: `${API_BASE_URL}/api/settings/arbitrage-config`,
  EXCHANGE_CONFIGS: `${API_BASE_URL}/api/settings/exchanges`,

  // Bot Control
  BOT_START: `${API_BASE_URL}/api/bot/start`,
  BOT_STOP: `${API_BASE_URL}/api/bot/stop`,
  BOT_STATUS: `${API_BASE_URL}/api/bot/status`,

  // Logs and Monitoring
  ACTIVITY_LOGS: `${API_BASE_URL}/api/logs/activity`,
  EXCHANGE_STATUS: `${API_BASE_URL}/api/health/exchanges`,
  
  // Real-time Hub URLs
  ARBITRAGE_HUB: `${API_BASE_URL}/hubs/arbitrage`,
  TRADES_HUB: `${API_BASE_URL}/hubs/trades`,
  ACTIVITY_HUB: `${API_BASE_URL}/hubs/activity`,
  HEALTH_HUB: `${API_BASE_URL}/hubs/health`
};

// Hub event names for SignalR
export const HubEvents = {
  OPPORTUNITY_DETECTED: 'ArbitrageOpportunityDetected',
  TRADE_COMPLETED: 'TradeCompleted',
  LOG_ENTRY_ADDED: 'LogEntryAdded',
  EXCHANGE_STATUS_UPDATED: 'ExchangeStatusUpdated'
};

// Legacy endpoints - for backwards compatibility
export const ApiEndpointsLegacy = {
  ARBITRAGE_OPPORTUNITIES: '/api/arbitrage/opportunities',
  ARBITRAGE_TRADES: '/api/arbitrage/trades',
  ARBITRAGE_STATISTICS: '/api/arbitrage/statistics',
  
  OPPORTUNITIES_RECENT: '/api/opportunities/recent',
  OPPORTUNITIES: '/api/opportunities',
  
  TRADES_RECENT: '/api/trades/recent',
  TRADES: '/api/trades',
  
  STATISTICS: '/api/statistics',
  
  SETTINGS_RISK_PROFILE: '/api/settings/risk-profile',
  SETTINGS_ARBITRAGE: '/api/settings/arbitrage',
  SETTINGS_EXCHANGES: '/api/settings/exchanges',
  
  BOT_START: '/api/bot/start',
  BOT_STOP: '/api/bot/stop',
  BOT_STATUS: '/api/bot/status',
  BOT_ACTIVITY_LOGS: '/api/bot/activity-logs',
  BOT_EXCHANGE_STATUS: '/api/bot/exchange-status',
}; 