using OSDP.Net;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents a device action that monitors various input types from a device.
/// </summary>
public class MonitoringAction : IDeviceAction
{
    private readonly string _name;
    
    /// <summary>
    /// Gets the type of monitoring being performed.
    /// </summary>
    public MonitoringType MonitoringType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MonitoringAction"/> class.
    /// </summary>
    /// <param name="monitoringType">The type of monitoring to perform.</param>
    public MonitoringAction(MonitoringType monitoringType)
    {
        MonitoringType = monitoringType;
        _name = monitoringType switch
        {
            MonitoringType.CardReads => "Monitor Card Reads",
            MonitoringType.KeypadReads => "Monitor Keypad Reads",
            _ => "Monitor Device"
        };
    }

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public string PerformActionName => string.Empty;

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        return await Task.FromResult(true);
    }
}

/// <summary>
/// Defines the type of monitoring to perform on a device.
/// </summary>
public enum MonitoringType
{
    /// <summary>
    /// Monitors card read events.
    /// </summary>
    CardReads,
    
    /// <summary>
    /// Monitors keypad input events.
    /// </summary>
    KeypadReads
}

/// <summary>
/// Extension methods for IDeviceAction that simplify working with monitoring actions.
/// </summary>
public static class DeviceActionExtensions
{
    /// <summary>
    /// Determines if the device action is a monitoring action of the specified type.
    /// </summary>
    /// <param name="action">The device action to check.</param>
    /// <param name="monitoringType">The monitoring type to check for.</param>
    /// <returns>True if the action is a monitoring action of the specified type; otherwise, false.</returns>
    public static bool IsMonitoringAction(this IDeviceAction action, MonitoringType monitoringType)
    {
        return action is MonitoringAction monitoringAction && monitoringAction.MonitoringType == monitoringType;
    }
    
    /// <summary>
    /// Determines if the device action is for monitoring card reads.
    /// </summary>
    /// <param name="action">The device action to check.</param>
    /// <returns>True if the action is for monitoring card reads; otherwise, false.</returns>
    public static bool IsCardReadsMonitor(this IDeviceAction action)
    {
        return action.IsMonitoringAction(MonitoringType.CardReads);
    }
    
    /// <summary>
    /// Determines if the device action is for monitoring keypad reads.
    /// </summary>
    /// <param name="action">The device action to check.</param>
    /// <returns>True if the action is for monitoring keypad reads; otherwise, false.</returns>
    public static bool IsKeypadReadsMonitor(this IDeviceAction action)
    {
        return action.IsMonitoringAction(MonitoringType.KeypadReads);
    }
}