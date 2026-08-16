# Service Bus Interview Prep

A deliberately small .NET 10 API for practicing senior-level Azure and backend interview topics. Its working feature is simple: inventory reservation events are published to Azure Service Bus. Development uses an in-memory publisher; production uses Azure Service Bus queues.

## Run it

```powershell
dotnet run --project Inventory
```

```powershell
Invoke-RestMethod http://localhost:5000/orders -Method Post -ContentType application/json -Body '{"productId":"keyboard","quantity":2}'
```

Use Service Bus without putting a secret in source control:

```powershell
$env:ServiceBus__UseInMemory = "false"
$env:ServiceBus__ConnectionString = "<connection-string-from-Key-Vault-or-managed-configuration>"
$env:ServiceBus__OrderPlacedQueueName = "order-placed"
$env:ServiceBus__StockResultsQueueName = "stock-results"
dotnet run --project Inventory
```

Run tests:

```powershell
dotnet test Inventory/Inventory.Tests
```

## Azure deployment configuration (Inventory)

For Azure App Service / Container Apps, set these app settings for the Inventory service:

- `ServiceBus__UseInMemory=false`
- `ServiceBus__ConnectionString=<from Key Vault or secure app setting>`
- `ServiceBus__OrderPlacedQueueName=order-placed`
- `ServiceBus__StockResultsQueueName=stock-results`

With this configuration, Inventory:

- consumes `OrderPlaced` messages from `order-placed`
- publishes `StockReserved` / `StockRejected` to `stock-results`

## Current queue wiring (writer/listener)

- `order-placed`
  - Writer: `CloudOrders.Api`
  - Listener: `Inventory` (`ServiceBusOrderPlacedConsumer`)
- `stock-results`
  - Writer: `Inventory` (`ServiceBusOrderEventsPublisher`)
  - Listener: `CloudOrders` (pending runtime Service Bus consumer wiring)

## Pending issues owned by CloudOrders (Santiago)

The following integration gaps are outside Inventory ownership and remain pending in CloudOrders:

1. `CONTRACTS.md` defines `OrderPlaced` with `CustomerId`, `Total`, `Lines`, and `OccurredAt`, but CloudOrders currently publishes a reduced payload (`OrderId`, `Items`).
2. CloudOrders still needs a consumer flow for fulfillment events (`StockReserved` / `StockRejected`) from queue `stock-results`.
3. CloudOrders production settings must include Service Bus keys (`ServiceBus:FullyQualifiedNamespace`, `ServiceBus:OrderPlacedQueue`) so startup validation passes in Azure.

## Service Bus first

`OrderSubmissionService` owns business intent. `IOrderEventsPublisher` is the application boundary. `ServiceBusOrderEventsPublisher` maps the event to a message with an idempotency-friendly `MessageId`, `CorrelationId`, event type, subject, and JSON content type. `ServiceBusMessageSender` is the Azure SDK adapter and is asynchronously disposed with its client. This separation makes the business logic testable without an Azure connection.

For a production consumer, use queue-based duplicate detection, retry/dead-letter policy, idempotent handling keyed by `MessageId`, and a separately deployed `ServiceBusProcessor` or Azure Function. Do not perform irreversible work before a message is safely published; use an outbox when a database transaction and publication must be atomic.

## Tests and mocking

The tests use xUnit and Moq to verify two things that are easy to miss in live coding:

- `OrderSubmissionService` actually awaits publishing, rather than fire-and-forget work.
- The Service Bus adapter produces the expected message metadata without mocking Azure SDK types directly.

The test pyramid for the next iteration is: domain/service unit tests, API plus Testcontainers/Azurite-style integration tests, then a small functional suite against a deployed environment.

## Interview concept map

| Topic | Where to discuss it |
|---|---|
| C#, async/await, cancellation, IDisposable | Async publisher boundary, cancellation propagation, `IAsyncDisposable` sender/client. |
| Thread safety, TPL, collections, LINQ | `ConcurrentQueue` local publisher; use bounded concurrency for consumers, not unbounded `Parallel.ForEach`. |
| ASP.NET Core, middleware, DI, configuration | Minimal API, exception handler, `ProblemDetails`, scoped service, options, environment variables. |
| Microservices and resilience | Service Bus decouples services; add retry/circuit breakers for REST or gRPC dependencies. |
| Azure Functions/App Service/managed services | Host this API in App Service or Container Apps; move subscription handling to Functions for event-driven scale. |
| Key Vault, IAM, TLS, OAuth/OIDC, OWASP | Managed identity and Key Vault for secrets; HTTPS; add JWT bearer auth and authorization before exposing write endpoints. |
| SQL versus Cosmos DB | SQL for transactional order/outbox data; Cosmos for partitioned, high-scale document/read models. Explain partition key and consistency trade-offs. |
| EF Core and query performance | Keep persistence behind a repository; project only needed columns, index filters, avoid N+1 loading, measure before optimizing. |
| CI/CD and release strategy | The included pipeline builds/tests. Add environment approvals, Key Vault references, health checks, canary/blue-green rollout, and feature flags. |
| Monitoring and diagnostics | Correlation id is carried into the event. Add OpenTelemetry/Application Insights traces, metrics, health checks, and alerts. |
| SOLID, KISS, DRY, YAGNI, patterns | One small interface boundary, DI, and adapter pattern. Avoid building a generic message framework before it is needed. |
| AI-assisted SDLC | Use AI to scaffold and propose tests, then inspect dependencies, cancellation/error behavior, security, and test assertions before merging. |
| React/full-stack and collaboration | A React client can call `POST /orders`; describe API contracts, pull-request review, mentoring, and incremental delivery. |

## Deliberate next exercises

1. Add a SQL outbox and a background publisher, then test the failure window.
2. Add a queue consumer with idempotency and dead-letter handling.
3. Add JWT validation through Microsoft Entra ID and an authorization policy.
4. Add OpenTelemetry/Application Insights plus a load test and endpoint benchmark.
5. Compare an Azure Function consumer with a hosted worker using measured throughput and cost.
