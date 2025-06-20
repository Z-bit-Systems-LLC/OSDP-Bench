# OSDP-Bench Development Guidelines

## References
- OSDP.Net source code: https://github.com/bytedreamer/OSDP.Net

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
