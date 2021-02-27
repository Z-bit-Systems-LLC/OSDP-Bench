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
                VendorCode = "CA-44-6C",
                Name = "Cypress Computer Systems, Inc.",
                Models = new[]
                {
                    new {Number = 1, Name = "OSM-1000"}
                }
            },
            new {
                VendorCode = "DA-0D-38",
                Name = "Farpointe Data, Inc.",
                Models = new[]
                {
                    new {Number = 1, Name = "Reader"}
                }
            },
            new {
                VendorCode = "00-06-8E", 
                Name = "HID Corporation", 
                Models = new[]
                {
                    new {Number = 1, Name = "Signo"}
                }
            },
            new {
                VendorCode = "5C-26-23",
                Name = "WaveLynx Technologies Corporation",
                Models = new[]
                {
                    new {Number = 10, Name = "Ethos ET10"},
                    new {Number = 20, Name = "Ethos ET20"},
                    new {Number = 25, Name = "Ethos ET25"}
                }
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
            }

            VersionNumber =
                $"{deviceIdentification.FirmwareMajor}.{deviceIdentification.FirmwareMinor}.{deviceIdentification.FirmwareBuild}";
        }

        public string VendorName { get; } = string.Empty;

        public string Model { get; } = string.Empty;

        public string VersionNumber { get; } = string.Empty;
    }
}
