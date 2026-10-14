using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Orders;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<Result<OrderDto>>;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetOrderByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<OrderDto>(Error.Unauthorized("Authentication required."));
        }

        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (order is null)
        {
            return Result.Failure<OrderDto>(Error.NotFound("Order not found."));
        }

        if (order.UserId != userId && !_currentUser.IsAdmin)
        {
            return Result.Failure<OrderDto>(Error.Forbidden("Access denied."));
        }

        return Result.Success(_mapper.Map<OrderDto>(order));
    }
}
