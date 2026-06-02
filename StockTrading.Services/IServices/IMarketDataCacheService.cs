using StockTrading.Common.DTOs;

namespace StockTrading.IServices;

public interface IMarketDataCacheService
{
    Task<List<StockPrice>> GetPricesAsync(
        IEnumerable<StockListItem> stocks,
        Func<IEnumerable<StockListItem>, Task<List<StockPrice>>> fetchPricesAsync,
        CancellationToken cancellationToken = default);

    Task<T?> GetAccountBalanceAsync<T>(
        Func<Task<T?>> fetchBalanceAsync,
        CancellationToken cancellationToken = default);
}
