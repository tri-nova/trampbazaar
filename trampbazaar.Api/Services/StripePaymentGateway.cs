using Stripe;
using Stripe.Checkout;

namespace trampbazaar.Api.Services;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripePaymentOptions options;

    public StripePaymentGateway(IConfiguration configuration)
    {
        options = configuration.GetSection("Payments").Get<PaymentGatewayOptions>()?.Stripe ?? new StripePaymentOptions();
    }

    public string ProviderName => "stripe";

    public bool IsEnabled => !string.IsNullOrWhiteSpace(options.SecretKey) && !string.IsNullOrWhiteSpace(options.WebhookSecret);

    public async Task<PaymentGatewayCheckoutSession> CreatePackageCheckoutAsync(PaymentGatewayCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Stripe odeme ayarlari eksik.");
        }

        StripeConfiguration.ApiKey = options.SecretKey;

        var service = new SessionService();
        var session = await service.CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = AppendPaymentId(request.SuccessUrl, request.PaymentId),
            CancelUrl = AppendPaymentId(request.CancelUrl, request.PaymentId),
            Metadata = new Dictionary<string, string>
            {
                ["paymentId"] = request.PaymentId.ToString(),
                ["packageId"] = request.PackageId.ToString(),
                ["userName"] = request.UserName,
                ["paymentType"] = request.PaymentType
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.CurrencyCode.Trim().ToLowerInvariant(),
                        UnitAmount = Convert.ToInt64(decimal.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero)),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.PackageName,
                            Description = $"TrampBazaar paket satin alma: {request.PaymentType}"
                        }
                    }
                }
            ]
        }, cancellationToken: cancellationToken);

        return new PaymentGatewayCheckoutSession
        {
            ProviderTransactionId = session.Id,
            CheckoutUrl = session.Url ?? string.Empty
        };
    }

    public PaymentWebhookParseResult ParseWebhook(string payload, string? signatureHeader)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Stripe webhook ayarlari eksik.");
        }

        var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, options.WebhookSecret);
        var session = stripeEvent.Data.Object as Session;
        if (session is null)
        {
            return new PaymentWebhookParseResult { EventType = stripeEvent.Type };
        }

        return new PaymentWebhookParseResult
        {
            EventType = stripeEvent.Type,
            ProviderTransactionId = session.Id,
            IsPaymentCompleted = stripeEvent.Type == EventTypes.CheckoutSessionCompleted,
            IsPaymentFailed = stripeEvent.Type == EventTypes.CheckoutSessionExpired,
            PaidAt = stripeEvent.Type == EventTypes.CheckoutSessionCompleted
                ? DateTimeOffset.UtcNow
                : null
        };
    }

    private static string AppendPaymentId(string url, Guid paymentId)
    {
        var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{separator}paymentId={paymentId}";
    }
}
