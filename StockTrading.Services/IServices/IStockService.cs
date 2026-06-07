using StockTrading.Common.DTOs;
using StockTrading.Common.Enums;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface IStockService
{
    Task<PagedResult<StockListItem>> GetStocksAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Stock> SaveStockAsync(SaveStockRequest request, CancellationToken cancellationToken = default);
    Task<StockServiceDeleteResult> DeleteStockAsync(int stockId, CancellationToken cancellationToken = default);
    Task<HoldingsResponse> GetHoldingsAsync(CancellationToken cancellationToken = default);
    Task<List<StockSearchResult>> SearchStocksAsync(
        string query,
        StockExchange exchange = StockExchange.NSE,
        CancellationToken cancellationToken = default);
    Task<List<StockCandle>> GetCandlesAsync(
        string symbolToken,
        StockExchange exchange = StockExchange.NSE,
        StockChartInterval interval = StockChartInterval.ONE_DAY,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
    Task<List<StockPrice>> GetConfiguredPricesAsync(CancellationToken cancellationToken = default);
    Task<List<StockPrice>> GetPricesAsync(IEnumerable<StockListItem> stocks, CancellationToken cancellationToken = default);
    Task<List<StockPrice>> RefreshConfiguredPricesAsync(CancellationToken cancellationToken = default);
}

public sealed record StockServiceDeleteResult(
    bool IsSuccess,
    string Message,
    IReadOnlyList<string>? Dependencies = null);
