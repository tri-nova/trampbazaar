using Microsoft.AspNetCore.Http;
using trampbazaar.Api.Services;

namespace trampbazaar.Tests;

public sealed class ApiAuthorizationPolicyTests
{
    [Theory]
    [InlineData("/api/dashboard", "GET", false)]
    [InlineData("/api/categories", "GET", false)]
    [InlineData("/api/packages", "GET", false)]
    [InlineData("/api/listings", "GET", false)]
    [InlineData("/api/listings/123", "GET", false)]
    [InlineData("/api/account", "GET", true)]
    [InlineData("/api/payments", "POST", true)]
    [InlineData("/api/notifications", "GET", true)]
    [InlineData("/api/listings", "POST", true)]
    [InlineData("/api/admin/overview", "GET", true)]
    public void RequiresAuthentication_ReturnsExpectedValue(string path, string method, bool expected)
    {
        var result = ApiAuthorizationPolicy.RequiresAuthentication(new PathString(path), method);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("/api/admin/overview", true)]
    [InlineData("/api/admin/users", true)]
    [InlineData("/api/admin/auth/login", false)]
    [InlineData("/api/account", false)]
    public void IsAdminRoute_ReturnsExpectedValue(string path, bool expected)
    {
        var result = ApiAuthorizationPolicy.IsAdminRoute(new PathString(path));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryGetBearerToken_ExtractsToken()
    {
        var success = ApiAuthorizationPolicy.TryGetBearerToken("Bearer abc.def", out var token);

        Assert.True(success);
        Assert.Equal("abc.def", token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Basic abc")]
    [InlineData("Bearer   ")]
    public void TryGetBearerToken_RejectsInvalidHeaders(string? header)
    {
        var success = ApiAuthorizationPolicy.TryGetBearerToken(header, out var token);

        Assert.False(success);
        Assert.Equal(string.Empty, token);
    }
}
