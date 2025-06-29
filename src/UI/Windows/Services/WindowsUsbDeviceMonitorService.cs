using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using OSDPBench.Core.Services;

namespace OSDPBench.Windows.Services;

/// <summary>
/// Windows implementation of USB device monitoring service using WMI events and polling.
/// </summary>
public class WindowsUsbDeviceMonitorService : IUsbDeviceMonitorService
{
    private readonly SynchronizationContext? _synchronizationContext;
    private ManagementEventWatcher? _deviceInsertWatcher;
    private ManagementEventWatcher? _deviceRemoveWatcher;
    private Timer? _pollingTimer;
    private HashSet<string> _previousPorts = new();
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
            catch (Exception ex)
            {
                // If WMI fails, continue with polling only
                Console.WriteLine($"WMI monitoring failed to start: {ex.Message}. Falling back to polling only.");
                
                if (_pollingTimer == null)
                {
                    _pollingTimer = new Timer(OnPollingTimerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
                }
                
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
        try
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
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start WMI monitoring: {ex.Message}");
            throw;
        }
    }
    
    private void StopWmiMonitoring()
    {
        try
        {
            _deviceInsertWatcher?.Stop();
            _deviceInsertWatcher?.Dispose();
            _deviceInsertWatcher = null;
            
            _deviceRemoveWatcher?.Stop();
            _deviceRemoveWatcher?.Dispose();
            _deviceRemoveWatcher = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping WMI monitoring: {ex.Message}");
        }
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
        try
        {
            var currentPorts = new HashSet<string>(SerialPort.GetPortNames());
            
            lock (_lock)
            {
                // Check if there are any changes
                var addedPorts = currentPorts.Except(_previousPorts).ToList();
                var removedPorts = _previousPorts.Except(currentPorts).ToList();
                
                if (addedPorts.Any() || removedPorts.Any())
                {
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking for port changes: {ex.Message}");
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
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;
        
        if (disposing)
        {
            StopMonitoring();
        }
        
        _isDisposed = true;
    }
}