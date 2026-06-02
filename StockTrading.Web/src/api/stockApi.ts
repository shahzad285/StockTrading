import { apiRequest } from "./apiClient";

export type HoldingStock = {
  stockName: string;
  tradingSymbol: string;
  exchange: string;
  symbolToken: string;
  purchasePrice: number;
  totalStocks: number;
  currentPrice: number;
  totalGainOrLoss: number;
};

export type HoldingsResponse = {
  stocks: HoldingStock[];
  totalProfitLoss: number;
};

export type StockPrice = {
  symbol: string;
  tradingSymbol: string;
  exchange: string;
  symbolToken: string;
  lastTradedPrice: number;
  isFetched: boolean;
  message: string;
};

export type StockExchange = "NSE" | "BSE";

export type StockSearchResult = {
  symbol: string;
  tradingSymbol: string;
  exchange: StockExchange;
  symbolToken: string;
  name?: string | null;
};

export type StockMaster = {
  id?: number;
  symbol: string;
  name?: string | null;
  exchange: StockExchange;
  symbolToken: string;
  tradingSymbol: string;
  holdingQuantity?: number;
  createdAtUtc?: string;
  updatedAtUtc?: string | null;
};

export type StockListItem = {
  stockId?: number;
  symbol: string;
  name?: string | null;
  exchange: string;
  symbolToken: string;
  tradingSymbol: string;
  holdingQuantity?: number;
  assetType?: string;
  theme?: string | null;
  sector?: string | null;
  industry?: string | null;
  classificationReason?: string | null;
  confidenceScore?: number | null;
  description?: string | null;
  updatedByNse?: boolean;
  updatedByYahoo?: boolean;
  updatedByTapetide?: boolean;
  dividendYield?: number | null;
  growthRate?: number | null;
  debtToEquity?: number | null;
  peRatio?: number | null;
  earningsPerShare?: number | null;
  priceToBook?: number | null;
  totalRevenue?: number | null;
  netIncome?: number | null;
  totalDebt?: number | null;
  totalCash?: number | null;
  cashFlow?: number | null;
  marketCap?: number | null;
  stockCategory?: string | null;
  stockCategoryReason?: string | null;
  stockCategoryConfidence?: number | null;
  stockCategoryUpdatedAtUtc?: string | null;
  lastAnalyzedAtUtc?: string | null;
};

export type StockChartRange = "OneDay" | "OneWeek" | "OneMonth" | "SixMonths" | "OneYear";

export type StockCandle = {
  time: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
};

export async function getHoldings(): Promise<HoldingsResponse> {
  return apiRequest<HoldingsResponse>("/Stock/holdings");
}

export async function getStocks(): Promise<{ stocks: StockListItem[] }> {
  return apiRequest<{ stocks: StockListItem[] }>("/Stock/stocks");
}

export async function saveStock(stock: StockMaster): Promise<{ stock: StockMaster }> {
  return apiRequest<{ stock: StockMaster }>("/Stock/stocks", {
    method: "POST",
    body: JSON.stringify(stock)
  });
}

export async function deleteStock(stockId: number): Promise<void> {
  await apiRequest<void>(`/Stock/stocks/${stockId}`, {
    method: "DELETE"
  });
}

export async function getPrices(): Promise<{ prices: StockPrice[] }> {
  return apiRequest<{ prices: StockPrice[] }>("/Stock/prices");
}

export async function searchStocks(query: string, exchange: StockExchange = "NSE"): Promise<{ stocks: StockSearchResult[] }> {
  const params = new URLSearchParams({
    query,
    exchange
  });

  return apiRequest<{ stocks: StockSearchResult[] }>(`/Common/StockSearch?${params.toString()}`);
}

export async function getStockChart(
  symbolToken: string,
  exchange: StockExchange = "NSE",
  range: StockChartRange = "OneMonth"
): Promise<{ candles: StockCandle[] }> {
  const params = new URLSearchParams({
    symbolToken,
    exchange,
    range
  });

  return apiRequest<{ candles: StockCandle[] }>(`/Common/StockChart?${params.toString()}`);
}
