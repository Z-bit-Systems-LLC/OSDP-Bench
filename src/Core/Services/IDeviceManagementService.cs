using System;
using System.Threading.Tasks;
using OSDPBench.Core.Models;
using OSDPBench.Core.Platforms;

namespace OSDPBench.Core.Services
{
    public interface IDeviceManagementService
    {
        IdentityLookup IdentityLookup { get; }
        CapabilitiesLookup CapabilitiesLookup { get; }

        Task<bool> DiscoverDevice(ISerialPortConnection connection, byte address, bool requireSecureChannel);

        Task<CommunicationParameters> SetCommunicationCommand(CommunicationParameters communicationParameters);

        void Shutdown();

        event EventHandler<bool> ConnectionStatusChange;
    }
}
