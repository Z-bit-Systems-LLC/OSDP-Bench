using Android.Hardware.Usb;
using Hoho.Android.UsbSerial.driver;
using Hoho.Android.UsbSerial.Extensions;
using MvvmCross;
using OSDPBench.Core.Platforms;
using OSDPBench.UI.Android.Platform;

[assembly: UsesFeature("android.hardware.usb.host")]

namespace OSDPBench.UI.Android;

[IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
[MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]

[Activity(Label = "@string/app_name", Theme = "@style/Theme.AppCompat.Light", MainLauncher = true)]
public class SplashScreen : MvvmCross.Platforms.Android.Views.MvxStartActivity
{
    public SplashScreen()
        : base(Resource.Layout.SplashScreen)
    {
    }

    protected override async void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);
    }
}