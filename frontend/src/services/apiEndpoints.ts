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
    GET_BY_ID: '/api/arbitrage/opportunities',
    GET_STATISTICS: '/api/arbitrage/statistics',
  },
  
  TRADES: {
    EXECUTE: '/api/trades/execute',
    GET_ALL: '/api/trades',
    GET_BY_ID: '/api/trades',
    GET_RECENT: '/api/trades/recent',
  },
  
  SETTINGS: {
    GET: '/api/settings',
    UPDATE: '/api/settings',
    GET_RISK_PROFILE: '/api/settings/risk-profile',
    UPDATE_RISK_PROFILE: '/api/settings/risk-profile',
    GET_EXCHANGES: '/api/settings/exchanges',
    UPDATE_EXCHANGE: '/api/settings/exchanges',
  },
  
  BOT: {
    START: '/api/bot/start',
    STOP: '/api/bot/stop',
    STATUS: '/api/bot/status',
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
  
  BOT_START: '/api/settings/bot/start',
  BOT_STOP: '/api/settings/bot/stop',
  BOT_STATUS: '/api/settings/bot/status',
}; 