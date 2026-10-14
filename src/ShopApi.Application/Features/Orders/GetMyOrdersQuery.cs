using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Orders;

public sealed record GetMyOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<OrderDto>>>;

public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, Result<PagedResult<OrderDto>>>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetMyOrdersQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<OrderDto>>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure<PagedResult<OrderDto>>(Error.Unauthorized("Authentication required."));
        }

        var paged = await _unitOfWork.Orders.GetByUserPagedAsync(
            userId,
            request.Page < 1 ? 1 : request.Page,
            Math.Clamp(request.PageSize, MinPageSize, MaxPageSize),
            cancellationToken);

        var dtos = _mapper.Map<IReadOnlyList<OrderDto>>(paged.Items);
        return Result.Success(new PagedResult<OrderDto>(dtos, paged.Page, paged.PageSize, paged.TotalCount));
    }
}
