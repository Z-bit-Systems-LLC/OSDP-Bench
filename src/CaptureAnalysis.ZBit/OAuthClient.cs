using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OSDPBench.CaptureAnalysis.ZBit;

/// <summary>
/// The parameters of one sign-in attempt: a PKCE verifier and challenge, and the state to match the callback with.
/// </summary>
internal sealed record PkceRequest(string Verifier, string Challenge, string State)
{
    public static PkceRequest Create()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return new PkceRequest(verifier, challenge, Base64Url(RandomNumberGenerator.GetBytes(16)));
    }

    internal static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>
/// A token endpoint response.
/// </summary>
internal sealed record TokenResponse(string AccessToken, string? RefreshToken, int? ExpiresIn, string? IdToken);

/// <summary>
/// The token endpoint refused a code or refresh token, so the user has to sign in again.
/// </summary>
internal sealed class OAuthTokenException(string message) : Exception(message);

/// <summary>
/// The OAuth calls a public client makes: authorization code with PKCE, then refresh tokens.
/// </summary>
internal sealed class OAuthClient(HttpClient httpClient, ZBitCaptureAnalysisOptions options)
{
    private Uri TokenEndpoint => new(options.Authority, "/oauth/token");

    public Uri BuildAuthorizeUri(PkceRequest request)
    {
        var query = string.Join('&',
            "response_type=code",
            $"client_id={Uri.EscapeDataString(options.ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(options.RedirectUri.AbsoluteUri)}",
            $"scope={Uri.EscapeDataString(options.Scopes)}",
            $"state={request.State}",
            $"code_challenge={request.Challenge}",
            "code_challenge_method=S256");
        return new Uri(options.Authority, $"/oauth/authorize?{query}");
    }

    public Task<TokenResponse> ExchangeCodeAsync(string code, string verifier, CancellationToken cancellationToken) =>
        PostAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = options.RedirectUri.AbsoluteUri,
            ["client_id"] = options.ClientId,
            ["code_verifier"] = verifier
        }, cancellationToken);

    public Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        PostAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = options.ClientId
        }, cancellationToken);

    private async Task<TokenResponse> PostAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form), cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // 400 and 401 mean the code or refresh token was refused (expired, already used or revoked); anything else
        // is the service failing, which HttpRequestException reports.
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            throw new OAuthTokenException(ReadError(body));
        }

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        return new TokenResponse(
            root.GetProperty("access_token").GetString() ?? string.Empty,
            GetString(root, "refresh_token"),
            root.TryGetProperty("expires_in", out var expiresIn) && expiresIn.TryGetInt32(out var seconds) ? seconds : null,
            GetString(root, "id_token"));
    }

    private static string ReadError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            return GetString(root, "error_description") ?? GetString(root, "error") ?? body;
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
