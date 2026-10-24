namespace OSDPBench.Core.Services.Analysis;

/// <summary>
/// Opens the platform's capture analysis view for a capture. Register an implementation, together with an
/// <see cref="ICaptureAnalysisService"/>, to offer capture analysis; without one the option is hidden.
/// </summary>
public interface ICaptureAnalysisLauncher
{
    /// <summary>
    /// Opens the capture analysis view for a capture.
    /// </summary>
    /// <param name="capture">The capture to analyze.</param>
    /// <returns>A task that completes once the view is open.</returns>
    Task OpenAsync(CaptureForAnalysis capture);
}
