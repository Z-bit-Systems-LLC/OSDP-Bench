using System.Linq;
using OSDP.Net.Model.ReplyData;

namespace OSDPBench.Core.Models
{
    public class CapabilitiesLookup
    {
        public CapabilitiesLookup(DeviceCapabilities deviceCapabilities)
        {
            if (deviceCapabilities == null) return;

            CRC = deviceCapabilities.Capabilities
                      .FirstOrDefault(capability => capability.Function == CapabilityFunction.CheckCharacterSupport)
                      ?.Compliance == 1;

            SecureChannel = deviceCapabilities.Capabilities
                      .FirstOrDefault(capability => capability.Function == CapabilityFunction.CommunicationSecurity)
                      ?.Compliance == 1;
        }

        public bool CRC { get; }

        public bool SecureChannel { get; }
    }
}
