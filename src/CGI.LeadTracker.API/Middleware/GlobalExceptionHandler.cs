using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace CGI.LeadTracker.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) =>
        _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationEx)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            await httpContext.Response.WriteAsJsonAsync(
                new { type = "ValidationError", errors },
                cancellationToken);

            return true;
        }

        _logger.LogError(exception, "Erro não tratado na requisição.");
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new { type = "InternalServerError", error = "Ocorreu um erro interno. Verifique os logs." },
            cancellationToken);

        return true;
    }
}
