namespace trampbazaar.Api.Services;

public interface IPaymentGateway
{
    string ProviderName { get; }

    bool IsEnabled { get; }

    Task<PaymentGatewayCheckoutSession> CreatePackageCheckoutAsync(PaymentGatewayCheckoutRequest request, CancellationToken cancellationToken = default);

    PaymentWebhookParseResult ParseWebhook(string payload, string? signatureHeader);
}

public sealed class PaymentGatewayCheckoutRequest
{
    public Guid PaymentId { get; set; }
    public Guid PackageId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public sealed class PaymentGatewayCheckoutSession
{
    public string ProviderTransactionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
}

public sealed class PaymentWebhookParseResult
{
    public string EventType { get; set; } = string.Empty;
    public string ProviderTransactionId { get; set; } = string.Empty;
    public DateTimeOffset? PaidAt { get; set; }
    public bool IsPaymentCompleted { get; set; }
    public bool IsPaymentFailed { get; set; }
}
