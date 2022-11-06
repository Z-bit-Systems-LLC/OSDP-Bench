using Android.Content;
using Android.Hardware.Usb;
using Android.Views;
using Hoho.Android.UsbSerial.Util;
using MvvmCross;
using MvvmCross.Base;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.ViewModels;
using OSDP_Bench_Android.Platform;
using OSDPBench.Core.Interactions;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.ViewModels;

namespace OSDP_Bench_Android.Activities;

[MvxActivityPresentation]
[Activity(Theme = "@style/Theme.AppCompat.Light",
    WindowSoftInputMode = SoftInput.AdjustPan)]
public class RootView : MvxActivity<RootViewModel>
{
    private UsbManager? _usbManager; 
    
    protected override async void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        await InitializeUsbService();

        SetContentView(Resource.Layout.RootView);
    }
    
    protected override void OnPause()
    {
        base.OnPause();

        var connection = Mvx.IoCProvider.Resolve<ISerialPortConnection>() as AndroidSerialPortConnection;
        connection?.Close();
    }

    protected override void OnResume()
    {
        base.OnResume();

        var connection = Mvx.IoCProvider.Resolve<ISerialPortConnection>() as AndroidSerialPortConnection;
        connection?.Open();
    }
    
    protected override void OnViewModelSet()
    {
        base.OnViewModelSet();

        BindingContext.DataContext = ViewModel;

        using var set = this.CreateBindingSet<RootView, RootViewModel>();

        set.Bind(this).For(view => view.AlertInteraction).To(viewModel => viewModel.AlertInteraction).OneWay();
        set.Apply();
    }
    
    private IMvxInteraction<Alert> _alertInteraction = null!;
    
    // ReSharper disable once MemberCanBePrivate.Global
    public IMvxInteraction<Alert> AlertInteraction
    {
        get => _alertInteraction;
        set
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (_alertInteraction != null) _alertInteraction.Requested -= OnAlertInteractionRequested!;

            _alertInteraction = value; 
            _alertInteraction.Requested += OnAlertInteractionRequested!;
        }
    }

    private void OnAlertInteractionRequested(object sender, MvxValueEventArgs<Alert> eventArgs)
    {
        AlertDialog.Builder dialog = new AlertDialog.Builder(this);  
        AlertDialog? alert = dialog.Create();  
        alert?.SetTitle("OSDP Bench");  
        alert?.SetMessage(eventArgs.Value.Message);  
        alert?.SetButton("OK", (_, _) => {});  
        alert?.Show();
    }

    private async Task InitializeUsbService()
    {
        _usbManager = GetSystemService(Context.UsbService) as UsbManager;
        var drivers = await OSDP_Bench_Android.SplashScreen.FindAllDriversAsync(_usbManager);
        
        var port = drivers.FirstOrDefault()?.Ports.FirstOrDefault();

        if (_usbManager != null && port != null)
        {
            var permissionGranted = await _usbManager.RequestPermissionAsync(port.Driver.Device, this);
            if (permissionGranted)
            {
                var connection = Mvx.IoCProvider.Resolve<ISerialPortConnection>() as AndroidSerialPortConnection;
                connection?.LoadPort(_usbManager, port);
            }
            else
            {
                AlertDialog.Builder dialog = new AlertDialog.Builder(this);  
                AlertDialog? alert = dialog.Create();  
                alert?.SetTitle("OSDP Bench");  
                alert?.SetMessage("Unable to discover without permission to use USB port.");  
                alert?.SetButton("OK", (_, _) =>  
                {  
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                });  
                alert?.Show();
            }
        }
    }
}