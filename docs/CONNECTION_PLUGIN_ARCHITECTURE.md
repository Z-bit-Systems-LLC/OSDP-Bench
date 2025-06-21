# Connection Plugin Architecture Plan

## Overview
This document outlines the plan to refactor OSDP-Bench to support pluggable connection types (Serial, Bluetooth, Network, etc.) through a provider-based architecture. This will allow external repositories to implement custom connection types without modifying the core codebase.

## Current Architecture Analysis

### Existing Components
1. **`ISerialPortConnectionService`** - Interface extending `IOsdpConnection` from OSDP.Net
2. **`WindowsSerialPortConnectionService`** - Windows-specific serial port implementation
3. **`ConnectViewModel`** - Manages connection UI state and logic
4. **`ConnectPage.xaml`** - UI for connection selection and configuration

### Current Limitations
- Hardcoded to serial port connections only
- UI tightly coupled to serial port concepts (COM ports, baud rates)
- No plugin mechanism for external connection types

## Proposed Architecture

### 1. Connection Provider Interface
```csharp
public interface IConnectionProvider
{
    string Name { get; }
    string Description { get; }
    
    // Connection discovery and creation
    Task<IEnumerable<AvailableConnection>> FindAvailableConnections();
    IEnumerable<IOsdpConnection> GetConnectionsForDiscovery(string connectionId, int[]? rates = null);
    IOsdpConnection GetConnection(string connectionId, int baudRate);
    
    // UI components
    UserControl GetConnectionSelectionControl();
    UserControl GetParameterConfigurationControl();
    bool SupportsDiscoveryMode { get; }
    bool SupportsManualMode { get; }
}
```

### 2. Connection Manager Service
```csharp
public interface IConnectionManagerService
{
    void RegisterProvider(IConnectionProvider provider);
    IEnumerable<IConnectionProvider> GetProviders();
    Task<IEnumerable<AvailableConnection>> GetAllAvailableConnections();
    IEnumerable<IOsdpConnection> GetConnectionsForDiscovery(string connectionId);
    IOsdpConnection GetConnection(string connectionId, int baudRate);
}
```

### 3. Generic Connection Model
```csharp
public class AvailableConnection
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ProviderType { get; set; }
    public IConnectionProvider Provider { get; set; }
}
```

## Implementation Steps

### Phase 1: Core Infrastructure
1. Create `IConnectionProvider` interface
2. Create `IConnectionManagerService` interface and implementation
3. Create generic `AvailableConnection` model to replace `AvailableSerialPort`
4. Implement plugin loading mechanism (reflection or dependency injection)

### Phase 2: Refactor Existing Code
1. Refactor `WindowsSerialPortConnectionService` into `SerialPortConnectionProvider`
2. Update `ConnectViewModel` to use `IConnectionManagerService`
3. Update dependency injection configuration
4. Migrate existing serial port logic to the new provider model

### Phase 3: UI Changes
1. **Connection Type Selector**
   - Add dropdown for selecting connection type (Serial, Bluetooth, Network)
   - Dynamically populate based on registered providers

2. **Three-Level Selection Hierarchy**
   ```
   Connection Type → Available Connections → Connection Mode
      (Serial)          (COM1, COM2...)        (Discover/Manual)
      (Bluetooth)       (Device1, Device2...)   (Discover/Manual)
      (Network)         (Host1, Host2...)       (Direct only)
   ```

3. **Dynamic UI Panels**
   - Load provider-specific UI controls dynamically
   - Each provider supplies its own parameter configuration UI
   - Maintain consistent styling and behavior

### Phase 4: Provider Implementations

#### Serial Port Provider (Built-in)
- Maintains current functionality
- Provides COM port selection UI
- Supports both discovery and manual modes

#### Bluetooth Provider (External/Private Repository)
```csharp
public class BluetoothConnectionProvider : IConnectionProvider
{
    // Implement Bluetooth device discovery
    // Provide Bluetooth-specific UI controls
    // Handle pairing and connection establishment
}

public class BluetoothOsdpConnection : IOsdpConnection
{
    // Implement OSDP communication over Bluetooth
}
```

#### Network Provider (Future)
- TCP/IP based connections
- IP address and port configuration
- Direct connection only (no discovery mode)

## Benefits

1. **Extensibility**: New connection types can be added without modifying core code
2. **Modularity**: Each provider is self-contained with its own UI and logic
3. **Testability**: Providers can be tested independently
4. **Platform Independence**: Different platforms can use different providers
5. **Private Implementation**: Sensitive or proprietary connection types can remain in private repositories

## Configuration

### appsettings.json
```json
{
  "ConnectionProviders": {
    "SerialPort": { 
      "Enabled": true,
      "Assembly": "Core.dll"
    },
    "Bluetooth": { 
      "Enabled": true,
      "Assembly": "BluetoothProvider.dll"
    },
    "Network": { 
      "Enabled": false,
      "Assembly": "NetworkProvider.dll"
    }
  }
}
```

### Dependency Injection
```csharp
// In App.xaml.cs or Startup
services.AddSingleton<IConnectionManagerService, ConnectionManagerService>();

// Load providers based on configuration
var providerConfig = configuration.GetSection("ConnectionProviders");
foreach (var provider in providerConfig.GetChildren())
{
    if (provider["Enabled"] == "true")
    {
        // Load provider assembly and register
        LoadAndRegisterProvider(provider["Assembly"]);
    }
}
```

## UI Mockup

```
┌─────────────────────────────────────────────────┐
│ Connection Type: [Bluetooth         ▼]          │
│                                                 │
│ Available Devices:                              │
│ ┌─────────────────────────────────────────────┐ │
│ │ • HC-05 Module (98:D3:31:XX:XX:XX)         │ │
│ │ • OSDP Reader BT (AA:BB:CC:DD:EE:FF)       │ │
│ │ • [Scan for devices...]                     │ │
│ └─────────────────────────────────────────────┘ │
│                                                 │
│ Connection Mode: [Discover ▼] [Manual]          │
│                                                 │
│ ┌─────────────────────────────────────────────┐ │
│ │ Bluetooth Settings:                         │ │
│ │ PIN: [____]  □ Save PIN                    │ │
│ │ □ Auto-reconnect                           │ │
│ └─────────────────────────────────────────────┘ │
│                                                 │
│ [Connect]  [Disconnect]                         │
└─────────────────────────────────────────────────┘
```

## Next Steps

1. Complete localization implementation (current priority)
2. Implement Phase 1 (Core Infrastructure)
3. Refactor existing serial port code (Phase 2)
4. Update UI to support multiple connection types (Phase 3)
5. Create example Bluetooth provider implementation

## Notes for External Implementation

When implementing a Bluetooth provider in a private repository:

1. Reference the OSDP-Bench.Core assembly
2. Implement `IConnectionProvider` interface
3. Create a class extending `IOsdpConnection` for Bluetooth communication
4. Provide WPF UserControls for connection selection and configuration
5. Handle platform-specific Bluetooth APIs (Windows.Devices.Bluetooth, 32feet.NET, etc.)
6. Package as a separate assembly that can be loaded dynamically