using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using OSDPBench.CaptureAnalysis.ZBit;
using OSDPBench.Core.Services.Analysis;
using CoreResources = OSDPBench.Core.Resources.Resources;

namespace OSDPBench.Windows.Services.Analysis;

/// <summary>
/// Signs in through the default browser, receiving the result on 127.0.0.1 (<see cref="LoopbackAuthorizationBroker"/>),
/// and brings OSDP Bench back in front of the browser once the user has signed in.
/// </summary>
public sealed class WindowsOAuthAuthorizationBroker : IOAuthAuthorizationBroker
{
    private readonly LoopbackAuthorizationBroker _loopbackBroker = new(OpenBrowser);

    /// <inheritdoc />
    public async Task<OAuthCallback> AuthorizeAsync(Func<Uri, Uri> buildAuthorizeUri, CancellationToken cancellationToken)
    {
        var dispatcher = Application.Current.Dispatcher;
        var window = await dispatcher.InvokeAsync(() =>
            Application.Current.Windows.OfType<Window>().FirstOrDefault(candidate => candidate.IsActive) ??
            Application.Current.MainWindow);

        var callback = await _loopbackBroker.AuthorizeAsync(buildAuthorizeUri, cancellationToken);
        await dispatcher.InvokeAsync(() => BringToFront(window));
        return callback;
    }

    private static void OpenBrowser(Uri uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            throw new CaptureAnalysisException(CaptureAnalysisErrorKind.SignInFailed,
                CoreResources.GetString("CaptureAnalysis_Error_NoBrowser"), innerException: exception);
        }
    }

    /// <summary>
    /// Puts the window back over the browser. Windows won't let an app take the focus from the browser, but it can
    /// raise its window, which then shows the user they're signed in.
    /// </summary>
    private static void BringToFront(Window? window)
    {
        if (window == null) return;

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Topmost = true;
        window.Topmost = false;
        window.Activate();
    }
}
