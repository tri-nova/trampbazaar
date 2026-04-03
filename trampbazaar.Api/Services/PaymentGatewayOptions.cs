namespace trampbazaar.Api.Services;

public sealed class PaymentGatewayOptions
{
    public string Provider { get; set; } = "demo";
    public string DefaultSuccessUrl { get; set; } = string.Empty;
    public string DefaultCancelUrl { get; set; } = string.Empty;
    public StripePaymentOptions Stripe { get; set; } = new();
}

public sealed class StripePaymentOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
