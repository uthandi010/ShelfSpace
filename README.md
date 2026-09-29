# ShelfSpace

An inventory and order management tool for a small seller — a real .NET Web API paired with a
native **.NET MAUI desktop client**, rather than another browser app. Track stock, take orders,
and watch what's selling, from a Windows desktop app.

## Features

- **Auth** — register/login with JWT, PBKDF2 password hashing
- **Inventory** — add products with a SKU, price, and starting stock; restock; a computed
  low-stock flag per product
- **Orders that actually manage stock** — placing an order checks every line has enough stock
  *before* touching anything (an order with one out-of-stock line is rejected as a whole, not
  partially applied), decrements inventory, and snapshots each line's price at order time so a
  later price change never rewrites history
- **Cancelling a pending order restores its stock**; fulfilling one doesn't touch stock again
  (it was already reserved at order time) - only a `Pending` order can be fulfilled or cancelled
- **Dashboard** — total products, low-stock count, pending orders, and revenue (only from
  *fulfilled* orders, not merely placed ones)

## Stack

| Layer | Tech |
| --- | --- |
| API | ASP.NET Core 9 (.NET 9), EF Core, SQLite, JWT bearer auth |
| Desktop client | .NET MAUI (Windows), MVVM via CommunityToolkit.Mvvm |
| Tests | xUnit + `WebApplicationFactory` (API, 30 examples), xUnit (client ViewModels/services, 38 examples) |

No paid services or cloud accounts are required to run this locally.

## Project layout

```
server/
  ShelfSpace.Api/         # the Web API
  ShelfSpace.Api.Tests/   # xUnit integration tests, run against an in-memory SQLite db
client/
  ShelfSpace.App.Core/    # platform-agnostic: models, services (ApiClient/Auth/Product/Order),
                          # and CommunityToolkit.Mvvm view models - no MAUI reference at all
  ShelfSpace.App/         # the MAUI head (Windows): XAML pages, DI wiring, Preferences-backed
                          # token storage
  ShelfSpace.App.Tests/   # plain xUnit tests against ShelfSpace.App.Core - no UI, no MAUI runtime
```

`ShelfSpace.App.Core` is deliberately its own class library targeting plain `net9.0`, not the
MAUI head's `net9.0-windows10.0.19041.0`. Splitting it out is what makes the ViewModels testable
with an ordinary `dotnet test` run: no UI automation, no MAUI test harness, no Windows dependency
in the test project at all - just the view models and services with a faked `HttpMessageHandler`.

## Running it locally

### API

```bash
cd server/ShelfSpace.Api
dotnet run
```

The first run applies EF Core migrations automatically and creates `shelfspace.db` (SQLite) next
to the project. By default it listens on `http://localhost:5098` (see `Properties/launchSettings.json`).

Run the backend tests:

```bash
cd server
dotnet test
```

### Desktop client

Requires the MAUI workload (`dotnet workload install maui`) and Windows.

```bash
cd client/ShelfSpace.App
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0
```

If your API isn't running on the default port, set `SHELFSPACE_API_URL` before launching (e.g.
`SHELFSPACE_API_URL=http://localhost:5210 dotnet run -f net9.0-windows10.0.19041.0`).

Run the client's tests (no MAUI/Windows runtime needed for this):

```bash
cd client
dotnet test ShelfSpace.App.Tests
```

## Notes on the design

- **Order creation is all-or-nothing.** Every line's stock is checked before any product is
  touched, so a 3-item order with one out-of-stock line is rejected entirely rather than silently
  fulfilling 2 of the 3 lines.
- **Price is snapshotted per order line** (`UnitPriceCentsAtOrder`) rather than looked up live, so
  changing a product's price later doesn't rewrite what a past order actually charged.
- SQLite serializes writes at the connection level, which is safe for this project's scale; a
  multi-writer production database would additionally want a row lock or an optimistic
  concurrency token on `Product.QuantityOnHand` for the stock-decrement step.
- The JWT signing key in `appsettings.json` is a placeholder for local development only - replace
  it (and move it to a secret store) before deploying anywhere real.

## What's intentionally left out

This is a portfolio-scale MVP, not a production inventory system. A few things that would come
next: barcode scanning, multiple staff accounts per shop (this app is deliberately single-account,
unlike the multi-tenant/team projects elsewhere in this set - a small seller usually just wants
their own login), and CSV import/export for bulk product updates.
