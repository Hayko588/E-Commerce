using CommerceCore.Notifications.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceCore.Notifications.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class NotificationsController(ISender sender) : ControllerBase
{
    // Notifications are created by order events, never by a client, so there is no POST here.
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder([FromQuery] Guid orderId, CancellationToken ct) =>
        Ok(await sender.Send(new GetNotificationsByOrderQuery(orderId), ct));
}
