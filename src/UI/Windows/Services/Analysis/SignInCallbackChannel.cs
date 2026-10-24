using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using OSDPBench.CaptureAnalysis.ZBit;

namespace OSDPBench.Windows.Services.Analysis;

/// <summary>
/// Gets sign-in callbacks to the running copy of OSDP Bench. The site's redirect page opens
/// <c>com.z-bitco.osdpbench://callback?…</c>, which Windows hands to a new copy of the app; that copy forwards the URI
/// over a named pipe to the copy that started the sign-in, and exits.
/// </summary>
public sealed class SignInCallbackChannel : IDisposable
{
    private const int AppModelErrorNoPackage = 15700;

    // One pipe per Windows user; CurrentUserOnly keeps other users from connecting to it.
    private static readonly string PipeName = $"OSDPBench.SignIn.{WindowsIdentity.GetCurrent().User?.Value}";

    private readonly CancellationTokenSource _stopping = new();

    /// <summary>
    /// Occurs when a sign-in callback URI arrives. Raised on a background thread.
    /// </summary>
    public event EventHandler<Uri>? CallbackReceived;

    /// <summary>
    /// Starts listening for callbacks forwarded by other copies of the app.
    /// </summary>
    public void Start()
    {
        _ = ListenAsync(_stopping.Token);
    }

    /// <summary>
    /// Sends a callback URI to the copy of the app that is listening.
    /// </summary>
    /// <param name="callbackUri">The URI the app was started with.</param>
    /// <param name="timeout">How long to wait for the listening copy.</param>
    /// <returns>True if the URI was delivered; false if no copy is listening.</returns>
    public static bool TryForward(string callbackUri, TimeSpan timeout)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(timeout);
            using var writer = new StreamWriter(client, new UTF8Encoding(false));
            writer.WriteLine(callbackUri);
            return true;
        }
        catch (Exception exception) when (exception is TimeoutException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Registers the sign-in URI scheme for the current user so that it starts this copy of the app. A packaged
    /// (MSIX) app declares the scheme in its manifest instead, so nothing is registered for it.
    /// </summary>
    public static void RegisterUriScheme()
    {
        if (IsPackaged() || Environment.ProcessPath is not { } executable) return;

        try
        {
            using var scheme = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{IOAuthAuthorizationBroker.CallbackScheme}");
            scheme.SetValue(string.Empty, "URL:OSDP Bench sign-in");
            scheme.SetValue("URL Protocol", string.Empty);
            using var command = scheme.CreateSubKey(@"shell\open\command");
            command.SetValue(string.Empty, $"\"{executable}\" \"%1\"");
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            // Signing in still works by pasting the code the site shows.
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(server, Encoding.UTF8);
                var line = await reader.ReadLineAsync(cancellationToken);
                if (OAuthCallback.IsCallbackUri(line))
                {
                    CallbackReceived?.Invoke(this, new Uri(line!));
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException)
            {
                // Another copy of the app already owns the pipe; this copy relies on a pasted code instead.
                return;
            }
        }
    }

    private static bool IsPackaged()
    {
        var length = 0;
        return GetCurrentPackageFullName(ref length, null) != AppModelErrorNoPackage;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, [Out] char[]? packageFullName);

    /// <inheritdoc />
    public void Dispose()
    {
        _stopping.Cancel();
        _stopping.Dispose();
    }
}
