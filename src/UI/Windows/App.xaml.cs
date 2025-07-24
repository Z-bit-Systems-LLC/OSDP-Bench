using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OSDPBench.Core.Services;
using OSDPBench.Core.ViewModels.Pages;
using OSDPBench.Core.ViewModels.Windows;
using OSDPBench.Windows.Services;
using OSDPBench.Windows.Views.Pages;
using OSDPBench.Windows.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;

namespace OSDPBench.Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost Host = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddHostedService<ApplicationHostService>();

            // Theme manipulation
            services.AddSingleton<IThemeService, ThemeService>();

            // TaskBar manipulation
            services.AddSingleton<ITaskBarService, TaskBarService>();

            // Service containing navigation, same as INavigationWindow... but without window
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<INavigationViewPageProvider, PageService>();

            // Main window with navigation
            services.AddSingleton<INavigationWindow, MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<ConnectPage>();
            services.AddSingleton<ConnectViewModel>();
            services.AddSingleton<ManagePage>();
            services.AddSingleton<ManageViewModel>();
            services.AddSingleton<MonitorPage>();
            services.AddSingleton<MonitorViewModel>();
            services.AddSingleton<InfoPage>();

            services.AddSingleton<IDeviceManagementService, DeviceManagementService>();
            services.AddSingleton<IDialogService, WindowsDialogService>();
            services.AddSingleton<ISerialPortConnectionService, WindowsSerialPortConnectionService>();
            services.AddSingleton<IUsbDeviceMonitorService, WindowsUsbDeviceMonitorService>();
            services.AddSingleton<IUserSettingsService, WindowsUserSettingsService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<ILanguageMismatchService, WindowsLanguageMismatchService>();
        }).Build();

    /// <summary>
    /// Gets registered service.
    /// </summary>
    /// <typeparam name="T">Type of the service to get.</typeparam>
    /// <returns>Instance of the service or <see langword="null"/>.</returns>
    public static T GetService<T>()
        where T : class
    {
        return (Host.Services.GetService(typeof(T)) as T)!;
    }

    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        Host.Start();
        
        // Initialize user settings before other services
        var userSettingsService = Host.Services.GetService<IUserSettingsService>();
        if (userSettingsService != null)
        {
            await userSettingsService.LoadAsync();
        }
        
        // Initialize the localization service to apply saved culture
        var localizationService = Host.Services.GetService<ILocalizationService>();
        if (localizationService != null && userSettingsService?.Settings.PreferredCulture != null)
        {
            try
            {
                localizationService.ChangeCulture(userSettingsService.Settings.PreferredCulture);
            }
            catch
            {
                // If culture loading fails, continue with system default
            }
        }
        
        Host.Services.GetService<ManageViewModel>();
        Host.Services.GetService<MonitorViewModel>();
        
        // Check for language mismatch after UI is initialized
        _ = Task.Run(async () =>
        {
            // Small delay to ensure UI is fully loaded
            await Task.Delay(500);
            
            // Run on UI thread to ensure proper dialog display and culture updates
            await Current.Dispatcher.InvokeAsync(async () =>
            {
                var languageMismatchService = Host.Services.GetService<ILanguageMismatchService>();
                if (languageMismatchService != null)
                {
                    await languageMismatchService.CheckAndPromptForLanguageMismatchAsync();
                }
            });
        });
    }

    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    private async void OnExit(object sender, ExitEventArgs e)
    {
        // Dispose of services that need explicit cleanup
        var connectViewModel = Host.Services.GetService<ConnectViewModel>();
        connectViewModel?.Dispose();
        
        var usbMonitor = Host.Services.GetService<IUsbDeviceMonitorService>();
        usbMonitor?.Dispose();
        
        await Host.StopAsync();

        Host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
    }
}