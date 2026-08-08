using CloudOrders.Api.Contracts;
using CloudOrders.Application.Orders;
using Microsoft.AspNetCore.Mvc;

namespace CloudOrders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly PlaceOrderHandler _placeOrderHandler;
    public OrdersController(PlaceOrderHandler placeOrderHandler)
    {
        _placeOrderHandler = placeOrderHandler;
    }

    [HttpPost]
    public async Task<ActionResult> PlaceOrder(CreateOrderRequest createOrderRequest, CancellationToken cancellationToken)
    {
        var placeOrderCommand = new PlaceOrderCommand(createOrderRequest.CustomerId, createOrderRequest.Total);

        var placedOrderGuid = await _placeOrderHandler.HandleAsync(placeOrderCommand, cancellationToken);

        return Created($"/api/orders/{placedOrderGuid}", new { id = placedOrderGuid });
    }
}