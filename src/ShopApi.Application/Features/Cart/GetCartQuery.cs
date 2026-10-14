using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Cart;

public sealed record GetCartQuery() : IRequest<Result<CartDto>>;

public sealed class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetCartQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<CartDto>(Error.Unauthorized("Authentication required."));
        }

        var lines = await _unitOfWork.Cart.GetByUserIdAsync(userId, cancellationToken);
        return Result.Success(CartMapping.ToCart(lines));
    }
}
