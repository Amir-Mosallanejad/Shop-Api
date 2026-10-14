using MediatR;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Categories;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result<bool>>;

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure<bool>(Error.NotFound("Category not found."));
        }

        if (await _unitOfWork.Products.CountByCategoryAsync(request.Id, cancellationToken) > 0)
        {
            return Result.Failure<bool>(Error.Conflict("Category contains products."));
        }

        if (await _unitOfWork.Categories.ExistsAsync(c => c.ParentId == request.Id, cancellationToken))
        {
            return Result.Failure<bool>(Error.Conflict("Category has subcategories."));
        }

        _unitOfWork.Categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
