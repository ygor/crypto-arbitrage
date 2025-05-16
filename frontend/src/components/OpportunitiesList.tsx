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

const getStatusColor = (status: ArbitrageOpportunityStatus) => {
  switch (status) {
    case ArbitrageOpportunityStatus.Detected:
      return 'info';
    case ArbitrageOpportunityStatus.Executing:
      return 'warning';
    case ArbitrageOpportunityStatus.Executed:
      return 'success';
    case ArbitrageOpportunityStatus.Failed:
      return 'error';
    case ArbitrageOpportunityStatus.Missed:
      return 'default';
    default:
      return 'default';
  }
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
                    {opportunity.tradingPair.baseCurrency}/{opportunity.tradingPair.quoteCurrency}
                  </TableCell>
                  <TableCell>{opportunity.buyExchangeId}</TableCell>
                  <TableCell>${opportunity.buyPrice.toFixed(2)}</TableCell>
                  <TableCell>{opportunity.sellExchangeId}</TableCell>
                  <TableCell>${opportunity.sellPrice.toFixed(2)}</TableCell>
                  <TableCell>{opportunity.spreadPercentage.toFixed(2)}%</TableCell>
                  <TableCell>${opportunity.estimatedProfit.toFixed(2)}</TableCell>
                  <TableCell>
                    <Chip 
                      label={opportunity.status} 
                      color={getStatusColor(opportunity.status) as any}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>{new Date(opportunity.detectedAt).toLocaleString()}</TableCell>
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