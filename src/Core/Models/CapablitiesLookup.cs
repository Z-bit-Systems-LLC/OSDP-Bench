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

        AudioOutputComplianceLevel = deviceCapabilities.Capabilities
            .FirstOrDefault(capability => capability.Function == CapabilityFunction.ReaderAudibleOutput)
            ?.Compliance ?? 0;

        AudioOutput = AudioOutputComplianceLevel >= 1;

        LedControlComplianceLevel = deviceCapabilities.Capabilities
            .FirstOrDefault(capability => capability.Function == CapabilityFunction.ReaderLEDControl)
            ?.Compliance ?? 0;

        LedControl = LedControlComplianceLevel >= 1;
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

    /// <summary>
    /// Is audio output (buzzer) capable
    /// </summary>
    public bool AudioOutput { get; }

    /// <summary>
    /// Audio output compliance level (0 = not supported, 1 = on/off only, 2 = timed operation)
    /// </summary>
    public byte AudioOutputComplianceLevel { get; }

    /// <summary>
    /// Is LED control capable
    /// </summary>
    public bool LedControl { get; }

    /// <summary>
    /// LED control compliance level (0 = not supported, 1 = on/off only, 2 = timed operation, 3 = timed, bi-color, tri-color)
    /// </summary>
    public byte LedControlComplianceLevel { get; }
}