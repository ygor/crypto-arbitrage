/**
 * API Endpoints
 * 
 * This file contains all the API endpoint URLs used by the frontend.
 * Keeping these in a single file makes it easier to ensure consistency
 * between frontend expectations and backend implementations.
 */

export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: '/api/auth/login',
    LOGOUT: '/api/auth/logout',
    REGISTER: '/api/auth/register',
    REFRESH_TOKEN: '/api/auth/refresh-token',
    RESET_PASSWORD: '/api/auth/reset-password',
  },
  
  MARKETS: {
    GET_ALL: '/api/markets',
    GET_BY_ID: '/api/markets',
  },
  
  ARBITRAGE: {
    GET_OPPORTUNITIES: '/api/arbitrage/opportunities',
    GET_TRADES: '/api/arbitrage/trades',
    GET_STATISTICS: '/api/arbitrage/statistics',
  },
  
  OPPORTUNITIES: {
    GET_RECENT: '/api/opportunities/recent',
    GET_BY_TIMERANGE: '/api/opportunities',
  },
  
  TRADES: {
    GET_RECENT: '/api/trades/recent',
    GET_BY_TIMERANGE: '/api/trades',
  },
  
  STATISTICS: {
    GET: '/api/statistics',
  },
  
  SETTINGS: {
    GET: '/api/settings',
    UPDATE: '/api/settings',
    RISK_PROFILE: '/api/settings/risk-profile',
    UPDATE_RISK_PROFILE: '/api/settings/risk-profile',
    ARBITRAGE: '/api/settings/arbitrage',
    EXCHANGES: '/api/settings/exchanges',
    UPDATE_EXCHANGE: '/api/settings/exchanges',
  },
  
  BOT: {
    START: '/api/bot/start',
    STOP: '/api/bot/stop',
    STATUS: '/api/bot/status',
    ACTIVITY_LOGS: '/api/bot/activity-logs',
    EXCHANGE_STATUS: '/api/bot/exchange-status',
  },
};

// Legacy endpoints - for backwards compatibility
export const ApiEndpoints = {
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