// Mock axios
jest.mock('axios');

import axios from 'axios';
import * as api from './api';
import { ApiEndpoints } from './apiEndpoints';

describe('API Services Contract Tests', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Arbitrage Opportunities', () => {
    test('getArbitrageOpportunities should call the correct endpoint', async () => {
      const responseData = [{ id: 1, profitPercentage: 1.5 }];
      
      (axios.get as jest.Mock).mockResolvedValueOnce({ data: responseData });
      
      const result = await api.getArbitrageOpportunities();
      
      expect(axios.get).toHaveBeenCalledWith(ApiEndpoints.ARBITRAGE_OPPORTUNITIES);
      expect(result).toEqual(responseData);
    });
  });

  describe('Trade Results', () => {
    test('getTradeResults should call the correct endpoint', async () => {
      const responseData = [{ id: 1, status: 'executed' }];
      
      (axios.get as jest.Mock).mockResolvedValueOnce({ data: responseData });
      
      const result = await api.getTradeResults();
      
      expect(axios.get).toHaveBeenCalledWith(ApiEndpoints.ARBITRAGE_TRADES);
      expect(result).toEqual(responseData);
    });
  });

  describe('Statistics', () => {
    test('getArbitrageStatistics should call the correct endpoint', async () => {
      const responseData = { totalProfit: 100, successRate: 95 };
      
      (axios.get as jest.Mock).mockResolvedValueOnce({ data: responseData });
      
      const result = await api.getArbitrageStatistics();
      
      expect(axios.get).toHaveBeenCalledWith(ApiEndpoints.ARBITRAGE_STATISTICS);
      expect(result).toEqual(responseData);
    });
  });

  describe('Bot Control', () => {
    test('startArbitrageService should call the correct endpoint', async () => {
      const responseData = { success: true, message: 'Service started' };
      
      (axios.post as jest.Mock).mockResolvedValueOnce({ data: responseData });
      
      const result = await api.startArbitrageService();
      
      expect(axios.post).toHaveBeenCalledWith(ApiEndpoints.BOT_START);
      expect(result).toEqual(responseData);
    });
    
    test('stopArbitrageService should call the correct endpoint', async () => {
      const responseData = { success: true, message: 'Service stopped' };
      
      (axios.post as jest.Mock).mockResolvedValueOnce({ data: responseData });
      
      const result = await api.stopArbitrageService();
      
      expect(axios.post).toHaveBeenCalledWith(ApiEndpoints.BOT_STOP);
      expect(result).toEqual(responseData);
    });
  });
}); 