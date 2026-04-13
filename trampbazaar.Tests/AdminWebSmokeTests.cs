using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace trampbazaar.Tests;

public sealed class AdminWebSmokeTests
{
    [Fact]
    public async Task HomePage_WithoutSession_RedirectsToLogin()
    {
        await using var factory = new AdminAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task LoginPage_RendersSuccessfully()
    {
        await using var factory = new AdminAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/Login");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Yonetim paneli girisi", html);
    }

    private sealed class AdminAppFactory : WebApplicationFactory<trampbazaar.AdminWeb.Services.AdminApiClient>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTest");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Api:BaseUrl"] = "http://127.0.0.1:65001/"
                });
            });
        }
    }
}
