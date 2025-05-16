import React, { useState } from 'react';
import { 
  Paper, Typography, Box, 
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Chip, IconButton, Collapse, Card
} from '@mui/material';
import {
  KeyboardArrowUp as KeyboardArrowUpIcon,
  KeyboardArrowDown as KeyboardArrowDownIcon
} from '@mui/icons-material';
import { TradeResult, TradeStatus, TradingPair } from '../models/types';

interface TradesListProps {
  trades: TradeResult[];
}

const TradesList: React.FC<TradesListProps> = ({ trades }) => {
  return (
    <Box sx={{ mt: 3 }}>
      <Typography variant="h5" gutterBottom>
        Trade Results
      </Typography>
      
      {trades.length === 0 ? (
        <Card sx={{ p: 3, textAlign: 'center' }}>
          <Typography color="text.secondary">
            No trades have been executed yet. Waiting for arbitrage opportunities...
          </Typography>
        </Card>
      ) : (
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell />
                <TableCell>ID</TableCell>
                <TableCell>Trading Pair</TableCell>
                <TableCell>Buy Exchange</TableCell>
                <TableCell>Sell Exchange</TableCell>
                <TableCell>Quantity</TableCell>
                <TableCell>Profit</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Time</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {trades.map((trade) => (
                <TradeRow key={trade.id} trade={trade} />
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
};

interface TradeRowProps {
  trade: TradeResult;
}

const TradeRow: React.FC<TradeRowProps> = ({ trade }) => {
  const [open, setOpen] = useState(false);
  
  // Function to format trading pair as string
  const formatTradingPair = (pair: TradingPair): string => {
    return `${pair.baseCurrency}/${pair.quoteCurrency}`;
  };

  // Function to get chip color based on trade status
  const getStatusChipColor = (status: TradeStatus): "success" | "warning" | "error" | "default" => {
    switch (status) {
      case TradeStatus.Pending:
        return "default";
      case TradeStatus.Executing:
        return "warning";
      case TradeStatus.Completed:
        return "success";
      case TradeStatus.Failed:
        return "error";
      default:
        return "default";
    }
  };

  return (
    <>
      <TableRow hover>
        <TableCell>
          <IconButton
            size="small"
            onClick={() => setOpen(!open)}
          >
            {open ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
          </IconButton>
        </TableCell>
        <TableCell>{trade.id.substring(0, 8)}...</TableCell>
        <TableCell>{formatTradingPair(trade.tradingPair)}</TableCell>
        <TableCell>{trade.buyExchangeId}</TableCell>
        <TableCell>{trade.sellExchangeId}</TableCell>
        <TableCell>{trade.quantity.toFixed(4)}</TableCell>
        <TableCell>
          <Typography color={trade.profitAmount > 0 ? "success.main" : "error.main"} fontWeight="bold">
            ${trade.profitAmount.toFixed(2)} ({trade.profitPercentage.toFixed(2)}%)
          </Typography>
        </TableCell>
        <TableCell>
          <Chip
            label={TradeStatus[trade.status]}
            size="small"
            color={getStatusChipColor(trade.status)}
          />
        </TableCell>
        <TableCell>{new Date(trade.timestamp).toLocaleString()}</TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={9}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box sx={{ m: 2 }}>
              <Typography variant="h6" gutterBottom component="div">
                Trade Details
              </Typography>
              <Table size="small">
                <TableBody>
                  <TableRow>
                    <TableCell component="th" scope="row">Opportunity ID</TableCell>
                    <TableCell>{trade.opportunityId}</TableCell>
                    <TableCell component="th" scope="row">Execution Time</TableCell>
                    <TableCell>{trade.executionTimeMs}ms</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell component="th" scope="row">Buy Price</TableCell>
                    <TableCell>${trade.buyPrice.toFixed(2)}</TableCell>
                    <TableCell component="th" scope="row">Sell Price</TableCell>
                    <TableCell>${trade.sellPrice.toFixed(2)}</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell component="th" scope="row">Fees</TableCell>
                    <TableCell>${trade.fees.toFixed(2)}</TableCell>
                    <TableCell component="th" scope="row">Net Profit</TableCell>
                    <TableCell>${(trade.profitAmount - trade.fees).toFixed(2)}</TableCell>
                  </TableRow>
                </TableBody>
              </Table>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  );
};

export default TradesList; 