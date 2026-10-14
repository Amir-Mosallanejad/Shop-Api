using Microsoft.AspNetCore.Mvc;
using ShopApi.Application.Common.Wrappers;

namespace ShopApi.API.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<T> ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var error = result.Error!;
        var statusCode = StatusFor(error.Code);
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Message
        };
        problem.Extensions["code"] = error.Code;

        return StatusCode(statusCode, problem);
    }

    private static int StatusFor(string code) => code switch
    {
        "NotFound" => StatusCodes.Status404NotFound,
        "Validation" => StatusCodes.Status400BadRequest,
        "Conflict" => StatusCodes.Status409Conflict,
        "Unauthorized" => StatusCodes.Status401Unauthorized,
        "Forbidden" => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}
