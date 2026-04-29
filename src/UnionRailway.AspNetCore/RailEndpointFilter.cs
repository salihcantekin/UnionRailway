using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace UnionRailway.AspNetCore;

/// <summary>
/// An <see cref="IEndpointFilter"/> that automatically converts <see cref="Rail{T}"/>
/// return values to <see cref="IResult"/> using the configured
/// <see cref="RailwayOptions"/> and optional <see cref="IUnionErrorMapper"/>.
/// <para>
/// When applied, endpoint handlers can return <c>Rail&lt;T&gt;</c> directly without
/// calling <c>.ToHttpResult()</c>.
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Per-endpoint:
/// app.MapGet("/users/{id}", async (int id, UserService svc) =>
///     await svc.GetUserAsync(id))
///     .AddEndpointFilter&lt;RailEndpointFilter&gt;();
///
/// // Per-group:
/// var group = app.MapGroup("/api").AddEndpointFilter&lt;RailEndpointFilter&gt;();
/// </code>
/// </example>
public sealed class RailEndpointFilter : IEndpointFilter
{
    private static readonly MethodInfo ConvertMethod =
        typeof(RailEndpointFilter).GetMethod(nameof(ConvertRail), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result is null or IResult)
        {
            return result;
        }

        var resultType = result.GetType();
        if (!IsRailType(resultType, out var successType))
        {
            return result;
        }

        var mapper = context.HttpContext.RequestServices.GetService<IUnionErrorMapper>();
        var options = context.HttpContext.RequestServices.GetService<IOptions<RailwayOptions>>()?.Value;

        var converter = ConvertMethod.MakeGenericMethod(successType);
        return converter.Invoke(null, [result, options?.ConfigureProblem, mapper]);
    }

    private static bool IsRailType(Type type, out Type successType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Rail<>))
        {
            successType = type.GetGenericArguments()[0];
            return true;
        }

        successType = default!;
        return false;
    }

    private static IResult ConvertRail<T>(
        Rail<T> rail,
        Action<ProblemDetails>? configureProblem,
        IUnionErrorMapper? errorMapper) =>
        rail.ToHttpResult(configureProblem: configureProblem, errorMapper: errorMapper);
}
