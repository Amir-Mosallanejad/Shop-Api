using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Categories;

public sealed record GetAllCategoriesQuery() : IRequest<Result<IReadOnlyList<CategoryDto>>>;

public sealed class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllCategoriesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        GetAllCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.ListAllAsync(cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<CategoryDto>>(categories.OrderBy(c => c.Name).ToList());
        return Result.Success(dtos);
    }
}
