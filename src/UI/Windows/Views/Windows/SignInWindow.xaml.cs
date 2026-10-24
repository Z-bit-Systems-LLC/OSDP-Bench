using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using OSDPBench.CaptureAnalysis.ZBit;

namespace OSDPBench.Windows.Views.Windows;

/// <summary>
/// Shown while the user signs in through the browser. It offers the sign-in link again, for when the browser doesn't
/// show it, and takes the code the site shows when the app can't be opened from the browser.
/// </summary>
public partial class SignInWindow
{
    private readonly Uri _authorizeUri;
    private readonly TaskCompletionSource<OAuthCallback> _result;
    private bool _closingForResult;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignInWindow"/> class.
    /// </summary>
    /// <param name="authorizeUri">The sign-in page.</param>
    /// <param name="result">Completed with a pasted code, or cancelled when the user gives up.</param>
    public SignInWindow(Uri authorizeUri, TaskCompletionSource<OAuthCallback> result)
    {
        _authorizeUri = authorizeUri;
        _result = result;
        InitializeComponent();
    }

    /// <summary>
    /// Opens the sign-in page in the default browser. If there's no browser to open, the window's link can still be
    /// copied.
    /// </summary>
    /// <param name="authorizeUri">The sign-in page.</param>
    public static void OpenBrowser(Uri authorizeUri)
    {
        try
        {
            Process.Start(new ProcessStartInfo(authorizeUri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            // No default browser; the user copies the link instead.
        }
    }

    /// <summary>
    /// Closes the window once the sign-in has a result.
    /// </summary>
    public void CloseForResult()
    {
        _closingForResult = true;
        Close();
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (!_closingForResult)
        {
            _result.TrySetCanceled();
        }
    }

    private void OnCopyLinkClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_authorizeUri.AbsoluteUri);
        }
        catch (ExternalException)
        {
            // Another app has the clipboard open; the user can try again.
        }
    }

    private void OnOpenBrowserClick(object sender, RoutedEventArgs e)
    {
        OpenBrowser(_authorizeUri);
    }

    private void OnCodeTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ContinueButton.IsEnabled = !string.IsNullOrWhiteSpace(CodeTextBox.Text);
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeTextBox.Text)) return;

        _result.TrySetResult(OAuthCallback.FromPastedCode(CodeTextBox.Text));
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
