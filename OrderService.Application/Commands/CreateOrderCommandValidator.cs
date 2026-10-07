using FluentValidation;
using OrderService.Application.Orders.Commands;

namespace OrderService.Application.Commands
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Order must contain at least one item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .NotEmpty().WithMessage("ProductId is required.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

                item.RuleFor(i => i.UnitPrice)
                    .GreaterThan(0).WithMessage("UnitPrice must be greater than 0.");
            });
        }
    }
}
