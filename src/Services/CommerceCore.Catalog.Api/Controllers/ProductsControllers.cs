using CommerceCore.Catalog.Application;
using CommerceCore.Catalog.Application.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommerceCore.Catalog.Api.Controllers;

public sealed record CreateProductRequest(string Sku, string Name, decimal Price, string Currency = "USD");

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await sender.Send(new ListProductsQuery(page, pageSize, search), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await sender.Send(new GetProductByIdQuery(id), ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct)
    {
        var id = await sender.Send(
            new CreateProductCommand(request.Sku, request.Name, request.Price, request.Currency), ct);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}
