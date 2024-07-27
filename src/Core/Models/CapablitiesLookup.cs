using OSDP.Net.Model.ReplyData;

namespace OSDPBench.Core.Models;

/// <summary>
/// Represents a lookup of device capabilities.
/// </summary>
public class CapabilitiesLookup
{
    /// <summary>
    /// Represents a lookup of device capabilities.
    /// </summary>
    public CapabilitiesLookup(DeviceCapabilities deviceCapabilities)
    {
        if (deviceCapabilities == null) throw new ArgumentNullException(nameof(deviceCapabilities));

        CRC = deviceCapabilities.Capabilities
            .FirstOrDefault(capability => capability.Function == CapabilityFunction.CheckCharacterSupport)
            ?.Compliance == 1;

        SecureChannel = deviceCapabilities.Capabilities
            .FirstOrDefault(capability => capability.Function == CapabilityFunction.CommunicationSecurity)
            ?.Compliance == 1;
    }

    /// <summary>
    /// Is CRC capable
    /// </summary>
    public bool CRC { get; }

    /// <summary>
    /// Is secure channel capable
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool SecureChannel { get; }
}