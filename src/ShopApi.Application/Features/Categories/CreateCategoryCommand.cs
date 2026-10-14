using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Domain.Exceptions;

namespace ShopApi.Application.Features.Categories;

public sealed record CreateCategoryCommand(string Name, Guid? ParentId) : IRequest<Result<CategoryDto>>;

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentId.HasValue &&
            !await _unitOfWork.Categories.ExistsAsync(c => c.Id == request.ParentId.Value, cancellationToken))
        {
            return Result.Failure<CategoryDto>(Error.NotFound("Parent category not found."));
        }

        Category category;
        try
        {
            category = new Category(request.Name, request.ParentId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<CategoryDto>(Error.Conflict(ex.Message));
        }

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(_mapper.Map<CategoryDto>(category));
    }
}
