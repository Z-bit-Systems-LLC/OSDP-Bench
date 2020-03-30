using MvvmCross.Logging;
using MvvmCross.Platforms.Uap.Core;
using MvvmCross.ViewModels;

namespace OSDPBenchUWP
{
    public class Setup : MvxWindowsSetup
    {
        protected override IMvxApplication CreateApp()
        {
            return new OSDPBench.Core.App();
        }
    }
}
