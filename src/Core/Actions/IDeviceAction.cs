﻿using OSDP.Net;

namespace OSDPBench.Core.Actions;

/// <summary>
/// Represents an action that can be performed on a device.
/// </summary>
public interface IDeviceAction
{
    /// <summary>
    /// Represents the name of a device action.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the name of the action to be shown on the button.
    /// </summary>
    string PerformActionName { get; }

    /// <summary>
    /// Performs an action on a device.
    /// </summary>
    /// <param name="panel">The control panel of the device.</param>
    /// <param name="connectionId"></param>
    /// <param name="address"></param>
    /// <param name="parameter">Extra parameter needed to perform action</param>
    /// <returns>A task that represents the asynchronous operation and contains the result of the action.</returns>
    Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter);
}