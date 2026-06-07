using StockTrading.Common.DTOs;
using StockTrading.Models;

namespace StockTrading.IServices;

public interface ITradePlanService
{
    Task<PagedResult<TradePlan>> GetAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<TradePlan> SaveAsync(TradePlan tradePlan, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
