using MvvmCross.Core;
using MvvmCross.Platforms.Wpf.Views;

namespace Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void RegisterSetup()
    {
        this.RegisterSetupType<Setup>();
    }
}