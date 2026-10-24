using System.IO;
using System.Security.Cryptography;
using System.Text;
using OSDPBench.CaptureAnalysis.ZBit;

namespace OSDPBench.Windows.Services.Analysis;

/// <summary>
/// Keeps sign-in tokens in files encrypted with DPAPI for the current Windows user.
/// </summary>
public sealed class DpapiTokenStore : ISecureTokenStore
{
    private static readonly byte[] Entropy = "OSDPBench.SignIn"u8.ToArray();

    private readonly string _folder;

    /// <summary>
    /// Initializes a new instance of the <see cref="DpapiTokenStore"/> class.
    /// </summary>
    /// <param name="folder">The folder to keep the files in; created when first needed.</param>
    public DpapiTokenStore(string folder)
    {
        _folder = folder ?? throw new ArgumentNullException(nameof(folder));
    }

    /// <summary>
    /// Gets the default folder: OSDP Bench's local application data folder.
    /// </summary>
    public static string DefaultFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OSDPBench", "SignIn");

    /// <inheritdoc />
    public async Task<string?> ReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathFor(key);
        if (!File.Exists(path)) return null;

        try
        {
            var encrypted = await File.ReadAllBytesAsync(path, cancellationToken);
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser));
        }
        catch (Exception exception) when (exception is CryptographicException or IOException)
        {
            // Written by another user or damaged: treat it as signed out.
            return null;
        }
    }

    /// <inheritdoc />
    public async Task WriteAsync(string key, string value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_folder);
        var path = PathFor(key);
        var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser);

        // Write a new file and swap it in, so a crash never leaves half a token behind.
        var temporaryPath = path + ".tmp";
        await File.WriteAllBytesAsync(temporaryPath, encrypted, cancellationToken);
        File.Move(temporaryPath, path, overwrite: true);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(PathFor(key));
        return Task.CompletedTask;
    }

    private string PathFor(string key)
    {
        if (string.IsNullOrEmpty(key) || !key.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
        {
            throw new ArgumentException(@"Use only letters, digits and dashes in a token name.", nameof(key));
        }

        return Path.Combine(_folder, key + ".bin");
    }
}
