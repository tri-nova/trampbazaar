namespace trampbazaar.Api.Services;

public sealed class DemoPaymentGateway : IPaymentGateway
{
    public string ProviderName => "demo";

    public bool IsEnabled => true;

    public Task<PaymentGatewayCheckoutSession> CreatePackageCheckoutAsync(PaymentGatewayCheckoutRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentGatewayCheckoutSession
        {
            ProviderTransactionId = request.PaymentId.ToString("N"),
            CheckoutUrl = string.Empty
        });

    public PaymentWebhookParseResult ParseWebhook(string payload, string? signatureHeader)
        => throw new NotSupportedException("Demo odeme saglayicisi webhook desteklemez.");
}
