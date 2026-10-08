namespace CommerceCore.Catalog.Application;

public sealed record ProductResponse(Guid Id, string Sku, string Name, decimal Price, string Currency, bool IsActive);

public sealed record PriceResponse(Guid ProductId, decimal Amount, string Currency);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);