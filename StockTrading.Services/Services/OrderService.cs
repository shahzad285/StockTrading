using StockTrading.Common.DTOs;
using StockTrading.Common.Enums;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class OrderService(
    IBrokerService brokerService,
    IMarketDataCacheService marketDataCacheService,
    IStockRepository stockRepository,
    IOrderRepository orderRepository) : IOrderService
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
        var stock = await ResolveStockAsync(request, cancellationToken);
        if (stock == null)
        {
            return new PlaceOrderResult(false, Message: "Symbol token is required to resolve the stock for order tracking.");
        }

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

            var balance = await marketDataCacheService.GetAccountBalanceAsync(
                () => brokerService.GetAccountBalanceAsync(),
                cancellationToken);

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

        var result = await brokerService.PlaceOrderAsync(request);
        if (result.IsSuccess &&
            !string.IsNullOrWhiteSpace(result.BrokerOrderId))
        {
            var order = await orderRepository.SaveAsync(new Order
            {
                StockId = stock.Id,
                TradePlanId = request.TradePlanId,
                BrokerOrderId = result.BrokerOrderId,
                TradingSymbol = string.IsNullOrWhiteSpace(request.TradingSymbol)
                    ? request.Symbol
                    : request.TradingSymbol,
                Exchange = request.Exchange,
                SymbolToken = request.SymbolToken ?? "",
                TransactionType = request.TransactionType,
                OrderType = request.OrderType,
                ProductType = request.ProductType,
                Duration = request.Duration,
                Source = request.Source,
                Status = OrderStatus.Pending,
                Quantity = request.Quantity,
                UnfilledShares = request.Quantity,
                Price = request.Price,
                TriggerPrice = request.TriggerPrice ?? 0
            }, cancellationToken);

            await orderRepository.AddHistoryAsync(CreateHistory(order, OrderEventType.Placed), cancellationToken);
        }

        return result;
    }

    public Task<CancelOrderResult> CancelOrderAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        return brokerService.CancelOrderAsync(brokerOrderId);
    }

    public async Task<List<OrderHistory>> GetHistoryAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        var history = await orderRepository.GetHistoryAsync(brokerOrderId, cancellationToken);
        return history.ToList();
    }

    private static bool IsBuyOrder(string transactionType)
    {
        return string.Equals(transactionType, "BUY", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Stock?> ResolveStockAsync(
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StockId is > 0)
        {
            var existingStock = await stockRepository.GetByIdAsync(request.StockId.Value, cancellationToken);
            if (existingStock != null)
            {
                return existingStock;
            }
        }

        if (string.IsNullOrWhiteSpace(request.SymbolToken))
        {
            return null;
        }

        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var exchange = string.IsNullOrWhiteSpace(request.Exchange)
            ? "NSE"
            : request.Exchange.Trim().ToUpperInvariant();
        var tradingSymbol = string.IsNullOrWhiteSpace(request.TradingSymbol)
            ? symbol
            : request.TradingSymbol.Trim().ToUpperInvariant();

        return await stockRepository.UpsertOrderStockAsync(
            symbol,
            tradingSymbol,
            exchange,
            request.SymbolToken.Trim(),
            tradingSymbol,
            cancellationToken);
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
