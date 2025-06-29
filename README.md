# OSDP Bench

A professional tool for configuring and troubleshooting OSDP devices.

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Eclipse%202.0-green.svg)](LICENSE)

## About

Physical access to spaces is typically granted using readers and badges. The readers are usually low-powered end point devices that depend on a control panel to determine if the card credential is authorized to gain access. The communication between the reader and control panel is done via the Open Supervised Device Protocol (OSDP). Current access control panels can lack good tools to manage their connected OSDP devices. The goal of this project is to fill this gap with the necessary tools needed for technicians who are working with OSDP.

Core functionality is under an open source license to help increase the adoption rate of OSDP. A fully functional OSDP Bench tool can be compiled under this license at no cost. We encourage OSDP hardware vendors to utilize this project to speed up the development of their own OSDP related tools.

## Features

- **Device Discovery** - Automatically discover OSDP devices on serial connections
- **Real-time Monitoring** - Monitor card reads, keypad entries, and device status
- **Device Configuration** - Configure LEDs, buzzers, and communication parameters
- **Packet Tracing** - View detailed OSDP communication packets
- **Multi-language Support** - Available in multiple languages
- **Cross-platform** - Built on .NET 8.0 for modern compatibility

## Get OSDP Bench

### Download the App

OSDP Bench is available for purchase on multiple platforms:

<a href="ms-windows-store://pdp/?productid=9N3W7QR3R5S7&cid=&mode=mini"><img src="https://get.microsoft.com/images/en-us%20dark.svg" alt="Microsoft Store" /></a> <a href="https://play.google.com/store/apps/details?id=com.z_bitco.com.osdpbenchmobile"><img src="https://play.google.com/intl/en_us/badges/static/images/badges/en_badge_web_generic.png" alt="Google Play" height="60" /></a>

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Windows 10/11 (for WinUI version)
- Serial port access for device communication

### Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/bytedreamer/OSDP-Bench.git
   cd OSDP-Bench
   ```

2. Build the solution:
   ```bash
   dotnet build OSDP-Bench.sln
   ```

3. Run the application:
   ```bash
   dotnet run --project src/UI/Windows
   ```

### Quick Start

1. Launch OSDP Bench
2. Select your serial port from the dropdown
3. Choose "Discover" to automatically find devices or "Manual" to connect directly
4. Begin monitoring device activity or configure device settings

## Documentation

### Project Documentation
- **[Developer Guidelines](docs/CLAUDE.md)** - Development guidelines and build commands
- **[UI Style Guide](src/UI/Windows/Styles/StyleGuide.md)** - Comprehensive design system and styling guidelines

### Architecture Plans
- **[Connection Plugin Architecture](docs/CONNECTION_PLUGIN_ARCHITECTURE.md)** - Plan for implementing pluggable connection types (Serial, Bluetooth, Network)

### Localization
- **[Localization Plan](docs/LOCALIZATION_PLAN.md)** - Multi-language support implementation
- **[Language Switching](docs/LANGUAGE_SWITCHING_DEMO.md)** - Language switching functionality
- **[Language Fixes](docs/LANGUAGE_SWITCHING_FIX.md)** - Language switching issue fixes

## Contributing

We welcome contributions! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

For documentation contributions:
1. Place new documentation in the `docs/` directory
2. Use descriptive filenames with `.md` extension
3. Update this README to include the new file
4. Follow the existing documentation style and structure

## License

This project is licensed under the Eclipse Public License 2.0 - see the [LICENSE](LICENSE) file for details.

## Contact

Contact [Z-bit Systems, LLC](https://z-bitco.com) for inquiries regarding this project.

## Related Projects

- [OSDP.Net](https://github.com/bytedreamer/OSDP.Net) - The core OSDP communication library
