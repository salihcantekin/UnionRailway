using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnionRailway.HttpClient;

/// <summary>
/// Extension methods that wrap <see cref="System.Net.Http.HttpClient"/> calls,
/// translating HTTP status codes and RFC 7807 error bodies into typed
/// <c>(T Value, UnionError? Error)</c> union tuples. All methods return
/// <see cref="ValueTask{TResult}"/> for allocation-free async composition.
/// </summary>
public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions defaultJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

    // ── GET ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a GET request and returns the deserialized response body as a union.
    /// </summary>
    /// <inheritdoc cref="SendAsUnionAsync{T}"/>
    public static ValueTask<(T Value, UnionError? Error)> GetFromJsonAsUnionAsync<T>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
        where T : class
        => SendAsUnionAsync<T>(client, HttpMethod.Get, requestUri, null, cancellationToken);

    // ── POST ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a POST request with a JSON body and returns the deserialized
    /// response body as a union.
    /// </summary>
    public static ValueTask<(T Value, UnionError? Error)> PostAsJsonAsUnionAsync<T>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        object? body,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var content = body is not null
            ? JsonContent.Create(body, options: defaultJsonOptions)
            : null;

        return SendAsUnionAsync<T>(client, HttpMethod.Post, requestUri, content, cancellationToken);
    }

    // ── PUT ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a PUT request with a JSON body and returns the deserialized
    /// response body as a union.
    /// </summary>
    public static ValueTask<(T Value, UnionError? Error)> PutAsJsonAsUnionAsync<T>(
        this System.Net.Http.HttpClient client,
        string requestUri,
        object? body,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var content = body is not null
            ? JsonContent.Create(body, options: defaultJsonOptions)
            : null;

        return SendAsUnionAsync<T>(client, HttpMethod.Put, requestUri, content, cancellationToken);
    }

    // ── DELETE ─────────────────────────────────────────────────────────────

    /// <summary>Sends a DELETE request and returns <see langword="true"/> on success.</summary>
    public static async ValueTask<(bool Value, UnionError? Error)> DeleteAsUnionAsync(
        this System.Net.Http.HttpClient client,
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            using var response = await client.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? (true, null)
                : (false, new UnionError.SystemFailure(
                      new HttpRequestException($"HTTP {(int)response.StatusCode}")));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (false, new UnionError.SystemFailure(ex));
        }
    }

    // ── Core implementation ────────────────────────────────────────────────

    /// <summary>
    /// Sends <paramref name="method"/> to <paramref name="requestUri"/> and
    /// maps HTTP status codes as follows:
    /// <list type="table">
    ///   <item><term>200 / 201</term><description>Ok(T)</description></item>
    ///   <item><term>400</term><description>Validation error (RFC 7807 body parsed)</description></item>
    ///   <item><term>401</term><description>Unauthorized</description></item>
    ///   <item><term>403</term><description>Forbidden</description></item>
    ///   <item><term>404</term><description>NotFound</description></item>
    ///   <item><term>409</term><description>Conflict</description></item>
    ///   <item><term>other 4xx / 5xx</term><description>SystemFailure</description></item>
    /// </list>
    /// Network and timeout exceptions are caught and mapped to SystemFailure.
    /// </summary>
    private static async ValueTask<(T Value, UnionError? Error)> SendAsUnionAsync<T>(
        System.Net.Http.HttpClient client,
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var request = new HttpRequestMessage(method, requestUri) { Content = content };
            using var response = await client.SendAsync(request, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.OK or HttpStatusCode.Created =>
                    await DeserializeOkAsync<T>(response, cancellationToken),

                HttpStatusCode.BadRequest =>
                    await ParseValidationErrorAsync<T>(response, cancellationToken),

                HttpStatusCode.Unauthorized =>
                    (default!, new UnionError.Unauthorized()),

                HttpStatusCode.Forbidden =>
                    (default!, new UnionError.Forbidden(
                        await TryReadReasonAsync(response, cancellationToken))),

                HttpStatusCode.NotFound =>
                    (default!, new UnionError.NotFound(
                        await TryReadReasonAsync(response, cancellationToken))),

                HttpStatusCode.Conflict =>
                    (default!, new UnionError.Conflict(
                        await TryReadReasonAsync(response, cancellationToken))),

                _ =>
                    (default!, new UnionError.SystemFailure(
                        new HttpRequestException(
                            $"Unexpected HTTP status {(int)response.StatusCode} from {requestUri}.")))
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return (default!, new UnionError.SystemFailure(ex));
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static async ValueTask<(T Value, UnionError? Error)> DeserializeOkAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(
                defaultJsonOptions, cancellationToken);

            return value is not null
                ? (value, null)
                : (default!, new UnionError.SystemFailure(
                      new InvalidOperationException("Server returned an empty response body.")));
        }
        catch (JsonException ex)
        {
            return (default!, new UnionError.SystemFailure(ex));
        }
    }

    private static async ValueTask<(T Value, UnionError? Error)> ParseValidationErrorAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            var body = await response.Content
                .ReadFromJsonAsync<ProblemDetailsDocument>(defaultJsonOptions, cancellationToken);

            if (body?.Errors is { Count: > 0 })
            {
                return (default!, new UnionError.Validation(
                    body.Errors.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal)));
            }

            return (default!, new UnionError.Validation(
                new Dictionary<string, string[]> { [""] = [body?.Title ?? "The request is invalid."] }));
        }
        catch
        {
            return (default!, new UnionError.Validation(
                new Dictionary<string, string[]> { [""] = ["The request is invalid."] }));
        }
    }

    private static async ValueTask<string> TryReadReasonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content
                .ReadFromJsonAsync<ProblemDetailsDocument>(defaultJsonOptions, cancellationToken);
            return body?.Detail ?? body?.Title ?? response.ReasonPhrase ?? string.Empty;
        }
        catch
        {
            return response.ReasonPhrase ?? string.Empty;
        }
    }

    private sealed class ProblemDetailsDocument
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public IDictionary<string, string[]>? Errors { get; set; }
    }
}
