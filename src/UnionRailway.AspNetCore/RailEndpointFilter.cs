using System.Collections.Concurrent;
using System.Linq.Expressions;
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
/// <para>
/// The generic conversion delegate is compiled and cached per <c>T</c> on first use,
/// so subsequent requests incur zero reflection overhead.
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
    private static readonly ConcurrentDictionary<Type, RailConverter> ConverterCache = new();

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
        if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(Rail<>))
        {
            return result;
        }

        var converter = ConverterCache.GetOrAdd(resultType, static type =>
            BuildConverter(type.GetGenericArguments()[0]));

        var mapper = context.HttpContext.RequestServices.GetService<IUnionErrorMapper>();
        var options = context.HttpContext.RequestServices.GetService<IOptions<RailwayOptions>>()?.Value;

        return converter(result, options?.ConfigureProblem, mapper);
    }

    /// <summary>
    /// Builds a compiled delegate that unboxes <c>object</c> to <c>Rail&lt;T&gt;</c>
    /// and calls <c>ToHttpResult</c> without any reflection on subsequent invocations.
    /// </summary>
    private static RailConverter BuildConverter(Type successType)
    {
        var railBoxedParam = Expression.Parameter(typeof(object), "railBoxed");
        var configureParam = Expression.Parameter(typeof(Action<ProblemDetails>), "configure");
        var mapperParam = Expression.Parameter(typeof(IUnionErrorMapper), "mapper");

        var railType = typeof(Rail<>).MakeGenericType(successType);
        var unboxed = Expression.Convert(railBoxedParam, railType);
        var nullString = Expression.Constant(null, typeof(string));

        var toHttpResult = typeof(ResultHttpExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(ResultHttpExtensions.ToHttpResult)
                        && m.IsGenericMethodDefinition
                        && m.GetParameters().Length == 4)
            .MakeGenericMethod(successType);

        var call = Expression.Call(
            toHttpResult,
            unboxed,
            nullString,
            configureParam,
            mapperParam);

        return Expression.Lambda<RailConverter>(
            call, railBoxedParam, configureParam, mapperParam)
            .Compile();
    }

    private delegate IResult RailConverter(
        object railBoxed,
        Action<ProblemDetails>? configureProblem,
        IUnionErrorMapper? errorMapper);
}
