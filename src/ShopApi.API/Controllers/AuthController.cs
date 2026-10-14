using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopApi.Application.Common.Dtos;
using ShopApi.Application.Features.Auth;

namespace ShopApi.API.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : BaseApiController
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(command, cancellationToken));

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginQuery query,
        CancellationToken cancellationToken) =>
        ToActionResult(await _sender.Send(query, cancellationToken));
}
