﻿using OSDP.Net;
using OSDP.Net.Model.CommandData;

namespace OSDPBench.Core.Actions;

public class FileTransferAction : IDeviceAction
{
    /// <inheritdoc />
    public string Name => "Test Buzzer";

    /// <inheritdoc />
    public string PerformActionName => "Three Quick Beeps";

    /// <inheritdoc />
    public async Task<object> PerformAction(ControlPanel panel, Guid connectionId, byte address, object? parameter)
    {
        var result = await panel.ReaderBuzzerControl(connectionId, address,
            new ReaderBuzzerControl(0, ToneCode.Default, 1, 1, 3));

        return result;
    }
}