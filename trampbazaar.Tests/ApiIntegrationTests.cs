using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using trampbazaar.Shared.Api;
using trampbazaar.Shared.Contracts;
using trampbazaar.Api.Services;

namespace trampbazaar.Tests;

public sealed class ApiIntegrationTests
{
    private const string ExpectedStripeApiVersion = "2026-03-25.dahlia";

    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_WhenDatabaseUnavailable_ReturnsServiceUnavailablePayload()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ApiRoutes.Dashboard);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("database_unavailable", payload["code"]);
    }

    [Fact]
    public async Task Account_WithoutBearerToken_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(ApiRoutes.Account);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(ApiRoutes.AuthRegister, new RegisterRequestDto
        {
            FullName = "Demo Kullanici",
            UserName = "demo_user",
            Email = "gecersiz",
            Password = "Password123!"
        });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Gecerli bir e-posta adresi girin", payload);
    }

    [Fact]
    public async Task Register_WithShortPassword_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(ApiRoutes.AuthRegister, new RegisterRequestDto
        {
            FullName = "Demo Kullanici",
            UserName = "demo_user",
            Email = "demo@example.com",
            Password = "1234567"
        });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Sifre en az 8 karakter olmalidir", payload);
    }

    [Fact]
    public async Task StripeWebhook_WithoutValidSignature_ReturnsBadRequest_WithoutAuthentication()
    {
        await using var factory = new ApiWebApplicationFactory(useStripeSettings: true);
        using var client = factory.CreateClient();

        using var content = new StringContent(
            $"{{\"id\":\"evt_test\",\"object\":\"event\",\"api_version\":\"{ExpectedStripeApiVersion}\",\"type\":\"checkout.session.completed\",\"data\":{{\"object\":{{\"id\":\"cs_test\",\"object\":\"checkout.session\"}}}}}}",
            Encoding.UTF8,
            "application/json");
        using var response = await client.PostAsync("/api/payments/webhooks/stripe", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_WithNonAdminBearerToken_ReturnsForbidden()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("batu", "user", isAdmin: false));

        using var response = await client.GetAsync(ApiRoutes.AdminUsers);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminUserStatus_WithMissingStatus_ReturnsValidationProblem_ForAdmin()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("superadmin", "superadmin", isAdmin: true));

        using var response = await client.PostAsJsonAsync(ApiRoutes.AdminUserStatus(Guid.NewGuid()), new AdminUserStatusUpdateRequest());
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Durum zorunludur", payload);
    }

    [Fact]
    public async Task CreatePayment_WithMissingPackageId_ReturnsValidationProblem()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("batu", "user", isAdmin: false));

        using var response = await client.PostAsJsonAsync(ApiRoutes.Payments, new CreatePaymentRequest());
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Paket zorunludur", payload);
    }

    [Fact]
    public async Task CreateListing_AuctionWithoutRequiredFields_ReturnsValidationProblem()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("batu", "user", isAdmin: false));

        using var response = await client.PostAsJsonAsync(ApiRoutes.Listings, new CreateListingRequest
        {
            Title = "Vintage gitar",
            Description = "Temiz durumda",
            CategorySlug = "muzik",
            SaleModeKey = "auction",
            Price = 0
        });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Acik artirma alanlari zorunludur", payload);
    }

    [Fact]
    public async Task UpdateAccountProfile_WithInvalidEmail_ReturnsValidationProblem()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("batu", "user", isAdmin: false));

        using var response = await client.PutAsJsonAsync(ApiRoutes.AccountProfile, new UpdateUserAccountProfileRequest
        {
            FirstName = "Batu",
            LastName = "Yildiz",
            Email = "invalid"
        });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Gecerli bir e-posta adresi girin", payload);
    }

    [Fact]
    public async Task UpdateBillingAddress_CorporateWithoutTaxInfo_ReturnsValidationProblem()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("batu", "user", isAdmin: false));

        using var response = await client.PutAsJsonAsync(ApiRoutes.AccountBillingAddress, new UpsertUserBillingAddressRequest
        {
            InvoiceType = "corporate",
            AddressTitle = "Merkez",
            FullName = "Makparsan",
            Country = "Turkiye",
            City = "Ankara",
            District = "Cankaya",
            PhoneNumber = "05050000000",
            AddressLine = "Test adres"
        });
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Kurumsal fatura icin vergi dairesi ve vergi numarasi zorunludur", payload);
    }

    [Fact]
    public async Task CreateAccountLedgerPayment_WithInvalidAmount_ReturnsValidationProblem()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("batu", "user", isAdmin: false));

        using var response = await client.PostAsJsonAsync(ApiRoutes.AccountLedgerPayments, new CreateAccountLedgerPaymentRequest
        {
            Amount = 0,
            Description = "Cari odeme"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddPriceAlert_WithInvalidTargetPrice_ReturnsValidationProblem()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("batu", "user", isAdmin: false));

        using var response = await client.PostAsJsonAsync(ApiRoutes.AccountPriceAlerts, new AddPriceAlertRequest
        {
            ListingId = Guid.NewGuid(),
            TargetPrice = 0
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateComplaint_WithMissingFields_ReturnsValidationProblem()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.CreateAccessToken("batu", "user", isAdmin: false));

        using var response = await client.PostAsJsonAsync(ApiRoutes.Complaints, new CreateComplaintRequest());
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Tum sikayet alanlari zorunludur", payload);
    }

    private sealed class ApiWebApplicationFactory(bool useStripeSettings = false) : WebApplicationFactory<trampbazaar.Api.Services.MarketplaceRepository>
    {
        public string CreateAccessToken(string userName, string roleName, bool isAdmin)
            => Services.GetRequiredService<ApiTokenService>().CreateToken(userName, roleName, isAdmin);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTest");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["Server:BaseUrl"] = "http://127.0.0.1:0",
                    ["Auth:SigningKey"] = "integration-test-signing-key-that-is-long-enough",
                    ["ConnectionStrings:SqlServer"] = "Server=tcp:127.0.0.1,65000;Database=TrampBazaar;User Id=sa;Password=invalid;Encrypt=False;TrustServerCertificate=True;Connect Timeout=1;",
                    ["Payments:Provider"] = "demo",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5000"
                };

                if (useStripeSettings)
                {
                    settings["Payments:Provider"] = "stripe";
                    settings["Payments:Stripe:SecretKey"] = "sk_test_123";
                    settings["Payments:Stripe:WebhookSecret"] = "whsec_test_123";
                }

                configBuilder.AddInMemoryCollection(settings);
            });
        }
    }
}
