using OSDPBench.Core.ViewModels.Windows;
using System.IO;
using System.Windows;
using System.Windows.Resources;
using Wpf.Ui.Controls;

namespace OSDPBench.Windows.Views.Pages
{
    /// <summary>
    /// Interaction logic for InfoPage.xaml
    /// </summary>
    public sealed partial class InfoPage : INavigableView<MainWindowViewModel>
    {
        const string EplFilePath = "pack://application:,,,/Assets/EPL.txt";
        const string ApacheFilePath = "pack://application:,,,/Assets/Apache.txt";
        const string MitFilePath = "pack://application:,,,/Assets/MIT.txt";

        public InfoPage(MainWindowViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            
            InitializeComponent();
            
            Loaded += (sender, args) =>
            {
                var info = Application.GetResourceStream(new Uri(EplFilePath));
                if (info != null)
                {
                    using StreamReader reader = new(info.Stream);
                    EplLicenseTextBlock.Text = reader.ReadToEnd();
                }
                
                info = Application.GetResourceStream(new Uri(ApacheFilePath));
                if (info != null)
                {
                    using StreamReader reader = new(info.Stream);
                    ApacheLicenseTextBlock.Text = reader.ReadToEnd();
                }

                info = Application.GetResourceStream(new Uri(MitFilePath));
                if (info != null)
                {
                    using StreamReader reader = new(info.Stream);
                    MitLicenseTextBlock.Text = reader.ReadToEnd();
                }
            };
        }

        public MainWindowViewModel ViewModel { get; }
    }
}
