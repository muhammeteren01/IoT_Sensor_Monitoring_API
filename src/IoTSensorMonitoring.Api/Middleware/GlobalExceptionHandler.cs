using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using IoTSensorMonitoring.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorMonitoring.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, errors) = Map(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            if (IsDatabaseUnavailable(exception))
            {
                _logger.LogError("Database unavailable: {Message}", GetInnermost(exception).Message);
            }
            else
            {
                _logger.LogError(exception, "Unhandled error: {Message}", exception.Message);
            }
        }
        else
        {
            _logger.LogWarning("HTTP {StatusCode} {Title}: {Message}", statusCode, title, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private (int StatusCode, string Title, string? Detail, IDictionary<string, string[]>? Errors) Map(Exception exception)
    {
        if (IsDatabaseUnavailable(exception))
        {
            return (
                StatusCodes.Status503ServiceUnavailable,
                "Database Unavailable",
                _environment.IsDevelopment()
                    ? "PostgreSQL is not reachable. Start it with: docker compose up -d postgres"
                    : "The database is temporarily unavailable.",
                null);
        }

        return exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                validation.Message,
                validation.Errors.Count == 0 ? null : validation.Errors),

            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFound.Message,
                null),

            ConflictException conflict => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflict.Message,
                null),

            UnauthorizedException unauthorized => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                unauthorized.Message,
                null),

            ForbiddenException forbidden => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                forbidden.Message,
                null),

            UnauthorizedAccessException unauthorizedAccess => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                unauthorizedAccess.Message,
                null),

            ArgumentException argument => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                argument.Message,
                null),

            JsonException json => (
                StatusCodes.Status400BadRequest,
                "Invalid JSON",
                json.Message,
                null),

            BadHttpRequestException badRequest => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                badRequest.Message,
                null),

            TimeoutException timeout => (
                StatusCodes.Status504GatewayTimeout,
                "Timeout",
                timeout.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                null)
        };
    }

    private static bool IsDatabaseUnavailable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException or TimeoutException)
            {
                return true;
            }

            var typeName = current.GetType().Name;
            if (typeName is "NpgsqlException" or "PostgresException")
            {
                return true;
            }

            if (current.Message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static Exception GetInnermost(Exception exception)
    {
        while (exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }
}
