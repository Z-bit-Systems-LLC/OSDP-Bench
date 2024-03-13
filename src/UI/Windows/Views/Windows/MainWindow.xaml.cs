using MvvmCore.ViewModels.Windows;
using OSDPBench.Windows.Views.Pages;
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

            navigationService.SetNavigationControl(RootNavigation);
        }
        
        /// <inheritdoc />
        public INavigationView GetNavigation() => RootNavigation;


        /// <inheritdoc />
        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);


        /// <inheritdoc />
        public void SetPageService(IPageService pageService) => RootNavigation.SetPageService(pageService);


        /// <inheritdoc />
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
        
        public void ShowWindow() => Show();

        public void CloseWindow() => Close();
    }
}
