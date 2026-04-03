using Microsoft.Extensions.Options;

namespace trampbazaar.Api.Services;

public sealed class PaymentGatewayRouter(
    IOptions<PaymentGatewayOptions> options,
    DemoPaymentGateway demoPaymentGateway,
    StripePaymentGateway stripePaymentGateway)
{
    private readonly PaymentGatewayOptions paymentOptions = options.Value;

    public IPaymentGateway Resolve()
        => string.Equals(paymentOptions.Provider, "stripe", StringComparison.OrdinalIgnoreCase)
            ? stripePaymentGateway
            : demoPaymentGateway;

    public string GetSuccessUrl(string? preferredUrl)
        => !string.IsNullOrWhiteSpace(preferredUrl)
            ? preferredUrl
            : paymentOptions.DefaultSuccessUrl;

    public string GetCancelUrl(string? preferredUrl)
        => !string.IsNullOrWhiteSpace(preferredUrl)
            ? preferredUrl
            : paymentOptions.DefaultCancelUrl;
}
