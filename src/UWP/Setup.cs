using MvvmCross;
using MvvmCross.Platforms.Uap.Core;
using MvvmCross.ViewModels;
using OSDP.Net;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.Services;
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
            var panel = new ControlPanel();

            Mvx.IoCProvider.RegisterSingleton<ISerialPortConnection>(new UwpSerialPort());
            Mvx.IoCProvider.RegisterSingleton(panel);
            Mvx.IoCProvider.RegisterSingleton<IDeviceManagementService>(new DeviceManagementService(panel));

            base.InitializeFirstChance();
        }
    }
}
