# Plan: Fix Inventory Azure Integration and Document External Pending Items

## Understanding
The user wants all actionable integration issues fixed inside the Inventory project only, without modifying CloudOrders-owned code. Inventory should be ready to interoperate in Azure as much as possible from its side, and remaining CloudOrders-side blockers should be documented.

## Assumptions
- CloudOrders.Api currently publishes `OrderPlaced` with shape `(OrderId, Items)` where each item has `(ProductId, Quantity)`.
- Inventory is allowed to add Service Bus consumption logic for `order-events` / `inventory-sub`.
- Secrets must not be committed; production values should be configured via environment/App Settings.
- CloudOrders code must remain untouched.

## Approach
Make Inventory capable of both publishing fulfillment events and consuming order events through Azure Service Bus when `UseInMemory` is disabled. This requires extending Inventory configuration/options, adding a hosted background consumer, and aligning Inventory's local order event model to the producer payload currently emitted by CloudOrders.Api. Kept local-dev behavior intact by retaining in-memory mode by default.

Also updated Inventory documentation to include: (1) exact Azure configuration needed for Inventory deployment and (2) a clear section listing pending issues that belong to CloudOrders (contract mismatch and missing fulfillment-event consumer).

## Key Files Modified
- `Inventory/Application/StockEvents.cs` - aligned consumed `OrderPlaced` model with current producer payload
- `Inventory/Infrastructure/ServiceBusOptions.cs` - added inbound/outbound topic/subscription settings
- `Inventory/Infrastructure/ServiceBusMessageSender.cs` - publish to configured fulfillment topic
- `Inventory/Infrastructure/ServiceBusOrderPlacedConsumer.cs` - new background consumer for order events
- `Inventory/Program.cs` - validated options and wired the hosted consumer in non-in-memory mode
- `Inventory/appsettings.json` - aligned default topic names and config keys
- `Inventory/README.md` - documented Inventory deployment config and CloudOrders-owned pending issues

## Completed Steps

### 1. Updated Inventory event contract models for inbound OrderPlaced compatibility
Updated `Inventory/Application/StockEvents.cs` so inbound `OrderPlaced` matches current CloudOrders publisher payload:
- `OrderPlaced(Guid OrderId, IReadOnlyCollection<OrderPlacedItem> Items)`
- `OrderPlacedItem(Guid ProductId, int Quantity)`
- Kept `StockReserved` and `StockRejected` unchanged

### 2. Extended Service Bus options and sender to separate inbound/outbound topic settings
- Extended `Inventory/Infrastructure/ServiceBusOptions.cs` with distinct settings:
  - `FulfillmentTopicName` (publishes StockReserved/StockRejected)
  - `OrderEventsTopicName` (subscribes to OrderPlaced)
  - `InventorySubscriptionName`
- Updated `Inventory/Infrastructure/ServiceBusMessageSender.cs` to publish to `FulfillmentTopicName`

### 3. Added hosted Service Bus consumer for OrderPlaced subscription processing
Created `Inventory/Infrastructure/ServiceBusOrderPlacedConsumer.cs`:
- Implements `IHostedService` and `IAsyncDisposable`
- Subscribes to order-events/inventory-sub
- Deserializes `OrderPlaced` payloads
- Reserves stock per line item using `OrderSubmissionService.ReserveAsync(...)`
- Completes/dead-letters messages appropriately

### 4. Wired DI and options validation in Program for Azure mode while preserving in-memory local mode
- Replaced `Configure<ServiceBusOptions>` with `AddOptions<ServiceBusOptions>()` + validation
- Added validation rules for required settings
- Called `ValidateOnStart()` to catch config issues at startup
- Preserved in-memory dev behavior by default
- Registered `ServiceBusOrderPlacedConsumer` as hosted service when `UseInMemory=false`

### 5. Updated Inventory appsettings defaults to contract-aligned topic/subscription names
`Inventory/appsettings.json`:
```json
"ServiceBus": {
  "UseInMemory": true,
  "ConnectionString": "",
  "FulfillmentTopicName": "order-fulfillment-events",
  "OrderEventsTopicName": "order-events",
  "InventorySubscriptionName": "inventory-sub"
}
```

### 6. Updated Inventory README with deployment configuration and CloudOrders pending issues
Added two new sections in `Inventory/README.md`:
- **Azure deployment configuration**: exact app settings needed for Azure App Service/Container Apps
- **Pending issues owned by CloudOrders**: three documented issues for Santiago:
  1. Contract mismatch (OrderPlaced payload vs CONTRACTS.md)
  2. Missing fulfillment-event consumer
  3. Incomplete Azure production configuration

### 7. Build and test validation
- `dotnet build Inventory/Inventory.csproj` ✅ successful
- `dotnet test Inventory/Inventory.Tests` ✅ 3 tests passed

## Related GitHub Issues
Created linked issues in `santiagocassone/CloudOrders`:
- Issue #1: Contract mismatch: OrderPlaced payload differs from CONTRACTS.md
- Issue #2: Add CloudOrders consumer for StockReserved/StockRejected fulfillment events
- Issue #3: Azure production config: complete Service Bus settings and startup validation alignment

## Pull Request
- PR #4: Fix Inventory Azure integration and document CloudOrders pending items
- Branch: `inventory-azure-integration-fixes`
- Target: `master`
- Status: Ready for review

## Pending External Items (CloudOrders Responsibility)
1. **Contract alignment**: Update `OrderPlaced` publisher in CloudOrders to match `CONTRACTS.md` schema
2. **Fulfillment consumer**: Implement Service Bus consumer in CloudOrders for `StockReserved` / `StockRejected`
3. **Azure config**: Add required Service Bus keys to production appsettings
