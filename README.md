# CloudOrders

Backend de un checkout de e-commerce (Order + Inventory), construido como proyecto de portfolio para practicar arquitectura .NET moderna, sistemas distribuidos y buenas prácticas de seguridad. Cada decisión técnica está documentada con su razón, no solo su implementación.

## Estado actual

- ✅ `CloudOrders.Api` — órdenes (CQRS completo), autenticación JWT, validación, exception handling global
- 🔜 `CloudOrders.Inventory` — reserva de stock vía eventos (en desarrollo compartido, ver `CONTRACTS.md`)
- 🔜 CI/CD, despliegue a Azure, tests automatizados

## Arquitectura

Clean Architecture con 4 capas, regla de dependencia estricta (las capas internas nunca conocen a las externas):

- `CloudOrders.Api` → Controllers, composition root (DI), configuración
- `CloudOrders.Infrastructure` → EF Core, repositorios concretos, JWT, SQL Server
- `CloudOrders.Application` → Casos de uso (CQRS), interfaces (Abstractions)
- `CloudOrders.Domain` → Entidades con invariantes protegidas. Sin dependencias.
- `CloudOrders.Contracts` → Eventos compartidos entre Api e Inventory (mensajería)

`Application` nunca conoce implementaciones concretas de `Infrastructure` — define interfaces (`IOrderRepository`, `IQuerySource`, `ITokenGenerator`) que `Infrastructure` implementa. Esto es Dependency Inversion aplicado, no solo "capas prolijas": permite testear casos de uso sin base de datos real, y mantiene el dominio libre de cualquier detalle técnico.

### Dónde encontrar cada cosa (por feature, ya que la organización de carpetas es por capa técnica)

**Crear una orden (`POST /api/orders`):**
- `Domain/Order.cs` — entidad con invariantes (factory method, transiciones de estado válidas)
- `Application/Orders/PlaceOrderCommand.cs` + `PlaceOrderHandler.cs` — caso de uso
- `Application/Abstractions/IOrderRepository.cs` — contrato de persistencia
- `Infrastructure/Persistence/SqlOrderRepository.cs` + `OrderConfiguration.cs` — implementación EF Core
- `Api/Contracts/CreateOrderRequest.cs` + `CreateOrderRequestValidator.cs` — DTO y validación HTTP
- `Api/Controllers/OrdersController.cs` — endpoint

**Consultar una orden (`GET /api/orders/{id}`):**
- `Application/Orders/GetOrderByIdQuery.cs` + `GetOrderByIdHandler.cs` + `OrderDto.cs`
- `Application/Abstractions/IQuerySource.cs` — lado read-only de CQRS, sin tracking

**Login (`POST /api/auth/login`):**
- `Domain/User.cs`, `Application/Auth/*`, `Infrastructure/Auth/JwtTokenGenerator.cs`, `Api/Controllers/AuthController.cs`

**Nota sobre nombres parecidos:** `src/CloudOrders.Api/Contracts/` (DTOs HTTP de esta API) y `src/CloudOrders.Contracts/` (eventos compartidos con Inventory) son carpetas distintas sin relación entre sí — coinciden en nombre por convención genérica de la industria, no por vínculo real.

## Decisiones técnicas destacadas

- **CQRS real, no solo en nombre**: el lado Command pasa por el dominio y protege invariantes; el lado Query usa `IQuerySource` con `AsNoTracking()`, sin pasar por `IOrderRepository`.
- **JWT con HS256**, contraseñas hasheadas con BCrypt (nunca en texto plano), claims mínimos (sin datos sensibles en el payload — el contenido de un JWT es público).
- **Exception handling global** vía `IExceptionHandler`, devolviendo `ProblemDetails` (RFC 7807) — nunca stack traces al cliente.
- **Validación de input separada de invariantes de dominio**: FluentValidation en el borde de la API, reglas de negocio en las entidades — las dos existen a propósito, no es redundancia.

## Stack

.NET 10 · ASP.NET Core · EF Core · SQL Server · FluentValidation · JWT Bearer · BCrypt.Net

## Correr localmente

Requisitos: SQL Server local (o LocalDB), .NET 10 SDK.

    dotnet restore
    dotnet ef database update --project src/CloudOrders.Infrastructure --startup-project src/CloudOrders.Api
    dotnet run --project src/CloudOrders.Api --launch-profile https

En `Development`, se siembra automáticamente un usuario de prueba: `test@cloudorders.com` / `Test123!`.

## Endpoints

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/api/auth/login` | No | Devuelve un JWT |
| POST | `/api/orders` | Sí (Bearer) | Crea una orden |
| GET | `/api/orders/{id}` | Sí (Bearer) | Consulta una orden |

## Trabajo compartido con Inventory

Ver [`CONTRACTS.md`](./CONTRACTS.md) — eventos, canales de Service Bus y endpoints acordados entre los dos servicios.