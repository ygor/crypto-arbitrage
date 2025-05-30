import React, { useState, useEffect } from 'react';
import {
  Typography,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  Box,
  CircularProgress
} from '@mui/material';
import { ArbitrageOpportunity, ArbitrageOpportunityStatus, TradingPair } from '../models/types';
import { getRecentOpportunities, subscribeToArbitrageOpportunities } from '../services/api';

interface OpportunityViewProps {
  maxOpportunities?: number;
}

const OpportunityView: React.FC<OpportunityViewProps> = ({ maxOpportunities = 10 }) => {
  const [opportunities, setOpportunities] = useState<ArbitrageOpportunity[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    const fetchOpportunities = async () => {
      try {
        const data = await getRecentOpportunities(maxOpportunities);
        setOpportunities(data);
      } catch (error) {
        console.error('Error fetching opportunities:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchOpportunities();

    // Subscribe to real-time updates
    const setupSubscription = async () => {
      try {
        const connection = await subscribeToArbitrageOpportunities((opportunity: ArbitrageOpportunity) => {
          setOpportunities(prev => {
            // Add the new opportunity at the beginning and limit the total
            const newOpportunities = [opportunity, ...prev.filter(op => op.id !== opportunity.id)];
            return newOpportunities.slice(0, maxOpportunities);
          });
        });

        return () => {
          connection.stop().catch(err => console.error('Error stopping connection:', err));
        };
      } catch (error) {
        console.error('Error setting up subscription:', error);
        return () => {};
      }
    };

    const cleanupSubscription = setupSubscription();

    return () => {
      cleanupSubscription.then(cleanup => cleanup());
    };
  }, [maxOpportunities]);

  const formatTradingPair = (pair?: TradingPair): string => {
    if (!pair) return 'N/A';
    return `${pair.baseCurrency}/${pair.quoteCurrency}`;
  };

  const getStatusChipColor = (status?: ArbitrageOpportunityStatus | string): "success" | "warning" | "error" | "default" => {
    if (status === undefined) return 'default';
    
    // Convert enum to string if it's a number
    const statusStr = typeof status === 'number' ? ArbitrageOpportunityStatus[status] : status;
    
    switch (statusStr) {
      case 'Detected':
        return 'default';
      case 'Executing':
        return 'warning';
      case 'Executed':
        return 'success';
      case 'Failed':
        return 'error';
      case 'Missed':
        return 'default';
      default:
        return 'default';
    }
  };

  const getStatusLabel = (status?: ArbitrageOpportunityStatus | string): string => {
    if (status === undefined) return 'Unknown';
    
    // Convert enum to string if it's a number
    const statusStr = typeof status === 'number' ? ArbitrageOpportunityStatus[status] : status;
    
    switch (statusStr) {
      case 'Detected':
        return 'Detected';
      case 'Executing':
        return 'Executing';
      case 'Executed':
        return 'Executed';
      case 'Failed':
        return 'Failed';
      case 'Missed':
        return 'Missed';
      default:
        return 'Unknown';
    }
  };

  // Calculate spread percentage if not already in the opportunity data
  const calculateSpreadPercentage = (buyPrice?: number, sellPrice?: number): number => {
    if (!buyPrice || !sellPrice) return 0;
    return ((sellPrice - buyPrice) / buyPrice) * 100;
  };

  // Calculate estimated profit if not already in the opportunity data
  const calculateEstimatedProfit = (buyPrice?: number, sellPrice?: number, quantity?: number): number => {
    if (!buyPrice || !sellPrice || !quantity) return 0;
    return (sellPrice - buyPrice) * quantity;
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <>
      <Typography variant="h6" gutterBottom>
        Arbitrage Opportunities
      </Typography>
      <TableContainer component={Paper} sx={{ maxHeight: '100%' }}>
        <Table stickyHeader aria-label="arbitrage opportunities table">
          <TableHead>
            <TableRow>
              <TableCell>Trading Pair</TableCell>
              <TableCell>Buy Exchange</TableCell>
              <TableCell>Buy Price</TableCell>
              <TableCell>Sell Exchange</TableCell>
              <TableCell>Sell Price</TableCell>
              <TableCell>Spread (%)</TableCell>
              <TableCell>Est. Profit</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Detected At</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {opportunities.length === 0 ? (
              <TableRow>
                <TableCell colSpan={9} align="center">
                  No opportunities found
                </TableCell>
              </TableRow>
            ) : (
              opportunities.map((opportunity) => (
                <TableRow key={opportunity.id}>
                  <TableCell>{formatTradingPair(opportunity.tradingPair)}</TableCell>
                  <TableCell>{opportunity.buyExchangeId}</TableCell>
                  <TableCell>${opportunity.buyPrice?.toFixed(2) || '0.00'}</TableCell>
                  <TableCell>{opportunity.sellExchangeId}</TableCell>
                  <TableCell>${opportunity.sellPrice?.toFixed(2) || '0.00'}</TableCell>
                  <TableCell>
                    {(opportunity.spreadPercentage !== undefined 
                      ? opportunity.spreadPercentage 
                      : calculateSpreadPercentage(opportunity.buyPrice, opportunity.sellPrice)).toFixed(2)}%
                  </TableCell>
                  <TableCell>
                    ${(opportunity.potentialProfit !== undefined 
                      ? opportunity.potentialProfit 
                      : calculateEstimatedProfit(opportunity.buyPrice, opportunity.sellPrice, opportunity.quantity)).toFixed(2)}
                  </TableCell>
                  <TableCell>
                    <Chip 
                      label={getStatusLabel(opportunity.status)} 
                      color={getStatusChipColor(opportunity.status)} 
                    />
                  </TableCell>
                  <TableCell>
                    {opportunity.timestamp 
                      ? new Date(opportunity.timestamp).toLocaleString()
                      : 'N/A'}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </>
  );
};

export default OpportunityView; 