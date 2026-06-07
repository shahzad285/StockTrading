using StockTrading.Common.DTOs;
using StockTrading.Common.Enums;
using StockTrading.IServices;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Services;

public sealed class TradePlanExecutionService(
    ITradePlanService tradePlanService,
    IStockService stockService,
    IMarketDataCacheService marketDataCacheService,
    IStockRepository stockRepository,
    IOrderRepository orderRepository,
    IOrderService orderService) : ITradePlanExecutionService
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var tradePlans = (await tradePlanService.GetAsync(pageSize: 0, cancellationToken: cancellationToken)).Items
            .Where(tradePlan => tradePlan.IsActive)
            .ToArray();
        if (tradePlans.Length == 0)
        {
            return;
        }

        var priceItems = tradePlans
            .Select(ToStockListItem)
            .ToArray();
        var prices = await marketDataCacheService.GetPricesAsync(
            priceItems,
            stocks => stockService.GetPricesAsync(stocks, cancellationToken),
            cancellationToken);
        var openOrders = await orderRepository.GetOpenOrdersAsync(cancellationToken);

        foreach (var tradePlan in tradePlans)
        {
            var price = prices.FirstOrDefault(item =>
                string.Equals(item.SymbolToken, tradePlan.SymbolToken, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Exchange, tradePlan.Exchange, StringComparison.OrdinalIgnoreCase));
            if (price == null || !price.IsFetched || price.LastTradedPrice <= 0)
            {
                continue;
            }

            var stock = await stockRepository.GetByIdAsync(tradePlan.StockId, cancellationToken);
            var holdingQuantity = stock?.HoldingQuantity ?? 0;

            if (price.LastTradedPrice <= tradePlan.BuyPrice)
            {
                var inProgressBuyQuantity = GetOpenTradePlanOrderQuantity(openOrders, tradePlan.Id, "BUY");
                var buyQuantity = tradePlan.MaxStocksAllowed - holdingQuantity - inProgressBuyQuantity;
                if (buyQuantity > 0)
                {
                    await PlaceTradePlanOrderAsync(
                        tradePlan,
                        "BUY",
                        buyQuantity,
                        tradePlan.BuyPrice,
                        cancellationToken);
                }
            }

            if (price.LastTradedPrice >= tradePlan.SellPrice &&
                !HasOpenTradePlanOrder(openOrders, tradePlan.Id, "SELL"))
            {
                if (holdingQuantity > 0)
                {
                    await PlaceTradePlanOrderAsync(
                        tradePlan,
                        "SELL",
                        holdingQuantity,
                        tradePlan.SellPrice,
                        cancellationToken);
                }
            }
        }
    }

    private async Task PlaceTradePlanOrderAsync(
        TradePlan tradePlan,
        string transactionType,
        int quantity,
        decimal price,
        CancellationToken cancellationToken)
    {
        if (quantity <= 0)
        {
            return;
        }

        await orderService.PlaceOrderAsync(
            new PlaceOrderRequest(
                tradePlan.Symbol,
                tradePlan.Exchange,
                transactionType,
                "LIMIT",
                "DELIVERY",
                "DAY",
                quantity,
                price,
                SymbolToken: tradePlan.SymbolToken,
                TradingSymbol: tradePlan.TradingSymbol,
                StockId: tradePlan.StockId,
                TradePlanId: tradePlan.Id,
                Source: OrderSource.TradePlan),
            cancellationToken);
    }

    private static bool HasOpenTradePlanOrder(
        IReadOnlyList<Order> openOrders,
        int tradePlanId,
        string transactionType)
    {
        return openOrders.Any(order =>
            order.TradePlanId == tradePlanId &&
            string.Equals(order.TransactionType, transactionType, StringComparison.OrdinalIgnoreCase));
    }

    private static int GetOpenTradePlanOrderQuantity(
        IReadOnlyList<Order> openOrders,
        int tradePlanId,
        string transactionType)
    {
        return openOrders
            .Where(order =>
                order.TradePlanId == tradePlanId &&
                string.Equals(order.TransactionType, transactionType, StringComparison.OrdinalIgnoreCase))
            .Sum(GetRemainingOrderQuantity);
    }

    private static int GetRemainingOrderQuantity(Order order)
    {
        if (order.UnfilledShares > 0)
        {
            return order.UnfilledShares;
        }

        return Math.Max(0, order.Quantity - order.FilledShares - order.CancelledShares);
    }

    private static StockListItem ToStockListItem(TradePlan tradePlan)
    {
        return new StockListItem
        {
            StockId = tradePlan.StockId,
            Symbol = tradePlan.Symbol,
            Name = tradePlan.Name,
            Exchange = tradePlan.Exchange,
            SymbolToken = tradePlan.SymbolToken,
            TradingSymbol = tradePlan.TradingSymbol
        };
    }
}
