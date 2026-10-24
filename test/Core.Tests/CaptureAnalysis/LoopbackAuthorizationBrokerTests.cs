using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OSDPBench.CaptureAnalysis.ZBit;

namespace OSDPBench.Core.Tests.CaptureAnalysis;

[TestFixture(TestOf = typeof(LoopbackAuthorizationBroker))]
public class LoopbackAuthorizationBrokerTests
{
    private static readonly Uri Issuer = new("https://issuer.test/oauth/authorize");

    private HttpClient _browser;
    private TaskCompletionSource<Uri> _opened;
    private LoopbackAuthorizationBroker _broker;

    [SetUp]
    public void Setup()
    {
        _browser = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        _opened = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        _broker = new LoopbackAuthorizationBroker(uri => _opened.TrySetResult(uri));
    }

    [TearDown]
    public void TearDown()
    {
        _browser.Dispose();
    }

    [Test]
    public async Task Redirect_CompletesSignInAndShowsSignedInPage()
    {
        var (signIn, redirectUri) = await StartAsync();

        var response = await _browser.GetAsync(new Uri(redirectUri, "?code=a%2Bb&state=s1&iss=x"));
        var page = await response.Content.ReadAsStringAsync();
        var callback = await signIn;

        Assert.Multiple(() =>
        {
            Assert.That(redirectUri.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(redirectUri.Port, Is.GreaterThan(0));
            Assert.That(redirectUri.AbsolutePath, Is.EqualTo("/callback"));
            Assert.That(callback, Is.EqualTo(new OAuthCallback("a+b", "s1", null, null)));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(page, Does.Contain("You&#39;re signed in"));
        });
    }

    [Test]
    public async Task OpensIssuerPageBuiltForRedirectUri()
    {
        var (signIn, redirectUri) = await StartAsync();

        Assert.That((await _opened.Task).Query, Does.Contain(Uri.EscapeDataString(redirectUri.AbsoluteUri)));
        await _browser.GetAsync(new Uri(redirectUri, "?code=c&state=s"));
        await signIn;
    }

    [Test]
    public async Task RefusedSignIn_IsPassedOnWithFailurePage()
    {
        var (signIn, redirectUri) = await StartAsync();

        var page = await _browser.GetStringAsync(new Uri(redirectUri, "?error=access_denied&error_description=No&state=s1"));
        var callback = await signIn;

        Assert.That(callback, Is.EqualTo(new OAuthCallback(null, "s1", "access_denied", "No")));
        Assert.That(page, Does.Contain("Signing in didn&#39;t finish"));
    }

    [Test]
    public async Task OtherRequests_AreNotFoundAndDoNotFinishSignIn()
    {
        var (signIn, redirectUri) = await StartAsync();

        var favicon = await _browser.GetAsync(new Uri(redirectUri, "/favicon.ico"));
        var empty = await _browser.GetAsync(redirectUri);

        Assert.That(favicon.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(signIn.IsCompleted, Is.False);

        await _browser.GetAsync(new Uri(redirectUri, "?code=c&state=s"));
        Assert.That((await signIn).Code, Is.EqualTo("c"));
    }

    [Test]
    public async Task IdleConnection_DoesNotHoldUpRedirect()
    {
        var (signIn, redirectUri) = await StartAsync();

        // Browsers open spare connections and may never send anything on them.
        using var idle = new TcpClient();
        await idle.ConnectAsync(IPAddress.Loopback, redirectUri.Port);
        var response = await _browser.GetAsync(new Uri(redirectUri, "?code=c&state=s")).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await signIn.WaitAsync(TimeSpan.FromSeconds(5))).Code, Is.EqualTo("c"));
    }

    [Test]
    public async Task Cancelling_StopsListening()
    {
        using var cancellation = new CancellationTokenSource();
        var (signIn, redirectUri) = await StartAsync(cancellation.Token);

        await cancellation.CancelAsync();

        Assert.That(async () => await signIn, Throws.InstanceOf<OperationCanceledException>());
        using var client = new TcpClient();
        var refused = false;
        try
        {
            await client.ConnectAsync(IPAddress.Loopback, redirectUri.Port);
        }
        catch (SocketException)
        {
            refused = true;
        }

        Assert.That(refused, Is.True, "Nothing should be listening once the sign-in is cancelled.");
    }

    [Test]
    public void BrowserFailure_IsPassedOn()
    {
        var broker = new LoopbackAuthorizationBroker(_ => throw new InvalidOperationException("No browser"));

        Assert.That(async () => await broker.AuthorizeAsync(redirect => redirect, CancellationToken.None),
            Throws.InvalidOperationException.With.Message.EqualTo("No browser"));
    }

    private async Task<(Task<OAuthCallback> SignIn, Uri RedirectUri)> StartAsync(
        CancellationToken cancellationToken = default)
    {
        var redirect = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        var signIn = _broker.AuthorizeAsync(redirectUri =>
        {
            redirect.TrySetResult(redirectUri);
            return new Uri($"{Issuer}?redirect_uri={Uri.EscapeDataString(redirectUri.AbsoluteUri)}");
        }, cancellationToken);
        return (signIn, await redirect.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }
}
