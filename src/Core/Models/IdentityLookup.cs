using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using OSDP.Net.Model.ReplyData;

namespace OSDPBench.Core.Models;

public partial class IdentityLookup : ObservableObject
{
    private readonly dynamic _lookup = new[]
    {
        new {
            VendorCode = "9C-F6-1A",
            Name = "LenelS2",
            Models = new[]
            {
                new {Number = 1, Name = "BlueDiamond Reader"}
            },
            ResetInstructions = "Use supplied card to enable secure channel pairing.",
            CanSendResetCommand = false
        },
        new {
            VendorCode = "CA-44-6C",
            Name = "Cypress Computer Systems, Inc.",
            Models = new[]
            {
                new {Number = 1, Name = "OSM-1000"}
            },
            ResetInstructions = "The reset command should be sent right after powering on the device. Do you want to continue?",
            CanSendResetCommand = true
        },
        new {
            VendorCode = "DA-0D-38",
            Name = "Farpointe Data, Inc.",
            Models = new[]
            {
                new {Number = 1, Name = "Reader"}
            },
            ResetInstructions = "The reset command should be sent right after powering on the device. Do you want to continue?",
            CanSendResetCommand = true
        },
        new {
            VendorCode = "00-06-8E", 
            Name = "HID Corporation", 
            Models = new[]
            {
                new {Number = 1, Name = "Signo Reader"}
            },
            ResetInstructions = "Use HID Reader Manager mobile application to reset settings.",
            CanSendResetCommand = false
        },
        new {
            VendorCode = "00-75-32",
            Name = "INID BV",
            Models = new[]
            {
                new {Number = 2, Name = "MultiSmart XS"}
            },
            ResetInstructions = "Use INID RF-DISTIFLEX mobile application to reset settings.",
            CanSendResetCommand = false
        },
        new {
            VendorCode = "5C-26-23",
            Name = "WaveLynx Technologies Corporation",
            Models = new[]
            {
                new {Number = 10, Name = "Ethos ET10"},
                new {Number = 20, Name = "Ethos ET20"},
                new {Number = 25, Name = "Ethos ET25"}
            },
            ResetInstructions = "To return to OSDP auto-detect mode (default mode), tilt the reader 45 degrees to simulate tamper and cycle power in this state. The power up sequence should indicate OSDP auto-detect with 4 beeps.",
            CanSendResetCommand = false
        }
    };

    /// <summary>
    /// Represents an identity lookup for a device.
    /// </summary>
    public IdentityLookup(DeviceIdentification deviceIdentification)
    {
        if (deviceIdentification == null) throw new ArgumentNullException(nameof(deviceIdentification));
        
        VendorName = "Unknown Vendor";
        Model = "Unknown Model";

        var foundDevice = ((IEnumerable) _lookup).Cast<dynamic>().FirstOrDefault(device =>
            device.VendorCode.ToString() == BitConverter.ToString(deviceIdentification.VendorCode.ToArray()));
        if (foundDevice != null)
        {
            VendorName = foundDevice.Name.ToString();
            var foundModel = ((IEnumerable)foundDevice.Models).Cast<dynamic>().FirstOrDefault(model =>
                model.Number == deviceIdentification.ModelNumber);
            if (foundModel != null)
            {
                Model = foundModel.Name.ToString();
            }

            ResetInstructions = foundDevice.ResetInstructions.ToString();
            CanSendResetCommand = foundDevice.CanSendResetCommand;
        }

        VersionNumber =
            $"{deviceIdentification.FirmwareMajor}.{deviceIdentification.FirmwareMinor}.{deviceIdentification.FirmwareBuild}";
    }

    [ObservableProperty] private string _vendorName = string.Empty;

    [ObservableProperty] private string _model = string.Empty;

    [ObservableProperty] private string _versionNumber = string.Empty;

    /// <summary>
    /// Provides information and instructions for resetting a device.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string ResetInstructions { get; } = "No reset instructions are available for this device.";

    /// <summary>
    /// Gets a value indicating whether the device can send a reset command.
    /// </summary>
    /// <remarks>
    /// This property is set based on the device information obtained from the identity lookup.
    /// If the device information is found and the "CanSendResetCommand" property is present,
    /// the value of this property will indicate whether the device can send a reset command.
    /// </remarks>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool CanSendResetCommand { get; }
}