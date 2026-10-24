using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDPBench.CaptureAnalysis.ZBit;
using OSDPBench.Core.Services.Analysis;

namespace OSDPBench.Core.Tests.CaptureAnalysis;

[TestFixture(TestOf = typeof(ZBitCaptureAnalysisService))]
public class ZBitCaptureAnalysisServiceTests
{
    private static readonly ZBitCaptureAnalysisOptions Options = new()
    {
        SiteUri = new Uri("https://site.test"),
        Authority = new Uri("https://issuer.test"),
        ClientId = "client-1"
    };

    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private ScriptedHandler _handler;
    private MemoryTokenStore _tokenStore;
    private FakeBroker _broker;
    private ZBitCaptureAnalysisService _service;

    [SetUp]
    public void Setup()
    {
        _handler = new ScriptedHandler();
        _tokenStore = new MemoryTokenStore();
        _broker = new FakeBroker();
        _service = new ZBitCaptureAnalysisService(new HttpClient(_handler), Options, _broker, _tokenStore,
            new FixedTimeProvider(Now));
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public async Task GetAccount_WithNothingSaved_ReturnsNull()
    {
        Assert.That(await _service.GetAccountAsync(), Is.Null);
    }

    [Test]
    public async Task SignIn_ExchangesCodeWithVerifierAndSavesSession()
    {
        _broker.Respond = authorizeUri => new OAuthCallback("the-code", Query(authorizeUri)["state"], null, null);
        _handler.Respond("POST", "https://issuer.test/oauth/token",
            Json(TokenJson("access-1", "refresh-1", IdToken(new { name = "Pat Lee", email = "pat@example.com" }))));

        var account = await _service.SignInAsync();

        Assert.That(account, Is.EqualTo(new CaptureAnalysisAccount("Pat Lee", "pat@example.com")));
        var authorize = Query(_broker.AuthorizeUri);
        Assert.Multiple(() =>
        {
            Assert.That(_broker.AuthorizeUri.GetLeftPart(UriPartial.Path), Is.EqualTo("https://issuer.test/oauth/authorize"));
            Assert.That(authorize["client_id"], Is.EqualTo("client-1"));
            Assert.That(authorize["redirect_uri"], Is.EqualTo("https://site.test/callback"));
            Assert.That(authorize["code_challenge_method"], Is.EqualTo("S256"));
            Assert.That(authorize["scope"], Does.Contain("offline_access"));
        });

        var form = FormOf(_handler.Requests.Single());
        Assert.That(form["grant_type"], Is.EqualTo("authorization_code"));
        Assert.That(form["code"], Is.EqualTo("the-code"));
        Assert.That(form["redirect_uri"], Is.EqualTo("https://site.test/callback"));
        Assert.That(form["code_verifier"], Has.Length.GreaterThanOrEqualTo(43));
        Assert.That(_tokenStore.Values.Single().Value, Does.Contain("refresh-1"));
    }

    [Test]
    public async Task SignIn_WithPastedCode_IsAccepted()
    {
        _broker.Respond = _ => OAuthCallback.FromPastedCode("  pasted  ");
        _handler.Respond("POST", "https://issuer.test/oauth/token", Json(TokenJson("access-1", "refresh-1")));

        await _service.SignInAsync();

        Assert.That(FormOf(_handler.Requests.Single())["code"], Is.EqualTo("pasted"));
    }

    [Test]
    public async Task SignIn_WithWrongState_Fails()
    {
        _broker.Respond = _ => new OAuthCallback("the-code", "someone-elses-state", null, null);

        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(() => _service.SignInAsync());

        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.SignInFailed));
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task SignIn_WhenRefused_ReportsReason()
    {
        _broker.Respond = _ => new OAuthCallback(null, null, "access_denied", "The member said no");

        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(() => _service.SignInAsync());

        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.SignInFailed));
        Assert.That(exception.Message, Does.Contain("The member said no"));
    }

    [Test]
    public async Task GetQuota_SendsAccessToken()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Respond("GET", "https://site.test/api/v1/capture-analysis/quota", Json("""{"remaining":4,"limit":5}"""));

        var quota = await _service.GetQuotaAsync();

        Assert.That(quota, Is.EqualTo(new CaptureAnalysisQuota(4, 5)));
        Assert.That(_handler.Requests.Single().Authorization, Is.EqualTo("Bearer access-1"));
    }

    [Test]
    public async Task ExpiredAccessToken_IsRefreshedAndRotatedRefreshTokenSaved()
    {
        await SaveSessionAsync(accessToken: "old-access", expires: Now.AddMinutes(1), refreshToken: "refresh-1");
        _handler.Respond("POST", "https://issuer.test/oauth/token", Json(TokenJson("new-access", "refresh-2")));
        _handler.Respond("GET", "https://site.test/api/v1/capture-analysis/quota", Json("""{"remaining":4,"limit":5}"""));

        await _service.GetQuotaAsync();

        Assert.That(FormOf(_handler.Requests[0])["refresh_token"], Is.EqualTo("refresh-1"));
        Assert.That(_handler.Requests[1].Authorization, Is.EqualTo("Bearer new-access"));
        var saved = _tokenStore.Values.Single().Value;
        Assert.That(saved, Does.Contain("refresh-2").And.Not.Contain("refresh-1"));
        Assert.That(saved, Does.Contain("pat@example.com"), "A refresh without an ID token keeps the saved identity.");
    }

    [Test]
    public async Task Unauthorized_RefreshesAndRetriesOnce()
    {
        await SaveSessionAsync(accessToken: "revoked-access", expires: Now.AddHours(1));
        _handler.Respond("GET", "https://site.test/api/v1/capture-analysis/quota", new HttpResponseMessage(HttpStatusCode.Unauthorized));
        _handler.Respond("POST", "https://issuer.test/oauth/token", Json(TokenJson("new-access", "refresh-2")));
        _handler.Respond("GET", "https://site.test/api/v1/capture-analysis/quota", Json("""{"remaining":4,"limit":5}"""));

        var quota = await _service.GetQuotaAsync();

        Assert.That(quota, Is.EqualTo(new CaptureAnalysisQuota(4, 5)));
        Assert.That(_handler.Requests.Last().Authorization, Is.EqualTo("Bearer new-access"));
    }

    [Test]
    public async Task UnauthorizedTwice_SignsOut()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Respond("GET", "https://site.test/api/v1/capture-analysis/quota", new HttpResponseMessage(HttpStatusCode.Unauthorized));
        _handler.Respond("POST", "https://issuer.test/oauth/token", Json(TokenJson("new-access", "refresh-2")));
        _handler.Respond("GET", "https://site.test/api/v1/capture-analysis/quota", new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(() => _service.GetQuotaAsync());

        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.NotSignedIn));
        Assert.That(_tokenStore.Values, Is.Empty);
        Assert.That(await _service.GetAccountAsync(), Is.Null);
    }

    [Test]
    public async Task RefusedRefreshToken_SignsOut()
    {
        await SaveSessionAsync(accessToken: "old-access", expires: Now.AddMinutes(-1));
        _handler.Respond("POST", "https://issuer.test/oauth/token",
            new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("""{"error":"invalid_grant"}""") });

        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(() => _service.GetQuotaAsync());

        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.NotSignedIn));
        Assert.That(_tokenStore.Values, Is.Empty);
    }

    [Test]
    public async Task NotSignedIn_Throws()
    {
        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(() => _service.GetQuotaAsync());

        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.NotSignedIn));
    }

    [Test]
    public async Task Analyze_UploadsCaptureAndStreamsEvents()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Respond("POST", "https://site.test/api/v1/capture-analysis", Ndjson(
            """{"type":"start","id":"abc","file":"t.osdpcap","records":12,"truncated":false,"checks":[{"name":"NAK replies","count":2}]}""",
            """{"type":"text","text":"# Report"}""",
            """{"type":"future_type","value":1}""",
            """{"type":"text","text":"\nDone."}""",
            """{"type":"done","truncated":true,"remaining":3,"limit":5}"""));

        var events = await CollectAsync(_service.AnalyzeAsync(new CaptureAnalysisRequest("t.osdpcap", new byte[] { 1, 2, 3 })
        {
            Question = "Why?",
            SecureChannelKey = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(),
            IncludeCredentials = true
        }));

        Assert.That(events, Has.Count.EqualTo(4));
        var started = (CaptureAnalysisEvent.Started)events[0];
        Assert.That(started.AnalysisId, Is.EqualTo("abc"));
        Assert.That(started.Summary!.Records, Is.EqualTo(12));
        Assert.That(started.Summary.Checks.Single(), Is.EqualTo(new CaptureAnalysisCheck("NAK replies", 2)));
        Assert.That(string.Concat(events.OfType<CaptureAnalysisEvent.TextReceived>().Select(t => t.Text)), Is.EqualTo("# Report\nDone."));
        Assert.That(events[3], Is.EqualTo(new CaptureAnalysisEvent.Completed(true, new CaptureAnalysisQuota(3, 5))));

        var body = _handler.Requests.Single().Body;
        Assert.That(body, Does.Contain("name=capture; filename=t.osdpcap"));
        Assert.That(body, Does.Contain("000102030405060708090A0B0C0D0E0F"));
        Assert.That(body, Does.Contain("name=includeCredentials"));
        Assert.That(body, Does.Contain("Why?"));
    }

    [Test]
    public async Task Analyze_WithoutOptions_SendsOnlyCapture()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Respond("POST", "https://site.test/api/v1/capture-analysis", Ndjson(
            """{"type":"start","id":"abc","records":1,"checks":[]}""", """{"type":"done","truncated":false,"remaining":3,"limit":5}"""));

        await CollectAsync(_service.AnalyzeAsync(new CaptureAnalysisRequest("t.osdpcap", new byte[] { 1 })));

        var body = _handler.Requests.Single().Body;
        Assert.That(body, Does.Not.Contain("name=scbk").And.Not.Contain("name=question").And.Not.Contain("name=includeCredentials"));
    }

    [Test]
    public async Task Analyze_ErrorBeforeStream_ThrowsWithKindAndQuota()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Respond("POST", "https://site.test/api/v1/capture-analysis", new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""{"error":"You've used today's 5 analyses.","code":"quota_exceeded","remaining":0,"limit":5}""")
        });

        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(() =>
            CollectAsync(_service.AnalyzeAsync(new CaptureAnalysisRequest("t.osdpcap", new byte[] { 1 }))));

        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.QuotaExceeded));
        Assert.That(exception.Message, Is.EqualTo("You've used today's 5 analyses."));
        Assert.That(exception.Quota, Is.EqualTo(new CaptureAnalysisQuota(0, 5)));
    }

    [Test]
    public async Task Analyze_ErrorLineInStream_ThrowsAfterText()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Respond("POST", "https://site.test/api/v1/capture-analysis", Ndjson(
            """{"type":"start","id":"abc","records":1,"checks":[]}""",
            """{"type":"text","text":"Part"}""",
            """{"type":"error","message":"Busy.","code":"service_busy","remaining":4,"limit":5}"""));

        var received = new List<CaptureAnalysisEvent>();
        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(async () =>
        {
            await foreach (var analysisEvent in _service.AnalyzeAsync(new CaptureAnalysisRequest("t.osdpcap", new byte[] { 1 })))
            {
                received.Add(analysisEvent);
            }
        });

        Assert.That(received, Has.Count.EqualTo(2));
        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.ServiceBusy));
        Assert.That(exception.Quota, Is.EqualTo(new CaptureAnalysisQuota(4, 5)));
    }

    [Test]
    public async Task Analyze_StreamEndsEarly_ThrowsIncomplete()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Respond("POST", "https://site.test/api/v1/capture-analysis", Ndjson(
            """{"type":"start","id":"abc","records":1,"checks":[]}""", """{"type":"text","text":"Part"}"""));

        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(() =>
            CollectAsync(_service.AnalyzeAsync(new CaptureAnalysisRequest("t.osdpcap", new byte[] { 1 }))));

        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.Incomplete));
    }

    [Test]
    public async Task Ask_PostsQuestionToAnalysis()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Respond("POST", "https://site.test/api/v1/capture-analysis/abc/questions", Ndjson(
            """{"type":"start","id":"abc"}""", """{"type":"text","text":"Because."}""",
            """{"type":"done","truncated":false,"remaining":2,"limit":5}"""));

        var events = await CollectAsync(_service.AskAsync("abc", "Why?"));

        Assert.That(((CaptureAnalysisEvent.Started)events[0]).Summary, Is.Null);
        using var body = JsonDocument.Parse(_handler.Requests.Single().Body);
        Assert.That(body.RootElement.GetProperty("question").GetString(), Is.EqualTo("Why?"));
    }

    [TestCase("unavailable", CaptureAnalysisErrorKind.Unavailable)]
    [TestCase("invalid_capture", CaptureAnalysisErrorKind.InvalidCapture)]
    [TestCase("missing_capture", CaptureAnalysisErrorKind.InvalidCapture)]
    [TestCase("invalid_key", CaptureAnalysisErrorKind.InvalidSecurityKey)]
    [TestCase("follow_up_limit", CaptureAnalysisErrorKind.FollowUpLimitReached)]
    [TestCase("analysis_expired", CaptureAnalysisErrorKind.AnalysisExpired)]
    [TestCase("answer_in_progress", CaptureAnalysisErrorKind.AnswerInProgress)]
    [TestCase("capture_too_large", CaptureAnalysisErrorKind.CaptureTooLarge)]
    [TestCase("refused", CaptureAnalysisErrorKind.Refused)]
    [TestCase("some_new_code", CaptureAnalysisErrorKind.Failed)]
    public void ErrorCodes_MapToKinds(string code, CaptureAnalysisErrorKind kind)
    {
        var exception = Assert.Throws<CaptureAnalysisException>(() =>
            ZBitCaptureAnalysisService.ParseEvent($$"""{"type":"error","message":"m","code":"{{code}}"}"""));

        Assert.That(exception!.Kind, Is.EqualTo(kind));
    }

    [Test]
    public async Task ConnectionFailure_IsUnavailable()
    {
        await SaveSessionAsync(accessToken: "access-1", expires: Now.AddHours(1));
        _handler.Fail(new HttpRequestException("No route"));

        var exception = await Assert.ThrowsAsync<CaptureAnalysisException>(() => _service.GetQuotaAsync());

        Assert.That(exception!.Kind, Is.EqualTo(CaptureAnalysisErrorKind.Unavailable));
        Assert.That(exception.Message, Does.Contain("site.test"));
    }

    [Test]
    public void Options_WithMissingValues_AreNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ZBitCaptureAnalysisOptions.TryCreate(null, null, null), Is.Null);
            Assert.That(ZBitCaptureAnalysisOptions.TryCreate("https://a.test", "https://b.test", ""), Is.Null);
            Assert.That(ZBitCaptureAnalysisOptions.TryCreate("not a uri", "https://b.test", "id"), Is.Null);
            Assert.That(ZBitCaptureAnalysisOptions.TryCreate("https://a.test", "https://b.test", "id")!.RedirectUri,
                Is.EqualTo(new Uri("https://a.test/callback")));
        });
    }

    [Test]
    public void Callback_IsReadFromUri()
    {
        var callback = OAuthCallback.FromUri(new Uri("com.z-bitco.osdpbench://callback?code=a%2Bb&state=s1&iss=x"));

        Assert.That(callback, Is.EqualTo(new OAuthCallback("a+b", "s1", null, null)));
        Assert.That(OAuthCallback.IsCallbackUri("com.z-bitco.osdpbench://callback?code=1"), Is.True);
        Assert.That(OAuthCallback.IsCallbackUri("https://site.test/callback"), Is.False);
        Assert.That(OAuthCallback.IsCallbackUri("--some-flag"), Is.False);
    }

    private async Task SaveSessionAsync(string accessToken, DateTimeOffset expires, string refreshToken = "refresh-1")
    {
        var json = JsonSerializer.Serialize(new
        {
            accessToken,
            accessTokenExpires = expires,
            refreshToken,
            name = "Pat",
            email = "pat@example.com"
        });
        await _tokenStore.WriteAsync("zbit-capture-analysis-client1", json, CancellationToken.None);
    }

    private static async Task<List<CaptureAnalysisEvent>> CollectAsync(IAsyncEnumerable<CaptureAnalysisEvent> events)
    {
        var list = new List<CaptureAnalysisEvent>();
        await foreach (var analysisEvent in events) list.Add(analysisEvent);
        return list;
    }

    private static string TokenJson(string accessToken, string refreshToken, string idToken = null) =>
        JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["access_token"] = accessToken,
            ["refresh_token"] = refreshToken,
            ["expires_in"] = 86400,
            ["id_token"] = idToken
        }.Where(pair => pair.Value != null).ToDictionary(pair => pair.Key, pair => pair.Value));

    private static string IdToken(object claims)
    {
        static string Encode(string json) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{Encode("""{"alg":"RS256"}""")}.{Encode(JsonSerializer.Serialize(claims))}.signature";
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Ndjson(params string[] lines) =>
        new(HttpStatusCode.OK) { Content = new StringContent(string.Join("\n", lines) + "\n", Encoding.UTF8, "application/x-ndjson") };

    private static Dictionary<string, string> Query(Uri uri) => ParsePairs(uri.Query.TrimStart('?'));

    private static Dictionary<string, string> FormOf(RecordedRequest request) => ParsePairs(request.Body);

    private static Dictionary<string, string> ParsePairs(string text) => text.Split('&', StringSplitOptions.RemoveEmptyEntries)
        .Select(pair => pair.Split('=', 2))
        .ToDictionary(parts => Uri.UnescapeDataString(parts[0]),
            parts => Uri.UnescapeDataString(parts.Length > 1 ? parts[1].Replace('+', ' ') : ""));

    private sealed record RecordedRequest(string Authorization, string Body);

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<(string Method, string Uri, HttpResponseMessage Response)> _responses = new();
        private Exception _failure;

        public List<RecordedRequest> Requests { get; } = [];

        public void Respond(string method, string uri, HttpResponseMessage response) => _responses.Enqueue((method, uri, response));

        public void Fail(Exception failure) => _failure = failure;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_failure != null) throw _failure;

            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(request.Headers.Authorization?.ToString(), body));

            Assert.That(_responses, Is.Not.Empty, $"Unexpected request: {request.Method} {request.RequestUri}");
            var (method, uri, response) = _responses.Dequeue();
            Assert.That($"{request.Method} {request.RequestUri!.AbsoluteUri}", Is.EqualTo($"{method} {uri}"));
            return response;
        }
    }

    private sealed class MemoryTokenStore : ISecureTokenStore
    {
        public Dictionary<string, string> Values { get; } = new();

        public Task<string> ReadAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(Values.GetValueOrDefault(key));

        public Task WriteAsync(string key, string value, CancellationToken cancellationToken)
        {
            Values[key] = value;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Values.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBroker : IOAuthAuthorizationBroker
    {
        public Func<Uri, OAuthCallback> Respond { get; set; } = _ => new OAuthCallback(null, null, "access_denied", null);
        public Uri AuthorizeUri { get; private set; }

        public Task<OAuthCallback> AuthorizeAsync(Uri authorizeUri, CancellationToken cancellationToken)
        {
            AuthorizeUri = authorizeUri;
            return Task.FromResult(Respond(authorizeUri));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
