using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Application.Features.Categories;

public sealed record UpdateCategoryCommand(Guid Id, string Name, Guid? ParentId) : IRequest<Result<CategoryDto>>;

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure<CategoryDto>(Error.NotFound("Category not found."));
        }

        if (request.ParentId.HasValue &&
            !await _unitOfWork.Categories.ExistsAsync(c => c.Id == request.ParentId.Value, cancellationToken))
        {
            return Result.Failure<CategoryDto>(Error.NotFound("Parent category not found."));
        }

        try
        {
            category.Rename(request.Name);
            category.SetParent(request.ParentId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<CategoryDto>(Error.Conflict(ex.Message));
        }

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.Map<CategoryDto>(category));
    }
}
