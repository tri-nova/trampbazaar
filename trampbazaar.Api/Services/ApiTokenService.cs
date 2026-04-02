using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace trampbazaar.Api.Services;

public sealed class ApiTokenService(IConfiguration configuration)
{
    private const string DefaultSigningKey = "trampbazaar-dev-signing-key-change-me";
    private readonly byte[] signingKey = Encoding.UTF8.GetBytes(configuration["Auth:SigningKey"] ?? DefaultSigningKey);
    private readonly int tokenLifetimeHours = Math.Max(1, configuration.GetValue<int?>("Auth:TokenLifetimeHours") ?? 12);

    public string CreateToken(string userName, string roleName, bool isAdmin)
    {
        var payload = new ApiAuthTokenPayload
        {
            UserName = userName,
            RoleName = roleName,
            IsAdmin = isAdmin,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(tokenLifetimeHours)
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signatureSegment = Base64UrlEncode(ComputeSignature(payloadSegment));
        return $"{payloadSegment}.{signatureSegment}";
    }

    public bool TryValidateToken(string token, out ApiAuthTokenPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var segments = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2)
        {
            return false;
        }

        var expectedSignature = Base64UrlEncode(ComputeSignature(segments[0]));
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expectedSignature), Encoding.UTF8.GetBytes(segments[1])))
        {
            return false;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(segments[0]);
            payload = JsonSerializer.Deserialize<ApiAuthTokenPayload>(payloadBytes);
            return payload is not null
                   && !string.IsNullOrWhiteSpace(payload.UserName)
                   && payload.ExpiresAt > DateTimeOffset.UtcNow;
        }
        catch
        {
            payload = null;
            return false;
        }
    }

    private byte[] ComputeSignature(string payloadSegment)
    {
        using var hmac = new HMACSHA256(signingKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadSegment));
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (base64.Length % 4);
        if (padding is > 0 and < 4)
        {
            base64 = base64.PadRight(base64.Length + padding, '=');
        }

        return Convert.FromBase64String(base64);
    }
}

public sealed class ApiAuthTokenPayload
{
    public string UserName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
