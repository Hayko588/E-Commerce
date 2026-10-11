using CommerceCore.Payments.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceCore.Payments.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PaymentsController(ISender sender) : ControllerBase
{
    // Payments are created by the order saga, never by a client, so there is no POST here.
    [HttpGet]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOrder([FromQuery] Guid orderId, CancellationToken ct)
    {
        var payment = await sender.Send(new GetPaymentByOrderQuery(orderId), ct);
        return payment is null ? NotFound() : Ok(payment);
    }
}
