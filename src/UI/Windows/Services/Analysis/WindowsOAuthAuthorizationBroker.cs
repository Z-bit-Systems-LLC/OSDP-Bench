using System.Windows;
using OSDPBench.CaptureAnalysis.ZBit;
using OSDPBench.Windows.Views.Windows;

namespace OSDPBench.Windows.Services.Analysis;

/// <summary>
/// Signs in through the default browser. The result arrives either through the sign-in URI scheme (forwarded by
/// <see cref="SignInCallbackChannel"/>) or as a code the user pastes into the sign-in window.
/// </summary>
public sealed class WindowsOAuthAuthorizationBroker : IOAuthAuthorizationBroker
{
    private readonly SignInCallbackChannel _callbackChannel;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsOAuthAuthorizationBroker"/> class.
    /// </summary>
    /// <param name="callbackChannel">Delivers callbacks from the copy of the app the browser starts.</param>
    public WindowsOAuthAuthorizationBroker(SignInCallbackChannel callbackChannel)
    {
        _callbackChannel = callbackChannel ?? throw new ArgumentNullException(nameof(callbackChannel));
    }

    /// <inheritdoc />
    public async Task<OAuthCallback> AuthorizeAsync(Uri authorizeUri, CancellationToken cancellationToken)
    {
        var result = new TaskCompletionSource<OAuthCallback>(TaskCreationOptions.RunContinuationsAsynchronously);
        _callbackChannel.CallbackReceived += OnCallbackReceived;
        await using var registration = cancellationToken.Register(() => result.TrySetCanceled(cancellationToken));

        var dispatcher = Application.Current.Dispatcher;
        var owner = await dispatcher.InvokeAsync(() =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive) ??
            Application.Current.MainWindow);
        var window = await dispatcher.InvokeAsync(() =>
        {
            var signInWindow = new SignInWindow(authorizeUri, result) { Owner = owner };
            signInWindow.Show();
            return signInWindow;
        });

        try
        {
            SignInWindow.OpenBrowser(authorizeUri);
            return await result.Task;
        }
        finally
        {
            _callbackChannel.CallbackReceived -= OnCallbackReceived;
            await dispatcher.InvokeAsync(() =>
            {
                window.CloseForResult();

                // The browser has focus after signing in; bring the app back.
                owner?.Activate();
            });
        }

        void OnCallbackReceived(object? sender, Uri callbackUri) => result.TrySetResult(OAuthCallback.FromUri(callbackUri));
    }
}
