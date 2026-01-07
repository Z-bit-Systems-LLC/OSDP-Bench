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
- Check resource usage: `pwsh scripts/check_resource_usage.ps1`
- The script analyzes unused resource strings and missing definitions across all language files
- Provides detailed reports with resource values and comments for cleanup decisions

## Localization
- **Do not manually translate resource strings** - Translations are handled by a separate automated service
- Only add new resource strings to the main `Resources.resx` file with English values
- The automated translation service will populate the language-specific `.resx` files (de, es, fr, ja, zh)

## Branching Strategy
- **Trunk-based development** - All work happens on the `main` branch
- Small, frequent commits directly to `main`
- Tag-based releases (`v1.2.3`) trigger packaging pipeline
- Short-lived feature branches are acceptable for multi-day work

## Code Inspection
The CI pipeline uses JetBrains ReSharper command-line tools to enforce code quality. Builds fail on any errors or warnings.

### Setup
```bash
dotnet tool install -g JetBrains.ReSharper.GlobalTools
```

### Run Inspection
```bash
jb inspectcode OSDP-Bench.sln -o=results.sarif
```

### Review Results
```powershell
# Parse SARIF and display issues
$sarif = Get-Content results.sarif | ConvertFrom-Json
foreach ($result in $sarif.runs[0].results) {
    $level = $result.level
    $message = $result.message.text
    $file = $result.locations[0].physicalLocation.artifactLocation.uri
    $line = $result.locations[0].physicalLocation.region.startLine
    Write-Host "$level : $file`:$line - $message"
}
```

### Common Issues
- Unused variables or parameters
- Missing null checks
- Inconsistent naming conventions
- Redundant code or casts
- Possible null reference exceptions

## Release Process
- Create a release: `pwsh scripts/release.ps1`
- The script automates the tag-based release workflow:
  - Validates working directory state (no uncommitted changes)
  - Ensures you're on the main branch
  - Prompts for new version number (semantic versioning)
  - Updates `VersionPrefix` in `Directory.Build.props`
  - Commits the version change
  - Creates version tag (`vX.Y.Z`)
  - Pushes commit and tag to trigger release pipeline
- The Azure DevOps pipeline will automatically:
  - Run build and tests
  - Run code inspection
  - Create Windows binaries (x64 and ARM64)
  - Create NuGet package
- Requirements:
  - Must be on main branch
  - No uncommitted changes
  - Version must follow semantic versioning (e.g., 3.0.14)

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
