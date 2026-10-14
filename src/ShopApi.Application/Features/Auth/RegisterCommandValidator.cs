using FluentValidation;

namespace ShopApi.Application.Features.Auth;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MinimumLength(1).MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MinimumLength(1).MaximumLength(100);
    }
}
