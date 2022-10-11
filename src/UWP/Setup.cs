using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Platforms.Uap.Core;
using MvvmCross.ViewModels;
using OSDP.Net;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.Services;
using OSDPBenchUWP.Platform;
using Serilog;
using Serilog.Extensions.Logging;

namespace OSDPBenchUWP
{
    public class Setup : MvxWindowsSetup<OSDPBench.Core.App>
    {
        protected override IMvxApplication CreateApp(IMvxIoCProvider iocProvider)
        {
            var panel = new ControlPanel();

            iocProvider.RegisterSingleton<ISerialPortConnection>(new UwpSerialPort());
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
}
