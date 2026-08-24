using OSDPBench.Core.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace OSDPBench.Windows.Views.Pages;

/// <summary>
/// Interaction logic for LineQualityPage.xaml
/// </summary>
public partial class LineQualityPage : INavigableView<LineQualityViewModel>
{
    /// <summary>
    /// Initializes the page.
    /// </summary>
    /// <param name="viewModel">The view model driving the page.</param>
    public LineQualityPage(LineQualityViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }

    /// <inheritdoc />
    public LineQualityViewModel ViewModel { get; }
}
