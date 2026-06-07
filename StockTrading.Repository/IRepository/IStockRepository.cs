using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IStockRepository
{
    Task<PagedResult<StockListItem>> GetAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Stock> UpsertAsync(SaveStockRequest request, CancellationToken cancellationToken = default);
    Task<Stock> UpsertOrderStockAsync(
        string symbol,
        string? name,
        string exchange,
        string symbolToken,
        string tradingSymbol,
        CancellationToken cancellationToken = default);
    Task<Stock?> GetByIdAsync(int stockId, CancellationToken cancellationToken = default);
    Task<StockDeleteCheck> GetDeleteCheckAsync(int stockId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int stockId, CancellationToken cancellationToken = default);
}
