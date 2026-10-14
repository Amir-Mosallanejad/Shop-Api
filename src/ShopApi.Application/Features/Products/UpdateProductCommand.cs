using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Exceptions;
using ShopApi.Domain.ValueObjects;

namespace ShopApi.Application.Features.Products;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int Stock,
    Guid CategoryId) : IRequest<Result<ProductDto>>;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound("Product not found."));
        }

        if (!await _unitOfWork.Categories.ExistsAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return Result.Failure<ProductDto>(Error.NotFound("Category not found."));
        }

        try
        {
            product.UpdateDetails(request.Name, request.Description);
            product.ChangePrice(new Money(request.Price, request.Currency));
            product.AdjustStock(request.Stock - product.Stock);
            product.SetCategory(request.CategoryId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<ProductDto>(Error.Conflict(ex.Message));
        }

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _unitOfWork.Products.GetByIdWithDetailsAsync(product.Id, cancellationToken);
        return Result.Success(_mapper.Map<ProductDto>(updated!));
    }
}
