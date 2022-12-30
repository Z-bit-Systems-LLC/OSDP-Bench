  using Android.OS;
  using Microsoft.Extensions.Logging;
using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.IoC;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.ViewModels;
using OSDPBench.Core;
using OSDPBench.Core.Platforms;
using OSDPBench.UI.Android.Bindings;
using OSDPBench.UI.Android.Platform;
using Serilog;
using Serilog.Extensions.Logging;

namespace OSDPBench.UI.Android;

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
    
    protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry)
    {
        base.FillTargetFactories(registry);
        registry.RegisterCustomBindingFactory<ImageView>("TintColor", view => new TintColorMvxTargetBinding(view));
    }
}