using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.ViewModels;
using OSDP_Bench_Android.Platform;
using OSDPBench.Core;
using OSDPBench.Core.Platforms;
using Serilog;
using Serilog.Extensions.Logging;

namespace OSDP_Bench_Android;

public class Setup : MvxAndroidSetup<App>
{
    protected override IMvxApplication CreateApp(IMvxIoCProvider iocProvider)
    {
        iocProvider.ConstructAndRegisterSingleton<ISerialPortConnection, AndroidSerialPortConnection>();

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
