using StockTrading.Common.DTOs;
using StockTrading.IServices;
using StockTrading.Models;

namespace StockTrading.Services;

public sealed class OrderService(IBrokerService brokerService) : IOrderService
{
    public Task<List<OrderDetails>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        return brokerService.GetOrdersAsync();
    }

    public async Task<OrderDetails?> GetOrderAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        var orders = await brokerService.GetOrdersAsync();
        return orders.FirstOrDefault(order =>
            string.Equals(order.OrderId, brokerOrderId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (IsBuyOrder(request.TransactionType))
        {
            if (request.Quantity <= 0)
            {
                return new PlaceOrderResult(false, Message: "Quantity must be greater than zero.");
            }

            if (request.Price <= 0)
            {
                return new PlaceOrderResult(false, Message: "Price must be greater than zero to validate available balance.");
            }

            var balance = await brokerService.GetAccountBalanceAsync();

            if (balance == null)
            {
                return new PlaceOrderResult(false, Message: "Unable to verify available balance. Please login to SmartAPI again.");
            }

            var affordableQuantity = (int)Math.Floor(balance.AvailableCash / request.Price);
            if (affordableQuantity <= 0)
            {
                return new PlaceOrderResult(
                    false,
                    Message: "Available cash is not enough to buy one share.");
            }

            var orderQuantity = Math.Min(request.Quantity, affordableQuantity);
            if (orderQuantity < request.Quantity)
            {
                request = request with { Quantity = orderQuantity };
            }
        }

        return await brokerService.PlaceOrderAsync(request);
    }

    public Task<CancelOrderResult> CancelOrderAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        return brokerService.CancelOrderAsync(brokerOrderId);
    }

    public Task<List<OrderHistory>> GetHistoryAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<OrderHistory>());
    }

    private static bool IsBuyOrder(string transactionType)
    {
        return string.Equals(transactionType, "BUY", StringComparison.OrdinalIgnoreCase);
    }
}
