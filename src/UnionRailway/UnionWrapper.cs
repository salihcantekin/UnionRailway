namespace UnionRailway;

/// <summary>
/// A utility bridge for migrating exception-throwing legacy code into the
/// union paradigm without rewriting services. Catches common exceptions and
/// maps them to typed <see cref="UnionError"/> values so callers receive a
/// predictable <see cref="Rail{T}"/> instead of a thrown
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
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="action">The async delegate wrapping existing code.</param>
    public static async ValueTask<Rail<T>> RunAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return Union.Fail<T>(new UnionError.Unauthorized());
        }
        catch (KeyNotFoundException ex)
        {
            return Union.Fail<T>(new UnionError.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Union.Fail<T>(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Executes <paramref name="action"/> and maps a <see langword="null"/> return value
    /// to <see cref="UnionError.NotFound"/>.
    /// </summary>
    public static async ValueTask<Rail<T>> RunNullableAsync<T>(
        Func<Task<T?>> action,
        string resourceName = "Result")
        where T : class
    {
        try
        {
            var value = await action();

            return value is null
                ? Union.Fail<T>(new UnionError.NotFound(resourceName))
                : value;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return Union.Fail<T>(new UnionError.Unauthorized());
        }
        catch (KeyNotFoundException ex)
        {
            return Union.Fail<T>(new UnionError.NotFound(ex.Message));
        }
        catch (Exception ex)
        {
            return Union.Fail<T>(new UnionError.SystemFailure(ex));
        }
    }
}
