using Android.Content;
using Android.Hardware.Usb;
using Hoho.Android.UsbSerial.Driver;
using Hoho.Android.UsbSerial.Extensions;
using MvvmCross;
using MvvmCross.Platforms.Android.Views;
using OSDP_Bench_Android.Platform;
using OSDPBench.Core.Platforms;

[assembly: UsesFeature("android.hardware.usb.host")]

namespace OSDP_Bench_Android;

[IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
[MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]

[Activity(Label = "@string/app_name", Theme = "@style/Theme.AppCompat.Light", MainLauncher = true)]
public class SplashScreen : MvxSplashScreenActivity
{
    public SplashScreen()
        : base(Resource.Layout.SplashScreen)
    {
    }
    
    private UsbManager? _usbManager; 
    
    protected override async void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);

        await InitializeUsbService();
    }
    
    private async Task InitializeUsbService()
    {
        _usbManager = GetSystemService(Context.UsbService) as UsbManager;
        var drivers = await FindAllDriversAsync(_usbManager);

        var connection = Mvx.IoCProvider.Resolve<ISerialPortConnection>() as AndroidSerialPortConnection;
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