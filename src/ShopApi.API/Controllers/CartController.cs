using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Features.Cart;

namespace ShopApi.API.Controllers;

[Route("api/cart")]
[Authorize]
public sealed class CartController : BaseApiController
{
    private readonly ISender _sender;

    public CartController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new GetCartQuery(), cancellationToken));

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddToCartCommand command,
        CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(command, cancellationToken));

    [HttpDelete("items/{productId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid productId, CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(new RemoveFromCartCommand(productId), cancellationToken));
}
