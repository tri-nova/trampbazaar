using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using trampbazaar.Api.Services;

namespace trampbazaar.Tests;

public sealed class ConfigurationSafetyValidatorTests
{
    [Fact]
    public void ValidateRuntimeConfiguration_SkipsChecks_InDevelopment()
    {
        var configuration = BuildConfiguration(
            signingKey: "change-this-production-signing-key",
            connectionString: "Server=tcp:db.example.com,1433;Database=TrampBazaar;User Id=app;Password=replace-me;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;",
            allowedOrigins: []);

        var exception = Record.Exception(() =>
            ConfigurationSafetyValidator.ValidateRuntimeConfiguration(configuration, new FakeHostEnvironment("Development")));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateRuntimeConfiguration_RejectsPlaceholderSigningKey_InProductionLikeEnvironment()
    {
        var configuration = BuildConfiguration(
            signingKey: "change-this-production-signing-key",
            connectionString: "Server=tcp:db-prod.example.com,1433;Database=TrampBazaar;User Id=app;Password=strong-password;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;",
            allowedOrigins: ["https://app.example.com"]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationSafetyValidator.ValidateRuntimeConfiguration(configuration, new FakeHostEnvironment("Production")));

        Assert.Contains("Auth:SigningKey", exception.Message);
    }

    [Fact]
    public void ValidateRuntimeConfiguration_RejectsPlaceholderConnectionString_InProductionLikeEnvironment()
    {
        var configuration = BuildConfiguration(
            signingKey: "production-signing-key-1234567890-abcdefghij",
            connectionString: "Server=tcp:db.example.com,1433;Database=TrampBazaar;User Id=app;Password=replace-me;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;",
            allowedOrigins: ["https://app.example.com"]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationSafetyValidator.ValidateRuntimeConfiguration(configuration, new FakeHostEnvironment("Staging")));

        Assert.Contains("ConnectionStrings:SqlServer", exception.Message);
    }

    [Fact]
    public void ValidateRuntimeConfiguration_RejectsMissingCorsOrigins_InProductionLikeEnvironment()
    {
        var configuration = BuildConfiguration(
            signingKey: "production-signing-key-1234567890-abcdefghij",
            connectionString: "Server=tcp:db-prod.example.com,1433;Database=TrampBazaar;User Id=app;Password=strong-password;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;",
            allowedOrigins: []);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationSafetyValidator.ValidateRuntimeConfiguration(configuration, new FakeHostEnvironment("Staging")));

        Assert.Contains("Cors:AllowedOrigins", exception.Message);
    }

    [Fact]
    public void ValidateRuntimeConfiguration_RejectsWildcardCorsOrigin_InProductionLikeEnvironment()
    {
        var configuration = BuildConfiguration(
            signingKey: "production-signing-key-1234567890-abcdefghij",
            connectionString: "Server=tcp:db-prod.example.com,1433;Database=TrampBazaar;User Id=app;Password=strong-password;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;",
            allowedOrigins: ["*"]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationSafetyValidator.ValidateRuntimeConfiguration(configuration, new FakeHostEnvironment("Staging")));

        Assert.Contains("Cors:AllowedOrigins", exception.Message);
    }

    [Fact]
    public void ValidateRuntimeConfiguration_RejectsDemoPayments_InProduction()
    {
        var configuration = BuildConfiguration(
            signingKey: "production-signing-key-1234567890-abcdefghij",
            connectionString: "Server=tcp:db-prod.example.com,1433;Database=TrampBazaar;User Id=app;Password=strong-password;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;",
            allowedOrigins: ["https://app.example.com"],
            paymentProvider: "demo");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationSafetyValidator.ValidateRuntimeConfiguration(configuration, new FakeHostEnvironment("Production")));

        Assert.Contains("Payments:Provider", exception.Message);
    }

    [Fact]
    public void ValidateRuntimeConfiguration_RejectsPlaceholderStripeSettings_InProductionLikeEnvironment()
    {
        var configuration = BuildConfiguration(
            signingKey: "production-signing-key-1234567890-abcdefghij",
            connectionString: "Server=tcp:db-prod.example.com,1433;Database=TrampBazaar;User Id=app;Password=strong-password;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;",
            allowedOrigins: ["https://app.example.com"],
            paymentProvider: "stripe",
            stripeSecretKey: "sk_live_xxx",
            stripeWebhookSecret: "whsec_xxx");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationSafetyValidator.ValidateRuntimeConfiguration(configuration, new FakeHostEnvironment("Staging")));

        Assert.Contains("Payments:Stripe", exception.Message);
    }

    [Fact]
    public void ValidateRuntimeConfiguration_AllowsRealisticValues_InProductionLikeEnvironment()
    {
        var configuration = BuildConfiguration(
            signingKey: "production-signing-key-1234567890-abcdefghij",
            connectionString: "Server=tcp:db-prod.example.com,1433;Database=TrampBazaar;User Id=app;Password=strong-password;Encrypt=True;TrustServerCertificate=False;Connect Timeout=30;",
            allowedOrigins: ["https://app.example.com"],
            paymentProvider: "stripe",
            stripeSecretKey: "sk_live_realistic_secret",
            stripeWebhookSecret: "whsec_realistic_secret");

        var exception = Record.Exception(() =>
            ConfigurationSafetyValidator.ValidateRuntimeConfiguration(configuration, new FakeHostEnvironment("Production")));

        Assert.Null(exception);
    }

    private static IConfiguration BuildConfiguration(
        string signingKey,
        string connectionString,
        string[] allowedOrigins,
        string paymentProvider = "stripe",
        string stripeSecretKey = "sk_live_realistic_secret",
        string stripeWebhookSecret = "whsec_realistic_secret")
    {
        var settings = new Dictionary<string, string?>
            {
                ["Auth:SigningKey"] = signingKey,
                ["ConnectionStrings:SqlServer"] = connectionString,
                ["Payments:Provider"] = paymentProvider,
                ["Payments:Stripe:SecretKey"] = stripeSecretKey,
                ["Payments:Stripe:WebhookSecret"] = stripeWebhookSecret
            };

        for (var index = 0; index < allowedOrigins.Length; index++)
        {
            settings[$"Cors:AllowedOrigins:{index}"] = allowedOrigins[index];
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "trampbazaar.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
