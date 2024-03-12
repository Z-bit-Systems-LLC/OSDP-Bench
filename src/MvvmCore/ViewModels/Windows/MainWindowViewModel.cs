using CommunityToolkit.Mvvm.ComponentModel;

namespace MvvmCore.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "OSDP Bench";
    }
}
