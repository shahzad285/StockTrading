using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface ITradePlanService
{
    Task<IReadOnlyList<TradePlan>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TradePlan>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TradePlan> SaveAsync(TradePlan tradePlan, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
