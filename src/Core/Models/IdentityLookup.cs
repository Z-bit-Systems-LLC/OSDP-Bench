using OSDP.Net.Model.ReplyData;

namespace OSDPBench.Core.Models
{
    public class IdentityLookup
    {
        public IdentityLookup(DeviceIdentification deviceIdentification)
        {
            if (deviceIdentification == null) return;

            VendorName = "Unknown Vendor";
            Model = "Unknown Model";

            VersionNumber =
                $"{deviceIdentification.FirmwareMajor}.{deviceIdentification.FirmwareMinor}.{deviceIdentification.FirmwareBuild}";
        }

        public string VendorName { get; } = string.Empty;

        public string Model { get; } = string.Empty;

        public string VersionNumber { get; } = string.Empty;
    }
}
