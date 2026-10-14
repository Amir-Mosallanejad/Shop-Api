namespace ShopApi.Application.Common.Wrappers;

public sealed record Error(string Code, string Message)
{
    public static Error NotFound(string message) => new("NotFound", message);

    public static Error Validation(string message) => new("Validation", message);

    public static Error Conflict(string message) => new("Conflict", message);

    public static Error Unauthorized(string message) => new("Unauthorized", message);

    public static Error Forbidden(string message) => new("Forbidden", message);
}
