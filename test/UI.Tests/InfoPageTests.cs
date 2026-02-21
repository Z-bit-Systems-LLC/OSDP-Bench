using FlaUI.Core.Definitions;
using NUnit.Framework;

namespace OSDPBench.UI.Tests;

[TestFixture]
public class InfoPageTests : UiTestBase
{
    [SetUp]
    public void NavigateToInfoPage()
    {
        NavigateToPage("NavItem_Info", "LanguageComboBox");
    }

    [Test]
    public void InfoPageLoadsSuccessfully()
    {
        Assert.That(MainWindow!.IsAvailable, Is.True,
            "Main window should be available after navigating to Info page.");
    }

    [Test]
    public void LanguageComboBoxExists()
    {
        var languageComboBox = WaitForElement("LanguageComboBox");
        Assert.That(languageComboBox, Is.Not.Null,
            "LanguageComboBox should exist on the Info page.");
    }

    [Test]
    public void ApplicationLogoExists()
    {
        // The logo image uses AutomationProperties.Name via localization
        var logo = MainWindow?.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Image));
        Assert.That(logo, Is.Not.Null,
            "Application logo image should exist on the Info page.");
    }
}
