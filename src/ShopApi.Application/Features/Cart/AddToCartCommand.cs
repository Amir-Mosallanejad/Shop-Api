using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Application.Features.Cart;

public sealed record AddToCartCommand(Guid ProductId, int Quantity) : IRequest<Result<CartDto>>;

public sealed class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<CartDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddToCartCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CartDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<CartDto>(Error.Unauthorized("Authentication required."));
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<CartDto>(Error.NotFound("Product not found."));
        }

        var line = await _unitOfWork.Cart.GetLineAsync(userId, request.ProductId, cancellationToken);
        var totalQuantity = (line?.Quantity ?? 0) + request.Quantity;
        if (product.Stock < totalQuantity)
        {
            return Result.Failure<CartDto>(Error.Conflict("Insufficient stock."));
        }

        try
        {
            if (line is null)
            {
                await _unitOfWork.Cart.AddAsync(new CartItem(userId, request.ProductId, request.Quantity), cancellationToken);
            }
            else
            {
                line.Increase(request.Quantity);
                _unitOfWork.Cart.Update(line);
            }
        }
        catch (DomainException ex)
        {
            return Result.Failure<CartDto>(Error.Conflict(ex.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lines = await _unitOfWork.Cart.GetByUserIdAsync(userId, cancellationToken);
        return Result.Success(CartMapping.ToCart(lines));
    }
}
