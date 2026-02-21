using System.Windows;
using System.Windows.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using NUnit.Framework;

namespace OSDPBench.UI.Tests;

/// <summary>
/// Assembly-level setup that creates a single TestApp instance shared by all test fixtures.
/// WPF only allows one Application per AppDomain, so this must be created once.
/// </summary>
[SetUpFixture]
public class AssemblySetup
{
    internal static TestApp? App { get; private set; }
    internal static UIA3Automation? Automation { get; private set; }
    internal static FlaUI.Core.AutomationElements.Window? MainWindow { get; private set; }
    private static Thread? _staThread;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Automation = new UIA3Automation();

        var appStarted = new ManualResetEventSlim(false);
        Exception? startupException = null;

        _staThread = new Thread(() =>
        {
            try
            {
                App = new TestApp();
                App.StartTestApp();

                // Poll for the main window to appear
                App.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
                {
                    var timer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(100)
                    };
                    timer.Tick += (_, _) =>
                    {
                        if (App.MainWindow is System.Windows.Window window)
                        {
                            timer.Stop();
                            if (window.IsLoaded)
                            {
                                appStarted.Set();
                            }
                            else
                            {
                                window.ContentRendered += (_, _) => appStarted.Set();
                            }
                        }
                    };
                    timer.Start();
                });

                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                startupException = ex;
                appStarted.Set();
            }
        });

        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.IsBackground = true;
        _staThread.Start();

        if (!appStarted.Wait(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("Application main window did not appear within 30 seconds.");
        }

        if (startupException != null)
        {
            throw new InvalidOperationException("Application failed to start.", startupException);
        }

        // Small delay for UI to stabilize
        Thread.Sleep(500);

        // Find the main window via FlaUI
        var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
        var desktop = Automation.GetDesktop();
        var windows = Retry(() =>
        {
            var found = desktop.FindAllChildren(cf =>
                cf.ByProcessId(processId)
                    .And(cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window)));
            return found.Length > 0 ? found : null;
        }, TimeSpan.FromSeconds(10));

        MainWindow = windows?.FirstOrDefault()?.AsWindow();
        if (MainWindow == null)
        {
            throw new InvalidOperationException("Could not find the main application window via FlaUI.");
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Automation?.Dispose();

        if (App != null && _staThread != null)
        {
            App.Dispatcher.InvokeAsync(async () =>
            {
                await App.StopTestApp();
                App.Dispatcher.InvokeShutdown();
            });

            _staThread.Join(TimeSpan.FromSeconds(10));
        }
    }

    internal static T? Retry<T>(Func<T?> func, TimeSpan timeout) where T : class
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var result = func();
            if (result != null) return result;
            Thread.Sleep(100);
        }
        return null;
    }
}

/// <summary>
/// Base class for FlaUI-based UI tests. Uses the shared application instance
/// created by <see cref="AssemblySetup"/>.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public abstract class UITestBase
{
    protected FlaUI.Core.AutomationElements.Window? MainWindow => AssemblySetup.MainWindow;
    protected TestApp TestApp => AssemblySetup.App!;
    protected UIA3Automation Automation => AssemblySetup.Automation!;

    /// <summary>
    /// Finds an element by its AutomationId within the main window.
    /// </summary>
    protected AutomationElement? FindByAutomationId(string automationId)
    {
        return MainWindow?.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
    }

    /// <summary>
    /// Waits for an element with the specified AutomationId to appear.
    /// </summary>
    protected AutomationElement? WaitForElement(string automationId, TimeSpan? timeout = null)
    {
        return AssemblySetup.Retry(
            () => FindByAutomationId(automationId),
            timeout ?? TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Invokes an action on the application's UI thread and waits for completion.
    /// </summary>
    protected void InvokeOnUI(Action action)
    {
        TestApp.Dispatcher.Invoke(action);
    }

    /// <summary>
    /// Invokes a function on the application's UI thread and waits for completion.
    /// </summary>
    protected T InvokeOnUI<T>(Func<T> func)
    {
        return TestApp.Dispatcher.Invoke(func);
    }
}
