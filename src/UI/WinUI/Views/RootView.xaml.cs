using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MvvmCross.Base;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.WinUi.Presenters.Attributes;
using MvvmCross.ViewModels;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [MvxViewFor(typeof(RootViewModel))]
    [MvxPagePresentation]
    public sealed partial class RootView : IMvxBindingContextOwner
    {
        public RootView()
        {
            InitializeComponent();
        }

        private IMvxInteraction<Alert> _alertInteraction;
        public IMvxInteraction<Alert> AlertInteraction
        {
            get => _alertInteraction;
            set
            {
                if (_alertInteraction != null) _alertInteraction.Requested -= OnAlertInteractionRequested;

                _alertInteraction = value;
                _alertInteraction.Requested += OnAlertInteractionRequested;
            }
        }

        private async void OnAlertInteractionRequested(object sender, MvxValueEventArgs<Alert> eventArgs)
        {
            var dialog = new ContentDialog
            {
                Title = "OSDP Bench",
                Content = eventArgs.Value.Message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private IMvxInteraction<YesNoQuestion> _yesNoInteraction;
        public IMvxInteraction<YesNoQuestion> YesNoInteraction
        {
            get => _yesNoInteraction;
            set
            {
                if (_yesNoInteraction != null) _yesNoInteraction.Requested -= OnYesNoInteractionRequested;

                _yesNoInteraction = value;
                _yesNoInteraction.Requested += OnYesNoInteractionRequested;
            }
        }

        private async void OnYesNoInteractionRequested(object sender, MvxValueEventArgs<YesNoQuestion> eventArgs)
        {
            var dialog = new ContentDialog
            {
                Title = "OSDP Bench",
                Content = eventArgs.Value.Question,
                SecondaryButtonText = "No",
                PrimaryButtonText = "Yes",
                XamlRoot = Content.XamlRoot
            };
            var results = await dialog.ShowAsync();

            await eventArgs.Value.YesNoCallback(results == ContentDialogResult.Primary);
        }

        private IMvxBindingContext _bindingContext;
        public IMvxBindingContext BindingContext
        {
            get => _bindingContext ??= new MvxBindingContext();
            set => _bindingContext = value;
        }

        protected override void OnViewModelSet()
        {
            base.OnViewModelSet();

            BindingContext.DataContext = ViewModel;

            using var set = this.CreateBindingSet<RootView, RootViewModel>();

            set.Bind(this).For(view => view.AlertInteraction).To(viewModel => viewModel.AlertInteraction).OneWay();
            set.Bind(this).For(view => view.YesNoInteraction).To(viewModel => viewModel.YesNoInteraction).OneWay();
            set.Apply();
        }
         
        protected override void OnNavigatedFrom(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedFrom(eventArgs);

            _alertInteraction.Requested -= OnAlertInteractionRequested;
            _yesNoInteraction.Requested -= OnYesNoInteractionRequested;
        }

        private async void AboutButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            var dialog = new ContentDialog
            {
                Title = "OSDP Bench",
                Content = "Brought to you by Z-bit Systems, LLC",
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
