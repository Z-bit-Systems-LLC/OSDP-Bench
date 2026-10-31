using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OSDP.Net.Model.ReplyData;

namespace OSDPBench.Core.Models;

public partial class IdentityLookup : ObservableObject
{
    // Typed rather than dynamic: the runtime binder needs reflection, which trimming (the browser app) breaks
    private static readonly KnownVendor[] KnownVendors =
    [
        new("9C-F6-1A", "LenelS2", [new KnownModel(1, "BlueDiamond Reader")],
            "Use supplied card to enable secure channel pairing.", false),
        new("CA-44-6C", "Cypress Computer Systems, Inc.", [new KnownModel(1, "OSM-1000")],
            "The reset command should be sent right after powering on the device. Do you want to continue?", true),
        new("DA-0D-38", "Farpointe Data, Inc.", [new KnownModel(1, "Reader")],
            "The reset command should be sent right after powering on the device. Do you want to continue?", true),
        new("00-06-8E", "HID Corporation", [new KnownModel(1, "Signo Reader")],
            "Use HID Reader Manager mobile application to reset settings.", false),
        new("00-75-32", "INID BV", [new KnownModel(2, "MultiSmart XS")],
            "Use INID RF-DISTIFLEX mobile application to reset settings.", false),
        new("5C-26-23", "WaveLynx Technologies Corporation",
            [new KnownModel(10, "Ethos ET10"), new KnownModel(20, "Ethos ET20"), new KnownModel(25, "Ethos ET25")],
            "To return to OSDP auto-detect mode (default mode), tilt the reader 45 degrees to simulate tamper and cycle power in this state. The power up sequence should indicate OSDP auto-detect with 4 beeps.",
            false)
    ];

    /// <summary>
    /// Represents an identity lookup for a device.
    /// </summary>
    public IdentityLookup(DeviceIdentification deviceIdentification)
    {
        if (deviceIdentification == null) throw new ArgumentNullException(nameof(deviceIdentification));
        
        VendorName = "Unknown Vendor";
        Model = "Unknown Model";
        VendorCode = BitConverter.ToString(deviceIdentification.VendorCode.ToArray());

        var foundDevice = KnownVendors.FirstOrDefault(device => device.VendorCode == VendorCode);
        if (foundDevice != null)
        {
            VendorName = foundDevice.Name;
            var foundModel = foundDevice.Models.FirstOrDefault(model =>
                model.Number == deviceIdentification.ModelNumber);
            if (foundModel != null)
            {
                Model = foundModel.Name;
            }

            ResetInstructions = foundDevice.ResetInstructions;
            CanSendResetCommand = foundDevice.CanSendResetCommand;
        }

        // OSDP.Net exposes the serial number as a signed int, but it is an unsigned 32-bit value
        SerialNumber = unchecked((uint)deviceIdentification.SerialNumber);
        SerialNumberDisplay = string.Create(CultureInfo.InvariantCulture, $"{SerialNumber} (0x{SerialNumber:X8})");

        VersionNumber =
            $"{deviceIdentification.FirmwareMajor}.{deviceIdentification.FirmwareMinor}.{deviceIdentification.FirmwareBuild}";
    }

    [ObservableProperty] private string _vendorCode = string.Empty;

    [ObservableProperty] private string _vendorName = string.Empty;

    [ObservableProperty] private string _model = string.Empty;

    [ObservableProperty] private string _versionNumber = string.Empty;
    
    [ObservableProperty] private uint _serialNumber;

    /// <summary>
    /// Gets the serial number formatted for display as unsigned decimal followed by hex, e.g. "4063516280 (0xF2345678)".
    /// </summary>
    [ObservableProperty] private string _serialNumberDisplay = string.Empty;

    /// <summary>
    /// Provides information and instructions for resetting a device.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual string ResetInstructions { get; } = "No reset instructions are available for this device.";

    /// <summary>
    /// Gets a value indicating whether the device can send a reset command.
    /// </summary>
    /// <remarks>
    /// This property is set based on the device information obtained from the identity lookup.
    /// If the device information is found and the "CanSendResetCommand" property is present,
    /// the value of this property will indicate whether the device can send a reset command.
    /// </remarks>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public virtual bool CanSendResetCommand { get; }

    private sealed record KnownVendor(string VendorCode, string Name, KnownModel[] Models, string ResetInstructions,
        bool CanSendResetCommand);

    private sealed record KnownModel(int Number, string Name);
}
