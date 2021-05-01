using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using MvvmCross.Base;
using MvvmCross.Binding.BindingContext;
using MvvmCross.ViewModels;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.ViewModels;

namespace OSDPBenchUWP.Views
{
    /// <summary>
    /// The main page
    /// </summary>
    public sealed partial class MainView : IMvxBindingContextOwner
    {
        public MainView()
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

        private static async void OnAlertInteractionRequested(object sender, MvxValueEventArgs<Alert> eventArgs)
        {
            var dialog = new MessageDialog(eventArgs.Value.Message);
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

        private static async void OnYesNoInteractionRequested(object sender, MvxValueEventArgs<YesNoQuestion> eventArgs)
        {
            var dialog = new MessageDialog(eventArgs.Value.Question);
            dialog.Commands.Add(new UICommand("Yes", null));
            dialog.Commands.Add(new UICommand("No", null));
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;
            var command = await dialog.ShowAsync();

            eventArgs.Value.YesNoCallback(command.Label == "Yes");
        }

        private IMvxBindingContext _bindingContext;
        public IMvxBindingContext BindingContext
        {
            get => _bindingContext ?? (_bindingContext = new MvxBindingContext());
            set => _bindingContext = value;
        }

        protected override void OnViewModelSet()
        {
            base.OnViewModelSet();

            BindingContext.DataContext = ViewModel;

            using (var set = this.CreateBindingSet<MainView, MainViewModel>())
            {
                set.Bind(this).For(view => view.AlertInteraction).To(viewModel => viewModel.AlertInteraction).OneWay();
                set.Bind(this).For(view => view.YesNoInteraction).To(viewModel => viewModel.YesNoInteraction).OneWay();
                set.Apply();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedFrom(eventArgs);

            _alertInteraction.Requested -= OnAlertInteractionRequested;
            _yesNoInteraction.Requested -= OnYesNoInteractionRequested;
        }

        private async void AboutButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            var dialog = new MessageDialog($"OSDP-Bench{Environment.NewLine}{Environment.NewLine}Brought to you by Z-bit");
            await dialog.ShowAsync();
        }
    }
}
