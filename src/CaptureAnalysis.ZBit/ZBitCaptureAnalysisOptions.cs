namespace OSDPBench.CaptureAnalysis.ZBit;

/// <summary>
/// Where the Z-bit capture analyzer and its sign-in live. The values differ between the test and production web
/// sites, so the app supplies them at build time rather than from source.
/// </summary>
public sealed class ZBitCaptureAnalysisOptions
{
    /// <summary>
    /// Gets the web site that hosts the analyzer API, e.g. https://z-bitco.com.
    /// </summary>
    public required Uri SiteUri { get; init; }

    /// <summary>
    /// Gets the OAuth issuer (the Clerk instance) members sign in with.
    /// </summary>
    public required Uri Authority { get; init; }

    /// <summary>
    /// Gets the OAuth client identifier of the app. It is a public client, so there is no secret.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the OAuth scopes to request.
    /// </summary>
    public string Scopes { get; init; } = "openid profile email offline_access";

    /// <summary>
    /// Gets the name of the analyzer to show to the user.
    /// </summary>
    public string DisplayName { get; init; } = "Z-bit Capture Analyzer";

    /// <summary>
    /// Gets the web site's sign-in relay page, which hands the result to the app through
    /// <see cref="IOAuthAuthorizationBroker.CallbackScheme"/>. It is the redirect URI for apps that can't receive the
    /// redirect on 127.0.0.1 (<see cref="LoopbackAuthorizationBroker"/>), such as phone apps.
    /// </summary>
    public Uri SiteRedirectUri => new(SiteUri, "/callback");

    /// <summary>
    /// Creates options from configuration values, or returns <see langword="null"/> if any value is missing or isn't
    /// an absolute URI, so that a build without them simply has no capture analysis.
    /// </summary>
    /// <param name="siteUri">The web site address.</param>
    /// <param name="authority">The OAuth issuer address.</param>
    /// <param name="clientId">The OAuth client identifier.</param>
    /// <returns>The options, or <see langword="null"/>.</returns>
    public static ZBitCaptureAnalysisOptions? TryCreate(string? siteUri, string? authority, string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId) ||
            !Uri.TryCreate(siteUri, UriKind.Absolute, out var site) ||
            !Uri.TryCreate(authority, UriKind.Absolute, out var issuer))
        {
            return null;
        }

        return new ZBitCaptureAnalysisOptions { SiteUri = site, Authority = issuer, ClientId = clientId.Trim() };
    }
}
