using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using OSDP.Net.LineQuality;
using OSDPBench.Core.Models;
using OSDPBench.Core.Services;
using OSDPBench.Core.Services.Analysis;
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
    public Mock<IDeviceManagementService> MockDeviceManagement { get; private set; } = new();
    public Mock<ISerialPortConnectionService> MockSerialPortConnection { get; private set; } = new();
    public Mock<IUsbDeviceMonitorService> MockUsbDeviceMonitor { get; private set; } = new();
    public Mock<IDialogService> MockDialog { get; private set; } = new();
    public Mock<ILanguageMismatchService> MockLanguageMismatch { get; private set; } = new();
    public Mock<IUserSettingsService> MockUserSettings { get; private set; } = new();
    public Mock<ILocalizationService> MockLocalization { get; private set; } = new();
    public Mock<ILineQualityService> MockLineQuality { get; private set; } = new();

    private IHost? _testHost;

    /// <summary>
    /// Gets a registered service from the test host.
    /// </summary>
    public new T GetService<T>() where T : class
    {
        return (_testHost!.Services.GetService(typeof(T)) as T)!;
    }

    /// <summary>
    /// The settings folder the test app reads and writes in place of the user's own, so window
    /// state saved by one run cannot change the layout the next run starts from.
    /// </summary>
    private const string SettingsAppName = "OSDPBench.UITests";

    public TestApp()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ConfigureMockDefaults();
    }

    /// <summary>
    /// Replaces every mock with a fresh one, so nothing a previous fixture set up or subscribed
    /// carries over into the next host.
    /// </summary>
    private void ReplaceMocks()
    {
        MockDeviceManagement = new Mock<IDeviceManagementService>();
        MockSerialPortConnection = new Mock<ISerialPortConnectionService>();
        MockUsbDeviceMonitor = new Mock<IUsbDeviceMonitorService>();
        MockDialog = new Mock<IDialogService>();
        MockLanguageMismatch = new Mock<ILanguageMismatchService>();
        MockUserSettings = new Mock<IUserSettingsService>();
        MockLocalization = new Mock<ILocalizationService>();
        MockLineQuality = new Mock<ILineQualityService>();

        ConfigureMockDefaults();
    }

    /// <summary>
    /// Closes the main window and discards the host behind it, then starts a new host with fresh
    /// mocks and a new main window. Every singleton view model is rebuilt along with it.
    /// </summary>
    public async Task RestartTestApp()
    {
        foreach (var window in Windows.OfType<Window>().ToList())
        {
            window.Close();
        }

        await StopTestApp();
        ReplaceMocks();
        StartTestApp();
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
        ClearSettings();

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

            // The main window restores and saves its size and position through this service
            services.RemoveAll<AppUserSettingsService>();
            services.AddSingleton(new AppUserSettingsService(SettingsAppName));

            // Test the app as the default build has it, without capture analysis, whatever this build was given
            services.RemoveAll<ICaptureAnalysisLauncher>();
        });

        _testHost.Start();

        // A window shown after the first one is not made the main window on its own
        MainWindow = Windows.OfType<OSDPBench.Windows.Views.Windows.MainWindow>().Single();
    }

    /// <summary>
    /// Deletes the test settings, so every host starts from the default window size and position
    /// whatever an earlier run saved.
    /// </summary>
    private static void ClearSettings()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), SettingsAppName);

        try
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        catch (IOException)
        {
            // A settings file left from an earlier run only changes where the window opens
        }
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
            _testHost = null;
        }
    }
}
