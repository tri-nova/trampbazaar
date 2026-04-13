using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using trampbazaar.Api.Services;

namespace trampbazaar.Tests;

public sealed class StripePaymentGatewayTests
{
    private const string ExpectedStripeApiVersion = "2026-03-25.dahlia";

    [Fact]
    public void ParseWebhook_MapsCompletedCheckoutSession()
    {
        var gateway = CreateGateway(secretKey: "sk_test_123", webhookSecret: "whsec_test_123");
        var payload = """
            {
              "id": "evt_test_completed",
              "object": "event",
              "api_version": "2026-03-25.dahlia",
              "type": "checkout.session.completed",
              "data": {
                "object": {
                  "id": "cs_test_123",
                  "object": "checkout.session"
                }
              }
            }
            """;

        var result = gateway.ParseWebhook(payload, CreateStripeSignatureHeader(payload, "whsec_test_123"));

        Assert.Equal("checkout.session.completed", result.EventType);
        Assert.Equal("cs_test_123", result.ProviderTransactionId);
        Assert.True(result.IsPaymentCompleted);
        Assert.False(result.IsPaymentFailed);
        Assert.NotNull(result.PaidAt);
    }

    [Fact]
    public void ParseWebhook_MapsExpiredCheckoutSession()
    {
        var gateway = CreateGateway(secretKey: "sk_test_123", webhookSecret: "whsec_test_123");
        var payload = """
            {
              "id": "evt_test_expired",
              "object": "event",
              "api_version": "2026-03-25.dahlia",
              "type": "checkout.session.expired",
              "data": {
                "object": {
                  "id": "cs_test_456",
                  "object": "checkout.session"
                }
              }
            }
            """;

        var result = gateway.ParseWebhook(payload, CreateStripeSignatureHeader(payload, "whsec_test_123"));

        Assert.Equal("checkout.session.expired", result.EventType);
        Assert.Equal("cs_test_456", result.ProviderTransactionId);
        Assert.False(result.IsPaymentCompleted);
        Assert.True(result.IsPaymentFailed);
        Assert.Null(result.PaidAt);
    }

    [Fact]
    public async Task CreatePackageCheckoutAsync_Throws_WhenStripeSettingsAreMissing()
    {
        var gateway = CreateGateway(secretKey: "", webhookSecret: "");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.CreatePackageCheckoutAsync(new PaymentGatewayCheckoutRequest
        {
            PaymentId = Guid.NewGuid(),
            PackageId = Guid.NewGuid(),
            PackageName = "Starter Paket",
            PaymentType = "package",
            UserName = "batu",
            Amount = 499,
            CurrencyCode = "TRY",
            SuccessUrl = "https://app.example.com/success",
            CancelUrl = "https://app.example.com/cancel"
        }));

        Assert.Contains("Stripe odeme ayarlari eksik", exception.Message);
    }

    private static StripePaymentGateway CreateGateway(string secretKey, string webhookSecret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Stripe:SecretKey"] = secretKey,
                ["Payments:Stripe:WebhookSecret"] = webhookSecret
            })
            .Build();

        return new StripePaymentGateway(configuration);
    }

    private static string CreateStripeSignatureHeader(string payload, string secret, long? timestamp = null)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{ts}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLowerInvariant();
        return $"t={ts},v1={signature}";
    }
}
