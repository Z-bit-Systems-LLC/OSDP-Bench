using System.Text;

namespace OSDPBench.Core.Services;

/// <summary>
/// Provides methods for converting hexadecimal strings to byte arrays.
/// </summary>
public static class HexConverter
{
    /// <summary>
    /// Normalizes user-supplied hexadecimal text by removing any delimiters, so a key copied in a
    /// formatted style such as "00-11-22", "00:11:22" or "00 11 22" can be accepted as-is.
    /// A leading "0x" prefix is removed as a unit rather than being treated as a delimiter, which
    /// would drop only the 'x' and shift every remaining character.
    /// </summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The uppercase hexadecimal characters contained in <paramref name="text"/>, which is
    /// an empty string when it holds none.</returns>
    public static string NormalizeHexInput(string text)
    {
        var trimmed = text.Trim();

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..];
        }

        var builder = new StringBuilder(trimmed.Length);
        foreach (var character in trimmed)
        {
            if (char.IsAsciiHexDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }


    /// <summary>
    /// Converts a hexadecimal string to a byte array.
    /// </summary>
    /// <param name="hex">The hexadecimal string to convert.</param>
    /// <param name="requiredLength">The required length of the hexadecimal string.</param>
    /// <returns>A byte array representation of the hexadecimal string.</returns>
    /// <exception cref="ArgumentException">Thrown when the hexadecimal string does not have the required length or has an odd number of characters.</exception>
    public static byte[] FromHexString(string hex, int requiredLength)
    {
        if (hex.Length != requiredLength)
            throw new ArgumentException($"Hex string must be exactly {requiredLength} characters long");

        if (hex.Length % 2 == 1)
            throw new ArgumentException("Hex string must have an even number of characters");
            
        byte[] bytes = new byte[hex.Length / 2];
        
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        
        return bytes;
    }
}