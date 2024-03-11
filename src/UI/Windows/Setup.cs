using Windows.Platform;
using Microsoft.Extensions.Logging;
using MvvmCross.IoC;
using MvvmCross.Platforms.Wpf.Core;
using MvvmCross.ViewModels;
using OSDPBench.Core.Platforms;
using Serilog;
using Serilog.Extensions.Logging;

namespace Windows
{
    public class Setup : MvxWpfSetup<OSDPBench.Core.App>
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
                .MinimumLevel.Verbose()
                .CreateLogger();

            return new SerilogLoggerFactory();
        }
    }
}
