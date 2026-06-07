using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface ITradePlanRepository
{
    Task<IReadOnlyList<TradePlan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TradePlan>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TradePlan> SaveAsync(TradePlan tradePlan, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
