using UnionRailway;
using UnionRailway.HttpClient;

/// <summary>
/// Client for the external payment gateway.
///
/// The UnionRailway.HttpClient extensions handle all HTTP status-code → error
/// mapping automatically (400 → Validation, 401 → Unauthorized, 403 → Forbidden,
/// 404 → NotFound, 409 → Conflict, 5xx → SystemFailure), so this class needs
/// zero error-handling code of its own.
/// </summary>
sealed class PaymentGatewayClient(System.Net.Http.HttpClient http)
{
    public ValueTask<Rail<PaymentResponse>> ChargeAsync(
        PaymentRequest request, CancellationToken ct = default)
        => http.PostAsJsonAsUnionAsync<PaymentResponse>("/v1/payments/charge", request, ct);
}

// ── Simulated payment gateway ─────────────────────────────────────────────────
// Stands in for a real HTTP server during local development.
// In production you'd point the HttpClient at the real gateway base address.

sealed class FakePaymentHandler : System.Net.Http.HttpMessageHandler
{
    protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(
        System.Net.Http.HttpRequestMessage req, CancellationToken ct)
    {
        // Inspect the card token from the serialised request body to pick the response
        var body = req.Content is not null
            ? await req.Content.ReadAsStringAsync(ct)
            : "";

        if (body.Contains("card-valid"))
        {
            return new(System.Net.HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(
                    """{"transactionId":"txn_abc123","status":"Approved"}""",
                    System.Text.Encoding.UTF8, "application/json")
            };
        }

        if (body.Contains("card-declined"))
        {
            return new(System.Net.HttpStatusCode.Conflict)
            {
                Content = new System.Net.Http.StringContent(
                    """{"title":"Payment Declined","detail":"Insufficient funds","status":409}""",
                    System.Text.Encoding.UTF8, "application/problem+json")
            };
        }

        if (body.Contains("card-stolen"))
        {
            return new(System.Net.HttpStatusCode.Forbidden)
            {
                Content = new System.Net.Http.StringContent(
                    """{"title":"Card Blocked","detail":"Card reported as lost/stolen","status":403}""",
                    System.Text.Encoding.UTF8, "application/problem+json")
            };
        }

        return new(System.Net.HttpStatusCode.InternalServerError);
    }
}
