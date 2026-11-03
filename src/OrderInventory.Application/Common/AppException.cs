namespace OrderInventory.Application.Common;

public class AppException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class UnauthorizedException(string message = "The user name or password is incorrect.")
    : AppException("unauthorized", message);

public sealed class ForbiddenException(string message = "This operation requires an administrator.")
    : AppException("forbidden", message);

public sealed class NotFoundException(string message = "The user was not found.")
    : AppException("not_found", message);

public sealed class ConflictException : AppException
{
    public ConflictException(string message = "The operation conflicts with the current state.") : base("conflict", message) { }
    public ConflictException(string code, string message) : base(code, message) { }
}

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : AppException("validation", "One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
