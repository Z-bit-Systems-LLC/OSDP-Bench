using MvvmCore.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Windows 
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }
        
        public MainWindow(MainWindowViewModel viewModel,
            IPageService pageService,
            INavigationService navigationService)
        {
            ViewModel = viewModel;
            DataContext = this;

            SystemThemeWatcher.Watch(this);
            
            InitializeComponent();

            SetPageService(pageService);

            //navigationService.SetNavigationControl(RootNavigation);
        }

        public INavigationView GetNavigation()
        {
            throw new NotImplementedException();
        }

        public bool Navigate(Type pageType)
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }

        public void SetPageService(IPageService pageService)
        {

        }
        
        public void ShowWindow() => Show();

        public void CloseWindow() => Close();
    }
}
