using FluentValidation;

namespace ShopApi.Application.Features.Orders;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty();
    }
}
