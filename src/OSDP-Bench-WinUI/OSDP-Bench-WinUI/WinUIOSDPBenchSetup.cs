using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Platforms.WinUi.Core;
using MvvmCross.ViewModels;
using OSDP.Net;
using OSDP_Bench_WinUI.Platform;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.Services;
using Serilog;
using Serilog.Extensions.Logging;

namespace OSDP_Bench_WinUI;

internal class WinUIOSDPBenchSetup : MvxWindowsSetup<OSDPBench.Core.App>
{
    protected override IMvxApplication CreateApp(IMvxIoCProvider iocProvider)
    {
        var panel = new ControlPanel();

        iocProvider.RegisterSingleton<ISerialPortConnection>(new WinUISerialPortConnection());
        iocProvider.RegisterSingleton(panel);
        iocProvider.RegisterSingleton<IDeviceManagementService>(new DeviceManagementService(panel));

        return new OSDPBench.Core.App();
    }

    protected override ILoggerProvider CreateLogProvider()
    {
        return new SerilogLoggerProvider();
    }

    protected override ILoggerFactory CreateLogFactory()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .CreateLogger();

        return new SerilogLoggerFactory();
    }
}