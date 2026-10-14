using FluentValidation;

namespace ShopApi.Application.Features.Cart;

public sealed class RemoveFromCartCommandValidator : AbstractValidator<RemoveFromCartCommand>
{
    public RemoveFromCartCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
