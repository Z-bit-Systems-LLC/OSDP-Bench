using System.Reflection;
using OSDPBench.CaptureAnalysis.ZBit;

namespace OSDPBench.Windows.Services.Analysis;

/// <summary>
/// Reads the capture analysis settings the build embedded in the app (see Windows.csproj). They are kept out of
/// source, so a build without them has no capture analysis.
/// </summary>
internal static class CaptureAnalysisSettings
{
    /// <summary>
    /// Gets the Z-bit capture analyzer's settings, or <see langword="null"/> when this build has none.
    /// </summary>
    /// <param name="assembly">The assembly the settings were embedded in.</param>
    /// <returns>The settings, or <see langword="null"/>.</returns>
    public static ZBitCaptureAnalysisOptions? Read(Assembly assembly)
    {
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .GroupBy(attribute => attribute.Key)
            .ToDictionary(group => group.Key, group => group.Last().Value);

        return ZBitCaptureAnalysisOptions.TryCreate(
            metadata.GetValueOrDefault("ZBitAnalysis.SiteUrl"),
            metadata.GetValueOrDefault("ZBitAnalysis.Authority"),
            metadata.GetValueOrDefault("ZBitAnalysis.ClientId"));
    }
}
