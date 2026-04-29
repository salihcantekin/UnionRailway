using Microsoft.AspNetCore.Http;

namespace UnionRailway.AspNetCore;

/// <summary>
/// Allows customizing how specific <see cref="UnionError"/> cases are mapped
/// to <see cref="IResult"/> responses. Register an implementation in DI to
/// override the default RFC 7807 mapping for any error type.
/// <para>
/// When <see cref="TryMap"/> returns a non-null <see cref="IResult"/>,
/// that result is used directly. When it returns <see langword="null"/>,
/// the default mapping in <see cref="ResultHttpExtensions.ToHttpResult(UnionError, Action{Microsoft.AspNetCore.Mvc.ProblemDetails}?)"/>
/// is applied instead.
/// </para>
/// </summary>
/// <example>
/// <code>
/// public class CustomErrorMapper : IUnionErrorMapper
/// {
///     public IResult? TryMap(UnionError error) => error.Value switch
///     {
///         UnionError.NotFound nf => Results.Problem(
///             detail: $"We could not locate '{nf.Resource}'. Please verify the identifier.",
///             statusCode: 404,
///             title: "Resource Not Found"),
///         _ => null // fall back to default mapping
///     };
/// }
///
/// // Registration:
/// builder.Services.AddSingleton&lt;IUnionErrorMapper, CustomErrorMapper&gt;();
/// </code>
/// </example>
public interface IUnionErrorMapper
{
    /// <summary>
    /// Attempts to map the given <paramref name="error"/> to an <see cref="IResult"/>.
    /// </summary>
    /// <param name="error">The error to map.</param>
    /// <returns>
    /// A custom <see cref="IResult"/> if this mapper handles the error;
    /// <see langword="null"/> to fall through to the default mapping.
    /// </returns>
    IResult? TryMap(UnionError error);
}
