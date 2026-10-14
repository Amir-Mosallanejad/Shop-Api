using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Features.Categories;

namespace ShopApi.API.Controllers;

[Route("api/categories")]
public sealed class CategoriesController : BaseApiController
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new GetAllCategoriesQuery(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new GetCategoryByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new UpdateCategoryCommand(id, request.Name, request.ParentId), cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<bool>> Delete(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new DeleteCategoryCommand(id), cancellationToken));

    public sealed record UpdateCategoryRequest(string Name, Guid? ParentId);
}
