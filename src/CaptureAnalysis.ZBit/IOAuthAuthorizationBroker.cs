namespace OSDPBench.CaptureAnalysis.ZBit;

/// <summary>
/// The platform's part of an OAuth sign-in: showing the sign-in page in the system browser and handing back what the
/// redirect page passed to the app.
/// </summary>
/// <remarks>
/// The site's redirect page opens <c>com.z-bitco.osdpbench://callback?code=…&amp;state=…</c> (or <c>error=…</c>), so
/// an implementation registers that URI scheme and completes when the app is opened with it. Where that isn't
/// possible, the page also shows the code for the user to paste in; return it with no state.
/// </remarks>
public interface IOAuthAuthorizationBroker
{
    /// <summary>
    /// The URI scheme the site's redirect page opens the app with.
    /// </summary>
    const string CallbackScheme = "com.z-bitco.osdpbench";

    /// <summary>
    /// Opens the sign-in page and waits for the result.
    /// </summary>
    /// <param name="authorizeUri">The sign-in page to open.</param>
    /// <param name="cancellationToken">A token to stop waiting.</param>
    /// <returns>The parameters the redirect page passed on.</returns>
    Task<OAuthCallback> AuthorizeAsync(Uri authorizeUri, CancellationToken cancellationToken);
}

/// <summary>
/// The result of an OAuth sign-in as passed back by the redirect page.
/// </summary>
/// <param name="Code">The authorization code, when the sign-in succeeded.</param>
/// <param name="State">The state sent with the request, or <see langword="null"/> when the user pasted the code.</param>
/// <param name="Error">The OAuth error code, when the sign-in failed.</param>
/// <param name="ErrorDescription">A description of the error, if one was given.</param>
public sealed record OAuthCallback(string? Code, string? State, string? Error, string? ErrorDescription)
{
    /// <summary>
    /// Creates a callback from a code the user pasted in.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The callback.</returns>
    public static OAuthCallback FromPastedCode(string code) => new(code.Trim(), null, null, null);

    /// <summary>
    /// Reads a callback from the URI the redirect page opened the app with.
    /// </summary>
    /// <param name="callbackUri">The URI, e.g. <c>com.z-bitco.osdpbench://callback?code=…&amp;state=…</c>.</param>
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

    /// <summary>
    /// Determines whether a URI is a sign-in callback for this app.
    /// </summary>
    /// <param name="value">A command-line argument or URI.</param>
    /// <returns>True when <paramref name="value"/> is a callback URI.</returns>
    public static bool IsCallbackUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, IOAuthAuthorizationBroker.CallbackScheme, StringComparison.OrdinalIgnoreCase);
}
