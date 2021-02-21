using MvvmCross;
using MvvmCross.Platforms.Uap.Core;
using MvvmCross.ViewModels;
using OSDPBench.Core.Platforms;
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
            Mvx.IoCProvider.RegisterSingleton<ISerialPortConnection>(new UwpSerialPort());

            base.InitializeFirstChance();
        }
    }
}
