using Dapper;
using StockTrading.Common.DTOs;
using StockTrading.Data;
using StockTrading.Models;
using StockTrading.Repository.IRepository;

namespace StockTrading.Repository.Repository;

public sealed class StockRepository(IDbConnectionFactory connectionFactory) : IStockRepository
{
    public async Task<PagedResult<StockListItem>> GetAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = pageSize == 0 ? 0 : Math.Clamp(pageSize, 1, 100);

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql =
            """
            select
                stocks.id as StockId,
                stocks.symbol as Symbol,
                stocks.name as Name,
                stocks.exchange as Exchange,
                stocks.symbol_token as SymbolToken,
                stocks.trading_symbol as TradingSymbol,
                stocks.holding_quantity as HoldingQuantity,
                coalesce(stock_profiles.asset_type, 'Unknown') as AssetType,
                stock_profiles.theme as Theme,
                stock_profiles.sector as Sector,
                stock_profiles.industry as Industry,
                stock_profiles.classification_reason as ClassificationReason,
                stock_profiles.confidence_score as ConfidenceScore,
                stock_profiles.description as Description,
                coalesce(stock_profiles.updated_by_nse, false) as UpdatedByNse,
                coalesce(stock_profiles.updated_by_yahoo, false) as UpdatedByYahoo,
                coalesce(stock_profiles.updated_by_tapetide, false) as UpdatedByTapetide,
                stock_profiles.dividend_yield as DividendYield,
                stock_profiles.growth_rate as GrowthRate,
                stock_profiles.debt_to_equity as DebtToEquity,
                stock_profiles.pe_ratio as PERatio,
                stock_profiles.earnings_per_share as EarningsPerShare,
                stock_profiles.price_to_book as PriceToBook,
                stock_profiles.total_revenue / 10000000 as TotalRevenue,
                stock_profiles.net_income / 10000000 as NetIncome,
                stock_profiles.total_debt / 10000000 as TotalDebt,
                stock_profiles.total_cash / 10000000 as TotalCash,
                stock_profiles.cash_flow / 10000000 as CashFlow,
                stock_profiles.market_cap as MarketCap,
                coalesce(stock_profiles.stock_category, 'Unknown') as StockCategory,
                stock_profiles.stock_category_reason as StockCategoryReason,
                stock_profiles.stock_category_confidence as StockCategoryConfidence,
                stock_profiles.stock_category_updated_at_utc as StockCategoryUpdatedAtUtc,
                stock_profiles.last_analyzed_at_utc as LastAnalyzedAtUtc
            from stocks
            left join stock_profiles
              on stock_profiles.stock_id = stocks.id
            order by stocks.exchange, stocks.symbol
            """;
        if (pageSize > 0)
        {
            sql += """

            limit @PageSize offset @Offset
            """;
        }

        var stocks = await connection.QueryAsync<StockListItem>(
            sql,
            new
            {
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            });
        var totalCount = await connection.ExecuteScalarAsync<int>("select count(*) from stocks");

        return new PagedResult<StockListItem>
        {
            Items = stocks.ToArray(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Stock> UpsertAsync(SaveStockRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        if (request.StockId > 0)
        {
            return await connection.QuerySingleAsync<Stock>(
                """
                update stocks
                set symbol = @Symbol,
                    name = @Name,
                    exchange = @Exchange,
                    symbol_token = @SymbolToken,
                    trading_symbol = @TradingSymbol,
                    updated_at_utc = now()
                where id = @StockId
                returning
                    id as Id,
                    symbol as Symbol,
                    exchange as Exchange,
                    symbol_token as SymbolToken,
                    trading_symbol as TradingSymbol,
                    name as Name,
                    holding_quantity as HoldingQuantity,
                    created_at_utc as CreatedAtUtc,
                    updated_at_utc as UpdatedAtUtc
                """,
                new
                {
                    request.StockId,
                    Symbol = request.Symbol,
                    request.Name,
                    Exchange = request.Exchange.ToString(),
                    request.SymbolToken,
                    request.TradingSymbol
                });
        }

        return await connection.QuerySingleAsync<Stock>(
            """
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
            returning
                id as Id,
                symbol as Symbol,
                exchange as Exchange,
                symbol_token as SymbolToken,
                trading_symbol as TradingSymbol,
                name as Name,
                holding_quantity as HoldingQuantity,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            """,
            new
            {
                Symbol = request.Symbol,
                request.Name,
                Exchange = request.Exchange.ToString(),
                request.SymbolToken,
                request.TradingSymbol
            });
    }

    public async Task<Stock> UpsertOrderStockAsync(
        string symbol,
        string? name,
        string exchange,
        string symbolToken,
        string tradingSymbol,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Stock>(
            """
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
            returning
                id as Id,
                symbol as Symbol,
                exchange as Exchange,
                symbol_token as SymbolToken,
                trading_symbol as TradingSymbol,
                name as Name,
                holding_quantity as HoldingQuantity,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            """,
            new
            {
                Symbol = symbol,
                Name = name,
                Exchange = exchange,
                SymbolToken = symbolToken,
                TradingSymbol = tradingSymbol
            });
    }

    public async Task<Stock?> GetByIdAsync(int stockId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Stock>(
            """
            select
                id as Id,
                symbol as Symbol,
                exchange as Exchange,
                symbol_token as SymbolToken,
                trading_symbol as TradingSymbol,
                name as Name,
                holding_quantity as HoldingQuantity,
                created_at_utc as CreatedAtUtc,
                updated_at_utc as UpdatedAtUtc
            from stocks
            where id = @StockId
            """,
            new { StockId = stockId });
    }

    public async Task<StockDeleteCheck> GetDeleteCheckAsync(int stockId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var deleteCheck = await connection.QuerySingleAsync<StockDeleteCheck>(
            """
            select
                (select count(*) from trade_plans where stock_id = @StockId) as TradePlanCount,
                (
                    select count(*)
                    from trade_plan_runs
                    join trade_plans
                      on trade_plans.id = trade_plan_runs.trade_plan_id
                    where trade_plans.stock_id = @StockId
                ) as TradePlanRunCount,
                0 as OrderCount
            """,
            new { StockId = stockId });

        deleteCheck.OrderCount = await GetOptionalTableReferenceCountAsync(connection, "orders", stockId);
        return deleteCheck;
    }

    public async Task DeleteAsync(int stockId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            delete from stocks
            where id = @StockId
            """,
            new { StockId = stockId });
    }

    private static async Task<int> GetOptionalTableReferenceCountAsync(
        System.Data.IDbConnection connection,
        string tableName,
        int stockId)
    {
        var tableExists = await connection.ExecuteScalarAsync<bool>(
            "select to_regclass(@TableName) is not null",
            new { TableName = $"public.{tableName}" });
        if (!tableExists)
        {
            return 0;
        }

        var hasStockIdColumn = await connection.ExecuteScalarAsync<bool>(
            """
            select exists (
                select 1
                from information_schema.columns
                where table_schema = 'public'
                  and table_name = @TableName
                  and column_name = 'stock_id'
            )
            """,
            new { TableName = tableName });
        if (!hasStockIdColumn)
        {
            return 0;
        }

        return await connection.ExecuteScalarAsync<int>(
            $"select count(*) from {tableName} where stock_id = @StockId",
            new { StockId = stockId });
    }
}
