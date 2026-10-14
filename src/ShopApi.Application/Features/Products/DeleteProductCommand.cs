using MediatR;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;

namespace ShopApi.Application.Features.Products;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result<bool>>;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure<bool>(Error.NotFound("Product not found."));
        }

        _unitOfWork.Products.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
