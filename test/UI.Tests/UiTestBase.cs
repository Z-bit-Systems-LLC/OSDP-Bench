using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using AutomationProperties = System.Windows.Automation.AutomationProperties;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Window = FlaUI.Core.AutomationElements.Window;

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
    internal static Window? MainWindow { get; private set; }
    private static Thread? _staThread;
    private static bool _appIsUntouched = true;
    private static IntPtr _mainWindowHandle;

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
                        if (App.MainWindow is not null)
                        {
                            timer.Stop();
                            if (App.MainWindow.IsLoaded)
                            {
                                appStarted.Set();
                            }
                            else
                            {
                                App.MainWindow.ContentRendered += (_, _) => appStarted.Set();
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

        MainWindow = FindMainWindow();
    }

    /// <summary>
    /// Replaces the host, the mocks and the main window with fresh ones, so one fixture's leftover
    /// state cannot fail the next. The first fixture skips this: the app it gets is already new.
    /// </summary>
    internal static void EnsureFreshApp()
    {
        if (_appIsUntouched)
        {
            _appIsUntouched = false;
            return;
        }

        if (!App!.Dispatcher.InvokeAsync(App.RestartTestApp).Task.Unwrap().Wait(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("Application did not restart within 30 seconds.");
        }

        var loaded = Retry(() => App.Dispatcher.Invoke(() => App.MainWindow is { IsLoaded: true }) ? "loaded" : null,
            TimeSpan.FromSeconds(30));
        if (loaded == null)
        {
            throw new TimeoutException("Restarted main window did not load within 30 seconds.");
        }

        MainWindow = FindMainWindow();
    }

    /// <summary>
    /// Finds the app's current main window through UI Automation and keeps it above other windows,
    /// so a window that opens elsewhere on the desktop cannot take the clicks a test sends.
    /// </summary>
    private static Window FindMainWindow()
    {
        // Small delay for UI to stabilize
        Thread.Sleep(500);

        var handle = App!.Dispatcher.Invoke(() =>
        {
            // Fully on the primary screen, wherever the default placement would put it
            var window = App.MainWindow!;
            window.Left = SystemParameters.WorkArea.Left;
            window.Top = SystemParameters.WorkArea.Top;
            window.Topmost = true;
            return new WindowInteropHelper(window).Handle;
        });

        _mainWindowHandle = handle;
        BringToFront();

        return Retry(() => Automation!.FromHandle(handle).AsWindow(), TimeSpan.FromSeconds(10))
               ?? throw new InvalidOperationException("Could not find the main application window via FlaUI.");
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

    /// <summary>
    /// Makes the main window the foreground window.
    /// </summary>
    /// <remarks>
    /// Windows refuses a foreground change from a process that did not receive the last input, and
    /// the test host never does: the request fails silently and every click lands on whatever
    /// window is in front. Joining the foreground window's input queue and tapping Alt are the two
    /// conditions Windows accepts in its place.
    /// </remarks>
    internal static void BringToFront()
    {
        var handle = _mainWindowHandle;
        if (handle == IntPtr.Zero || GetForegroundWindow() == handle) return;

        var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        var currentThread = GetCurrentThreadId();
        var attached = foregroundThread != currentThread && AttachThreadInput(currentThread, foregroundThread, true);

        try
        {
            keybd_event(VkMenu, 0, 0, UIntPtr.Zero);
            keybd_event(VkMenu, 0, KeyEventKeyUp, UIntPtr.Zero);
            ShowWindow(handle, SwRestore);
            BringWindowToTop(handle);
            SetForegroundWindow(handle);
        }
        finally
        {
            if (attached) AttachThreadInput(currentThread, foregroundThread, false);
        }
    }

    /// <summary>
    /// Gets the handle of the window that currently holds the foreground.
    /// </summary>
    internal static IntPtr ForegroundWindow => GetForegroundWindow();

    private const byte VkMenu = 0x12;
    private const uint KeyEventKeyUp = 0x0002;
    private const int SwRestore = 9;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

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
public abstract class UiTestBase
{
    [OneTimeSetUp]
    public void StartFromAFreshApp()
    {
        AssemblySetup.EnsureFreshApp();
    }

    [SetUp]
    public void BringMainWindowToFront()
    {
        // A test clicks with the real mouse, which lands on whatever window is on top
        AssemblySetup.BringToFront();
    }

    [TearDown]
    public void RecordFailure()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed) return;

        try
        {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory,
                $"{TestContext.CurrentContext.Test.Name}.png");
            Capture.Screen().ToFile(path);
            TestContext.AddTestAttachment(path, "The screen when the test failed");
        }
        catch (Exception exception)
        {
            TestContext.Out.WriteLine($"Could not capture the screen: {exception.Message}");
        }

        TestContext.Out.WriteLine(DescribeDesktop());
    }

    /// <summary>
    /// Describes what a click would have landed on: the screen, the main window, the window that
    /// holds the foreground and the element under the mouse.
    /// </summary>
    private string DescribeDesktop()
    {
        var lines = new List<string>
        {
            $"Screen: {SystemParameters.PrimaryScreenWidth}x{SystemParameters.PrimaryScreenHeight}, " +
            $"work area {SystemParameters.WorkArea}"
        };

        lines.Add(InvokeOnUI(DescribeInputState));
        lines.Add(Describe("Main window", () => MainWindow));
        lines.Add(Describe("Foreground window", () => Automation.FromHandle(AssemblySetup.ForegroundWindow)));
        lines.Add(Describe($"Under the mouse at {Mouse.Position}", () => Automation.FromPoint(Mouse.Position)));

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Describes the app's own view of input: which element holds the mouse, which window is
    /// active and how many popups are open. A held capture or a stray open popup swallows every
    /// click sent to the window.
    /// </summary>
    private static string DescribeInputState()
    {
        var captured = System.Windows.Input.Mouse.Captured;
        var capturedName = captured is FrameworkElement { Name.Length: > 0 } named ? $" '{named.Name}'" : "";

        var openPopups = PresentationSource.CurrentSources.OfType<HwndSource>()
            .Count(source => source.RootVisual?.GetType().Name == "PopupRoot");

        var activeWindow = Application.Current.Windows.OfType<System.Windows.Window>()
            .FirstOrDefault(window => window.IsActive);

        return $"Mouse capture: {captured?.GetType().Name ?? "none"}{capturedName}; " +
               $"active window: {activeWindow?.GetType().Name ?? "none"}; " +
               $"topmost: {Application.Current.MainWindow?.Topmost}; " +
               $"open popups: {openPopups}";
    }

    private static string Describe(string label, Func<AutomationElement?> find)
    {
        try
        {
            var element = find();
            if (element == null) return $"{label}: none";

            string process;
            try
            {
                process = System.Diagnostics.Process.GetProcessById(element.Properties.ProcessId).ProcessName;
            }
            catch (ArgumentException)
            {
                process = "exited";
            }

            return $"{label}: '{element.Properties.Name.ValueOrDefault}' " +
                   $"[{element.Properties.AutomationId.ValueOrDefault}] in {process}, " +
                   $"bounds {element.BoundingRectangle}, offscreen {element.IsOffscreen}, " +
                   $"enabled {element.IsEnabled}";
        }
        catch (Exception exception)
        {
            return $"{label}: unavailable ({exception.Message})";
        }
    }

    protected Window? MainWindow => AssemblySetup.MainWindow;
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
    /// Scrolls the element with the given AutomationId into view within the main window.
    /// </summary>
    /// <remarks>
    /// UI Automation reports an element below the bottom of a scrolled page as on screen, and a
    /// click sent to it lands on whatever lies under those coordinates: another window, or the
    /// taskbar on a small build agent screen.
    /// </remarks>
    protected void ScrollIntoView(string automationId)
    {
        InvokeOnUI(() => FindInVisualTree(Application.Current.MainWindow, automationId)?.BringIntoView());

        // Layout runs above background priority, so this returns once the scroll has been applied
        TestApp.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Clicks the element after scrolling it into view, and fails instead of clicking when it
    /// still lies outside the main window, where the click would land on something else.
    /// </summary>
    /// <remarks>
    /// Elements in a popup are skipped by the bounds check: a popup is its own window and may
    /// extend past the main window's edge.
    /// </remarks>
    protected void ClickInView(AutomationElement element)
    {
        var automationId = element.Properties.AutomationId.ValueOrDefault;
        var inMainWindow = !string.IsNullOrEmpty(automationId) &&
                           InvokeOnUI(() => FindInVisualTree(Application.Current.MainWindow, automationId) != null);

        if (inMainWindow)
        {
            ScrollIntoView(automationId!);

            Assert.That(MainWindow!.BoundingRectangle.Contains(element.GetClickablePoint()), Is.True,
                $"{automationId} lies outside the main window, so a click would land elsewhere.");
        }

        element.Click();
    }

    private static FrameworkElement? FindInVisualTree(DependencyObject? parent, string automationId)
    {
        if (parent == null) return null;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement element && AutomationProperties.GetAutomationId(element) == automationId)
            {
                return element;
            }

            var found = FindInVisualTree(child, automationId);
            if (found != null) return found;
        }

        return null;
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

    /// <summary>
    /// Clicks a navigation item and waits for a known element on the target page to appear.
    /// </summary>
    protected void NavigateToPage(string navItemId, string pageElementId)
    {
        var navItem = WaitForElement(navItemId);
        navItem?.Click();
        WaitForElement(pageElementId);
    }

    /// <summary>
    /// Clicks a navigation item and waits for the UI thread to finish processing.
    /// Use when the target page has no always-visible element to wait for.
    /// </summary>
    protected void NavigateToPage(string navItemId)
    {
        var navItem = WaitForElement(navItemId);
        navItem?.Click();
        InvokeOnUI(() => { }); // Flush the dispatcher queue
    }
}
