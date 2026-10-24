using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OSDPBench.Core.Services.Analysis;
using CoreResources = OSDPBench.Core.Resources.Resources;

namespace OSDPBench.CaptureAnalysis.ZBit;

/// <summary>
/// Analyzes captures with the capture analyzer on the Z-bit web site (<c>/api/v1/capture-analysis</c>), using the
/// member's z-bitco.com account. Members sign in through the system browser (OAuth authorization code with PKCE);
/// the refresh token is kept in an <see cref="ISecureTokenStore"/> so they stay signed in.
/// </summary>
public sealed class ZBitCaptureAnalysisService : ICaptureAnalysisService, IDisposable
{
    private const string ApiPath = "/api/v1/capture-analysis";

    private readonly HttpClient _httpClient;
    private readonly ZBitCaptureAnalysisOptions _options;
    private readonly IOAuthAuthorizationBroker _authorizationBroker;
    private readonly ISecureTokenStore _tokenStore;
    private readonly TimeProvider _timeProvider;
    private readonly OAuthClient _oauthClient;
    private readonly string _tokenKey;

    // Refresh tokens work once, so refreshing, signing in and signing out are serialized.
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private SignInSession? _session;
    private bool _sessionLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZBitCaptureAnalysisService"/> class.
    /// </summary>
    /// <param name="httpClient">The client to call the web site and the OAuth issuer with.</param>
    /// <param name="options">Where the analyzer and its sign-in live.</param>
    /// <param name="authorizationBroker">The platform's way of showing the sign-in page and receiving the result.</param>
    /// <param name="tokenStore">Where to keep the member's sign-in.</param>
    /// <param name="timeProvider">The clock; defaults to the system clock.</param>
    public ZBitCaptureAnalysisService(HttpClient httpClient, ZBitCaptureAnalysisOptions options,
        IOAuthAuthorizationBroker authorizationBroker, ISecureTokenStore tokenStore, TimeProvider? timeProvider = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _authorizationBroker = authorizationBroker ?? throw new ArgumentNullException(nameof(authorizationBroker));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _oauthClient = new OAuthClient(httpClient, options);

        // One sign-in per OAuth client, so test and production sign-ins are kept apart.
        _tokenKey = "zbit-capture-analysis-" + new string(options.ClientId.Where(char.IsAsciiLetterOrDigit).ToArray());
    }

    /// <inheritdoc />
    public string DisplayName => _options.DisplayName;

    /// <inheritdoc />
    public bool RequiresSignIn => true;

    /// <inheritdoc />
    public bool SupportsFollowUpQuestions => true;

    /// <inheritdoc />
    public async Task<CaptureAnalysisAccount?> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return (await LoadSessionAsync(cancellationToken).ConfigureAwait(false))?.Account;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CaptureAnalysisAccount> SignInAsync(CancellationToken cancellationToken = default)
    {
        var request = PkceRequest.Create();
        Uri? redirectUri = null;
        var callback = await _authorizationBroker.AuthorizeAsync(redirect =>
        {
            redirectUri = redirect;
            return _oauthClient.BuildAuthorizeUri(request, redirect);
        }, cancellationToken).ConfigureAwait(false);

        if (callback.Error != null)
        {
            throw SignInFailed(callback.ErrorDescription ?? callback.Error);
        }

        if (redirectUri == null || string.IsNullOrEmpty(callback.Code) || callback.State != request.State)
        {
            throw new CaptureAnalysisException(CaptureAnalysisErrorKind.SignInFailed,
                CoreResources.GetString("CaptureAnalysis_Error_SignInMismatch"));
        }

        await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tokens = await CallIssuerAsync(
                () => _oauthClient.ExchangeCodeAsync(callback.Code, request.Verifier, redirectUri, cancellationToken),
                exception => SignInFailed(exception.Message)).ConfigureAwait(false);
            var session = SignInSession.FromTokens(tokens, _timeProvider.GetUtcNow());
            await SaveSessionAsync(session, cancellationToken).ConfigureAwait(false);
            return session.Account;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ForgetSessionAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CaptureAnalysisQuota?> GetQuotaAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, ApiUri("/quota")),
            cancellationToken).ConfigureAwait(false);
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return ReadQuota(document.RootElement) ?? throw UnexpectedReply();
        }
        catch (JsonException exception)
        {
            throw UnexpectedReply(exception);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CaptureAnalysisEvent> AnalyzeAsync(CaptureAnalysisRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await SendAsync(() =>
        {
            var capture = new ReadOnlyMemoryContent(request.Capture);
            capture.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var form = new MultipartFormDataContent { { capture, "capture", request.FileName } };
            if (!string.IsNullOrWhiteSpace(request.Question))
            {
                form.Add(new StringContent(request.Question), "question");
            }

            if (request.SecureChannelKey != null)
            {
                form.Add(new StringContent(Convert.ToHexString(request.SecureChannelKey)), "scbk");
            }

            if (request.IncludeCredentials)
            {
                form.Add(new StringContent("true"), "includeCredentials");
            }

            return new HttpRequestMessage(HttpMethod.Post, ApiUri(string.Empty)) { Content = form };
        }, cancellationToken).ConfigureAwait(false);

        await foreach (var analysisEvent in ReadEventsAsync(response, cancellationToken).ConfigureAwait(false))
        {
            yield return analysisEvent;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<CaptureAnalysisEvent> AskAsync(string analysisId, string question,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(analysisId);

        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post,
                ApiUri($"/{Uri.EscapeDataString(analysisId)}/questions"))
            {
                Content = JsonContent.Create(new { question })
            }, cancellationToken).ConfigureAwait(false);

        await foreach (var analysisEvent in ReadEventsAsync(response, cancellationToken).ConfigureAwait(false))
        {
            yield return analysisEvent;
        }
    }

    /// <summary>
    /// Sends an API request with the member's access token. A 401 refreshes the token and retries once; a second
    /// 401 means the sign-in is no longer valid. Any other failure is turned into a
    /// <see cref="CaptureAnalysisException"/>.
    /// </summary>
    /// <param name="createRequest">Creates the request; called again for the retry, since content can only be sent
    /// once.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(null, cancellationToken).ConfigureAwait(false);
        var response = await SendWithTokenAsync(createRequest, accessToken, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            accessToken = await GetAccessTokenAsync(accessToken, cancellationToken).ConfigureAwait(false);
            response = await SendWithTokenAsync(createRequest, accessToken, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                await SignOutAsync(cancellationToken).ConfigureAwait(false);
                throw SignInExpired();
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                throw await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            }
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(Func<HttpRequestMessage> createRequest,
        string accessToken, CancellationToken cancellationToken)
    {
        using var request = createRequest();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        try
        {
            // The analysis streams, so return as soon as the headers arrive.
            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (IsConnectionFailure(exception, cancellationToken))
        {
            throw Unreachable(exception);
        }
    }

    /// <summary>
    /// Gets an access token, refreshing it when it is about to expire or when <paramref name="rejectedToken"/> (the
    /// one the API just refused) is still the current one.
    /// </summary>
    private async Task<string> GetAccessTokenAsync(string? rejectedToken, CancellationToken cancellationToken)
    {
        await _sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var session = await LoadSessionAsync(cancellationToken).ConfigureAwait(false) ?? throw SignInExpired();
            var now = _timeProvider.GetUtcNow();
            if (session.AccessToken != rejectedToken && session.IsAccessTokenFresh(now))
            {
                return session.AccessToken;
            }

            TokenResponse tokens;
            try
            {
                tokens = await CallIssuerAsync(() => _oauthClient.RefreshAsync(session.RefreshToken, cancellationToken),
                    _ => SignInExpired()).ConfigureAwait(false);
            }
            catch (CaptureAnalysisException exception) when (exception.Kind == CaptureAnalysisErrorKind.NotSignedIn)
            {
                await ForgetSessionAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }

            // Save the new refresh token before using the access token: the old refresh token no longer works.
            var refreshed = SignInSession.FromTokens(tokens, now, session);
            await SaveSessionAsync(refreshed, cancellationToken).ConfigureAwait(false);
            return refreshed.AccessToken;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    /// <summary>
    /// Calls the OAuth issuer, turning a refused code or token into <paramref name="refused"/>'s exception and a
    /// connection failure into <see cref="CaptureAnalysisErrorKind.Unavailable"/>.
    /// </summary>
    private async Task<TokenResponse> CallIssuerAsync(Func<Task<TokenResponse>> call,
        Func<OAuthTokenException, CaptureAnalysisException> refused)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (OAuthTokenException exception)
        {
            throw refused(exception);
        }
        catch (JsonException exception)
        {
            throw UnexpectedReply(exception);
        }
        catch (Exception exception) when (IsConnectionFailure(exception, CancellationToken.None))
        {
            throw Unreachable(exception);
        }
    }

    private async Task<SignInSession?> LoadSessionAsync(CancellationToken cancellationToken)
    {
        if (!_sessionLoaded)
        {
            _session = SignInSession.Deserialize(
                await _tokenStore.ReadAsync(_tokenKey, cancellationToken).ConfigureAwait(false));
            _sessionLoaded = true;
        }

        return _session;
    }

    private async Task SaveSessionAsync(SignInSession session, CancellationToken cancellationToken)
    {
        _session = session;
        _sessionLoaded = true;

        // Not cancellable: a rotated refresh token that isn't saved is a lost sign-in.
        await _tokenStore.WriteAsync(_tokenKey, session.Serialize(), CancellationToken.None).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ForgetSessionAsync(CancellationToken cancellationToken)
    {
        _session = null;
        _sessionLoaded = true;
        await _tokenStore.DeleteAsync(_tokenKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the NDJSON stream: a "start" line, "text" lines, then "done" or "error". Unknown line types are skipped,
    /// since the site may add new ones.
    /// </summary>
    private async IAsyncEnumerable<CaptureAnalysisEvent> ReadEventsAsync(HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false));
        while (await ReadLineAsync(reader, cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var analysisEvent = ParseEvent(line);
            if (analysisEvent == null) continue;

            yield return analysisEvent;
            if (analysisEvent is CaptureAnalysisEvent.Completed) yield break;
        }

        throw new CaptureAnalysisException(CaptureAnalysisErrorKind.Incomplete,
            CoreResources.GetString("CaptureAnalysis_Error_Incomplete"));
    }

    private async Task<string?> ReadLineAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            return await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsConnectionFailure(exception, cancellationToken))
        {
            throw Unreachable(exception);
        }
    }

    /// <summary>
    /// Parses one NDJSON line into an event; an "error" line is thrown.
    /// </summary>
    /// <returns>The event, or <see langword="null"/> for a line type this version doesn't know.</returns>
    internal static CaptureAnalysisEvent? ParseEvent(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            switch (GetString(root, "type"))
            {
                case "start":
                    return new CaptureAnalysisEvent.Started(GetString(root, "id"), ReadSummary(root));
                case "text":
                    return new CaptureAnalysisEvent.TextReceived(GetString(root, "text") ?? string.Empty);
                case "done":
                    return new CaptureAnalysisEvent.Completed(GetBoolean(root, "truncated"), ReadQuota(root));
                case "error":
                    throw ToException(root, GetString(root, "message"));
                default:
                    return null;
            }
        }
        catch (JsonException exception)
        {
            throw UnexpectedReply(exception);
        }
    }

    private static CaptureAnalysisSummary? ReadSummary(JsonElement root)
    {
        if (!root.TryGetProperty("records", out var records) || !records.TryGetInt32(out var recordCount))
        {
            return null; // A follow-up's start line only has the id.
        }

        var checks = new List<CaptureAnalysisCheck>();
        if (root.TryGetProperty("checks", out var checkArray) && checkArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var check in checkArray.EnumerateArray())
            {
                if (GetString(check, "name") is { } name &&
                    check.TryGetProperty("count", out var count) && count.TryGetInt32(out var countValue))
                {
                    checks.Add(new CaptureAnalysisCheck(name, countValue));
                }
            }
        }

        return new CaptureAnalysisSummary(GetString(root, "file"), recordCount, GetBoolean(root, "truncated"), checks);
    }

    private static async Task<CaptureAnalysisException> ReadErrorAsync(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(body);
            return ToException(document.RootElement, GetString(document.RootElement, "error"));
        }
        catch (JsonException)
        {
            // Not the API's error format, e.g. a proxy's error page.
            return (int)response.StatusCode switch
            {
                (int)HttpStatusCode.RequestEntityTooLarge => new CaptureAnalysisException(
                    CaptureAnalysisErrorKind.CaptureTooLarge, CoreResources.GetString("CaptureAnalysis_Error_TooLarge")),
                >= 500 => new CaptureAnalysisException(CaptureAnalysisErrorKind.Unavailable,
                    CoreResources.GetString("CaptureAnalysis_Error_Unavailable")),
                _ => UnexpectedReply()
            };
        }
    }

    /// <summary>
    /// Turns the API's error (a JSON body before the stream, or an "error" line in it) into an exception.
    /// </summary>
    private static CaptureAnalysisException ToException(JsonElement root, string? message)
    {
        var kind = GetString(root, "code") switch
        {
            "unavailable" => CaptureAnalysisErrorKind.Unavailable,
            "service_busy" => CaptureAnalysisErrorKind.ServiceBusy,
            "invalid_request" => CaptureAnalysisErrorKind.InvalidRequest,
            "missing_capture" or "invalid_capture" => CaptureAnalysisErrorKind.InvalidCapture,
            "invalid_key" => CaptureAnalysisErrorKind.InvalidSecurityKey,
            "invalid_question" => CaptureAnalysisErrorKind.InvalidQuestion,
            "follow_up_limit" => CaptureAnalysisErrorKind.FollowUpLimitReached,
            "capture_too_large" => CaptureAnalysisErrorKind.CaptureTooLarge,
            "analysis_expired" => CaptureAnalysisErrorKind.AnalysisExpired,
            "answer_in_progress" => CaptureAnalysisErrorKind.AnswerInProgress,
            "quota_exceeded" => CaptureAnalysisErrorKind.QuotaExceeded,
            "refused" => CaptureAnalysisErrorKind.Refused,
            "incomplete" => CaptureAnalysisErrorKind.Incomplete,
            _ => CaptureAnalysisErrorKind.Failed // Includes "failed" and codes added after this version.
        };

        return new CaptureAnalysisException(kind,
            string.IsNullOrWhiteSpace(message) ? CoreResources.GetString("CaptureAnalysis_Error_UnexpectedReply") : message,
            ReadQuota(root));
    }

    private static CaptureAnalysisQuota? ReadQuota(JsonElement root) =>
        root.TryGetProperty("remaining", out var remaining) && remaining.TryGetInt32(out var remainingValue) &&
        root.TryGetProperty("limit", out var limit) && limit.TryGetInt32(out var limitValue)
            ? new CaptureAnalysisQuota(remainingValue, limitValue)
            : null;

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool GetBoolean(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>
    /// A network failure or timeout, as opposed to the caller cancelling.
    /// </summary>
    private static bool IsConnectionFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or IOException ||
        (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private Uri ApiUri(string path) => new(_options.SiteUri, ApiPath + path);

    private CaptureAnalysisException Unreachable(Exception exception) =>
        new(CaptureAnalysisErrorKind.Unavailable,
            CoreResources.GetString("CaptureAnalysis_Error_Unreachable").Replace("{0}", _options.SiteUri.Host),
            innerException: exception);

    private static CaptureAnalysisException SignInExpired() =>
        new(CaptureAnalysisErrorKind.NotSignedIn, CoreResources.GetString("CaptureAnalysis_Error_SignInExpired"));

    private static CaptureAnalysisException SignInFailed(string reason) =>
        new(CaptureAnalysisErrorKind.SignInFailed,
            CoreResources.GetString("CaptureAnalysis_Error_SignInFailed").Replace("{0}", reason));

    private static CaptureAnalysisException UnexpectedReply(Exception? exception = null) =>
        new(CaptureAnalysisErrorKind.Failed, CoreResources.GetString("CaptureAnalysis_Error_UnexpectedReply"),
            innerException: exception);

    /// <inheritdoc />
    public void Dispose()
    {
        _sessionLock.Dispose();
    }
}
