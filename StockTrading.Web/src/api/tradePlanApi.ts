import { apiRequest } from "./apiClient";
import { StockExchange, StockSearchResult } from "./stockApi";

export type TradePlan = {
  id?: number;
  stockId?: number;
  buyPrice: number;
  sellPrice: number;
  quantity: number;
  maxBudget?: number | null;
  status?: "Active" | "Paused" | "Completed" | "Cancelled";
  isActive: boolean;
  repeatEnabled: boolean;
  buyTriggerCount?: number;
  sellTriggerCount?: number;
  lastBuyTriggeredAtUtc?: string | null;
  lastSellTriggeredAtUtc?: string | null;
  symbol: string;
  name?: string | null;
  exchange: string;
  symbolToken: string;
  tradingSymbol: string;
};

export async function getTradePlans(): Promise<{ tradePlans: TradePlan[] }> {
  return apiRequest<{ tradePlans: TradePlan[] }>("/TradePlan");
}

export async function searchTradePlanStocks(
  query: string,
  exchange: StockExchange = "NSE"
): Promise<{ stocks: StockSearchResult[] }> {
  const params = new URLSearchParams({
    query,
    exchange
  });

  return apiRequest<{ stocks: StockSearchResult[] }>(`/Common/StockSearch?${params.toString()}`);
}

export async function saveTradePlan(tradePlan: TradePlan): Promise<{ tradePlan: TradePlan }> {
  return apiRequest<{ tradePlan: TradePlan }>("/TradePlan", {
    method: "POST",
    body: JSON.stringify(tradePlan)
  });
}

export async function deleteTradePlan(id: number): Promise<void> {
  await apiRequest<void>(`/TradePlan/${id}`, {
    method: "DELETE"
  });
}
