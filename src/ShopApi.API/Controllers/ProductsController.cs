using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Features.Products;

namespace ShopApi.API.Controllers;

[Route("api/products")]
public sealed class ProductsController : BaseApiController
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDescending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await _sender.Send(
            new GetProductsQuery(categoryId, search, minPrice, maxPrice, sortBy, sortDescending, page, pageSize),
            cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new GetProductByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(
            new UpdateProductCommand(
                id,
                request.Name,
                request.Description,
                request.Price,
                request.Currency,
                request.Stock,
                request.CategoryId),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<bool>> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new DeleteProductCommand(id), cancellationToken));

    public sealed record UpdateProductRequest(
        string Name,
        string Description,
        decimal Price,
        string Currency,
        int Stock,
        Guid CategoryId);
}
