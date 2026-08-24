using NUnit.Framework;
using OSDPBench.Core.ViewModels.Windows;

namespace OSDPBench.UI.Tests;

/// <summary>
/// Proves the shell's navigation rules reach the actual navigation items. The bindings live on
/// elements inside <c>NavigationView.MenuItems</c>, where inherited DataContext is not something to
/// take on trust, so these assert the rendered controls rather than the view model.
/// </summary>
[TestFixture]
public class NavigationGatingTests : UiTestBase
{
    [TearDown]
    public void RestoreUnrestrictedNavigation()
    {
        // The view model and the mocks behind it are singletons shared across the assembly.
        TestApp.MockLineQuality.Setup(service => service.IsBusy).Returns(false);
        TestApp.MockDeviceManagement.Setup(service => service.IsConnected).Returns(false);
        TestApp.MockDeviceManagement.Setup(service => service.IsPassiveMonitoring).Returns(false);
        TestApp.MockDeviceManagement.Setup(service => service.IsPortInUse).Returns(false);
        RaiseLineQualityBusyChanged();
        RaisePortInUseChanged();
    }

    [Test]
    public void AllPagesAreReachableWhenNothingHoldsThePort()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IsNavItemEnabled("NavItem_Connect"), Is.True);
            Assert.That(IsNavItemEnabled("NavItem_Manage"), Is.True);
            Assert.That(IsNavItemEnabled("NavItem_Monitor"), Is.True);
            Assert.That(IsNavItemEnabled("NavItem_LineQuality"), Is.True);
            Assert.That(IsNavItemEnabled("NavItem_Info"), Is.True);
        });
    }

    [Test]
    public void ALineQualityRunClosesEveryOtherPage()
    {
        TestApp.MockLineQuality.Setup(service => service.IsBusy).Returns(true);
        RaiseLineQualityBusyChanged();

        Assert.Multiple(() =>
        {
            Assert.That(IsNavItemEnabled("NavItem_Connect"), Is.False);
            Assert.That(IsNavItemEnabled("NavItem_Manage"), Is.False);
            Assert.That(IsNavItemEnabled("NavItem_Monitor"), Is.False);
            Assert.That(IsNavItemEnabled("NavItem_Info"), Is.False);

            // The page the run is on has to stay reachable, or its Cancel button becomes
            // unreachable the moment the user clicks elsewhere.
            Assert.That(IsNavItemEnabled("NavItem_LineQuality"), Is.True);
        });
    }

    [Test]
    public void AConnectedDeviceClosesTheLineQualityPage()
    {
        TestApp.MockDeviceManagement.Setup(service => service.IsConnected).Returns(true);
        TestApp.MockDeviceManagement.Setup(service => service.IsPortInUse).Returns(true);
        RaisePortInUseChanged();

        Assert.Multiple(() =>
        {
            Assert.That(IsNavItemEnabled("NavItem_LineQuality"), Is.False,
                "The test needs the port to itself.");
            Assert.That(IsNavItemEnabled("NavItem_Manage"), Is.True,
                "A connection does not restrict the other pages.");
        });
    }

    [Test]
    public void ADiscoverySweepAlsoClosesTheLineQualityPage()
    {
        // A sweep holds the port with IsConnected still false, which is exactly the case the
        // narrower check used to miss.
        TestApp.MockDeviceManagement.Setup(service => service.IsConnected).Returns(false);
        TestApp.MockDeviceManagement.Setup(service => service.IsPortInUse).Returns(true);
        RaisePortInUseChanged();

        Assert.That(IsNavItemEnabled("NavItem_LineQuality"), Is.False);
    }

    [Test]
    public void DisabledItemsExplainWhy()
    {
        var viewModel = InvokeOnUI(() => TestApp.GetService<MainWindowViewModel>());

        TestApp.MockDeviceManagement.Setup(service => service.IsPortInUse).Returns(true);
        RaisePortInUseChanged();

        Assert.That(InvokeOnUI(() => viewModel.LineQualityDisabledReason),
            Is.Not.Null.And.Not.Empty,
            "A disabled item that gives no reason just looks broken.");

        // WPF surfaces ToolTip as the automation HelpText, which is what a screen reader reads and
        // what ToolTipService.ShowOnDisabled puts in front of a mouse user.
        var navItem = WaitForElement("NavItem_LineQuality");
        Assert.That(navItem!.HelpText, Is.EqualTo(InvokeOnUI(() => viewModel.LineQualityDisabledReason)));
    }

    private bool IsNavItemEnabled(string automationId)
    {
        var navItem = WaitForElement(automationId);
        Assert.That(navItem, Is.Not.Null, $"Navigation item '{automationId}' should exist.");
        return navItem!.IsEnabled;
    }

    private void RaiseLineQualityBusyChanged() => InvokeOnUI(() =>
        TestApp.MockLineQuality.Raise(service => service.BusyChanged += null,
            TestApp.MockLineQuality.Object, EventArgs.Empty));

    private void RaisePortInUseChanged() => InvokeOnUI(() =>
        TestApp.MockDeviceManagement.Raise(service => service.PortInUseChanged += null,
            TestApp.MockDeviceManagement.Object, EventArgs.Empty));
}
