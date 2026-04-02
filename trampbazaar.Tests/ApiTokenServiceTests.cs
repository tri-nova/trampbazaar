using Microsoft.Extensions.Configuration;
using trampbazaar.Api.Services;

namespace trampbazaar.Tests;

public sealed class ApiTokenServiceTests
{
    [Fact]
    public void CreateToken_ValidatesAndPreservesPayload()
    {
        var service = CreateService();

        var token = service.CreateToken("batu", "Admin", isAdmin: true);

        var isValid = service.TryValidateToken(token, out var payload);

        Assert.True(isValid);
        Assert.NotNull(payload);
        Assert.Equal("batu", payload.UserName);
        Assert.Equal("Admin", payload.RoleName);
        Assert.True(payload.IsAdmin);
        Assert.True(payload.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void TryValidateToken_ReturnsFalse_WhenTokenIsTampered()
    {
        var service = CreateService();
        var token = service.CreateToken("batu", "User", isAdmin: false);
        var tamperedToken = token[..^1] + (token[^1] == 'a' ? 'b' : 'a');

        var isValid = service.TryValidateToken(tamperedToken, out var payload);

        Assert.False(isValid);
        Assert.Null(payload);
    }

    [Fact]
    public void TryValidateToken_ReturnsFalse_WhenTokenIsMalformed()
    {
        var service = CreateService();

        var isValid = service.TryValidateToken("not-a-token", out var payload);

        Assert.False(isValid);
        Assert.Null(payload);
    }

    private static ApiTokenService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:SigningKey"] = "unit-test-signing-key-1234567890",
                ["Auth:TokenLifetimeHours"] = "4"
            })
            .Build();

        return new ApiTokenService(configuration);
    }
}
