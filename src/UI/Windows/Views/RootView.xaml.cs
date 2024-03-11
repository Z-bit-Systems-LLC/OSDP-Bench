using MvvmCross.ViewModels;
using OSDPBench.Core.ViewModels;

namespace Windows.Views;

/// <summary>
/// Interaction logic for RootView.xaml
/// </summary>
[MvxViewFor(typeof(RootViewModel))]
public partial class RootView 
{
    public RootView()
    {
        InitializeComponent();
    }
}