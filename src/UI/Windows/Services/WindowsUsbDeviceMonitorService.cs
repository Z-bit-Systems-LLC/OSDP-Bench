using System.IO.Ports;
using System.Management;
using OSDPBench.Core.Services;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Windows implementation of the USB device monitoring service using WMI events and polling.
/// </summary>
public sealed class WindowsUsbDeviceMonitorService : IUsbDeviceMonitorService
{
    private readonly SynchronizationContext? _synchronizationContext;
    private ManagementEventWatcher? _deviceInsertWatcher;
    private ManagementEventWatcher? _deviceRemoveWatcher;
    private Timer? _pollingTimer;
    private HashSet<string> _previousPorts;
    private bool _isDisposed;
    private readonly object _lock = new();
    
    /// <inheritdoc />
    public event EventHandler<UsbDeviceChangedEventArgs>? UsbDeviceChanged;
    
    /// <inheritdoc />
    public bool IsMonitoring { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsUsbDeviceMonitorService"/> class.
    /// </summary>
    public WindowsUsbDeviceMonitorService()
    {
        _synchronizationContext = SynchronizationContext.Current;
        _previousPorts = new HashSet<string>(SerialPort.GetPortNames());
    }
    
    /// <inheritdoc />
    public void StartMonitoring()
    {
        lock (_lock)
        {
            if (IsMonitoring) return;
            
            try
            {
                // Start WMI monitoring for USB device changes
                StartWmiMonitoring();
                
                // Start polling as a fallback (every 2 seconds)
                _pollingTimer = new Timer(OnPollingTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
                
                IsMonitoring = true;
            }
            catch (Exception)
            {
                _pollingTimer ??= new Timer(OnPollingTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
                
                IsMonitoring = true;
            }
        }
    }
    
    /// <inheritdoc />
    public void StopMonitoring()
    {
        lock (_lock)
        {
            if (!IsMonitoring) return;
            
            StopWmiMonitoring();
            
            _pollingTimer?.Dispose();
            _pollingTimer = null;
            
            IsMonitoring = false;
        }
    }

    private void StartWmiMonitoring()
    {
        // Query for USB serial port device insertion
        var insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 " +
                                            "WHERE TargetInstance ISA 'Win32_PnPEntity' " +
                                            "AND (TargetInstance.PNPClass = 'Ports' OR TargetInstance.PNPClass = 'USB')");

        _deviceInsertWatcher = new ManagementEventWatcher(insertQuery);
        _deviceInsertWatcher.EventArrived += OnDeviceInserted;
        _deviceInsertWatcher.Start();

        // Query for USB serial port device removal
        var removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 " +
                                            "WHERE TargetInstance ISA 'Win32_PnPEntity' " +
                                            "AND (TargetInstance.PNPClass = 'Ports' OR TargetInstance.PNPClass = 'USB')");

        _deviceRemoveWatcher = new ManagementEventWatcher(removeQuery);
        _deviceRemoveWatcher.EventArrived += OnDeviceRemoved;
        _deviceRemoveWatcher.Start();
    }

    private void StopWmiMonitoring()
    {
        _deviceInsertWatcher?.Stop();
        _deviceInsertWatcher?.Dispose();
        _deviceInsertWatcher = null;

        _deviceRemoveWatcher?.Stop();
        _deviceRemoveWatcher?.Dispose();
        _deviceRemoveWatcher = null;
    }

    private void OnDeviceInserted(object sender, EventArrivedEventArgs e)
    {
        Task.Run(() => CheckForPortChanges(UsbDeviceChangeType.Connected));
    }
    
    private void OnDeviceRemoved(object sender, EventArrivedEventArgs e)
    {
        Task.Run(() => CheckForPortChanges(UsbDeviceChangeType.Disconnected));
    }
    
    private void OnPollingTimerElapsed(object? state)
    {
        CheckForPortChanges(UsbDeviceChangeType.Unknown);
    }

    private void CheckForPortChanges(UsbDeviceChangeType suggestedChangeType)
    {
        var currentPorts = new HashSet<string>(SerialPort.GetPortNames());

        lock (_lock)
        {
            // Check if there are any changes
            var addedPorts = currentPorts.Except(_previousPorts).ToArray();
            var removedPorts = _previousPorts.Except(currentPorts).ToArray();

            if (addedPorts.Any() && removedPorts.Any()) return;

            // Determine the actual change type
            UsbDeviceChangeType actualChangeType;
            if (addedPorts.Any() && !removedPorts.Any())
            {
                actualChangeType = UsbDeviceChangeType.Connected;
            }
            else if (removedPorts.Any() && !addedPorts.Any())
            {
                actualChangeType = UsbDeviceChangeType.Disconnected;
            }
            else
            {
                // Both added and removed - use suggested or Unknown
                actualChangeType = suggestedChangeType != UsbDeviceChangeType.Unknown
                    ? suggestedChangeType
                    : UsbDeviceChangeType.Unknown;
            }

            _previousPorts = currentPorts;

            // Raise event on the UI thread if possible
            var eventArgs = new UsbDeviceChangedEventArgs(actualChangeType, currentPorts.ToList());
            RaiseUsbDeviceChanged(eventArgs);
        }
    }

    private void RaiseUsbDeviceChanged(UsbDeviceChangedEventArgs e)
    {
        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(_ => UsbDeviceChanged?.Invoke(this, e), null);
        }
        else
        {
            UsbDeviceChanged?.Invoke(this, e);
        }
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
    }
    
    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    private void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        
        if (disposing)
        {
            StopMonitoring();
        }
        
        _isDisposed = true;
    }
}