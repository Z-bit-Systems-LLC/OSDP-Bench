using System;
using System.Collections;
using System.Linq;
using OSDP.Net.Model.ReplyData;

namespace OSDPBench.Core.Models
{
    public class IdentityLookup
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

        public IdentityLookup(DeviceIdentification deviceIdentification)
        {
            if (deviceIdentification == null) return;

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

        public string VendorName { get; } = string.Empty;

        public string Model { get; } = string.Empty;

        public string VersionNumber { get; } = string.Empty;

        public string ResetInstructions { get; } = "No reset instructions are available for this device.";

        public bool CanSendResetCommand { get; }
    }
}
