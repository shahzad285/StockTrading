# StockTrading Business And Implementation Notes

This file is the business-context companion to `AGENTS.md`.

Future agents should read this file before changing business behavior. The user will
keep adding current business rules, domain decisions, app behavior, and workflow
notes here as the StockTrading app evolves.

Do not put real secrets, broker credentials, OTP provider keys, tokens, passwords,
or private account values in this file.

## Product Purpose

StockTrading is a personal trading assistant focused on Indian stock workflows.
The app currently supports:

- OTP-based user login and JWT-authenticated API access.
- Super-admin broker login for Angel One SmartAPI.
- Stock master management for NSE/BSE stocks.
- Stock charting using SmartAPI historical candles.
- Current price refresh for saved Stock Master rows.
- Stock fundamentals enrichment from NSE India, Yahoo Finance, and Tapetide.
- Trade plan creation, editing, listing, details, and deletion.
- Broker account balance lookup through Angel One SmartAPI RMS limits.
- Basic order passthrough operations through the active broker service.
- Manual and trade-plan-created orders are tracked locally after successful broker placement.

## Business Concepts

### Stock Master

The Stock Master is the app's saved universe of known stocks.

- A stock is identified primarily by `exchange` plus `symbol_token`.
- `symbol`, `trading_symbol`, and `name` are saved for display and broker calls.
- Stock Master rows are stored in the `stocks` table.
- Stock fundamentals and classification data are stored separately in
  `stock_profiles`.
- Deleting a stock is blocked when active dependencies exist, such as trade plans, trade plan runs, or optional order records.

### Trade Plans

Trade plans represent intended buy/sell setups for a stock.

- Trade plans are stored in `trade_plans`.
- A trade plan always points to a stock through `stock_id`.
- The UI can create, edit, view details for, and delete trade plans.
- Backend trade-plan monitoring can place broker orders automatically.
- Buy execution uses the trade plan's max stocks allowed.
- Sell execution uses the stock's confirmed holding quantity.
- Trade plan runs are modeled in `trade_plan_runs`, but active backend execution
  workflow still needs to be expanded around local order tracking.

### Fundamentals

Fundamental data is saved in `stock_profiles`.

Supported/profiled fields include:

- Asset type, theme, sector, industry, description.
- Data-source flags: NSE, Yahoo Finance, Tapetide.
- Dividend yield, growth rate, debt-to-equity, P/E ratio.
- EPS, price-to-book, revenue, net income, debt, cash, cash flow, market cap.
- Stock category fields for later classification work.

Current enrichment order:

1. NSE India profile for NSE stocks.
2. Yahoo Finance company profile.
3. Tapetide company profile as a fallback or supplement.

The app treats a stock profile as incomplete when important profile or financial
fields are missing.

### Charts

Charts use the shared common endpoint and Angel One SmartAPI candle data.

Current chart ranges:

- `OneDay`: previous 1 day, `FIVE_MINUTE` interval.
- `OneWeek`: previous 7 days, `THIRTY_MINUTE` interval.
- `OneMonth`: previous 1 month, `ONE_DAY` interval.
- `SixMonths`: previous 6 months, `ONE_DAY` interval.
- `OneYear`: previous 1 year, `ONE_DAY` interval.

The frontend renders a closing-price line chart and also calculates/display values
such as open, high, low, close, 52-week high, and 52-week low from candle data.

## Backend Implementation

### API Project

Project: `StockTrading.Apis`

Important setup:

- Controllers use route pattern `[controller]`.
- JWT bearer auth is configured globally.
- Fallback authorization requires authenticated users.
- `AccountController` has anonymous login/register/OTP endpoints.
- SmartAPI login is restricted to the `SuperAdmin` role.
- CORS uses `Cors:AllowedOrigins`.
- Database initialization runs on app startup.
- Background workers are registered from `Program.cs`.

Registered major services/repositories include:

- Account, stock, order, and trade plan services.
- Stock fundamentals and market schedule services.
- Application user, role, OTP repositories.
- Stock, stock profile, and trade plan repositories.
- Broker session repository and encrypted broker session store.
- Angel One as the active broker service.

### Controllers

#### `AccountController`

Routes:

- `POST /Account/register`
- `POST /Account/login/request-otp`
- `POST /Account/login`
- `POST /Account/smartapi/login`
- `GET /Account/profile`
- `GET /Account/balance`
- `GET /Account/me`

Behavior:

- OTP login supports configured login methods.
- Login returns JWT data.
- Profile returns authenticated user profile data.
- Balance returns broker RMS fund, cash, collateral, and utilized margin data.
- SmartAPI login stores broker session information for broker-backed calls.

#### `StockController`

Routes:

- `GET /Stock/stocks`
- `POST /Stock/stocks`
- `DELETE /Stock/stocks/{stockId}`
- `GET /Stock/holdings`
- `GET /Stock/prices`

Behavior:

- Stock save validates symbol, symbol token, and exchange.
- Stock delete checks dependencies before deleting.
- Holdings and prices are retrieved through the broker/stock service.
- Configured prices are based on saved Stock Master rows.

#### `TradePlanController`

Routes:

- `GET /TradePlan`
- `POST /TradePlan`
- `DELETE /TradePlan/{id}`

Behavior:

- `POST` creates or updates based on whether `Id > 0`.
- Saves/updates the related stock by exchange and symbol token.
- Validates symbol, symbol token, buy price, sell price, and quantity.

#### `CommonController`

Routes:

- `GET /Common/StockSearch`
- `GET /Common/StockChart`

Behavior:

- Shared stock search endpoint used by Stock Master and Trade Plan UI.
- Shared chart endpoint used wherever stock charting is needed.
- Search currently supports NSE/BSE with NSE as the default.

#### `OrderController`

Routes:

- `GET /Order`
- `GET /Order/{brokerOrderId}`
- `POST /Order`
- `DELETE /Order/{brokerOrderId}`
- `GET /Order/{brokerOrderId}/history`

Behavior:

- Order operations pass through `IOrderService`, which currently delegates to the
  active broker service.
- Buy orders are checked against broker `AvailableCash` before being sent to the
  active broker service.

## Data Model

Main tables created/maintained by `DapperDatabaseInitializer`:

- `roles`
- `users`
- `user_roles`
- `user_otps`
- `stocks`
- `stock_profiles`
- `market_job_decisions`
- `trade_plans`
- `trade_plan_runs`
- `broker_sessions`

Important relationships:

- `stock_profiles.stock_id -> stocks.id`
- `trade_plans.stock_id -> stocks.id`
- `trade_plan_runs.trade_plan_id -> trade_plans.id`

Important uniqueness rules:

- Stocks are unique by `(exchange, symbol_token)`.
- Stock profiles are unique by `stock_id`.

Historical cleanup note:

- The initializer drops old watchlist tables and trade-plan watchlist columns.

## Repository And Service Behavior

### `StockService`

- Reads Stock Master from `StockRepository`.
- Saves normalized stocks.
- Blocks stock deletion when dependent records exist.
- Searches stocks through the broker.
- Retrieves chart candles through the broker.
- Retrieves configured prices from saved Stock Master rows through the shared market-data cache.
- Applies market schedule decisions before price calls.

### `MarketDataCacheService`

- Caches latest stock prices by `exchange + symbol_token`.
- Caches active broker account balance.
- Uses shorter TTLs during trading hours and longer TTLs outside trading hours.
- TTLs are configured through the `MarketDataCache` configuration section.
- Current development TTLs: trading-hours prices 15 seconds, trading-hours balance 30 seconds, after-hours prices 120 minutes, after-hours balance 30 minutes.
- Failed broker calls are cached briefly for 5 seconds to avoid tight retry loops.

### `TradePlanService`

- Normalizes trade-plan stock identity and status.
- Requires valid symbol, symbol token, buy price, sell price, and quantity.
- Delegates create/update/delete to `TradePlanRepository`.

### `StockFundamentalsService`

- Finds incomplete stock profiles through `StockProfileRepository`.
- Uses NSE India, Yahoo Finance, and Tapetide to fill profile/fundamental fields.
- Saves source-specific fields without wiping existing non-null data.

### `MarketScheduleService`

- Decides whether market-sensitive jobs should run.
- Stores decisions in `market_job_decisions`.
- Used by price polling and configured price refresh.

### `BrokerSessionStore`

- Stores broker sessions for broker-backed work.
- Broker session values should be encrypted and must not be documented in tracked
  files.

### `OrderService`

- Checks broker account balance before buy orders.
- Uses `AvailableCash` from the Angel One RMS balance response as the buy-order
  affordability amount.
- Reads broker balance through the shared market-data cache.
- Rejects buy orders when `quantity * price` is greater than available cash.
- Sell orders do not use this cash-balance check.
- Resolves or creates the related Stock Master row before broker placement.
- Saves successfully placed broker orders locally when a broker order id is returned.
- Records local order history for placed orders.

## Background Jobs

### `StockPricePollingWorker`

Purpose:

- Refreshes saved Stock Master prices in the background.

Behavior:

- Controlled by `StockPolling` configuration.
- Skips when market schedule says jobs are disabled.
- Skips when Angel One broker session is unavailable.
- Uses `IStockService.RefreshConfiguredPricesAsync`.

### `StockFundamentalsPollingWorker`

Purpose:

- Refreshes missing or incomplete stock fundamentals in the background.

Behavior:

- Controlled by `FundamentalsPolling` configuration.
- Processes up to `MaxStocksPerRun`.
- Pulls from the full `stocks` table.

### `OrderStatusTrackingWorker`

Purpose:

- Tracks open local orders against the active broker order book.

Behavior:

- Updates local order status and history.
- After confirmed partial or full fills, reconciles `stocks.holding_quantity` from broker holdings when available.
- Falls back to newly confirmed filled-share deltas only when broker holdings cannot be matched or fetched.
- Buy fills increase holdings; sell fills decrease holdings when fallback delta logic is used.

### `TradePlanExecutionWorker`

Purpose:

- Checks active trade plans and creates broker orders when buy/sell prices trigger.

Behavior:

- Reads prices through the shared market-data cache.
- Buy orders use `MaxStocksAllowed`.
- Sell orders use current confirmed `stocks.holding_quantity`.
- Skips duplicate open orders for the same trade plan side.

## Frontend Implementation

Project: `StockTrading.Web`

Main app file:

- `src/App.tsx`

Main pages:

- `dashboard`
- `stocks`
- `tradeplans`

API client modules:

- `accountApi.ts`
- `stockApi.ts`
- `tradePlanApi.ts`
- `orderApi.ts`

### Login And Session

- Users request an OTP and then log in.
- JWT is stored client-side through the existing auth helper.
- When auth expires, the app clears session state and shows a login message.

### Dashboard

The dashboard loads:

- Profile.
- Broker connection state.
- Holdings.
- Prices.
- Orders.

### Stocks Page

The Stocks page is the Stock Master UI.

Capabilities:

- Search stock through `/Common/StockSearch`.
- Select NSE/BSE stock result.
- Save stock to the master list.
- View saved stock rows.
- Open stock details.
- Open chart.
- Remove stock if no dependencies block deletion.

### Trade Plan Page

Capabilities:

- Search stock through `/Common/StockSearch`.
- Select stock.
- Enter buy price, sell price, quantity, optional max budget.
- Set status.
- Set active/repeat flags.
- Create, edit, list, inspect, and delete trade plans.

Current limitation:

- Trade plans do not yet have backend monitoring/execution.

### Charts

Shared chart behavior:

- Uses `/Common/StockChart`.
- Opens from Stock Master rows.
- Uses selected range.
- Displays candle-derived summary values.
- Loads one-year candles for 52-week high/low.

## Current Business Rules

- Exchanges are limited to NSE and BSE in current UI/API stock search flows.
- NSE is the default exchange.
- Stock identity should be selected from broker search results instead of manually
  typed wherever the search UI is available.
- `exchange + symbol_token` is the stable stock identity for upsert behavior.
- Stock Master deletion is hard delete only after dependency checks pass.
- Market-sensitive price refresh should respect market schedule decisions.
- Background price polling requires an available Angel One broker session.
- Fundamentals polling works for all saved stocks.
- Buy order placement requires enough broker `AvailableCash` for
  `quantity * price`.
- Stock holding quantity is updated only after partial or full broker fills are confirmed, preferring broker holdings as the source of truth.

## Known Gaps And Planned Work

From current project notes:

- Add details for stocks created or recommended by reputable investors/retailers.
- Plan stock category/type classification using Peter Lynch categories:
  Slow Grower, Stalwart, Fast Grower, Cyclical, Turnaround, and Asset Play.
- Add realtime or near-realtime price updates for trade plans.
- Add backend monitoring for trade plans so planned trades can be checked even when
  the browser is closed.
- Consider SignalR after backend monitoring exists.
- Improve stock search with a local instrument master or fallback strategy.
- Track Angel One JWT and refresh token expiry in broker session storage.
- Research a source for identifying Shariah-compliant stocks in India.
- Add chunked historical candle fetching if charts need more data than one SmartAPI
  candle request can reliably return.

## Agent Guidance For Future Changes

- Read `AGENTS.md` first, then this file.
- Keep this file updated when business rules or implemented workflows change.
- When adding a new domain rule, document the rule and where it is enforced.
- When adding a new user workflow, document the UI page, API endpoint, service, and
  database table involved.
- Keep `Models` limited to database-mapped classes.
- Keep common DTOs, requests, and responses under `StockTrading.Common`.
- Keep enums under `StockTrading.Common/Enums`.
- Do not add real configuration values to this file or any tracked public file.
