using Android.Content;
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

        await InitializeUsbService();
    }
    
    private async Task InitializeUsbService()
    {
        var usbManager = GetSystemService(Context.UsbService) as UsbManager;
        var drivers = await FindAllDriversAsync(usbManager);

        var connection = Mvx.IoCProvider.GetSingleton<ISerialPortConnection>() as AndroidSerialPortConnection;
        connection?.GetSerialPorts(drivers);
    }

    public static async Task<IList<IUsbSerialDriver>> FindAllDriversAsync(UsbManager? usbManager)
    {
        // adding a custom driver to the default probe table
        var table = UsbSerialProber.DefaultProbeTable;
        table.AddProduct(0x1b4f, 0x0008, typeof(CdcAcmSerialDriver)); // IOIO OTG

        table.AddProduct(0x09D8, 0x0420, typeof(CdcAcmSerialDriver)); // Elatec TWN4

        var prober = new UsbSerialProber(table);
        return await prober.FindAllDriversAsync(usbManager);
    }
}