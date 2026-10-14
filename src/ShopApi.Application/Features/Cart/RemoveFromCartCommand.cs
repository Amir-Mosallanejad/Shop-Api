using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Cart;

public sealed record RemoveFromCartCommand(Guid ProductId) : IRequest<Result<CartDto>>;

public sealed class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Result<CartDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RemoveFromCartCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CartDto>> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<CartDto>(Error.Unauthorized("Authentication required."));
        }

        var line = await _unitOfWork.Cart.GetLineAsync(userId, request.ProductId, cancellationToken);
        if (line is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("Product is not in the cart."));
        }

        _unitOfWork.Cart.Remove(line);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lines = await _unitOfWork.Cart.GetByUserIdAsync(userId, cancellationToken);
        return Result.Success(CartMapping.ToCart(lines));
    }
}
