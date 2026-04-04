using Microsoft.EntityFrameworkCore;
using UnionRailway;

namespace UnionRailway.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for <see cref="IQueryable{T}"/> that return
/// <see cref="Result{T}"/> instead of throwing exceptions or returning null.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Returns the first element matching the query as a <see cref="Result{T}"/>.
    /// Returns <see cref="UnionError.NotFound"/> if no element is found.
    /// Catches any exception and maps it to <see cref="UnionError.SystemFailure"/>.
    /// </summary>
    public static async Task<Result<T>> FirstOrDefaultAsUnionAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var entity = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return entity is not null
                ? new Result<T>.Ok(entity)
                : new Result<T>.Error(new UnionError.NotFound(typeof(T).Name));
        }
        catch (Exception ex)
        {
            return new Result<T>.Error(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Returns the single element matching the query as a <see cref="Result{T}"/>.
    /// Returns <see cref="UnionError.NotFound"/> if no element is found.
    /// Returns <see cref="UnionError.Conflict"/> if more than one element is found.
    /// Catches any exception and maps it to <see cref="UnionError.SystemFailure"/>.
    /// </summary>
    public static async Task<Result<T>> SingleOrDefaultAsUnionAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var entity = await query.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return entity is not null
                ? new Result<T>.Ok(entity)
                : new Result<T>.Error(new UnionError.NotFound(typeof(T).Name));
        }
        catch (InvalidOperationException)
        {
            return new Result<T>.Error(new UnionError.Conflict($"More than one {typeof(T).Name} element was found."));
        }
        catch (Exception ex)
        {
            return new Result<T>.Error(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Returns all elements matching the query as a <see cref="Result{T}"/> containing a list.
    /// Catches any exception and maps it to <see cref="UnionError.SystemFailure"/>.
    /// </summary>
    public static async Task<Result<List<T>>> ToListAsUnionAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var list = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
            return new Result<List<T>>.Ok(list);
        }
        catch (Exception ex)
        {
            return new Result<List<T>>.Error(new UnionError.SystemFailure(ex));
        }
    }
}
