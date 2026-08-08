# CloudOrders — Contratos entre servicios

## 1. Eventos compartidos (`CloudOrders.Contracts`)

```csharp
namespace CloudOrders.Contracts;

public sealed record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    IReadOnlyList<OrderLine> Lines,
    DateTime OccurredAt);

public sealed record OrderLine(Guid ProductId, int Quantity);

public sealed record StockReserved(Guid OrderId, DateTime OccurredAt);

public sealed record StockRejected(Guid OrderId, string Reason, DateTime OccurredAt);
```

> Pendiente: `Order` (dominio de Api) todavía no tiene `OrderLines`. Hay que ampliarlo antes de publicar `OrderPlaced` real.

## 2. Mensajería — Azure Service Bus

| Canal | Tipo | Publica | Consume |
|---|---|---|---|
| `order-events` | Topic | Api (`OrderPlaced`) | Inventory, vía subscription `inventory-sub` |
| `order-fulfillment-events` | Topic | Inventory (`StockReserved` / `StockRejected`) | Api, vía subscription `api-sub` |

Desarrollo local: [Service Bus Emulator](https://learn.microsoft.com/azure/service-bus-messaging/overview-emulator) vía Docker, mismos nombres de topic/subscription que en producción.

## 3. Endpoints HTTP síncronos

**CloudOrders.Api**

| Método | Ruta | Estado |
|---|---|---|
| POST | `/api/orders` | ✅ hecho |
| GET | `/api/orders/{id}` | 🔜 en progreso |

**CloudOrders.Inventory** (a cargo de [nombre de tu amigo])

| Método | Ruta | Para qué |
|---|---|---|
| GET | `/api/inventory/{productId}/stock` | Consulta síncrona de stock disponible |

## 4. Reglas de trabajo

- `Contracts` es la única carpeta que ambos pueden tocar — avisar antes de mergear cambios ahí.
- Rama propia por persona + Pull Request antes de mergear a `main`.
- Nombres de canales de Service Bus son fijos — no se cambian unilateralmente.