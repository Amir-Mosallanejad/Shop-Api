using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShopApi.Domain.Exceptions;

namespace ShopApi.API.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, detail, logLevel) = Map(exception);

        if (logLevel == LogLevel.Error)
        {
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning("Handled {Code}. TraceId: {TraceId}", code, httpContext.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = code,
            Detail = detail
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private (int StatusCode, string Code, string Detail, LogLevel Level) Map(Exception exception)
    {
        switch (exception)
        {
            case DomainException domainException:
                return (StatusCodes.Status409Conflict, "Conflict", domainException.Message, LogLevel.Warning);

            case ValidationException validationException:
                return (StatusCodes.Status400BadRequest, "Validation", validationException.Message, LogLevel.Warning);

            case DbUpdateException dbUpdateException when IsConflict(dbUpdateException):
                return (
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    "The operation conflicts with existing data.",
                    LogLevel.Warning);

            default:
                return (
                    StatusCodes.Status500InternalServerError,
                    "InternalError",
                    _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                    LogLevel.Error);
        }
    }

    private static bool IsConflict(DbUpdateException exception)
    {
        var sqlException = exception.InnerException as SqlException ?? exception.GetBaseException() as SqlException;
        return sqlException is not null && (sqlException.Number is 2601 or 2627 or 547);
    }
}
