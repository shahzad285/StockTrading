using Dapper;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class OrderRepository(IDbConnectionFactory connectionFactory) : IOrderRepository
{
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var orders = await connection.QueryAsync<Order>(
            """
            select
                id as Id,
                stock_id as StockId,
                trade_plan_id as TradePlanId,
                broker_order_id as BrokerOrderId,
                trading_symbol as TradingSymbol,
                exchange as Exchange,
                symbol_token as SymbolToken,
                transaction_type as TransactionType,
                order_type as OrderType,
                product_type as ProductType,
                duration as Duration,
                source as Source,
                status as Status,
                rejection_reason as RejectionReason,
                quantity as Quantity,
                filled_shares as FilledShares,
                unfilled_shares as UnfilledShares,
                cancelled_shares as CancelledShares,
                price as Price,
                trigger_price as TriggerPrice,
                average_price as AveragePrice,
                update_time as UpdateTime,
                exchange_time as ExchangeTime,
                parent_broker_order_id as ParentBrokerOrderId,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            from orders
            order by created_at_utc desc
            """);

        return orders.ToArray();
    }

    public async Task<Order?> GetByBrokerOrderIdAsync(string brokerOrderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Order>(
            """
            select
                id as Id,
                stock_id as StockId,
                trade_plan_id as TradePlanId,
                broker_order_id as BrokerOrderId,
                trading_symbol as TradingSymbol,
                exchange as Exchange,
                symbol_token as SymbolToken,
                transaction_type as TransactionType,
                order_type as OrderType,
                product_type as ProductType,
                duration as Duration,
                source as Source,
                status as Status,
                rejection_reason as RejectionReason,
                quantity as Quantity,
                filled_shares as FilledShares,
                unfilled_shares as UnfilledShares,
                cancelled_shares as CancelledShares,
                price as Price,
                trigger_price as TriggerPrice,
                average_price as AveragePrice,
                update_time as UpdateTime,
                exchange_time as ExchangeTime,
                parent_broker_order_id as ParentBrokerOrderId,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            from orders
            where broker_order_id = @BrokerOrderId
            """,
            new { BrokerOrderId = brokerOrderId });
    }

    public async Task<IReadOnlyList<Order>> GetOpenOrdersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var orders = await connection.QueryAsync<Order>(
            """
            select
                id as Id,
                stock_id as StockId,
                trade_plan_id as TradePlanId,
                broker_order_id as BrokerOrderId,
                trading_symbol as TradingSymbol,
                exchange as Exchange,
                symbol_token as SymbolToken,
                transaction_type as TransactionType,
                order_type as OrderType,
                product_type as ProductType,
                duration as Duration,
                source as Source,
                status as Status,
                rejection_reason as RejectionReason,
                quantity as Quantity,
                filled_shares as FilledShares,
                unfilled_shares as UnfilledShares,
                cancelled_shares as CancelledShares,
                price as Price,
                trigger_price as TriggerPrice,
                average_price as AveragePrice,
                update_time as UpdateTime,
                exchange_time as ExchangeTime,
                parent_broker_order_id as ParentBrokerOrderId,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            from orders
            where status in (1, 2)
            order by updated_at_utc
            """);

        return orders.ToArray();
    }

    public async Task<IReadOnlyList<OrderHistory>> GetHistoryAsync(
        string brokerOrderId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var history = await connection.QueryAsync<OrderHistory>(
            """
            select
                order_histories.id as Id,
                order_histories.order_id as OrderId,
                order_histories.stock_id as StockId,
                order_histories.trade_plan_id as TradePlanId,
                order_histories.event_type as EventType,
                order_histories.broker_order_id as BrokerOrderId,
                order_histories.trading_symbol as TradingSymbol,
                order_histories.exchange as Exchange,
                order_histories.symbol_token as SymbolToken,
                order_histories.transaction_type as TransactionType,
                order_histories.order_type as OrderType,
                order_histories.product_type as ProductType,
                order_histories.duration as Duration,
                order_histories.source as Source,
                order_histories.status as Status,
                order_histories.rejection_reason as RejectionReason,
                order_histories.quantity as Quantity,
                order_histories.filled_shares as FilledShares,
                order_histories.unfilled_shares as UnfilledShares,
                order_histories.cancelled_shares as CancelledShares,
                order_histories.price as Price,
                order_histories.trigger_price as TriggerPrice,
                order_histories.average_price as AveragePrice,
                order_histories.update_time as UpdateTime,
                order_histories.exchange_time as ExchangeTime,
                order_histories.parent_broker_order_id as ParentBrokerOrderId,
                order_histories.recorded_at_utc as RecordedAtUtc
            from order_histories
            join orders
              on orders.id = order_histories.order_id
            where orders.broker_order_id = @BrokerOrderId
            order by order_histories.recorded_at_utc
            """,
            new { BrokerOrderId = brokerOrderId });

        return history.ToArray();
    }

    public async Task<Order> SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Order>(
            """
            insert into orders (
                stock_id,
                trade_plan_id,
                broker_order_id,
                trading_symbol,
                exchange,
                symbol_token,
                transaction_type,
                order_type,
                product_type,
                duration,
                source,
                status,
                rejection_reason,
                quantity,
                filled_shares,
                unfilled_shares,
                cancelled_shares,
                price,
                trigger_price,
                average_price,
                update_time,
                exchange_time,
                parent_broker_order_id,
                created_at_utc,
                updated_at_utc
            )
            values (
                @StockId,
                @TradePlanId,
                @BrokerOrderId,
                @TradingSymbol,
                @Exchange,
                @SymbolToken,
                @TransactionType,
                @OrderType,
                @ProductType,
                @Duration,
                @Source,
                @Status,
                @RejectionReason,
                @Quantity,
                @FilledShares,
                @UnfilledShares,
                @CancelledShares,
                @Price,
                @TriggerPrice,
                @AveragePrice,
                @UpdateTime,
                @ExchangeTime,
                @ParentBrokerOrderId,
                now(),
                now()
            )
            on conflict (broker_order_id) do update
            set stock_id = excluded.stock_id,
                trade_plan_id = excluded.trade_plan_id,
                trading_symbol = excluded.trading_symbol,
                exchange = excluded.exchange,
                symbol_token = excluded.symbol_token,
                transaction_type = excluded.transaction_type,
                order_type = excluded.order_type,
                product_type = excluded.product_type,
                duration = excluded.duration,
                source = excluded.source,
                status = excluded.status,
                rejection_reason = excluded.rejection_reason,
                quantity = excluded.quantity,
                filled_shares = excluded.filled_shares,
                unfilled_shares = excluded.unfilled_shares,
                cancelled_shares = excluded.cancelled_shares,
                price = excluded.price,
                trigger_price = excluded.trigger_price,
                average_price = excluded.average_price,
                update_time = excluded.update_time,
                exchange_time = excluded.exchange_time,
                parent_broker_order_id = excluded.parent_broker_order_id,
                updated_at_utc = now()
            returning
                id as Id,
                stock_id as StockId,
                trade_plan_id as TradePlanId,
                broker_order_id as BrokerOrderId,
                trading_symbol as TradingSymbol,
                exchange as Exchange,
                symbol_token as SymbolToken,
                transaction_type as TransactionType,
                order_type as OrderType,
                product_type as ProductType,
                duration as Duration,
                source as Source,
                status as Status,
                rejection_reason as RejectionReason,
                quantity as Quantity,
                filled_shares as FilledShares,
                unfilled_shares as UnfilledShares,
                cancelled_shares as CancelledShares,
                price as Price,
                trigger_price as TriggerPrice,
                average_price as AveragePrice,
                update_time as UpdateTime,
                exchange_time as ExchangeTime,
                parent_broker_order_id as ParentBrokerOrderId,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            """,
            ToParameters(order));
    }

    public async Task AddHistoryAsync(OrderHistory history, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            insert into order_histories (
                order_id,
                stock_id,
                trade_plan_id,
                event_type,
                broker_order_id,
                trading_symbol,
                exchange,
                symbol_token,
                transaction_type,
                order_type,
                product_type,
                duration,
                source,
                status,
                rejection_reason,
                quantity,
                filled_shares,
                unfilled_shares,
                cancelled_shares,
                price,
                trigger_price,
                average_price,
                update_time,
                exchange_time,
                parent_broker_order_id,
                recorded_at_utc
            )
            values (
                @OrderId,
                @StockId,
                @TradePlanId,
                @EventType,
                @BrokerOrderId,
                @TradingSymbol,
                @Exchange,
                @SymbolToken,
                @TransactionType,
                @OrderType,
                @ProductType,
                @Duration,
                @Source,
                @Status,
                @RejectionReason,
                @Quantity,
                @FilledShares,
                @UnfilledShares,
                @CancelledShares,
                @Price,
                @TriggerPrice,
                @AveragePrice,
                @UpdateTime,
                @ExchangeTime,
                @ParentBrokerOrderId,
                now()
            )
            """,
            ToParameters(history));
    }

    public async Task SetHoldingQuantityAsync(
        int stockId,
        int holdingQuantity,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update stocks
            set holding_quantity = greatest(0, @HoldingQuantity),
                updated_at_utc = now()
            where id = @StockId
            """,
            new { StockId = stockId, HoldingQuantity = holdingQuantity });
    }

    public async Task ApplyHoldingQuantityDeltaAsync(
        int stockId,
        int quantityDelta,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            update stocks
            set holding_quantity = greatest(0, holding_quantity + @QuantityDelta),
                updated_at_utc = now()
            where id = @StockId
            """,
            new { StockId = stockId, QuantityDelta = quantityDelta });
    }

    private static object ToParameters(Order order)
    {
        return new
        {
            order.StockId,
            order.TradePlanId,
            order.BrokerOrderId,
            order.TradingSymbol,
            order.Exchange,
            order.SymbolToken,
            order.TransactionType,
            order.OrderType,
            order.ProductType,
            order.Duration,
            Source = (int)order.Source,
            Status = (int)order.Status,
            order.RejectionReason,
            order.Quantity,
            order.FilledShares,
            order.UnfilledShares,
            order.CancelledShares,
            order.Price,
            order.TriggerPrice,
            order.AveragePrice,
            order.UpdateTime,
            order.ExchangeTime,
            order.ParentBrokerOrderId
        };
    }

    private static object ToParameters(OrderHistory history)
    {
        return new
        {
            history.OrderId,
            history.StockId,
            history.TradePlanId,
            EventType = (int)history.EventType,
            history.BrokerOrderId,
            history.TradingSymbol,
            history.Exchange,
            history.SymbolToken,
            history.TransactionType,
            history.OrderType,
            history.ProductType,
            history.Duration,
            Source = (int)history.Source,
            Status = (int)history.Status,
            history.RejectionReason,
            history.Quantity,
            history.FilledShares,
            history.UnfilledShares,
            history.CancelledShares,
            history.Price,
            history.TriggerPrice,
            history.AveragePrice,
            history.UpdateTime,
            history.ExchangeTime,
            history.ParentBrokerOrderId
        };
    }
}
