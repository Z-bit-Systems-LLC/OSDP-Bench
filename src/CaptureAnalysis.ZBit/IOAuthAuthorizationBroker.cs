namespace OSDPBench.CaptureAnalysis.ZBit;

/// <summary>
/// The platform's part of an OAuth sign-in: showing the sign-in page in the system browser and handing back the
/// parameters the issuer redirected to.
/// </summary>
/// <remarks>
/// Desktop apps use <see cref="LoopbackAuthorizationBroker"/>, which receives the redirect on 127.0.0.1. Apps that
/// can't listen there, such as phone apps, can use the site's relay page instead
/// (<see cref="ZBitCaptureAnalysisOptions.SiteRedirectUri"/>), which opens the app through <see cref="CallbackScheme"/>.
/// </remarks>
public interface IOAuthAuthorizationBroker
{
    /// <summary>
    /// The URI scheme the site's relay page opens the app with: <c>com.z-bitco.osdpbench://callback?code=…</c>.
    /// </summary>
    const string CallbackScheme = "com.z-bitco.osdpbench";

    /// <summary>
    /// Opens the sign-in page and waits for the result.
    /// </summary>
    /// <param name="buildAuthorizeUri">Builds the sign-in page address for the redirect URI the broker receives on.
    /// The same redirect URI is sent again when the code is exchanged.</param>
    /// <param name="cancellationToken">A token to stop waiting.</param>
    /// <returns>The parameters the issuer redirected with.</returns>
    Task<OAuthCallback> AuthorizeAsync(Func<Uri, Uri> buildAuthorizeUri, CancellationToken cancellationToken);
}

/// <summary>
/// The result of an OAuth sign-in as passed back in the redirect.
/// </summary>
/// <param name="Code">The authorization code, when the sign-in succeeded.</param>
/// <param name="State">The state sent with the request.</param>
/// <param name="Error">The OAuth error code, when the sign-in failed.</param>
/// <param name="ErrorDescription">A description of the error, if one was given.</param>
public sealed record OAuthCallback(string? Code, string? State, string? Error, string? ErrorDescription)
{
    /// <summary>
    /// Gets a value indicating whether the redirect carried a result, rather than being some other request.
    /// </summary>
    public bool HasResult => Code != null || Error != null;

    /// <summary>
    /// Reads a callback from the redirect URI's query.
    /// </summary>
    /// <param name="callbackUri">The URI, e.g. <c>http://127.0.0.1:50123/callback?code=…&amp;state=…</c>.</param>
    /// <returns>The callback.</returns>
    public static OAuthCallback FromUri(Uri callbackUri)
    {
        ArgumentNullException.ThrowIfNull(callbackUri);

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in callbackUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            parameters[Uri.UnescapeDataString(parts[0])] =
                parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
        }

        return new OAuthCallback(
            parameters.GetValueOrDefault("code"),
            parameters.GetValueOrDefault("state"),
            parameters.GetValueOrDefault("error"),
            parameters.GetValueOrDefault("error_description"));
    }
}
