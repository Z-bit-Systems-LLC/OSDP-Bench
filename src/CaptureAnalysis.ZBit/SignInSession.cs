using System.Text;
using System.Text.Json;
using OSDPBench.Core.Services.Analysis;

namespace OSDPBench.CaptureAnalysis.ZBit;

/// <summary>
/// A member's saved sign-in: the tokens, and who they are for showing "Signed in as …".
/// </summary>
internal sealed record SignInSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpires,
    string RefreshToken,
    string? Name,
    string? Email)
{
    /// <summary>Access tokens are refreshed this long before they expire, so one never expires mid-request.</summary>
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(5);

    /// <summary>Assumed lifetime when the token endpoint doesn't give one; a 401 refreshes early if it's wrong.</summary>
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(1);

    public CaptureAnalysisAccount Account => new(Name, Email);

    public bool IsAccessTokenFresh(DateTimeOffset now) => now < AccessTokenExpires - ExpiryMargin;

    /// <summary>
    /// Creates the session from a token response. A refresh may omit the refresh token or ID token, in which case the
    /// ones from <paramref name="previous"/> are kept.
    /// </summary>
    public static SignInSession FromTokens(TokenResponse tokens, DateTimeOffset now, SignInSession? previous = null)
    {
        var refreshToken = tokens.RefreshToken ?? previous?.RefreshToken ??
            throw new OAuthTokenException("The sign-in didn't return a refresh token.");
        var lifetime = tokens.ExpiresIn is > 0 ? TimeSpan.FromSeconds(tokens.ExpiresIn.Value) : DefaultLifetime;
        var (name, email) = tokens.IdToken != null ? ReadIdentity(tokens.IdToken) : (previous?.Name, previous?.Email);
        return new SignInSession(tokens.AccessToken, now + lifetime, refreshToken, name, email);
    }

    public string Serialize() => JsonSerializer.Serialize(this, JsonSerializerOptions.Web);

    public static SignInSession? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            var session = JsonSerializer.Deserialize<SignInSession>(json, JsonSerializerOptions.Web);
            return string.IsNullOrEmpty(session?.RefreshToken) ? null : session;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the member's name and email from an ID token. The token came straight from the token endpoint over TLS,
    /// and is only used for display, so its signature isn't checked.
    /// </summary>
    internal static (string? Name, string? Email) ReadIdentity(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length != 3) return (null, null);

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            var root = document.RootElement;

            var name = GetString(root, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.Join(' ', new[] { GetString(root, "given_name"), GetString(root, "family_name") }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
            }

            return (string.IsNullOrWhiteSpace(name) ? null : name, GetString(root, "email"));
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return (null, null);
        }
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
