namespace OSDPBench.Core.Services;

/// <summary>
/// Provides methods for converting hexadecimal strings to byte arrays.
/// </summary>
public static class HexConverter
{
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