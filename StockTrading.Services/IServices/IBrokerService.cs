using StockTrading.Common.DTOs;
using StockTrading.Common.Enums;

namespace StockTrading.IServices;

public interface IBrokerService
{
    Task<bool> LoginAsync(string? otp = null);
    Task<AccountProfile?> GetProfileAsync();
    Task<AccountBalanceResponse?> GetAccountBalanceAsync();
    Task<HoldingsResponse> GetHoldingsAsync();
    Task<List<StockSearchResult>> SearchStocksAsync(string query, StockExchange exchange = StockExchange.NSE);
    Task<List<StockCandle>> GetCandlesAsync(
        string symbolToken,
        StockExchange exchange = StockExchange.NSE,
        StockChartInterval interval = StockChartInterval.ONE_DAY,
        DateTime? from = null,
        DateTime? to = null);
    Task<List<StockPrice>> GetPricesAsync(IEnumerable<StockListItem> stocks);
    Task<List<OrderDetails>> GetOrdersAsync();
    Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request);
    Task<CancelOrderResult> CancelOrderAsync(string brokerOrderId);
}
