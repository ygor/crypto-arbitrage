export enum ExchangeId {
  Binance = 'binance',
  Coinbase = 'coinbase',
  Kraken = 'kraken',
  Huobi = 'huobi',
  Bitstamp = 'bitstamp'
}

// Add activity log related types
export enum ActivityType {
  Info = 'Info',
  Warning = 'Warning',
  Error = 'Error',
  Success = 'Success'
}

export interface ActivityLogEntry {
  id: string;
  timestamp: Date | string;
  type: ActivityType;
  message: string;
  details?: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
}

// Add exchange status related types
export interface ExchangeStatus {
  exchangeId: string;
  exchangeName: string;
  isUp: boolean;
  lastChecked: Date | string;
  responseTimeMs: number;
  additionalInfo?: string;
}

export enum ArbitrageOpportunityStatus {
  Detected = 0,
  Executing = 1,
  Executed = 2,
  Failed = 3,
  Missed = 4
}

export interface TradingPair {
  baseCurrency: string;
  quoteCurrency: string;
}

export interface ArbitrageOpportunity {
  id?: string;
  tradingPair?: TradingPair;
  buyExchangeId?: string | ExchangeId;
  sellExchangeId?: string | ExchangeId;
  buyPrice?: number;
  sellPrice?: number;
  quantity?: number;
  timestamp?: Date | string;
  status?: ArbitrageOpportunityStatus;
  potentialProfit?: number;
  spreadPercentage?: number;
  estimatedProfit?: number;
  detectedAt?: Date | string;
  spread?: number;
  effectiveQuantity?: number;
  isQualified?: boolean;
}

export interface PriceQuote {
  exchangeId: string;
  tradingPair: TradingPair;
  timestamp: Date | string;
  bestBidPrice: number;
  bestBidQuantity: number;
  bestAskPrice: number;
  bestAskQuantity: number;
  spread: number;
  spreadPercentage: number;
}

export interface TradeResult {
  id?: string;
  opportunityId?: string;
  tradingPair?: TradingPair;
  buyExchangeId?: string | ExchangeId;
  sellExchangeId?: string | ExchangeId;
  buyPrice?: number;
  sellPrice?: number;
  quantity?: number;
  timestamp?: Date | string;
  status?: TradeStatus;
  profitAmount?: number;
  profitPercentage?: number;
  fees?: number;
  executionTimeMs?: number;
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
  timestamp: Date | string;
  isSuccess: boolean;
  buyResult?: TradeResult;
  sellResult?: TradeResult;
  profitAmount: number;
  profitPercentage: number;
  errorMessage?: string;
}

export interface ArbitrageStatistics {
  startTime?: Date | string;
  endTime?: Date | string;
  totalProfit?: number;
  totalVolume?: number;
  totalFees?: number;
  averageProfit?: number;
  highestProfit?: number;
  lowestProfit?: number;
  totalOpportunitiesDetected?: number;
  totalTradesExecuted?: number;
  successfulTrades?: number;
  failedTrades?: number;
  averageExecutionTimeMs?: number;
  profitFactor?: number;
}

export interface Balance {
  exchangeId: string;
  currency: string;
  total: number;
  available: number;
  reserved: number;
  timestamp: Date | string;
}

export interface RiskProfile {
  name?: string;
  isActive?: boolean;
  minProfitPercentage?: number;
  minProfitAmount?: number;
  minimumProfitPercentage?: number;
  maxSlippagePercentage?: number;
  riskTolerance?: number;
  maxRetryAttempts?: number;
  maxSpreadVolatility?: number;
  stopLossPercentage?: number;
  dailyLossLimitPercent?: number;
  usePriceProtection?: boolean;
  maxTradeAmount?: number;
  maxAssetExposurePercentage?: number;
  maxTotalExposurePercentage?: number;
  dynamicSizingFactor?: number;
  maxCapitalPerTradePercent?: number;
  maxCapitalPerAssetPercent?: number;
  executionAggressiveness?: number;
  maxExecutionTimeMs?: number;
  orderBookDepthFactor?: number;
  cooldownPeriodMs?: number;
  maxConcurrentTrades?: number;
  tradeCooldownMs?: number;
  useAdaptiveParameters?: boolean;
  
  // Additional properties from frontend models
  maxExposurePerTradingPair?: number;
  maxTotalExposure?: number;
  verifyOpportunitiesBeforeExecution?: boolean;
  maxCapitalUtilizationPercentage?: number;
}

export interface ExchangeConfig {
  exchangeId: string;
  name?: string;
  isEnabled: boolean;
  apiKey?: string;
  apiSecret?: string;
  maxRequestsPerSecond?: number;
}

export interface ExchangePair {
  buyExchangeId: string;
  sellExchangeId: string;
}

export enum ArbitrageConfigEvaluationStrategy {
  Default = 0,
  Conservative = 1,
  Balanced = 2,
  Aggressive = 3
}

export enum ArbitrageConfigExecutionStrategy {
  Default = 0,
  Conservative = 1,
  Balanced = 2,
  Aggressive = 3
}

export interface ArbitrageConfig {
  isEnabled?: boolean;
  enabledTradingPairs?: string[];
  enabledBaseCurrencies?: string[];
  enabledQuoteCurrencies?: string[];
  enabledExchanges?: string[];
  enabledExchangePairs?: ExchangePair[];
  scanIntervalMs?: number;
  maxConcurrentScans?: number;
  autoTradeEnabled?: boolean;
  maxDailyTrades?: number;
  maxDailyVolume?: number;
  minOrderBookDepth?: number;
  useWebsockets?: boolean;
  usePolling?: boolean;
  pollingIntervalMs?: number;
  evaluationStrategy?: ArbitrageConfigEvaluationStrategy;
  executionStrategy?: ArbitrageConfigExecutionStrategy;
  
  // Additional properties from frontend models
  maxConcurrentExecutions?: number;
  maxTradeAmount?: number;
  autoExecuteTrades?: boolean;
  paperTradingEnabled?: boolean;
  minimumProfitPercentage?: number;
  maxExecutionTimeMs?: number;
  riskProfile?: RiskProfile;
  tradingPairs?: TradingPair[];
} 