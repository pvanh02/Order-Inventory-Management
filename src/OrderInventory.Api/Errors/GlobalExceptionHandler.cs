using Microsoft.AspNetCore.Diagnostics;
using OrderInventory.Application.Common;

namespace OrderInventory.Api.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            CommitOutcomeUnknownException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };
        var applicationError = exception as AppException;
        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError("Unhandled request failure {FailureType}, trace {TraceId}",
                exception.GetType().Name, context.TraceIdentifier);
        if (status == StatusCodes.Status401Unauthorized)
            context.Response.Headers.WWWAuthenticate = "Bearer";
        var problem = ProblemResponses.Create(context, status, applicationError?.Code ?? "internal_error",
            applicationError?.Message ?? "An unexpected error occurred.",
            (exception as ValidationException)?.Errors);
        if (exception is CommitOutcomeUnknownException unknown)
        {
            problem.Extensions["orderId"] = unknown.OrderId;
            logger.LogWarning("Order commit outcome unknown for {OrderId}, trace {TraceId}", unknown.OrderId, context.TraceIdentifier);
        }
        await ProblemResponses.WriteAsync(context, problem, cancellationToken);
        return true;
    }
}
