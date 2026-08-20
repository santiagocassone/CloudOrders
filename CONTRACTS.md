# CloudOrders — Contratos entre servicios

Este documento define el contrato de integración entre **CloudOrders (Orders)** e **Inventory**. Cada servicio mantiene su propio dominio y su propia implementación; lo compartido es la semántica del mensaje y su representación en el wire.

## 1. Contratos canónicos (`CloudOrders.Contracts`)

### `OrderPlaced`

Publicado por CloudOrders cuando una orden fue creada y debe evaluarse/reservarse en Inventory.

```csharp
namespace CloudOrders.Contracts;

public sealed record OrderPlaced(
    Guid OrderId,
    IReadOnlyCollection<OrderPlacedItem> Items);

public sealed record OrderPlacedItem(
    Guid ProductId,
    int Quantity);
```

### `StockReserved`

Resultado de integración que actualmente CloudOrders interpreta como una reserva de stock exitosa para la orden.

```csharp
namespace CloudOrders.Contracts;

public sealed record StockReserved(
    Guid OrderId,
    DateTime ReservedAt);
```

### `StockRejected`

Resultado de integración que actualmente CloudOrders interpreta como una reserva de stock rechazada para la orden.

```csharp
namespace CloudOrders.Contracts;

public sealed record StockRejected(
    Guid OrderId,
    string Reason,
    DateTime RejectedAt);
```

> **Regla de compatibilidad:** Inventory no está obligado a referenciar físicamente el assembly `CloudOrders.Contracts`, pero los mensajes que publique y consuma deben ser wire-compatible con estos contratos acordados. Los nombres, tipos y semántica de los campos no se cambian unilateralmente.

## 2. Topología real de Azure Service Bus

| Queue | Publica | Consume | Mensaje |
|---|---|---|---|
| `order-placed` | CloudOrders | Inventory | `OrderPlaced` |
| `stock-results` | Inventory | CloudOrders | `StockReserved` / `StockRejected` |

La configuración de CloudOrders usa `ServiceBus:OrderPlacedQueue` y `ServiceBus:StockResultsQueue`. Los nombres anteriores basados en topics (`order-events`, `order-fulfillment-events`) ya no forman parte de la topología vigente.

## 3. Responsabilidades de cada bounded context

### CloudOrders / Orders

CloudOrders es autoridad sobre:

- validez e invariantes de la `Order`;
- lifecycle y transiciones de estado de la `Order`;
- qué información de Inventory es suficiente para decidir `Confirm()` o `Reject()`;
- idempotencia del efecto al consumir resultados de stock.

### Inventory

Inventory es autoridad sobre:

- disponibilidad de stock;
- reservas de productos y cantidades;
- resultado técnico/de negocio de una operación de reserva de stock;
- detalle de qué productos/cantidades pudieron o no reservarse.

Inventory **no decide directamente el estado de la `Order`** de CloudOrders. Devuelve hechos/resultados sobre stock; CloudOrders decide qué transición corresponde en su propio dominio.

## 4. Pendiente de diseño — resultados parciales por item

**Estado: diferido; no bloquea el P0 actual de alineación de contratos/topología.**

El contrato vigente (`StockReserved` / `StockRejected`) expresa un resultado agregado a nivel `OrderId`. Antes de considerar definitiva esa semántica, hay que resolver explícitamente el caso de reservas parciales.

Ejemplo pendiente:

```text
Order 123
- Product A: reservado
- Product B: reservado
- Product C: sin stock
```

Preguntas que deben resolverse en un bloque posterior:

- ¿Inventory debe devolver un resultado por item, un resultado agregado con detalle por item, o ambos?
- ¿Qué información mínima necesita Orders para aplicar su propia regla de dominio?
- ¿La regla actual exige que todos los items estén reservados para confirmar la Order?
- ¿Qué ocurre con reservas parciales ya realizadas si la Order finalmente no puede confirmarse?
- ¿La liberación/compensación de reservas parciales pertenece completamente a Inventory o requiere coordinación de workflow?

**Principio acordado:** la regla que determina cuándo una `Order` pasa a `Confirmed` o `Rejected` pertenece al dominio de Orders. Inventory debe proporcionar hechos suficientes sobre la reserva para que Orders pueda tomar esa decisión sin delegar su lifecycle al bounded context de Inventory.

Hasta resolver este punto, no se debe ampliar el contrato de forma unilateral ni asumir que un mensaje por item equivale a una transición de estado de la Order.

## 5. Reglas operativas del contrato

- Los nombres de queues son parte de la integración y no se cambian unilateralmente.
- Los cambios de contrato requieren coordinación entre CloudOrders e Inventory antes del despliegue.
- Los cambios deben mantener compatibilidad durante el rollout cuando pueda existir más de una versión de los servicios en ejecución.
- Los mensajes inválidos o incompatibles no deben convertirse silenciosamente en estados de dominio.
- `MessageId`, `ContentType`, `Subject`, versionado del contrato y política de mensajes inválidos se completarán en el bloque P1 de boundary validation de Service Bus.

## 6. Pendientes relacionados, fuera de este P0

- Definir la semántica definitiva de resultados parciales por item.
- Completar boundary validation de Service Bus (`ContentType`, `Subject`, `MessageId`, serializer/versioning).
- Revisar DLQ, `MaxDeliveryCount`, lock/renewal, TTL, queue depth y alertas.
- Implementar Transactional Outbox para eliminar el dual-write entre persistencia de Orders y publicación de `OrderPlaced`.
