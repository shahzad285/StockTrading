using StockTrading.Common.DTOs;
using StockTrading.Common.Enums;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class OrderStatusTrackingService(
    IBrokerService brokerService,
    IOrderRepository orderRepository) : IOrderStatusTrackingService
{
    public async Task TrackOpenOrdersAsync(CancellationToken cancellationToken = default)
    {
        var localOrders = await orderRepository.GetOpenOrdersAsync(cancellationToken);
        if (localOrders.Count == 0)
        {
            return;
        }

        var brokerOrders = await brokerService.GetOrdersAsync();
        foreach (var localOrder in localOrders)
        {
            var brokerOrder = brokerOrders.FirstOrDefault(order =>
                string.Equals(order.OrderId, localOrder.BrokerOrderId, StringComparison.OrdinalIgnoreCase));
            if (brokerOrder == null)
            {
                continue;
            }

            await UpdateLocalOrderAsync(localOrder, brokerOrder, cancellationToken);
        }
    }

    private async Task UpdateLocalOrderAsync(
        Order localOrder,
        OrderDetails brokerOrder,
        CancellationToken cancellationToken)
    {
        var filledDelta = Math.Max(0, brokerOrder.FilledShares - localOrder.FilledShares);

        localOrder.Status = GetOrderStatus(brokerOrder.StatusCategory, brokerOrder.Status);
        localOrder.RejectionReason = brokerOrder.RejectionReason ?? "";
        localOrder.Quantity = brokerOrder.Quantity > 0 ? brokerOrder.Quantity : localOrder.Quantity;
        localOrder.FilledShares = brokerOrder.FilledShares;
        localOrder.UnfilledShares = brokerOrder.UnfilledShares;
        localOrder.CancelledShares = brokerOrder.CancelledShares;
        localOrder.Price = brokerOrder.Price;
        localOrder.TriggerPrice = brokerOrder.TriggerPrice;
        localOrder.AveragePrice = brokerOrder.AveragePrice;
        localOrder.UpdateTime = brokerOrder.UpdateTime ?? "";
        localOrder.ExchangeTime = brokerOrder.ExchangeTime ?? "";
        localOrder.ParentBrokerOrderId = brokerOrder.ParentOrderId ?? "";

        var savedOrder = await orderRepository.SaveAsync(localOrder, cancellationToken);
        await orderRepository.AddHistoryAsync(
            CreateHistory(savedOrder, GetEventType(savedOrder.Status)),
            cancellationToken);

        if (filledDelta > 0)
        {
            var holdingDelta = IsSellOrder(savedOrder.TransactionType)
                ? -filledDelta
                : filledDelta;
            await orderRepository.ApplyHoldingQuantityDeltaAsync(
                savedOrder.StockId,
                holdingDelta,
                cancellationToken);
        }
    }

    private static OrderStatus GetOrderStatus(string statusCategory, string status)
    {
        var normalized = string.IsNullOrWhiteSpace(statusCategory) ? status : statusCategory;
        return normalized.Trim().ToLowerInvariant() switch
        {
            "pending" => OrderStatus.Pending,
            "open" => OrderStatus.Open,
            "executed" or "complete" or "completed" or "filled" => OrderStatus.Executed,
            "cancelled" or "canceled" => OrderStatus.Cancelled,
            "rejected" => OrderStatus.Rejected,
            "failed" => OrderStatus.Failed,
            _ => OrderStatus.Unknown
        };
    }

    private static OrderEventType GetEventType(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Executed => OrderEventType.Executed,
            OrderStatus.Cancelled => OrderEventType.Cancelled,
            OrderStatus.Rejected => OrderEventType.Rejected,
            OrderStatus.Failed => OrderEventType.Failed,
            _ => OrderEventType.Updated
        };
    }

    private static bool IsSellOrder(string transactionType)
    {
        return string.Equals(transactionType, "SELL", StringComparison.OrdinalIgnoreCase);
    }

    private static OrderHistory CreateHistory(Order order, OrderEventType eventType)
    {
        return new OrderHistory
        {
            OrderId = order.Id,
            StockId = order.StockId,
            TradePlanId = order.TradePlanId,
            EventType = eventType,
            BrokerOrderId = order.BrokerOrderId,
            TradingSymbol = order.TradingSymbol,
            Exchange = order.Exchange,
            SymbolToken = order.SymbolToken,
            TransactionType = order.TransactionType,
            OrderType = order.OrderType,
            ProductType = order.ProductType,
            Duration = order.Duration,
            Source = order.Source,
            Status = order.Status,
            RejectionReason = order.RejectionReason,
            Quantity = order.Quantity,
            FilledShares = order.FilledShares,
            UnfilledShares = order.UnfilledShares,
            CancelledShares = order.CancelledShares,
            Price = order.Price,
            TriggerPrice = order.TriggerPrice,
            AveragePrice = order.AveragePrice,
            UpdateTime = order.UpdateTime,
            ExchangeTime = order.ExchangeTime,
            ParentBrokerOrderId = order.ParentBrokerOrderId
        };
    }
}
