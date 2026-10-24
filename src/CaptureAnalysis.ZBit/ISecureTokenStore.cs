namespace OSDPBench.CaptureAnalysis.ZBit;

/// <summary>
/// Keeps sign-in tokens on the device, protected from other users (for example with DPAPI on Windows, or the
/// platform's secure storage on mobile).
/// </summary>
public interface ISecureTokenStore
{
    /// <summary>
    /// Reads a value.
    /// </summary>
    /// <param name="key">The value's name; letters, digits and dashes only.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The value, or <see langword="null"/> if none is stored or it can't be read.</returns>
    Task<string?> ReadAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Stores a value, replacing any stored before.
    /// </summary>
    /// <param name="key">The value's name; letters, digits and dashes only.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task WriteAsync(string key, string value, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a value, if one is stored.
    /// </summary>
    /// <param name="key">The value's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
