using System;

namespace OSDPBench.Core.Services;

/// <summary>
/// Service for monitoring USB device connection and disconnection events.
/// </summary>
public interface IUsbDeviceMonitorService : IDisposable
{
    /// <summary>
    /// Event raised when a USB serial port device is connected or disconnected.
    /// </summary>
    event EventHandler<UsbDeviceChangedEventArgs>? UsbDeviceChanged;
    
    /// <summary>
    /// Starts monitoring for USB device changes.
    /// </summary>
    void StartMonitoring();
    
    /// <summary>
    /// Stops monitoring for USB device changes.
    /// </summary>
    void StopMonitoring();
    
    /// <summary>
    /// Gets a value indicating whether the service is currently monitoring.
    /// </summary>
    bool IsMonitoring { get; }
}

/// <summary>
/// Event arguments for USB device change events.
/// </summary>
public class UsbDeviceChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public UsbDeviceChangeType ChangeType { get; }
    
    /// <summary>
    /// Gets the list of currently available serial ports after the change.
    /// </summary>
    public IReadOnlyList<string> AvailablePorts { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UsbDeviceChangedEventArgs"/> class.
    /// </summary>
    public UsbDeviceChangedEventArgs(UsbDeviceChangeType changeType, IReadOnlyList<string> availablePorts)
    {
        ChangeType = changeType;
        AvailablePorts = availablePorts ?? throw new ArgumentNullException(nameof(availablePorts));
    }
}

/// <summary>
/// Specifies the type of USB device change.
/// </summary>
public enum UsbDeviceChangeType
{
    /// <summary>
    /// A USB device was connected.
    /// </summary>
    Connected,
    
    /// <summary>
    /// A USB device was disconnected.
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// The change type could not be determined.
    /// </summary>
    Unknown
}