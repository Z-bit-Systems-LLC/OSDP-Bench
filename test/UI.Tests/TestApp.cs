using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using OSDP.Net.LineQuality;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Windows;
using OSDPBench.Windows.Services;

namespace OSDPBench.UI.Tests;

/// <summary>
/// Test application that replaces hardware-dependent services with mocks.
/// Does not call InitializeComponent to avoid XAML resource loading issues
/// across assemblies — instead loads resource dictionaries manually.
/// </summary>
public class TestApp : App
{
    public Mock<IDeviceManagementService> MockDeviceManagement { get; } = new();
    public Mock<ISerialPortConnectionService> MockSerialPortConnection { get; } = new();
    public Mock<IUsbDeviceMonitorService> MockUsbDeviceMonitor { get; } = new();
    public Mock<IDialogService> MockDialog { get; } = new();
    public Mock<ILanguageMismatchService> MockLanguageMismatch { get; } = new();
    public Mock<IUserSettingsService> MockUserSettings { get; } = new();
    public Mock<ILocalizationService> MockLocalization { get; } = new();
    public Mock<ILineQualityService> MockLineQuality { get; } = new();

    private IHost? _testHost;

    /// <summary>
    /// Gets a registered service from the test host.
    /// </summary>
    public new T GetService<T>() where T : class
    {
        return (_testHost!.Services.GetService(typeof(T)) as T)!;
    }

    public TestApp()
    {
        ConfigureMockDefaults();
    }

    /// <summary>
    /// Restores the line quality mock's default behaviour.
    /// </summary>
    /// <remarks>
    /// Tests that reconfigure this mock must call this rather than <c>Mock.Reset()</c>. Reset also
    /// discards the event subscriptions the mock is holding, and MainWindowViewModel subscribes to
    /// BusyChanged once at startup: reset it and navigation gating silently stops working for
    /// every test that runs afterwards.
    /// </remarks>
    public void RestoreLineQualityDefaults()
    {
        MockLineQuality.Setup(s => s.IsBusy).Returns(false);
        MockLineQuality.Setup(s => s.IsTestRunning).Returns(false);
        MockLineQuality.Setup(s => s.IsResponderRunning).Returns(false);
        MockLineQuality.Setup(s => s.IsSupported(It.IsAny<string>())).Returns(true);
        MockLineQuality
            .Setup(s => s.RunTestAsync(It.IsAny<string>(), It.IsAny<LineQualityOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => throw new OperationCanceledException());
    }

    private void ConfigureMockDefaults()
    {
        RestoreLineQualityDefaults();

        MockSerialPortConnection
            .Setup(s => s.FindAvailableSerialPorts())
            .ReturnsAsync(new List<AvailableSerialPort>
            {
                new("COM3", "COM3", "Test Serial Port 1"),
                new("COM4", "COM4", "Test Serial Port 2")
            });

        MockUsbDeviceMonitor
            .Setup(m => m.IsMonitoring)
            .Returns(false);

        MockLanguageMismatch
            .Setup(s => s.CheckAndPromptForLanguageMismatchAsync())
            .Returns(Task.CompletedTask);

        MockLocalization
            .Setup(s => s.SupportedCultures)
            .Returns(new List<System.Globalization.CultureInfo>
            {
                new("en-US")
            });

        MockLocalization
            .Setup(s => s.CurrentCulture)
            .Returns(new System.Globalization.CultureInfo("en-US"));
    }

    /// <summary>
    /// Loads the application resource dictionaries that would normally be
    /// loaded by InitializeComponent via App.xaml.
    /// </summary>
    private void LoadResources()
    {
        Resources = new ResourceDictionary();
        Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ThemesDictionary());
        Resources.MergedDictionaries.Add(new Wpf.Ui.Markup.ControlsDictionary());
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/ZBitSystems.Wpf.UI;component/Styles/DesignTokens.xaml")
        });
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/ZBitSystems.Wpf.UI;component/Styles/ThemeSemanticColors.xaml")
        });
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/ZBitSystems.Wpf.UI;component/Styles/ComponentStyles.xaml")
        });
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/OSDPBench;component/Styles/LayoutTemplates.xaml")
        });
    }

    /// <summary>
    /// Starts the test application with mocked services.
    /// </summary>
    public void StartTestApp()
    {
        ZBitSystems.Wpf.UI.Localization.LocalizationService.Provider =
            new ResourceLocalizationProvider();

        LoadResources();

        _testHost = BuildHost(services =>
        {
            // Replace hardware-dependent services with mocks
            services.AddSingleton(MockDeviceManagement.Object);
            services.AddSingleton(MockSerialPortConnection.Object);
            services.AddSingleton(MockUsbDeviceMonitor.Object);
            services.AddSingleton(MockDialog.Object);
            services.AddSingleton(MockLanguageMismatch.Object);
            services.AddSingleton(MockUserSettings.Object);
            services.AddSingleton(MockLocalization.Object);
            services.AddSingleton(MockLineQuality.Object);
        });

        _testHost.Start();
    }

    /// <summary>
    /// Shuts down the test application.
    /// </summary>
    public async Task StopTestApp()
    {
        if (_testHost != null)
        {
            await _testHost.StopAsync();
            _testHost.Dispose();
        }
    }
}
