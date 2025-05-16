export enum ExchangeId {
  Binance = 'binance',
  Coinbase = 'coinbase',
  Kraken = 'kraken',
  Huobi = 'huobi',
  Bitstamp = 'bitstamp'
}

export enum ArbitrageOpportunityStatus {
  Detected = 0,
  Executing = 1,
  Executed = 2,
  Failed = 3
}

export interface TradingPair {
  baseCurrency: string;
  quoteCurrency: string;
}

export interface ArbitrageOpportunity {
  id: string;
  tradingPair: TradingPair;
  buyExchangeId: ExchangeId;
  sellExchangeId: ExchangeId;
  buyPrice: number;
  sellPrice: number;
  quantity: number;
  timestamp: string;
  status: ArbitrageOpportunityStatus;
  potentialProfit: number;
}

export interface PriceQuote {
  exchangeId: string;
  tradingPair: TradingPair;
  timestamp: string;
  bestBidPrice: number;
  bestBidQuantity: number;
  bestAskPrice: number;
  bestAskQuantity: number;
  spread: number;
  spreadPercentage: number;
}

export interface TradeResult {
  id: string;
  opportunityId: string;
  tradingPair: TradingPair;
  buyExchangeId: ExchangeId;
  sellExchangeId: ExchangeId;
  buyPrice: number;
  sellPrice: number;
  quantity: number;
  timestamp: string;
  status: TradeStatus;
  profitAmount: number;
  profitPercentage: number;
  fees: number;
  executionTimeMs: number;
}

export enum TradeStatus {
  Pending = 0,
  Executing = 1,
  Completed = 2,
  Failed = 3
}

export enum TradeType {
  Buy = "Buy",
  Sell = "Sell"
}

export interface ArbitrageTradeResult {
  opportunity: ArbitrageOpportunity;
  timestamp: string;
  isSuccess: boolean;
  buyResult?: TradeResult;
  sellResult?: TradeResult;
  profitAmount: number;
  profitPercentage: number;
  errorMessage?: string;
}

export interface ArbitrageStatistics {
  startTime: string;
  endTime: string;
  totalProfit: number;
  totalVolume: number;
  totalFees: number;
  averageProfit: number;
  highestProfit: number;
  lowestProfit: number;
  totalOpportunitiesDetected: number;
  totalTradesExecuted: number;
  successfulTrades: number;
  failedTrades: number;
  averageExecutionTimeMs: number;
  profitFactor: number;
}

export interface Balance {
  exchangeId: string;
  currency: string;
  total: number;
  available: number;
  reserved: number;
  timestamp: string;
}

export interface RiskProfile {
  minimumProfitPercentage: number;
  maxTradeAmount: number;
  maxExposurePerTradingPair: number;
  maxTotalExposure: number;
  maxExecutionTimeMs: number;
  verifyOpportunitiesBeforeExecution: boolean;
  maxCapitalPerTradePercent: number;
  maxCapitalPerAssetPercent: number;
  maxSlippagePercentage: number;
  stopLossPercentage: number;
  maxConcurrentTrades: number;
  tradeCooldownMs: number;
  dailyLossLimitPercent: number;
  maxCapitalUtilizationPercentage: number;
  maxRetryAttempts: number;
}

export interface ExchangeConfig {
  exchangeId: string;
  isEnabled: boolean;
  apiKey?: string;
  apiSecret?: string;
  maxRequestsPerSecond: number;
}

export interface ArbitrageConfig {
  isEnabled: boolean;
  pollingIntervalMs: number;
  maxConcurrentExecutions: number;
  maxTradeAmount: number;
  autoExecuteTrades: boolean;
  paperTradingEnabled: boolean;
  minimumProfitPercentage: number;
  maxExecutionTimeMs: number;
  riskProfile: RiskProfile;
  tradingPairs: TradingPair[];
} 