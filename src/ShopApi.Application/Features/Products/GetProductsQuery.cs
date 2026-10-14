using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Products;

public sealed record GetProductsQuery(
    Guid? CategoryId,
    string? Search,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<ProductDto>>>;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new ProductFilter(
            request.CategoryId,
            request.Search,
            request.MinPrice,
            request.MaxPrice,
            request.SortBy,
            request.SortDescending,
            request.Page < 1 ? 1 : request.Page,
            Math.Clamp(request.PageSize, MinPageSize, MaxPageSize));

        var paged = await _unitOfWork.Products.GetPagedAsync(filter, cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<ProductDto>>(paged.Items);

        return Result.Success(new PagedResult<ProductDto>(dtos, paged.Page, paged.PageSize, paged.TotalCount));
    }
}
