using Microsoft.EntityFrameworkCore;
using UnionRailway;

namespace UnionRailway.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for <see cref="DbContext"/> that return
/// <see cref="Result{T}"/> instead of throwing exceptions.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Saves all changes made in the context as a <see cref="Result{T}"/>.
    /// Catches <see cref="DbUpdateConcurrencyException"/> and maps to <see cref="UnionError.Conflict"/>.
    /// Catches <see cref="DbUpdateException"/> and maps to <see cref="UnionError.SystemFailure"/>.
    /// </summary>
    public static async Task<Result<int>> SaveChangesAsUnionAsync(
        this DbContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new Result<int>.Ok(count);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return new Result<int>.Error(new UnionError.Conflict(ex.Message));
        }
        catch (DbUpdateException ex)
        {
            return new Result<int>.Error(new UnionError.SystemFailure(ex));
        }
        catch (Exception ex)
        {
            return new Result<int>.Error(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Finds an entity by its primary key values as a <see cref="Result{T}"/>.
    /// Returns <see cref="UnionError.NotFound"/> if no entity is found.
    /// </summary>
    public static async Task<Result<T>> FindAsUnionAsync<T>(
        this DbContext context,
        params object[] keyValues) where T : class
    {
        try
        {
            var entity = await context.FindAsync<T>(keyValues).ConfigureAwait(false);

            return entity is not null
                ? new Result<T>.Ok(entity)
                : new Result<T>.Error(new UnionError.NotFound(typeof(T).Name));
        }
        catch (Exception ex)
        {
            return new Result<T>.Error(new UnionError.SystemFailure(ex));
        }
    }
}
