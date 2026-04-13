using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace trampbazaar.Api.Services;

public static class ConfigurationSafetyValidator
{
    private const string PlaceholderSigningKey = "change-this-production-signing-key";
    private const string PlaceholderSqlServerConnection =
        "Server=tcp:db.example.com,1433;Database=TrampBazaar;User Id=app;Password=replace-me;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;";
    private const string PlaceholderStripeSecretKey = "sk_live_xxx";
    private const string PlaceholderStripeWebhookSecret = "whsec_xxx";

    public static void ValidateRuntimeConfiguration(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("IntegrationTest"))
        {
            return;
        }

        var signingKey = configuration["Auth:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) ||
            string.Equals(signingKey, PlaceholderSigningKey, StringComparison.Ordinal) ||
            signingKey.Length < 32)
        {
            throw new InvalidOperationException("Auth:SigningKey production icin guvenli bir deger olmali.");
        }

        var sqlServerConnection = configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(sqlServerConnection) ||
            string.Equals(sqlServerConnection, PlaceholderSqlServerConnection, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("ConnectionStrings:SqlServer production icin gercek bir baglanti dizesi olmali.");
        }

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0 || allowedOrigins.Any(origin => string.IsNullOrWhiteSpace(origin) || origin == "*"))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins production benzeri ortamlarda acik veya bos olamaz.");
        }

        var paymentProvider = configuration["Payments:Provider"];
        if (environment.IsProduction() && string.Equals(paymentProvider, "demo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Payments:Provider Production ortaminda demo olamaz.");
        }

        if (string.Equals(paymentProvider, "stripe", StringComparison.OrdinalIgnoreCase))
        {
            var stripeSecretKey = configuration["Payments:Stripe:SecretKey"];
            var stripeWebhookSecret = configuration["Payments:Stripe:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(stripeSecretKey) ||
                string.IsNullOrWhiteSpace(stripeWebhookSecret) ||
                string.Equals(stripeSecretKey, PlaceholderStripeSecretKey, StringComparison.Ordinal) ||
                string.Equals(stripeWebhookSecret, PlaceholderStripeWebhookSecret, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Payments:Stripe ayarlari production benzeri ortamlarda gercek degerler icermeli.");
            }
        }
    }
}
