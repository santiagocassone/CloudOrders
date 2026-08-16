# Copilot Instructions

## Project Guidelines
- CloudOrders integration should use Azure Service Bus queue names: order-placed (inbound from CloudOrders) and stock-results (outbound from Inventory) on namespace cloudorders-prod-escasan.
- For Inventory Service Bus events, MessageId must be unique per StockReserved/StockRejected event, and OrderId should be sent as CorrelationId for Orders-side idempotency.

## User Information
- Azure Microsoft account: mscalella911@hotmail.com.