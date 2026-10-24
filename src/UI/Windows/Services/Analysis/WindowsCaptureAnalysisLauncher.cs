using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OSDPBench.Core.Services.Analysis;
using OSDPBench.Core.ViewModels.Windows;
using OSDPBench.Windows.Views.Windows;

namespace OSDPBench.Windows.Services.Analysis;

/// <summary>
/// Opens a capture analysis window for each capture.
/// </summary>
public sealed class WindowsCaptureAnalysisLauncher : ICaptureAnalysisLauncher
{
    private readonly IServiceProvider _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsCaptureAnalysisLauncher"/> class.
    /// </summary>
    /// <param name="services">Provides a new view model for each window.</param>
    public WindowsCaptureAnalysisLauncher(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc />
    public Task OpenAsync(CaptureForAnalysis capture)
    {
        var viewModel = _services.GetRequiredService<CaptureAnalysisViewModel>();
        viewModel.Load(capture);
        new CaptureAnalysisWindow(viewModel) { Owner = Application.Current.MainWindow }.Show();
        return Task.CompletedTask;
    }
}
