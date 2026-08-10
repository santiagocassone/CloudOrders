# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

CloudOrders is a .NET 10 / ASP.NET Core backend for an e-commerce checkout (Order + Inventory), built as a portfolio project to practice modern .NET architecture, distributed systems, and security practices. It's in active early development (no CI/CD or automated test coverage yet). There is no `.sln` file — build/run against individual `.csproj` files or the whole `src`/`tests` tree.

## Commands

```bash
dotnet restore
dotnet build

# Apply EF Core migrations (requires local SQL Server / LocalDB)
dotnet ef database update --project src/CloudOrders.Infrastructure --startup-project src/CloudOrders.Api

# Run the API
dotnet run --project src/CloudOrders.Api --launch-profile https

# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"

# Add a new migration
dotnet ef migrations add <Name> --project src/CloudOrders.Infrastructure --startup-project src/CloudOrders.Api
```

In `Development`, a test user is auto-seeded on startup: `test@cloudorders.com` / `Test123!` (see `Program.cs`).

## Architecture

Clean Architecture, 4 layers with a strict dependency rule — inner layers never reference outer ones:

- `CloudOrders.Api` — Controllers, composition root (DI wiring in `Program.cs`), configuration
- `CloudOrders.Infrastructure` — EF Core, concrete repositories, JWT generation, SQL Server
- `CloudOrders.Application` — Use cases (CQRS handlers), abstractions/interfaces
- `CloudOrders.Domain` — Entities with protected invariants (factory methods, guarded state transitions). No dependencies on other layers.
- `CloudOrders.Contracts` — Events shared with the separate `CloudOrders.Inventory` service (messaging contracts, not HTTP DTOs — see note below)

`Application` never depends on concrete `Infrastructure` types — it defines interfaces (`IOrderRepository`, `IQuerySource`, `ITokenGenerator`, `IUserRepository`) that `Infrastructure` implements. This is genuine Dependency Inversion: use cases are testable without a real database, and the domain stays free of technical detail.

**CQRS is real, not just a naming convention**: the Command side goes through the domain and enforces invariants (`IOrderRepository`); the Query side reads via `IQuerySource` with `AsNoTracking()`, bypassing the repository entirely.

### Folder organization is by technical layer, not by feature — where to find things

**Create an order (`POST /api/orders`):**
- `src/CloudOrders.Domain/Order.cs` — entity with invariants (factory method, valid state transitions)
- `src/CloudOrders.Application/Orders/PlaceOrderCommand.cs` + `PlaceOrderHandler.cs` — use case
- `src/CloudOrders.Application/Abstractions/IOrderRepository.cs` — persistence contract
- `src/CloudOrders.Infrastructure/Persistence/SqlOrderRepository.cs` + `OrderConfiguration.cs` — EF Core implementation
- `src/CloudOrders.Api/Contracts/CreateOrderRequest.cs` + `CreateOrderRequestValidator.cs` — HTTP DTO and validation
- `src/CloudOrders.Api/Controllers/OrdersController.cs` — endpoint

**Get an order (`GET /api/orders/{id}`):**
- `src/CloudOrders.Application/Orders/GetOrderByIdQuery.cs` + `GetOrderByIdHandler.cs` + `OrderDto.cs`
- `src/CloudOrders.Application/Abstractions/IQuerySource.cs` — read-only side of CQRS, no tracking

**Login (`POST /api/auth/login`):**
- `src/CloudOrders.Domain/User.cs`, `src/CloudOrders.Application/Auth/*`, `src/CloudOrders.Infrastructure/Auth/JwtTokenGenerator.cs`, `src/CloudOrders.Api/Controllers/AuthController.cs`

**Naming trap**: `src/CloudOrders.Api/Contracts/` (this API's HTTP DTOs) and `src/CloudOrders.Contracts/` (messaging events shared with Inventory) are unrelated folders that happen to share a generic industry-standard name.

## Key technical decisions

- **JWT with HS256**, passwords hashed with BCrypt (never stored plain), minimal claims (no sensitive data in the payload — JWT contents are public).
- **Global exception handling** via `IExceptionHandler` (`GlobalExceptionHandler`), returning `ProblemDetails` (RFC 7807) — never raw stack traces to the client.
- **Input validation is kept separate from domain invariants on purpose**: FluentValidation runs at the API boundary; business rules live in the entities themselves (e.g. `Order.Create`, `Order.Confirm`). This is intentional duplication, not redundancy to clean up.

## Cross-service work (CloudOrders.Inventory)

CloudOrders.Api integrates with a separate `CloudOrders.Inventory` service (owned by a collaborator, not in this repo) via Azure Service Bus. Contracts, channel names, and endpoint status are tracked in [`CONTRACTS.md`](./CONTRACTS.md) — check it before touching `src/CloudOrders.Contracts/` or adding order-fulfillment messaging, and update it when contracts change. Local dev uses the Service Bus Emulator via Docker with the same topic/subscription names as production.

Note: `Order` (in `CloudOrders.Domain`) does not yet have `OrderLines`, so the real `OrderPlaced` event described in `CONTRACTS.md` can't be published until the domain model is extended.

## Stack

.NET 10 · ASP.NET Core · EF Core · SQL Server · FluentValidation · JWT Bearer · BCrypt.Net
