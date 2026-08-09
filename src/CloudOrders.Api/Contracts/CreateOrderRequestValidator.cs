using FluentValidation;

namespace CloudOrders.Api.Contracts;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEqual(Guid.Empty);
        RuleFor(x => x.Total).GreaterThan(0);
    }
}