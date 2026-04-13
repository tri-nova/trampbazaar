using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace trampbazaar.Tests;

public sealed class WebSmokeTests
{
    [Fact]
    public async Task HomePage_WhenApiUnavailable_RendersStatusBanner()
    {
        await using var factory = new WebAppFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("TrampBazaar", html);
        Assert.Contains("Veritabani baglantisi su anda kullanilamiyor", html);
    }

    [Fact]
    public async Task ListingsPage_WhenApiUnavailable_StillReturnsOk()
    {
        await using var factory = new WebAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/Listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class WebAppFactory : WebApplicationFactory<trampbazaar.Web.Services.MarketplaceWebApiClient>
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
