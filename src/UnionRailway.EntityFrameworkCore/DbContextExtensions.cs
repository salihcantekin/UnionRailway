using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace UnionRailway.EntityFrameworkCore;

/// <summary>
/// Extension methods that integrate EF Core queries and save operations with the
/// <see cref="Rail{T}"/> pattern, eliminating null-check
/// boilerplate and wrapping database exceptions into typed <see cref="UnionError"/>
/// values.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Asynchronously returns the first matching entity as a union tuple,
    /// mapping a <see langword="null"/> result to <see cref="UnionError.NotFound"/>
    /// and <see cref="DbException"/> to <see cref="UnionError.SystemFailure"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The queryable source.</param>
    /// <param name="resourceName">Human-readable resource name used in the NotFound message.</param>
    /// <param name="predicate">Optional filter predicate; when omitted the first row is returned.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    public static async ValueTask<Rail<TEntity>> FirstOrDefaultAsUnionAsync<TEntity>(
        this IQueryable<TEntity> query,
        string resourceName,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        try
        {
            IQueryable<TEntity> filtered = predicate is not null ? query.Where(predicate) : query;
            TEntity? entity = await filtered.FirstOrDefaultAsync(cancellationToken);

            return entity is not null
                ? entity
                : Union.Fail<TEntity>(new UnionError.NotFound(resourceName));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbException ex)
        {
            return Union.Fail<TEntity>(new UnionError.SystemFailure(ex));
        }
        catch (Exception ex)
        {
            return Union.Fail<TEntity>(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Asynchronously materializes the query to a list and maps provider failures to <see cref="UnionError.SystemFailure"/>.
    /// </summary>
    public static async ValueTask<Rail<List<TEntity>>> ToListAsUnionAsync<TEntity>(
        this IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await query.ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbException ex)
        {
            return Union.Fail<List<TEntity>>(new UnionError.SystemFailure(ex));
        }
        catch (Exception ex)
        {
            return Union.Fail<List<TEntity>>(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Asynchronously saves all pending changes and returns the number of
    /// affected rows. Maps concurrency and unique-constraint violations to
    /// <see cref="UnionError.Conflict"/> and all other exceptions to
    /// <see cref="UnionError.SystemFailure"/>.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> to save.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    public static async ValueTask<Rail<int>> SaveChangesAsUnionAsync(
        this DbContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int affected = await context.SaveChangesAsync(cancellationToken);

            return affected;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Union.Fail<int>(new UnionError.Conflict($"Concurrency conflict: {ex.Message}"));
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Union.Fail<int>(new UnionError.Conflict("A unique constraint violation occurred."));
        }
        catch (DbUpdateException ex)
        {
            return Union.Fail<int>(new UnionError.SystemFailure(ex));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Union.Fail<int>(new UnionError.SystemFailure(ex));
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        Exception? inner = exception.InnerException;

        if (inner is null)
        {
            return false;
        }

        string message = inner.Message;

        return message.Contains("unique", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || message.Contains("2627", StringComparison.Ordinal)
            || message.Contains("2601", StringComparison.Ordinal)
            || message.Contains("23505", StringComparison.Ordinal);
    }
}
