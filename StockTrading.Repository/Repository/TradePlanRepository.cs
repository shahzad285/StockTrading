using Dapper;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class TradePlanRepository(IDbConnectionFactory connectionFactory) : ITradePlanRepository
{
    public async Task<IReadOnlyList<TradePlan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var tradePlans = await connection.QueryAsync<TradePlan>(
            """
            select
                trade_plans.id as Id,
                trade_plans.stock_id as StockId,
                trade_plans.buy_price as BuyPrice,
                trade_plans.sell_price as SellPrice,
                trade_plans.max_stocks_allowed as MaxStocksAllowed,
                trade_plans.is_active as IsActive,
                trade_plans.repeat_enabled as RepeatEnabled,
                trade_plans.buy_trigger_count as BuyTriggerCount,
                trade_plans.sell_trigger_count as SellTriggerCount,
                trade_plans.last_buy_triggered_at_utc as LastBuyTriggeredAtUtc,
                trade_plans.last_sell_triggered_at_utc as LastSellTriggeredAtUtc,
                trade_plans.created_at_utc as CreatedAtUtc,
                trade_plans.updated_at_utc as UpdatedAtUtc,
                stocks.symbol as Symbol,
                stocks.name as Name,
                stocks.exchange as Exchange,
                stocks.symbol_token as SymbolToken,
                stocks.trading_symbol as TradingSymbol
            from trade_plans
            join stocks
              on stocks.id = trade_plans.stock_id
            order by trade_plans.created_at_utc desc
            """);

        return tradePlans.ToArray();
    }

    public async Task<TradePlan> SaveAsync(TradePlan tradePlan, CancellationToken cancellationToken = default)
    {
        if (tradePlan.Id > 0)
        {
            return await UpdateAsync(tradePlan, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var savedTradePlan = await connection.QuerySingleAsync<TradePlan>(
            """
            with saved_stock as (
                insert into stocks (
                    symbol,
                    name,
                    exchange,
                    symbol_token,
                    trading_symbol,
                    created_at_utc
                )
                values (
                    @Symbol,
                    @Name,
                    @Exchange,
                    @SymbolToken,
                    @TradingSymbol,
                    now()
                )
                on conflict (exchange, symbol_token) do update
                set symbol = excluded.symbol,
                    name = coalesce(excluded.name, stocks.name),
                    trading_symbol = excluded.trading_symbol,
                    updated_at_utc = now()
                returning id
            ),
            saved_trade_plan as (
                insert into trade_plans (
                    stock_id,
                    buy_price,
                    sell_price,
                    max_stocks_allowed,
                    is_active,
                    repeat_enabled,
                    created_at_utc
                )
                select
                    saved_stock.id,
                    @BuyPrice,
                    @SellPrice,
                    @MaxStocksAllowed,
                    @IsActive,
                    @RepeatEnabled,
                    now()
                from saved_stock
                returning *
            )
            select
                saved_trade_plan.id as Id,
                saved_trade_plan.stock_id as StockId,
                saved_trade_plan.buy_price as BuyPrice,
                saved_trade_plan.sell_price as SellPrice,
                saved_trade_plan.max_stocks_allowed as MaxStocksAllowed,
                saved_trade_plan.is_active as IsActive,
                saved_trade_plan.repeat_enabled as RepeatEnabled,
                saved_trade_plan.buy_trigger_count as BuyTriggerCount,
                saved_trade_plan.sell_trigger_count as SellTriggerCount,
                saved_trade_plan.last_buy_triggered_at_utc as LastBuyTriggeredAtUtc,
                saved_trade_plan.last_sell_triggered_at_utc as LastSellTriggeredAtUtc,
                saved_trade_plan.created_at_utc as CreatedAtUtc,
                saved_trade_plan.updated_at_utc as UpdatedAtUtc,
                stocks.symbol as Symbol,
                stocks.name as Name,
                stocks.exchange as Exchange,
                stocks.symbol_token as SymbolToken,
                stocks.trading_symbol as TradingSymbol
            from saved_trade_plan
            join stocks
              on stocks.id = saved_trade_plan.stock_id
            """,
            ToParameters(tradePlan));

        return savedTradePlan;
    }

    private async Task<TradePlan> UpdateAsync(TradePlan tradePlan, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var savedTradePlan = await connection.QuerySingleAsync<TradePlan>(
            """
            with saved_stock as (
                insert into stocks (
                    symbol,
                    name,
                    exchange,
                    symbol_token,
                    trading_symbol,
                    created_at_utc
                )
                values (
                    @Symbol,
                    @Name,
                    @Exchange,
                    @SymbolToken,
                    @TradingSymbol,
                    now()
                )
                on conflict (exchange, symbol_token) do update
                set symbol = excluded.symbol,
                    name = coalesce(excluded.name, stocks.name),
                    trading_symbol = excluded.trading_symbol,
                    updated_at_utc = now()
                returning id
            ),
            saved_trade_plan as (
                update trade_plans
                set stock_id = saved_stock.id,
                    buy_price = @BuyPrice,
                    sell_price = @SellPrice,
                    max_stocks_allowed = @MaxStocksAllowed,
                    is_active = @IsActive,
                    repeat_enabled = @RepeatEnabled,
                    updated_at_utc = now()
                from saved_stock
                where trade_plans.id = @Id
                returning trade_plans.*
            )
            select
                saved_trade_plan.id as Id,
                saved_trade_plan.stock_id as StockId,
                saved_trade_plan.buy_price as BuyPrice,
                saved_trade_plan.sell_price as SellPrice,
                saved_trade_plan.max_stocks_allowed as MaxStocksAllowed,
                saved_trade_plan.is_active as IsActive,
                saved_trade_plan.repeat_enabled as RepeatEnabled,
                saved_trade_plan.buy_trigger_count as BuyTriggerCount,
                saved_trade_plan.sell_trigger_count as SellTriggerCount,
                saved_trade_plan.last_buy_triggered_at_utc as LastBuyTriggeredAtUtc,
                saved_trade_plan.last_sell_triggered_at_utc as LastSellTriggeredAtUtc,
                saved_trade_plan.created_at_utc as CreatedAtUtc,
                saved_trade_plan.updated_at_utc as UpdatedAtUtc,
                stocks.symbol as Symbol,
                stocks.name as Name,
                stocks.exchange as Exchange,
                stocks.symbol_token as SymbolToken,
                stocks.trading_symbol as TradingSymbol
            from saved_trade_plan
            join stocks
              on stocks.id = saved_trade_plan.stock_id
            """,
            ToParameters(tradePlan));

        return savedTradePlan;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            delete from trade_plans
            where id = @Id
            """,
            new { Id = id });
    }

    private static object ToParameters(TradePlan tradePlan)
    {
        return new
        {
            tradePlan.Id,
            tradePlan.StockId,
            tradePlan.BuyPrice,
            tradePlan.SellPrice,
            tradePlan.MaxStocksAllowed,
            tradePlan.IsActive,
            tradePlan.RepeatEnabled,
            tradePlan.Symbol,
            tradePlan.Name,
            tradePlan.Exchange,
            tradePlan.SymbolToken,
            tradePlan.TradingSymbol
        };
    }
}
