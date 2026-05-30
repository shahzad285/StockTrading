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
- A single active watchlist of tracked stocks.
- Watchlist and stock charting using SmartAPI historical candles.
- Current price refresh for configured/watchlist stocks.
- Stock fundamentals enrichment from NSE India, Yahoo Finance, and Tapetide.
- Trade plan creation, editing, listing, details, and deletion.
- Broker account balance lookup through Angel One SmartAPI RMS limits.
- Basic order passthrough operations through the active broker service.

## Business Concepts

### Stock Master

The Stock Master is the app's saved universe of known stocks.

- A stock is identified primarily by `exchange` plus `symbol_token`.
- `symbol`, `trading_symbol`, and `name` are saved for display and broker calls.
- Stock Master rows are stored in the `stocks` table.
- Stock fundamentals and classification data are stored separately in
  `stock_profiles`.
- Deleting a stock is blocked when active dependencies exist, such as watchlist
  entries, trade plans, trade plan runs, or optional order records.

### Watchlist

The current watchlist implementation is one active tracked-stock list, not multiple
named lists.

- Watchlist entries are stored in `watchlist`.
- A watchlist row points to one stock through `stock_id`.
- Removing from watchlist soft-disables the row by setting `is_active = false`.
- Re-adding the same stock reactivates the existing watchlist row.
- The watchlist page also displays profile/fundamental data from
  `stock_profiles`.
- Watchlist price samples are stored in `watchlist_data`.

Business interpretation:

- Stock Master means "stocks known to the app."
- Watchlist means "stocks currently being actively tracked."

### Trade Plans

Trade plans represent intended buy/sell setups for a stock.

- Trade plans are stored in `trade_plans`.
- A trade plan always points to a stock through `stock_id`.
- A trade plan can optionally point to a watchlist row through `watchlist_id`.
- The UI can create, edit, view details for, and delete trade plans.
- Backend trade-plan monitoring and automated execution are not implemented yet.
- Trade plan runs are modeled in `trade_plan_runs`, but active backend execution
  workflow still needs to be built.

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

- Account, stock, order, watchlist, trade plan services.
- Stock fundamentals and market schedule services.
- Application user, role, OTP repositories.
- Stock, stock profile, watchlist, watchlist data, trade plan repositories.
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
- Configured prices are based on active watchlist stocks.

#### `WatchlistController`

Routes:

- `GET /Watchlist/stocks`
- `POST /Watchlist/stocks`
- `DELETE /Watchlist/stocks/{symbol}`
- `DELETE /Watchlist/stocks/by-id/{watchlistId}`

Behavior:

- `GET` returns active watchlist entries with stock/profile data.
- `POST` upserts stock, stock profile basics, and active watchlist row.
- Delete soft-removes a stock from watchlist.

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

- Shared stock search endpoint used by Stock Master, Watchlist, and Trade Plan UI.
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
- `watchlist`
- `watchlist_data`
- `market_job_decisions`
- `trade_plans`
- `trade_plan_runs`
- `broker_sessions`

Important relationships:

- `stock_profiles.stock_id -> stocks.id`
- `watchlist.stock_id -> stocks.id`
- `watchlist_data.watchlist_id -> watchlist.id`
- `watchlist_data.stock_id -> stocks.id`
- `trade_plans.stock_id -> stocks.id`
- `trade_plans.watchlist_id -> watchlist.id`
- `trade_plan_runs.trade_plan_id -> trade_plans.id`

Important uniqueness rules:

- Stocks are unique by `(exchange, symbol_token)`.
- Stock profiles are unique by `stock_id`.
- Watchlist rows are unique by `stock_id`.
- Watchlist data is unique by `(watchlist_id, trading_date)` and also by
  `(stock_id, trading_date)`.

Historical migration note:

- The initializer migrates old `watchlist_items` data into the current `watchlist`
  table if that old table exists.
- It also migrates old trade-plan `watchlist_item_id` references into
  `watchlist_id` when possible.

## Repository And Service Behavior

### `StockService`

- Reads Stock Master from `StockRepository`.
- Saves normalized stocks.
- Blocks stock deletion when dependent records exist.
- Searches stocks through the broker.
- Retrieves chart candles through the broker.
- Retrieves configured prices from active watchlist rows.
- Stores daily price samples through `WatchlistDataRepository`.
- Applies market schedule decisions before price calls.

### `WatchlistService`

- Reads active watchlist rows.
- Normalizes stock identity fields before saving.
- Saves a watchlist stock through `WatchlistRepository.UpsertAsync`.
- Deletes by symbol or watchlist id.

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
- Rejects buy orders when `quantity * price` is greater than available cash.
- Sell orders do not use this cash-balance check.

## Background Jobs

### `StockPricePollingWorker`

Purpose:

- Refreshes configured/watchlist prices in the background.

Behavior:

- Controlled by `StockPolling` configuration.
- Skips when market schedule says jobs are disabled.
- Skips when Angel One broker session is unavailable.
- Uses `IStockService.RefreshConfiguredPricesAsync`.
- Persists watchlist daily price samples.

### `StockFundamentalsPollingWorker`

Purpose:

- Refreshes missing or incomplete stock fundamentals in the background.

Behavior:

- Controlled by `FundamentalsPolling` configuration.
- Processes up to `MaxStocksPerRun`.
- Pulls from the full `stocks` table, not only the watchlist.

## Frontend Implementation

Project: `StockTrading.Web`

Main app file:

- `src/App.tsx`

Main pages:

- `dashboard`
- `stocks`
- `watchlists`
- `tradeplans`

API client modules:

- `accountApi.ts`
- `stockApi.ts`
- `watchlistApi.ts`
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

### Watchlists Page

The Watchlists page is the active tracked-stock UI.

Capabilities:

- Search stock through `/Common/StockSearch`.
- Select NSE/BSE stock result.
- Save to active watchlist.
- Add optional theme, sector, industry, confidence, and classification reason.
- Display active tracked stocks.
- Open details.
- Open chart.
- Remove from watchlist by `watchlistId`.

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
- Opens from Stock Master and Watchlist rows.
- Uses selected range.
- Displays candle-derived summary values.
- Loads one-year candles for 52-week high/low.

## Current Business Rules

- Exchanges are limited to NSE and BSE in current UI/API stock search flows.
- NSE is the default exchange.
- Stock identity should be selected from broker search results instead of manually
  typed wherever the search UI is available.
- `exchange + symbol_token` is the stable stock identity for upsert behavior.
- Watchlist removal is soft delete.
- Stock Master deletion is hard delete only after dependency checks pass.
- Market-sensitive price refresh should respect market schedule decisions.
- Background price polling requires an available Angel One broker session.
- Fundamentals polling should work for all saved stocks, not only watchlist stocks.
- Buy order placement requires enough broker `AvailableCash` for
  `quantity * price`.

## Known Gaps And Planned Work

From current project notes:

- Add details for stocks created or recommended by reputable investors/retailers.
- Plan stock category/type classification using Peter Lynch categories:
  Slow Grower, Stalwart, Fast Grower, Cyclical, Turnaround, and Asset Play.
- Add realtime or near-realtime price updates for watchlists and trade plans.
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
