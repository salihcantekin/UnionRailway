using System.Net;
using System.Net.Http.Json;
using UnionRailway;

namespace UnionRailway.HttpClient;

/// <summary>
/// Provides extension methods for <see cref="System.Net.Http.HttpClient"/> that return
/// <see cref="Result{T}"/> by mapping HTTP status codes to union error types.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Sends a GET request and deserializes the response as a <see cref="Result{T}"/>.
    /// Maps HTTP status codes to the appropriate <see cref="UnionError"/> variant.
    /// </summary>
    public static async Task<Result<T>> GetAsUnionAsync<T>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            return await MapResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new Result<T>.Error(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Sends a POST request with the specified content and deserializes the response as a <see cref="Result{T}"/>.
    /// </summary>
    public static async Task<Result<T>> PostAsUnionAsync<T>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
            return await MapResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new Result<T>.Error(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Sends a PUT request with the specified content and deserializes the response as a <see cref="Result{T}"/>.
    /// </summary>
    public static async Task<Result<T>> PutAsUnionAsync<T>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.PutAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
            return await MapResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new Result<T>.Error(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Sends a DELETE request and deserializes the response as a <see cref="Result{T}"/>.
    /// </summary>
    public static async Task<Result<T>> DeleteAsUnionAsync<T>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
            return await MapResponseAsync<T>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new Result<T>.Error(new UnionError.SystemFailure(ex));
        }
    }

    /// <summary>
    /// Maps an <see cref="HttpResponseMessage"/> to a <see cref="Result{T}"/> based on its status code.
    /// </summary>
    private static async Task<Result<T>> MapResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.OK or HttpStatusCode.Created or HttpStatusCode.Accepted =>
                await DeserializeResponseAsync<T>(response, cancellationToken).ConfigureAwait(false),

            HttpStatusCode.NoContent =>
                HandleNoContent<T>(),

            HttpStatusCode.NotFound =>
                new Result<T>.Error(new UnionError.NotFound(response.RequestMessage?.RequestUri?.ToString() ?? "Unknown resource")),

            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new Result<T>.Error(new UnionError.Unauthorized()),

            HttpStatusCode.Conflict =>
                new Result<T>.Error(new UnionError.Conflict(
                    await ReadBodyOrDefaultAsync(response, "A conflict occurred.", cancellationToken).ConfigureAwait(false))),

            HttpStatusCode.BadRequest =>
                await HandleBadRequestAsync<T>(response, cancellationToken).ConfigureAwait(false),

            _ =>
                new Result<T>.Error(new UnionError.SystemFailure(
                    new HttpRequestException($"Unexpected status code: {(int)response.StatusCode} {response.StatusCode}")))
        };
    }

    private static async Task<Result<T>> DeserializeResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return data is not null
                ? new Result<T>.Ok(data)
                : new Result<T>.Error(new UnionError.SystemFailure(
                    new InvalidOperationException("Response deserialized to null.")));
        }
        catch (Exception ex)
        {
            return new Result<T>.Error(new UnionError.SystemFailure(ex));
        }
    }

    private static Result<T> HandleNoContent<T>()
    {
        if (default(T) is null)
        {
            return new Result<T>.Ok(default!);
        }

        return new Result<T>.Error(new UnionError.SystemFailure(
            new InvalidOperationException($"Cannot map 204 No Content to non-nullable type {typeof(T).Name}.")));
    }

    private static async Task<Result<T>> HandleBadRequestAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var fields = await response.Content
                .ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Result<T>.Error(new UnionError.Validation(
                fields ?? new Dictionary<string, string>()));
        }
        catch
        {
            var body = await ReadBodyOrDefaultAsync(response, "Bad request.", cancellationToken).ConfigureAwait(false);
            return new Result<T>.Error(new UnionError.Validation(
                new Dictionary<string, string> { ["General"] = body }));
        }
    }

    private static async Task<string> ReadBodyOrDefaultAsync(
        HttpResponseMessage response,
        string defaultMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(
#if NET8_0_OR_GREATER
                cancellationToken
#endif
            ).ConfigureAwait(false);

            return string.IsNullOrWhiteSpace(body) ? defaultMessage : body;
        }
        catch
        {
            return defaultMessage;
        }
    }
}
