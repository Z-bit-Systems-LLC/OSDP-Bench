using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MvvmCross.Base;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.WinUi.Presenters.Attributes;
using MvvmCross.ViewModels;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.ViewModels;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OSDP_Bench_WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    [MvxViewFor(typeof(UpdateCommunicationViewModel))]
    [MvxPagePresentation]
    public sealed partial class UpdateCommunicationView : IMvxBindingContextOwner
    {
        public UpdateCommunicationView()
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

            using var set = this.CreateBindingSet<UpdateCommunicationView, UpdateCommunicationViewModel>();
            set.Bind(this).For(view => view.AlertInteraction).To(viewModel => viewModel.AlertInteraction).OneWay();
            set.Apply();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedFrom(eventArgs);

            _alertInteraction.Requested -= OnAlertInteractionRequested;
        }
    }
}
