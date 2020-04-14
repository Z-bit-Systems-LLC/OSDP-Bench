using Moq;
using MvvmCross.Base;
using MvvmCross.Tests;
using MvvmCross.Views;
using NUnit.Framework;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;
using OSDPBench.Core.ViewModels;

namespace OSDPBench.Core.Tests.ViewModels
{
    public class MainViewModelTests : MvxIoCSupportingTest
    {
        protected MockDispatcher MockDispatcher { get; private set; }

        protected override void AdditionalSetup()
        {
            MockDispatcher = new MockDispatcher();
            Ioc.RegisterSingleton<IMvxViewDispatcher>(MockDispatcher);
            Ioc.RegisterSingleton<IMvxMainThreadAsyncDispatcher>(MockDispatcher);
        }

        [Test]
        public void MainViewModel_InitializedAvailableBaudRates()
        {
            Setup();

            var serialPort = new Mock<ISerialPort>();
            Ioc.RegisterSingleton(serialPort.Object);

            var mainViewModel = Ioc.IoCConstruct<MainViewModel>();

            Assert.AreEqual(6, mainViewModel.AvailableBaudRates.Count);
        }

        [Test]
        public void MainViewModel_InitializedAvailableSerialPorts()
        {
            Setup();

            var serialPort = new Mock<ISerialPort>();
            serialPort.Setup(expression => expression.FindAvailableSerialPorts())
                .ReturnsAsync(new[] {new SerialPort("test1", "desc1"), new SerialPort("test2", "desc2")});
            Ioc.RegisterSingleton(serialPort.Object);

            var mainViewModel = Ioc.IoCConstruct<MainViewModel>();
            mainViewModel.ViewAppeared();

            Assert.AreEqual(2, mainViewModel.AvailableSerialPorts.Count);
        }
    }
}