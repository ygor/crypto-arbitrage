// Mock axios
jest.mock('axios');

import axios from 'axios';
import * as api from './api';
import { ApiEndpoints } from './apiEndpoints';
import { Client } from './generated/api-client';

// Mock the generated client
jest.mock('./generated/api-client', () => {
  const mockClient = {
    getArbitrageOpportunities: jest.fn(),
    getArbitrageTrades: jest.fn(),
    getArbitrageStatistics: jest.fn(),
    getRecentOpportunities: jest.fn(),
    getRecentTrades: jest.fn(),
    startArbitrageBot: jest.fn(),
    stopArbitrageBot: jest.fn(),
    getBotStatus: jest.fn()
  };
  
  return {
    Client: jest.fn(() => mockClient),
    mockClient // Export the mock client for direct access
  };
});

// Get the mocked client directly
const { mockClient } = require('./generated/api-client');

describe('API Services Contract Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Arbitrage Opportunities', () => {
    test('getArbitrageOpportunities should call the correct client method', async () => {
      const responseData = [{ id: 1, profitPercentage: 1.5 }];
      mockClient.getArbitrageOpportunities.mockResolvedValueOnce(responseData);
      
      const result = await api.getArbitrageOpportunities();
      
      expect(mockClient.getArbitrageOpportunities).toHaveBeenCalledWith(100);
      expect(result).toEqual(expect.any(Array));
    });
  });

  describe('Trade Results', () => {
    test('getTradeResults should call the correct client method', async () => {
      const responseData = [{ id: 1, status: 2 }];
      mockClient.getArbitrageTrades.mockResolvedValueOnce(responseData);
      
      const result = await api.getTradeResults();
      
      expect(mockClient.getArbitrageTrades).toHaveBeenCalled();
      expect(result).toEqual(expect.any(Array));
    });
  });

  describe('Statistics', () => {
    test('getArbitrageStatistics should call the correct client method', async () => {
      const responseData = { totalProfit: 100, successRate: 95 };
      mockClient.getArbitrageStatistics.mockResolvedValueOnce(responseData);
      
      const result = await api.getArbitrageStatistics();
      
      expect(mockClient.getArbitrageStatistics).toHaveBeenCalled();
      expect(result).toHaveProperty('totalProfit');
    });

    test('getArbitrageStatistics should handle errors', async () => {
      mockClient.getArbitrageStatistics.mockRejectedValueOnce(new Error('API Error'));
      
      await expect(api.getArbitrageStatistics()).rejects.toThrow('API Error');
    });
  });

  describe('Bot Control', () => {
    test('startArbitrageService should call the correct client method', async () => {
      const responseData = { message: 'Service started' };
      mockClient.startArbitrageBot.mockResolvedValueOnce(responseData);
      
      const result = await api.startArbitrageService();
      
      expect(mockClient.startArbitrageBot).toHaveBeenCalled();
      expect(result).toEqual({
        success: true,
        message: 'Service started'
      });
    });
    
    test('stopArbitrageService should call the correct client method', async () => {
      const responseData = { message: 'Service stopped' };
      mockClient.stopArbitrageBot.mockResolvedValueOnce(responseData);
      
      const result = await api.stopArbitrageService();
      
      expect(mockClient.stopArbitrageBot).toHaveBeenCalled();
      expect(result).toEqual({
        success: true,
        message: 'Service stopped'
      });
    });

    test('getServiceStatus should call the correct client method', async () => {
      const responseData = { isRunning: true };
      mockClient.getBotStatus.mockResolvedValueOnce(responseData);
      
      const result = await api.getServiceStatus();
      
      expect(mockClient.getBotStatus).toHaveBeenCalled();
      expect(result).toHaveProperty('isRunning', true);
      expect(result).toHaveProperty('paperTradingEnabled');
    });
  });
}); 