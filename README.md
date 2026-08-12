# CloudOrders

An e-commerce checkout backend (Order + Inventory) built as a portfolio project to practice modern .NET architecture, distributed systems, and security practices.

## Architecture

Clean Architecture, four layers with a strict dependency rule — inner layers never reference outer ones:

- **`CloudOrders.Api`** — Controllers, composition root (DI wiring in `Program.cs`), configuration, middleware pipeline
- **`CloudOrders.Infrastructure`** — EF Core, concrete repository implementations, JWT generation, SQL Server access
- **`CloudOrders.Application`** — Use cases (CQRS command/query handlers), abstractions/interfaces consumed by handlers
- **`CloudOrders.Domain`** — Entities with protected invariants (private setters, factory methods, guarded state transitions). Zero dependencies on any other layer.
- **`CloudOrders.Contracts`** — Event contracts shared with the separate `CloudOrders.Inventory` service (messaging DTOs, distinct from the API's own HTTP DTOs in `Api/Contracts/`)

`Application` never references concrete `Infrastructure` types — it defines interfaces (`IOrderRepository`, `IQuerySource`, `ITokenGenerator`, `IUserRepository`) that `Infrastructure` implements. This is genuine Dependency Inversion: `Infrastructure` depends on `Application`, not the other way around, even though at runtime `Application`'s handlers are the ones calling into the concrete implementations via injected interfaces.

## Domain layer

- **`Order`**: private constructor, static factory (`Order.Create(customerId, total)`) validating `total > 0`, private setters on all properties. State transitions (`Confirm()`, `Reject()`) throw `InvalidOperationException` when called from an invalid state — distinguished intentionally from `ArgumentException` (bad input) vs `InvalidOperationException` (bad timing/state).
- **`User`**: same pattern — private constructor, `Create(email, passwordHash)` factory. Receives an already-hashed password; the entity has no knowledge of BCrypt or any hashing algorithm.
- Enum defaults are handled explicitly (`Status = OrderStatus.Pending` set in the private constructor) rather than relying on the coincidental `0` default value.

## CQRS (Command/Query Responsibility Segregation)

Implemented at the application level, not the infrastructure level — single database, two distinct code paths:

- **Command side**: `PlaceOrderCommand` (record, immutable) → `PlaceOrderHandler`, which goes through `Order.Create` (domain validation) and persists via `IOrderRepository`.
- **Query side**: `GetOrderByIdQuery` → `GetOrderByIdHandler`, which reads via `IQuerySource.GetOrderByIdAsync(id, ct)` — a named method (not an exposed `IQueryable<Order>`) implemented in `CloudOrdersDbContext` using `AsNoTracking()`, since the read path never needs change tracking. Returns an `OrderDto` (plain record), never the domain entity directly.

## Data access (EF Core)

- `CloudOrdersDbContext`, `Scoped` lifetime, registered via `AddDbContext`.
- Explicit Fluent API configuration per entity (`OrderConfiguration`, `UserConfiguration`) — no Data Annotations. `Order.Total` mapped as `decimal(18,2)`; `Order.Status` stored as `nvarchar` via `HasConversion<string>()` (immune to enum reordering, unlike storing the raw int).
- `User.Email` has a unique index enforced at the database level (`HasIndex().IsUnique()`), not just in application code, to guard against race conditions on concurrent inserts.
- Migrations: `InitialCreate`, `AddUsers`, tracked via `__EFMigrationsHistory`.
- `EnableRetryOnFailure` configured on `UseSqlServer` to handle Azure SQL serverless auto-pause/resume transient failures (error 40613).

## Authentication (JWT)

- `POST /api/auth/login` validates credentials against `IUserRepository.GetByEmailAsync` + `BCrypt.Verify`; both "user not found" and "wrong password" return the same `null`/`401` to prevent user enumeration.
- Token generation (`JwtTokenGenerator`, implements `ITokenGenerator`) uses HS256, with `sub`/`email` claims only — no sensitive data in the payload (JWT contents are base64-encoded, not encrypted, and readable by anyone holding the token).
- JWT configuration (`Key`, `Issuer`, `Audience`) bound via **Options Pattern** (`JwtOptions`), with `[Required]`/`[MinLength]` Data Annotations and `.ValidateOnStart()` — the app fails to start immediately if configuration is missing or malformed, rather than failing on the first login attempt.
- `[Authorize]` on `OrdersController`; `UseAuthentication()` → `UseAuthorization()` ordering enforced in the middleware pipeline.
- Development seeds a test user (`test@cloudorders.com` / `Test123!`) programmatically on startup, gated behind `IsDevelopment()`.

## Cross-cutting concerns

- **Global exception handling**: `GlobalExceptionHandler` (`IExceptionHandler`), maps `ArgumentException` → 400, `InvalidOperationException` → 409, anything else → 500, returning `ProblemDetails` (RFC 7807). Full exception details are logged server-side only, never exposed in the response body.
- **Input validation**: FluentValidation (`CreateOrderRequestValidator`), auto-registered via `AddValidatorsFromAssemblyContaining` and wired into the pipeline with `AddFluentValidationAutoValidation()`. Kept intentionally separate from domain invariants — the API boundary validates shape/input, the domain enforces business rules, regardless of caller.
- **Health Checks**: `GET /health` (unauthenticated by design — infrastructure/load balancers need to query it without credentials), checks SQL Server connectivity via `AspNetCore.HealthChecks.SqlServer`.

## API surface

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | No | Returns a JWT |
| POST | `/api/orders` | Bearer | Creates an order, returns `201` + `Location` via `CreatedAtAction` |
| GET | `/api/orders/{id:guid}` | Bearer | Returns an `OrderDto` or `404` |
| GET | `/health` | No | `200 Healthy` / `503 Unhealthy` |

## Testing

xUnit + Moq, 7 tests covering `PlaceOrderHandler`, `GetOrderByIdHandler`, and `LoginHandler` — happy path plus failure paths (invalid total, order not found, user not found, wrong password). Dependencies (`IOrderRepository`, `IQuerySource`, `IUserRepository`, `ITokenGenerator`) are mocked; logic that belongs to the class under test (e.g. `BCrypt.Verify` inside `LoginHandler`) is allowed to run for real, since mocking it would test nothing meaningful.

## Deployment (Azure)

- **App Service**: Linux, F1 tier, Brazil South region.
- **Azure SQL Database**: serverless, free tier, same region (co-located with the App Service to avoid cross-region latency).
- Configuration (connection string, JWT settings) supplied via App Service environment variables using the `Section__Key` naming convention, which ASP.NET Core automatically maps to the equivalent `appsettings.json` hierarchy (`ConnectionStrings:CloudOrdersDb`, `Jwt:Key`, etc.) — no code changes required between local and cloud config.
- **CI/CD**: Azure Pipelines, YAML-defined, two stages:
  - `Build`: restore → build → test → publish → artifact
  - `Deploy` (`dependsOn: Build`, `condition: succeeded()`): deploys the published artifact to the App Service via the `AzureWebApp@1` task, authenticated through a Service Connection using Workload Identity Federation (no long-lived secrets stored in Azure DevOps).
  - Tests act as a real gate — a failing test blocks the deploy stage entirely.

## Stack

.NET 10 · ASP.NET Core · EF Core · SQL Server / Azure SQL · FluentValidation · JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) · BCrypt.Net-Next · xUnit + Moq · Azure Pipelines · Azure App Service · Azure SQL Database