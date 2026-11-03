using System.Diagnostics;
using System.Text.Json;
using Daycare.Application.Common.Exceptions;
using Daycare.Domain.Exceptions;

namespace Daycare.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, title, detail, code, errors) = exception switch
        {
            ValidationFailedException ex => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                ex.Message,
                ex.Code,
                (IDictionary<string, string[]>?)ex.Errors),

            UnauthorizedException ex => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                ex.Message,
                ex.Code,
                null),

            UnauthorizedAccessException ex => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                ex.Message,
                "UNAUTHORIZED",
                null),

            ForbiddenException ex => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                ex.Message,
                ex.Code,
                null),

            NotFoundException ex => (
                StatusCodes.Status404NotFound,
                "Not found",
                ex.Message,
                ex.Code,
                null),

            ConflictException ex => (
                StatusCodes.Status409Conflict,
                "Conflict",
                ex.Message,
                ex.Code,
                null),

            DomainRuleException ex => (
                StatusCodes.Status422UnprocessableEntity,
                "Business rule violation",
                ex.Message,
                ex.Code,
                null),

            BadHttpRequestException ex => (
                ex.StatusCode,
                ex.StatusCode == StatusCodes.Status413PayloadTooLarge ? "Payload too large" : "Bad request",
                ex.Message,
                ex.StatusCode == StatusCodes.Status413PayloadTooLarge ? "PAYLOAD_TOO_LARGE" : "BAD_REQUEST",
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred. Please try again later.",
                "INTERNAL_ERROR",
                null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning("Handled exception ({Code}): {Message}. TraceId: {TraceId}", code, exception.Message, traceId);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new Dictionary<string, object?>
        {
            { "title", title },
            { "status", statusCode },
            { "detail", detail },
            { "code", code },
            { "traceId", traceId }
        };

        if (errors != null)
        {
            problemDetails["errors"] = errors;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, jsonOptions));
    }
}
