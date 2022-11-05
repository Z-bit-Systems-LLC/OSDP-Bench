
using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.ViewModels;
using OSDP_Bench_Android.Platform;
using OSDP.Net;
using OSDPBench.Core;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.Services;
using Serilog;
using Serilog.Extensions.Logging;

namespace OSDP_Bench_Android;

public class Setup : MvxAndroidSetup<App>
{
    protected override IMvxApplication CreateApp(IMvxIoCProvider iocProvider)
    {
        var panel = new ControlPanel();

        iocProvider.RegisterSingleton<ISerialPortConnection>(new AndroidSerialPortConnection());
        iocProvider.RegisterSingleton(panel);
        iocProvider.RegisterSingleton<IDeviceManagementService>(new DeviceManagementService(panel));

        return new App();
    }
    
    protected override ILoggerProvider CreateLogProvider()
    {
        return new SerilogLoggerProvider();
    }

    protected override ILoggerFactory CreateLogFactory()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.AndroidLog()
            .CreateLogger();

        return new SerilogLoggerFactory();
    }
}
