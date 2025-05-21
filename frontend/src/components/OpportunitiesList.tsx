import React, { useState } from 'react';
import { 
  Table, TableBody, TableCell, TableContainer, TableHead, 
  TableRow, Paper, Typography, Box, Chip 
} from '@mui/material';
import { 
  ArbitrageOpportunity, 
  ArbitrageOpportunityStatus
} from '../models/types';

interface OpportunitiesListProps {
  opportunities: ArbitrageOpportunity[];
  title?: string;
}

const getStatusColor = (status?: ArbitrageOpportunityStatus | string) => {
  if (status === undefined) return 'default';
  
  switch (status) {
    case ArbitrageOpportunityStatus.Detected:
    case 'Detected':
      return 'info';
    case ArbitrageOpportunityStatus.Executing:
    case 'Executing':
      return 'warning';
    case ArbitrageOpportunityStatus.Executed:
    case 'Executed':
      return 'success';
    case ArbitrageOpportunityStatus.Failed:
    case 'Failed':
      return 'error';
    case ArbitrageOpportunityStatus.Missed:
    case 'Missed':
      return 'default';
    default:
      return 'default';
  }
};

// Calculate spread percentage as a fallback
const calculateSpreadPercentage = (buyPrice?: number, sellPrice?: number): number => {
  if (!buyPrice || !sellPrice) return 0;
  return ((sellPrice - buyPrice) / buyPrice) * 100;
};

// Calculate estimated profit as a fallback
const calculateEstimatedProfit = (buyPrice?: number, sellPrice?: number, quantity?: number): number => {
  if (!buyPrice || !sellPrice || !quantity) return 0;
  return (sellPrice - buyPrice) * quantity;
};

const OpportunitiesList: React.FC<OpportunitiesListProps> = ({ 
  opportunities, 
  title = 'Arbitrage Opportunities' 
}) => {
  return (
    <Box sx={{ width: '100%', mt: 3 }}>
      <Typography variant="h6" gutterBottom component="div">
        {title}
      </Typography>
      <TableContainer component={Paper} sx={{ maxHeight: 400 }}>
        <Table stickyHeader aria-label="opportunities table" size="small">
          <TableHead>
            <TableRow>
              <TableCell>Trading Pair</TableCell>
              <TableCell>Buy Exchange</TableCell>
              <TableCell>Buy Price</TableCell>
              <TableCell>Sell Exchange</TableCell>
              <TableCell>Sell Price</TableCell>
              <TableCell>Spread %</TableCell>
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
              opportunities.map((opportunity, index) => (
                <TableRow key={opportunity.id || index} hover>
                  <TableCell>
                    {opportunity.tradingPair?.baseCurrency}/{opportunity.tradingPair?.quoteCurrency}
                  </TableCell>
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
                    ${(opportunity.estimatedProfit !== undefined 
                      ? opportunity.estimatedProfit 
                      : (opportunity.potentialProfit !== undefined 
                          ? opportunity.potentialProfit
                          : calculateEstimatedProfit(opportunity.buyPrice, opportunity.sellPrice, opportunity.quantity))).toFixed(2)}
                  </TableCell>
                  <TableCell>
                    <Chip 
                      label={opportunity.status || 'Unknown'} 
                      color={getStatusColor(opportunity.status) as any}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    {opportunity.detectedAt || opportunity.timestamp 
                      ? new Date(opportunity.detectedAt || opportunity.timestamp || '').toLocaleString()
                      : 'N/A'}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};

export default OpportunitiesList; 