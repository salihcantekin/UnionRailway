namespace UnionRailway;

/// <summary>
/// A utility bridge for migrating exception-throwing legacy code into the
/// union paradigm without rewriting services. Catches common exceptions and
/// maps them to typed <see cref="UnionError"/> values so callers receive a
/// predictable <c>(T Value, UnionError? Error)</c> union instead of a thrown
/// exception.
/// </summary>
/// <example>
/// <code>
/// // Before (legacy code throws):
/// var user = await legacyRepo.GetUserAsync(id); // may throw KeyNotFoundException
///
/// // After (wrapped in union):
/// var result = await UnionWrapper.RunAsync(() => legacyRepo.GetUserAsync(id));
/// if (!result.IsSuccess(out var user, out var err))
///     return err!.ToHttpResult();
/// </code>
/// </example>
public static class UnionWrapper
{
    /// <summary>
    /// Executes <paramref name="action"/> and translates common exceptions
    /// into typed <see cref="UnionError"/> values:
    /// <list type="bullet">
    ///   <item><description>Null return → <see cref="UnionError.NotFound"/></description></item>
    ///   <item><description><see cref="UnauthorizedAccessException"/> → <see cref="UnionError.Unauthorized"/></description></item>
    ///   <item><description><see cref="KeyNotFoundException"/> → <see cref="UnionError.NotFound"/></description></item>
    ///   <item><description>Any other exception → <see cref="UnionError.SystemFailure"/></description></item>
    /// </list>
    /// <see cref="OperationCanceledException"/> is always re-thrown so
    /// that cancellation propagates correctly.
    /// </summary>
    /// <typeparam name="T">The success value type (must be a reference type).</typeparam>
    /// <param name="action">The async delegate wrapping existing code.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> of <c>(T Value, UnionError? Error)</c>.
    /// </returns>
    public static async ValueTask<(T Value, UnionError? Error)> RunAsync<T>(
        Func<Task<T?>> action) where T : class
    {
        try
        {
            var value = await action();
            
            return value is null
                ? (default!, new UnionError.NotFound("Result"))
                : (value, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return (default!, new UnionError.Unauthorized());
        }
        catch (KeyNotFoundException ex)
        {
            return (default!, new UnionError.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return (default!, new UnionError.SystemFailure(ex));
        }
    }
}
