using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UnionRailway.AspNetCore;

/// <summary>
/// Middleware that catches unhandled exceptions and returns them as RFC 7807
/// <see cref="ProblemDetails"/> responses consistent with UnionRailway's error format.
/// <para>
/// This ensures all API errors — whether from <see cref="Rail{T}"/> or unexpected
/// exceptions — use the same response shape.
/// </para>
/// </summary>
/// <example>
/// <code>
/// app.UseRailwayExceptionHandler();
/// </code>
/// </example>
public sealed class RailwayExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RailwayExceptionMiddleware> _logger;

    /// <summary>Creates a new instance of <see cref="RailwayExceptionMiddleware"/>.</summary>
    public RailwayExceptionMiddleware(
        RequestDelegate next,
        ILogger<RailwayExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invokes the middleware.</summary>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var error = new UnionError.SystemFailure(ex);
            var options = context.RequestServices.GetService<IOptions<RailwayOptions>>()?.Value;
            var mapper = context.RequestServices.GetService<IUnionErrorMapper>();

            var httpResult = ((UnionError)error).ToHttpResult(options?.ConfigureProblem, mapper);
            await httpResult.ExecuteAsync(context);
        }
    }
}

/// <summary>
/// Extension methods for adding the <see cref="RailwayExceptionMiddleware"/>
/// to the request pipeline.
/// </summary>
public static class RailwayMiddlewareExtensions
{
    /// <summary>
    /// Adds middleware that catches unhandled exceptions and returns RFC 7807
    /// <see cref="ProblemDetails"/> responses consistent with UnionRailway's format.
    /// <para>
    /// Place this early in the pipeline, before routing and authentication.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.UseRailwayExceptionHandler();
    /// app.UseAuthentication();
    /// app.UseAuthorization();
    /// app.MapControllers();
    /// </code>
    /// </example>
    public static IApplicationBuilder UseRailwayExceptionHandler(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<RailwayExceptionMiddleware>();
    }
}
