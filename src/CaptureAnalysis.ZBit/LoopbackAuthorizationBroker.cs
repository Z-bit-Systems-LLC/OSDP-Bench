using System.Net;
using System.Net.Sockets;
using System.Text;
using CoreResources = OSDPBench.Core.Resources.Resources;

namespace OSDPBench.CaptureAnalysis.ZBit;

/// <summary>
/// Signs in through the system browser and receives the redirect on <c>http://127.0.0.1:{port}/callback</c>, on a
/// port picked for each sign-in (RFC 8252, section 7.3). The browser then shows a short page saying the user can go
/// back to the app. Nothing has to be registered with the operating system.
/// </summary>
/// <remarks>
/// The OAuth application must list <c>http://127.0.0.1:{any port}/callback</c> as a redirect URI; the issuer accepts
/// it on any port.
/// </remarks>
public sealed class LoopbackAuthorizationBroker : IOAuthAuthorizationBroker
{
    /// <summary>The path the redirect arrives on.</summary>
    public const string CallbackPath = "/callback";

    /// <summary>How long a connection gets to send its request. Browsers open spare connections they never use.</summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The most a request head may take; the redirect is one short GET.</summary>
    private const int MaxRequestHeadLength = 16 * 1024;

    private readonly Action<Uri> _openBrowser;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoopbackAuthorizationBroker"/> class.
    /// </summary>
    /// <param name="openBrowser">Opens a page in the system browser.</param>
    public LoopbackAuthorizationBroker(Action<Uri> openBrowser)
    {
        _openBrowser = openBrowser ?? throw new ArgumentNullException(nameof(openBrowser));
    }

    /// <inheritdoc />
    public async Task<OAuthCallback> AuthorizeAsync(Func<Uri, Uri> buildAuthorizeUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buildAuthorizeUri);

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var redirectUri = new Uri($"http://127.0.0.1:{port}{CallbackPath}");
            var result = new TaskCompletionSource<OAuthCallback>(TaskCreationOptions.RunContinuationsAsynchronously);
            var accepting = AcceptAsync(listener, redirectUri, result, stop.Token);

            _openBrowser(buildAuthorizeUri(redirectUri));
            var callback = await result.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            await stop.CancelAsync().ConfigureAwait(false);
            await accepting.ConfigureAwait(false);
            return callback;
        }
        finally
        {
            await stop.CancelAsync().ConfigureAwait(false);
            listener.Stop();
        }
    }

    private static async Task AcceptAsync(TcpListener listener, Uri redirectUri,
        TaskCompletionSource<OAuthCallback> result, CancellationToken cancellationToken)
    {
        var connections = new List<Task>();
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is OperationCanceledException or SocketException
                                                  or ObjectDisposedException)
            {
                break;
            }

            // Each connection is served on its own, so one the browser opened and left idle doesn't hold up the
            // redirect.
            connections.Add(RespondAsync(client, redirectUri, result, cancellationToken));
            connections.RemoveAll(connection => connection.IsCompleted);
        }

        // Let the page that finished the sign-in reach the browser.
        await Task.WhenAll(connections).ConfigureAwait(false);
    }

    private static async Task RespondAsync(TcpClient client, Uri redirectUri,
        TaskCompletionSource<OAuthCallback> result, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);
        try
        {
            await using var stream = client.GetStream();
            var target = await ReadRequestTargetAsync(stream, timeout.Token).ConfigureAwait(false);
            var callback = target != null && Uri.TryCreate(redirectUri, target, out var requestUri) &&
                           requestUri.AbsolutePath == CallbackPath
                ? OAuthCallback.FromUri(requestUri)
                : null;

            if (callback is not { HasResult: true } || result.Task.IsCompleted)
            {
                await WriteResponseAsync(stream, "404 Not Found", null, timeout.Token).ConfigureAwait(false);
                return;
            }

            await WriteResponseAsync(stream, "200 OK", CompletionPage(callback.Error == null), timeout.Token)
                .ConfigureAwait(false);
            result.TrySetResult(callback);
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException
                                              or ObjectDisposedException)
        {
            // The browser went away or never sent a request; the user can still finish from another try.
        }
        finally
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// Reads the request line and headers, and returns the request target of a GET, or <see langword="null"/>.
    /// </summary>
    private static async Task<string?> ReadRequestTargetAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var head = new StringBuilder();
        var buffer = new byte[1024];
        while (head.Length < MaxRequestHeadLength)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) return null;

            head.Append(Encoding.ASCII.GetString(buffer, 0, read));
            if (head.ToString().Contains("\r\n\r\n", StringComparison.Ordinal)) break;
        }

        var requestLine = head.ToString().Split("\r\n", 2)[0].Split(' ');
        return requestLine.Length == 3 && requestLine[0] == "GET" && requestLine[1].StartsWith('/')
            ? requestLine[1]
            : null;
    }

    private static async Task WriteResponseAsync(NetworkStream stream, string status, string? html,
        CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(html ?? string.Empty);
        var head = $"HTTP/1.1 {status}\r\n" +
                   "Content-Type: text/html; charset=utf-8\r\n" +
                   $"Content-Length: {body.Length}\r\n" +
                   "Cache-Control: no-store\r\n" +
                   "Referrer-Policy: no-referrer\r\n" +
                   "Connection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head), cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The page the browser shows once it has handed over the sign-in result.
    /// </summary>
    internal static string CompletionPage(bool succeeded)
    {
        var heading = WebUtility.HtmlEncode(CoreResources.GetString(
            succeeded ? "SignIn_Page_SignedIn" : "SignIn_Page_NotSignedIn"));
        var message = WebUtility.HtmlEncode(CoreResources.GetString(
            succeeded ? "SignIn_Page_ReturnToApp" : "SignIn_Page_SeeApp"));
        return $$"""
            <!doctype html>
            <html>
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>OSDP Bench</title>
            <style>
              :root { color-scheme: light dark; --text: #1b1b1b; --muted: #5c5c5c; --background: #f3f3f3; --card: #ffffff; }
              @media (prefers-color-scheme: dark) {
                :root { --text: #ffffff; --muted: #c5c5c5; --background: #202020; --card: #2b2b2b; }
              }
              body { margin: 0; min-height: 100vh; display: grid; place-items: center; padding: 16px; box-sizing: border-box;
                     font-family: "Segoe UI Variable Text", "Segoe UI", system-ui, sans-serif;
                     background: var(--background); color: var(--text); }
              main { background: var(--card); border-radius: 8px; padding: 32px; max-width: 420px;
                     box-shadow: 0 2px 8px rgb(0 0 0 / 12%); }
              h1 { font-size: 20px; font-weight: 600; margin: 0 0 8px; }
              p { margin: 0; color: var(--muted); line-height: 1.5; }
            </style>
            </head>
            <body>
            <main>
            <h1>{{heading}}</h1>
            <p>{{message}}</p>
            </main>
            </body>
            </html>
            """;
    }
}
