using CommerceCore.Catalog.Domain;
using CommerceCore.Catalog.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace CommerceCore.Catalog.Application.Products;

public sealed record CreateProductCommand(string Sku, string Name, decimal Price, string Currency) : IRequest<Guid>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public sealed class CreateProductCommandHandler(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = Product.Create(
            Guid.NewGuid(), request.Sku, request.Name, new Money(request.Price, request.Currency), clock);

        if (await products.SkuExistsAsync(product.Sku, ct))
            throw new ConflictException($"A product with SKU '{product.Sku}' already exists.");

        await products.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return product.Id;
    }
}