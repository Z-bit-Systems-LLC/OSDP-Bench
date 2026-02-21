using NUnit.Framework;

namespace OSDPBench.UI.Tests;

[TestFixture]
[Category("Smoke")]
public class SmokeTests : UiTestBase
{
    [Test]
    public void ApplicationLaunches()
    {
        Assert.That(MainWindow, Is.Not.Null, "Main window should exist.");
        Assert.That(MainWindow!.IsAvailable, Is.True, "Main window should be available.");
    }

    [Test]
    public void MainWindowHasNavigationItems()
    {
        var connectNav = FindByAutomationId("NavItem_Connect");
        var manageNav = FindByAutomationId("NavItem_Manage");
        var monitorNav = FindByAutomationId("NavItem_Monitor");
        var infoNav = FindByAutomationId("NavItem_Info");

        Assert.Multiple(() =>
        {
            Assert.That(connectNav, Is.Not.Null, "Connect navigation item should exist.");
            Assert.That(manageNav, Is.Not.Null, "Manage navigation item should exist.");
            Assert.That(monitorNav, Is.Not.Null, "Monitor navigation item should exist.");
            Assert.That(infoNav, Is.Not.Null, "Info navigation item should exist.");
        });
    }

    [Test]
    public void NavigateToEachPage()
    {
        var navIds = new[] { "NavItem_Manage", "NavItem_Monitor", "NavItem_Info", "NavItem_Connect" };

        foreach (var navId in navIds)
        {
            NavigateToPage(navId);

            // Verify the window is still responsive after navigation
            Assert.That(MainWindow!.IsAvailable, Is.True,
                $"Main window should remain available after navigating to '{navId}'.");
        }
    }
}
