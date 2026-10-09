using CommerceCore.Catalog.Application;
using CommerceCore.Catalog.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceCore.Catalog.Api.Controllers;

[ApiController]
[Route("internal/v1/prices")]
[Produces("application/json")]
public class InternalPricesController(ISender sender) : ControllerBase
{
    // GET /internal/v1/prices?ids=<guid>&ids=<guid>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PriceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] Guid[] ids, CancellationToken ct)
    {
        var prices = await sender.Send(new GetPricesQuery(ids), ct);
        return Ok(prices.Values);
    }
}
