using System;
using Windows.UI.Popups;
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
                if (_alertInteraction != null)
                    _alertInteraction.Requested -= OnAlertInteractionRequested;

                _alertInteraction = value;
                _alertInteraction.Requested += OnAlertInteractionRequested;
            }
        }

        private static async void OnAlertInteractionRequested(object sender, MvxValueEventArgs<Alert> eventArgs)
        {
            var dialog = new MessageDialog(eventArgs.Value.Message);
            await dialog.ShowAsync();
        }

        private IMvxBindingContext _bindingContext;
        public IMvxBindingContext BindingContext
        {
            get => _bindingContext ?? (_bindingContext = new MvxBindingContext());
            set => _bindingContext = value;
        }

        protected override void OnViewModelSet()
        {
            BindingContext.DataContext = ViewModel;

            var set = this.CreateBindingSet<MainView, MainViewModel>();
            set.Bind(this).For(view => view.AlertInteraction).To(viewModel => viewModel.AlertInteraction).OneWay();
            set.Apply();
            
            base.OnViewModelSet();
        }
    }
}
