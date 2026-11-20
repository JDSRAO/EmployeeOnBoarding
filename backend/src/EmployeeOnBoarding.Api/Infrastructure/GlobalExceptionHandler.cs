using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EmployeeOnBoarding.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isDuplicate = exception is SqlException { Number: 2601 or 2627 };
        var statusCode = isDuplicate
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;

        logger.LogError(
            exception,
            "Unhandled exception. TraceId: {TraceId}, Path: {Path}",
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = isDuplicate ? "A record already exists" : "An unexpected error occurred",
            Detail = isDuplicate
                ? "An employee with this email address already exists."
                : environment.IsDevelopment()
                    ? exception.Message
                    : "The request could not be completed. Please contact support with the trace id.",
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
