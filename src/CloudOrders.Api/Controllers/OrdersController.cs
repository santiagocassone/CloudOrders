using CloudOrders.Api.Contracts;
using CloudOrders.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudOrders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly PlaceOrderHandler _placeOrderHandler;
    private readonly GetOrderByIdHandler _getOrderByIdHandler;
    public OrdersController(PlaceOrderHandler placeOrderHandler, GetOrderByIdHandler getOrderByIdHandler)
    {
        _placeOrderHandler = placeOrderHandler;
        _getOrderByIdHandler = getOrderByIdHandler;
    }

    [HttpPost]
    public async Task<ActionResult> PlaceOrder(CreateOrderRequest createOrderRequest, CancellationToken cancellationToken)
    {

        var placeOrderCommand = new PlaceOrderCommand(createOrderRequest.CustomerId, createOrderRequest.Items.Select(item => new PlaceOrderItem(item.ProductId, item.Quantity, item.UnitPrice)).ToList());

        var placedOrderGuid = await _placeOrderHandler.HandleAsync(placeOrderCommand, cancellationToken);

        return CreatedAtAction(nameof(GetOrderById), new { id = placedOrderGuid }, new { id = placedOrderGuid });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var orderDto = await _getOrderByIdHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);

        if (orderDto is null)
        {
            return NotFound();
        }

        return Ok(orderDto);
    }
}