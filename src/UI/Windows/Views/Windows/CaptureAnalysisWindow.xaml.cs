using System.Collections.Specialized;
using System.Windows.Threading;
using OSDPBench.Core.ViewModels.Windows;
using Wpf.Ui.Appearance;

namespace OSDPBench.Windows.Views.Windows;

/// <summary>
/// Shows the analysis of one capture, with sign-in and follow-up questions.
/// </summary>
public partial class CaptureAnalysisWindow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaptureAnalysisWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this window.</param>
    public CaptureAnalysisWindow(CaptureAnalysisViewModel viewModel)
    {
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = this;

        SystemThemeWatcher.Watch(this);
        InitializeComponent();

        ViewModel.Exchanges.CollectionChanged += OnExchangesChanged;
        Loaded += (_, _) => ViewModel.InitializeCommand.Execute(null);
    }

    /// <summary>
    /// Gets the view model for this window.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public CaptureAnalysisViewModel ViewModel { get; }

    private void OnExchangesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Bring a new follow-up question into view; a new report starts at the top.
        if (e.Action == NotifyCollectionChangedAction.Add && ViewModel.Exchanges.Count > 1)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ReportScrollViewer.ScrollToEnd);
        }
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        ViewModel.Exchanges.CollectionChanged -= OnExchangesChanged;
        ViewModel.Dispose();
        base.OnClosed(e);
    }
}
