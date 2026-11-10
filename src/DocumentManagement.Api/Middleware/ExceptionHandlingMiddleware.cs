using DocumentManagement.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DocumentManagement.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly Action<ILogger, string, string, Exception> LogUnhandledException =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(LogUnhandledException)),
            "Unhandled exception while processing {Method} {Path}");

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            DocumentValidationException => (StatusCodes.Status400BadRequest, "Invalid upload", exception.Message),
            DocumentNotFoundException => (StatusCodes.Status404NotFound, "Document not found", exception.Message),
            DocumentFileMissingException => (StatusCodes.Status404NotFound, "Stored file not found", exception.Message),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred.")
        };

        if (status >= 500)
        {
            LogUnhandledException(logger, context.Request.Method, context.Request.Path, exception);
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
    }
}

