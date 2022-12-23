using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Platforms.WinUi.Core;
using MvvmCross.ViewModels;
using OSDPBench.Core.Platforms;
using Serilog;
using Serilog.Extensions.Logging;
using WinUI.Platform;

namespace WinUI;

internal class Setup : MvxWindowsSetup<OSDPBench.Core.App>
{
    protected override IMvxApplication CreateApp(IMvxIoCProvider iocProvider)
    {
        iocProvider.ConstructAndRegisterSingleton<ISerialPortConnection, WinUISerialPortConnection>();

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