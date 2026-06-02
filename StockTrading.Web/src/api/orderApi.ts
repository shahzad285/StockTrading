import { apiRequest } from "./apiClient";

export type OrderDetails = {
  orderId: string;
  tradingSymbol: string;
  exchange: string;
  transactionType: string;
  orderType: string;
  productType: string;
  status: string;
  statusCategory: string;
  quantity: number;
  filledShares: number;
  averagePrice: number;
  updateTime: string;
};

export type PlaceOrderRequest = {
  symbol: string;
  exchange: string;
  transactionType: "BUY" | "SELL";
  orderType: string;
  productType: string;
  duration: string;
  quantity: number;
  price: number;
  triggerPrice?: number | null;
  symbolToken?: string | null;
  tradingSymbol?: string | null;
};

export type PlaceOrderResult = {
  isSuccess: boolean;
  brokerOrderId?: string | null;
  message?: string | null;
};

export async function getOrders(): Promise<{ orders: OrderDetails[] }> {
  return apiRequest<{ orders: OrderDetails[] }>("/Order");
}

export async function placeOrder(order: PlaceOrderRequest): Promise<PlaceOrderResult> {
  return apiRequest<PlaceOrderResult>("/Order", {
    method: "POST",
    body: JSON.stringify(order)
  });
}
