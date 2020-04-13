using MvvmCross;
using MvvmCross.IoC;
using MvvmCross.Platforms.Uap.Core;
using MvvmCross.ViewModels;
using OSDPBench.Core.ViewModels;
using OSDPBenchUWP.Platform;

namespace OSDPBenchUWP
{
    public class Setup : MvxWindowsSetup
    {
        protected override IMvxApplication CreateApp()
        {
            return new OSDPBench.Core.App();
        }

        protected override void InitializeFirstChance()
        {
            Mvx.IoCProvider.RegisterSingleton<ISerialPort>(new UwpSerialPort());

            base.InitializeFirstChance();
        }
    }
}
