# OSDP-Bench Development Guidelines

## Build Commands
- Build solution: `dotnet build OSDP-Bench.sln`
- Build specific project: `dotnet build src/Core/Core.csproj`
- Build release version: `dotnet build -c Release OSDP-Bench.sln`

## Test Commands
- Run all tests: `dotnet test test/Core.Tests/Core.Tests.csproj`
- Run single test class: `dotnet test test/Core.Tests/Core.Tests.csproj --filter "FullyQualifiedName~ConnectViewModelTests"`
- Run specific test: `dotnet test test/Core.Tests/Core.Tests.csproj --filter "Name=ConnectViewModel_InitializedAvailableBaudRates"`
- Run with code coverage: `dotnet test test/Core.Tests/Core.Tests.csproj --collect:"XPlat Code Coverage"`

## Code Style Guidelines
- Use C# 8.0+ features with async/await patterns for asynchronous operations
- Follow MVVM design pattern for view models with ObservableObject and RelayCommand
- Use dependency injection for services
- Include XML documentation for public interfaces and methods
- Use PascalCase for class, method, and public property names
- Use _camelCase for private fields with underscore prefix
- Implement defensive programming with null checks for constructor parameters
- Use standard exception handling with try/catch blocks around external operations
- Prefer async/await over direct Task management
- Organize files into clear namespaces (Core, Models, Services, ViewModels, etc.)
- Use meaningful variable names that reflect their purpose
- Keep methods focused and small with a single responsibility

## Refactoring Opportunities

1. DeviceManagementService.cs: 
   - Extract duplicate event raising patterns into helper methods
   - Improve error handling in empty catch blocks
   - Split long class (374 lines) into focused components

2. ConnectViewModel.cs:
   - Extract large switch statement in DiscoverDevice method
   - Split ScanSerialPorts method with multiple responsibilities
   - Simplify nested logic in ConnectDevice

3. ManageViewModel.cs:
   - Refactor 57-line ExecuteDeviceAction method
   - Extract special handling for ResetCypressDeviceAction

4. Consolidate nearly identical implementations:
   - MonitorCardReads.cs and MonitorKeyPadReads.cs

5. Test improvements:
   - Remove duplicated setup code in ConnectViewModelTests.cs
   - Increase test coverage beyond just ConnectViewModel

6. Cross-cutting concerns:
   - Standardize inconsistent error handling approaches
   - Reduce ViewModels coupling to DeviceManagementService
   - Fix naming inconsistencies (MonitorKeypadReads vs MonitorKeyPadReads)
   - Convert hardcoded values (BaudRates, timeouts) to constants