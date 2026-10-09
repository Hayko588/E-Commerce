using FluentValidation;
using MediatR;

namespace CommerceCore.Catalog.Application.Products;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductResponse?>;

public sealed record ListProductsQuery(int Page, int PageSize, string? Search) : IRequest<PagedResult<ProductResponse>>;

public sealed record GetPricesQuery(IReadOnlyCollection<Guid> ProductIds)
    : IRequest<IReadOnlyDictionary<Guid, PriceResponse>>;

public sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetPricesQueryValidator : AbstractValidator<GetPricesQuery>
{
    public GetPricesQueryValidator()
    {
        RuleFor(x => x.ProductIds).NotEmpty().Must(ids => ids.Count <= 100)
            .WithMessage("At most 100 products per request.");
    }
}

public sealed class GetProductByIdQueryHandler(IProductReadService reads)
    : IRequestHandler<GetProductByIdQuery, ProductResponse?>
{
    public Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken ct) =>
        reads.GetByIdAsync(request.Id, ct);
}

public sealed class ListProductsQueryHandler(IProductReadService reads)
    : IRequestHandler<ListProductsQuery, PagedResult<ProductResponse>>
{
    public Task<PagedResult<ProductResponse>> Handle(ListProductsQuery request, CancellationToken ct) =>
        reads.ListAsync(request.Page, request.PageSize, request.Search, ct);
}

public sealed class GetPricesQueryHandler(IProductReadService reads)
    : IRequestHandler<GetPricesQuery, IReadOnlyDictionary<Guid, PriceResponse>>
{
    public Task<IReadOnlyDictionary<Guid, PriceResponse>> Handle(GetPricesQuery request, CancellationToken ct) =>
        reads.GetPricesAsync(request.ProductIds, ct);
}
