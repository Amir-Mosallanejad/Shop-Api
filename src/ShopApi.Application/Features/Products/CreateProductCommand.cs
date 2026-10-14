using AutoMapper;
using MediatR;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Interfaces;
using ShopApi.Domain.Entities;
using ShopApi.Domain.Exceptions;
using ShopApi.Domain.ValueObjects;

namespace ShopApi.Application.Features.Products;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int Stock,
    Guid CategoryId,
    IReadOnlyList<CreateProductImageRequest>? Images) : IRequest<Result<ProductDto>>;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Categories.ExistsAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            return Result.Failure<ProductDto>(Error.NotFound("Category not found."));
        }

        Product product;
        try
        {
            product = new Product(
                request.Name,
                request.Description,
                new Money(request.Price, request.Currency),
                request.Stock,
                request.CategoryId);

            foreach (var image in request.Images ?? [])
            {
                product.AddImage(image.Url, image.IsMain);
            }
        }
        catch (DomainException ex)
        {
            return Result.Failure<ProductDto>(Error.Conflict(ex.Message));
        }

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Duplicate slugs are rejected by the Phase 04 database unique constraint.
        var created = await _unitOfWork.Products.GetByIdWithDetailsAsync(product.Id, cancellationToken);
        return Result.Success(_mapper.Map<ProductDto>(created!));
    }
}
