using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Common.Wrappers;
using ShopApi.Application.Features.Orders;
using ShopApi.Domain.Enums;

namespace ShopApi.API.Controllers;

[Route("api/orders")]
[Authorize]
public sealed class OrdersController : BaseApiController
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        ToActionResult(await _sender.Send(new GetMyOrdersQuery(page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new GetOrderByIdQuery(id), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new UpdateOrderStatusCommand(id, request.Status), cancellationToken));

    public sealed record UpdateStatusRequest(OrderStatus Status);
}
