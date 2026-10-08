using CommerceCore.Ordering.Application.Commands;
using CommerceCore.Ordering.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceCore.Ordering.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var command = new CreateOrderCommand(request.CustomerId, request.Items);
        var orderId = await sender.Send(command, ct);

        return CreatedAtAction(nameof(GetOrderById), new { id = orderId }, orderId);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var query = new GetOrderByIdQuery(id);
        var response = await sender.Send(query, ct);

        if (response is null)
            return NotFound();

        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ICollection<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllOrders(CancellationToken ct)
    {
        var query = new GetAllOrders();
        var response = await sender.Send(query, ct);

        if (response is null)
            return NotFound();

        return Ok(response);
    }
}