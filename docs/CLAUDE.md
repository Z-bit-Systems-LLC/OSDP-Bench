# OSDP-Bench Development Guidelines

## Language Server
- **csharp-ls** is configured for this project - use the LSP tool for code intelligence features:
  - `goToDefinition` - Find where a symbol is defined
  - `findReferences` - Find all references to a symbol
  - `hover` - Get type info and documentation
  - `documentSymbol` - Get all symbols in a file
  - `workspaceSymbol` - Search for symbols across the workspace

## References
- OSDP.Net source code: https://github.com/bytedreamer/OSDP.Net

## Build Commands
- Build solution: `dotnet build OSDP-Bench.sln`
- Build a specific project: `dotnet build src/Core/Core.csproj`
- Build release version: `dotnet build -c Release OSDP-Bench.sln`

## Test Commands
- Run all tests: `dotnet test test/Core.Tests/Core.Tests.csproj`
- Run single test class: `dotnet test test/Core.Tests/Core.Tests.csproj --filter "FullyQualifiedName~ConnectViewModelTests"`
- Run specific test: `dotnet test test/Core.Tests/Core.Tests.csproj --filter "Name=ConnectViewModel_InitializedAvailableBaudRates"`
- Run with code coverage: `dotnet test test/Core.Tests/Core.Tests.csproj --collect:"XPlat Code Coverage"`

## Resource Management
- Check resource usage: `pwsh ci/check_resource_usage_progress.ps1`
- The script analyzes unused resource strings and missing definitions across all language files
- Shows progress indicators for both resource usage checking and missing definition scanning
- Provides detailed reports with resource values and comments for cleanup decisions
- Can be integrated into Azure DevOps pipelines using `ci/azure-pipeline-resource-check.yml`

## Localization
- **Do not manually translate resource strings** - Translations are handled by a separate automated service
- Only add new resource strings to the main `Resources.resx` file with English values
- The automated translation service will populate the language-specific `.resx` files (de, es, fr, ja, zh)

## Release Process
- Create a release: `pwsh ci/release.ps1`
- The script automates the release workflow:
  - Validates working directory state (no uncommitted changes)
  - Ensures you're on the develop branch
  - Fetches latest changes from remote
  - Verifies develop is ahead of main
  - Shows commits to be released
  - Merges develop into main with no-fast-forward
  - Pushes to trigger CI/CD pipeline
- The Azure DevOps pipeline will automatically:
  - Run tests
  - Bump version number
  - Create a tagged release
- Requirements:
  - Must be on develop branch
  - No uncommitted changes
  - Develop must be ahead of main

## Code Style Guidelines
- Use C# 8.0+ features with async/await patterns for asynchronous operations
- Follow the MVVM design pattern for view models with ObservableObject and RelayCommand
- Use dependency injection for services
- Include XML documentation for public interfaces and methods
- Use PascalCase for class, method, and public property names
- Use _camelCase for private fields with an underscore prefix
- Implement defensive programming with null checks for constructor parameters
- Use standard exception handling with try/catch blocks around external operations
- Prefer async/await over direct Task management
- Organize files into clear namespaces (Core, Models, Services, ViewModels, etc.)
- Use meaningful variable names that reflect their purpose
- Keep methods focused and small with a single responsibility

## UI Style Guidelines
- **Always use standard styles** - Apply predefined styles from the design system instead of inline properties
- **Use design tokens for spacing** - Reference `{StaticResource Margin.Card}` instead of hardcoding values
- **Apply theme-aware semantic colors** - Use `{DynamicResource SemanticSuccessBrush}` for automatic light/dark theme support
- **Follow the style hierarchy** - Check ComponentStyles.xaml and LayoutTemplates.xaml before creating custom styles
- **Update existing code** - When modifying files, replace inline styling with standard styles
- **Create reusable patterns** - If you find yourself repeating XAML structures, consider adding a new style or template
- **Use WrapPanel for responsive layouts** - When controls should be horizontal on wide screens but wrap to vertical on narrow screens, use WrapPanel instead of fixed Grid layouts
- **Prefer dynamic resources for colors** - Use `{DynamicResource}` instead of `{StaticResource}` for colors to ensure theme compatibility

For detailed UI styling guidelines and examples, see: `src/UI/Windows/Styles/StyleGuide.md`
