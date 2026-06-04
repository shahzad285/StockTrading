using StockTrading.Models;

namespace StockTrading.Repository.IRepository;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetByBrokerOrderIdAsync(string brokerOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetOpenOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderHistory>> GetHistoryAsync(string brokerOrderId, CancellationToken cancellationToken = default);
    Task<Order> SaveAsync(Order order, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(OrderHistory history, CancellationToken cancellationToken = default);
    Task SetHoldingQuantityAsync(int stockId, int holdingQuantity, CancellationToken cancellationToken = default);
    Task ApplyHoldingQuantityDeltaAsync(int stockId, int quantityDelta, CancellationToken cancellationToken = default);
}
