using FluentValidation;

namespace CommerceCore.Ordering.Application.Commands
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("CustomerId is required.");

            RuleFor(x => x.Items)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Order must contain at least one item.")
                .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
                    .WithMessage("Each product may appear only once.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .NotEmpty().WithMessage("ProductId is required.");

                item.RuleFor(i => i.Quantity)
                    .InclusiveBetween(1, 100).WithMessage("Quantity must be between 1 and 100.");
            });
        }
    }
}