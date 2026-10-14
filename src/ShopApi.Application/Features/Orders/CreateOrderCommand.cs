using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Application.Features.Orders;

public sealed record CreateOrderCommand(Guid AddressId) : IRequest<Result<OrderDto>>;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public CreateOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<OrderDto>(Error.Unauthorized("Authentication required."));
        }

        var lines = await _unitOfWork.Cart.GetByUserIdAsync(userId, cancellationToken);
        if (lines.Count == 0)
        {
            return Result.Failure<OrderDto>(Error.Conflict("Cart is empty."));
        }

        var addresses = await _unitOfWork.Addresses.FindAsync(
            a => a.Id == request.AddressId && a.UserId == userId,
            cancellationToken);
        if (addresses.Count == 0)
        {
            return Result.Failure<OrderDto>(Error.NotFound("Address not found."));
        }

        foreach (var line in lines)
        {
            if (line.Product is null)
            {
                return Result.Failure<OrderDto>(Error.NotFound("Product not found."));
            }

            if (line.Product.Stock < line.Quantity)
            {
                return Result.Failure<OrderDto>(Error.Conflict("Insufficient stock."));
            }
        }

        Order order;
        try
        {
            order = Order.Create(
                userId,
                request.AddressId,
                lines.Select(line => (line.ProductId, line.Quantity, line.Product!.Price)));

            foreach (var line in lines)
            {
                line.Product!.AdjustStock(-line.Quantity);
                _unitOfWork.Products.Update(line.Product);
            }
        }
        catch (DomainException ex)
        {
            return Result.Failure<OrderDto>(Error.Conflict(ex.Message));
        }

        await _unitOfWork.Orders.AddAsync(order, cancellationToken);
        await _unitOfWork.Cart.ClearUserCartAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _unitOfWork.Orders.GetByIdWithItemsAsync(order.Id, cancellationToken);
        return Result.Success(_mapper.Map<OrderDto>(created!));
    }
}
