using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace OrderInventory.Api.Errors;

public static class ProblemResponses
{
    public static ProblemDetails Create(HttpContext context, int status, string code, string? detail = null,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        ProblemDetails problem = errors is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(errors.ToDictionary(x => x.Key, x => x.Value));
        problem.Status = status;
        problem.Title = ReasonPhrases.GetReasonPhrase(status);
        problem.Type = $"urn:order-inventory:problem:{code}";
        problem.Detail = detail ?? "The request could not be completed.";
        problem.Instance = context.Request.Path;
        problem.Extensions["errorCode"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        return problem;
    }

    public static async Task WriteAsync(HttpContext context, ProblemDetails problem, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        if (problem is ValidationProblemDetails validation)
            await context.Response.WriteAsJsonAsync(validation, options: null,
                contentType: "application/problem+json", cancellationToken);
        else
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken);
    }
}
