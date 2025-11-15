# GitHub Copilot Custom Instructions for BlastMerge

## Overview

BlastMerge is a cross-repository file synchronization tool built with **C# and .NET 9** that uses intelligent iterative merging to unify multiple versions of files across repositories, directories, and codebases. Unlike traditional diff tools, BlastMerge progressively merges file versions by finding the most similar pairs and resolving conflicts interactively.

### Key Features
- **Iterative Merging Engine**: Smart file detection, similarity-based progression, and cross-repository updates
- **Advanced File Analysis**: Hash-based comparison using FNV-1a algorithm, content similarity scoring
- **Interactive TUI**: Rich terminal interface built with Spectre.Console
- **Batch Operations**: Save and reuse complex file processing setups
- **Performance Optimized**: Parallel processing, async operations, optimized hashing

## Tech Stack

- **Language**: C# with .NET 9.0 SDK
- **SDK**: Custom ktsu.Sdk family (ktsu.Sdk.Lib, ktsu.Sdk.ConsoleApp, ktsu.Sdk.Test)
- **UI Framework**: Spectre.Console for rich terminal interface
- **Testing Framework**: MSTest with FluentAssertions
- **Key Dependencies**:
  - DiffPlex (for diff operations)
  - TestableIO.System.IO.Abstractions (for testable file system operations)
  - ktsu.AppDataStorage (for persistent storage)
  - CommandLineParser (for CLI argument parsing)
  - Coverlet (for code coverage)

## Project Structure

```
BlastMerge/
├── .github/                 # GitHub configuration and workflows
│   ├── workflows/          # CI/CD workflows (dotnet.yml, dependabot-merge.yml, etc.)
│   └── dependabot.yml      # Dependabot configuration
├── BlastMerge/             # Core library
│   ├── Contracts/          # Interfaces and contracts
│   ├── Models/             # Domain models and data structures
│   ├── Services/           # Business logic and services
│   └── Text/               # Text processing and visualization
├── BlastMerge.ConsoleApp/  # Console application entry point
├── BlastMerge.Test/        # Test project (MSTest)
├── scripts/                # Build and deployment scripts (PowerShell)
│   ├── PSBuild.psm1       # Main build module
│   └── update-winget-manifests.ps1
├── winget/                 # WinGet package manifests
├── Directory.Packages.props # Centralized package version management
└── global.json             # .NET SDK version specification
```

## Development Workflow

### Build Instructions

The project uses a custom PowerShell-based build system (PSBuild) integrated with CI/CD.

**Standard .NET build:**
```bash
dotnet restore
dotnet build BlastMerge.sln
```

**Run tests:**
```bash
dotnet test BlastMerge.sln
```

**Run the console application:**
```bash
dotnet run --project BlastMerge.ConsoleApp/BlastMerge.ConsoleApp.csproj
```

### CI/CD Pipeline

The project uses GitHub Actions with a custom PowerShell build module:
- Main workflow: `.github/workflows/dotnet.yml`
- Build script: `scripts/PSBuild.psm1`
- Runs on: Windows (windows-latest)
- Key steps:
  1. Restore dependencies
  2. Build solution
  3. Run tests with coverage (Coverlet)
  4. SonarQube analysis (when configured)
  5. Package and release (automated versioning)
  6. Update WinGet manifests

### Testing

- **Framework**: MSTest with Microsoft.Testing.Extensions
- **Assertions**: FluentAssertions for readable test assertions
- **Mocking**: NSubstitute and Moq available
- **Coverage**: Coverlet with OpenCover format
- **Test Project**: `BlastMerge.Test/`

## Coding Standards

### C# Style Guidelines

The project enforces strict coding standards via `.editorconfig`:

1. **Indentation**: Tabs for C# files (not spaces)
2. **Line Endings**: CRLF (Windows-style)
3. **File Header**: Required copyright header:
   ```csharp
   // Copyright (c) ktsu.dev
   // All rights reserved.
   // Licensed under the MIT license.
   ```

4. **Naming Conventions**:
   - No `this.` qualification (enforce via analyzer)
   - Use language keywords over framework types (e.g., `string` not `String`)
   - Always specify accessibility modifiers
   - Readonly fields where applicable

5. **Code Quality**:
   - All analyzer diagnostics are treated as **errors** (not warnings)
   - Remove unused parameters
   - Prefer expression-bodied members where appropriate
   - Use parentheses for clarity in complex expressions

6. **Formatting**: Auto-formatting is enforced via IDE0055 analyzer rule

### Best Practices

- **Async/Await**: Use async operations for I/O-bound work (file operations, hashing)
- **Dependency Injection**: Use constructor injection for services and dependencies
- **Testability**: Abstract file system operations using `IFileSystem` from TestableIO
- **Error Handling**: Use exceptions for exceptional cases; return results for expected failures
- **Resource Management**: Use `using` statements or `IDisposable` patterns for resource cleanup
- **Parallel Processing**: Use configurable parallelism to avoid overloading system resources

## Package Management

- **Central Package Management**: All package versions defined in `Directory.Packages.props`
- **Version Control**: Use `<PackageVersion Include="..." Version="..." />` pattern
- **Updates**: Dependabot configured for automatic dependency updates

## Contribution Guidelines

1. **Branching**: Feature branches from `main` or `develop`
2. **Commits**: Clear, descriptive commit messages
3. **Pull Requests**: Must pass all CI checks including:
   - Build validation
   - All tests passing
   - Code coverage requirements
   - SonarQube quality gate (when configured)
4. **Code Review**: At least one approval required
5. **Documentation**: Update README.md and inline XML documentation for public APIs

## Architecture Principles

- **Separation of Concerns**: Core library (BlastMerge) separate from console app
- **Interface-Based Design**: Use contracts/interfaces for testability
- **Single Responsibility**: Services should have a focused, single purpose
- **Immutability**: Prefer immutable data structures where appropriate
- **Progressive Enhancement**: Iterative merging reduces complexity progressively

## Performance Considerations

- **Hashing**: FNV-1a algorithm with 4KB buffers for efficient file comparison
- **Parallelism**: Configurable concurrent operations (file discovery, hashing)
- **Memory Management**: Smart buffering for large file sets
- **Async I/O**: Non-blocking file operations throughout
- **Progress Reporting**: Real-time feedback for long-running operations

## Specialized Components

- **SecureTempFileHelper**: Secure temporary file management
- **WhitespaceVisualizer**: Visual representation of whitespace differences
- **ProgressMessages**: Standardized progress reporting messages
- **FileHasher**: Optimized file hashing service
- **MergeService**: Core iterative merge logic

## Development Environment

- **Required**: .NET 9.0 SDK (specified in `global.json`)
- **Recommended IDE**: Visual Studio 2022, VS Code, or JetBrains Rider
- **Optional**: PowerShell 7+ for build scripts
- **Platform**: Cross-platform (Windows, macOS, Linux)

## Security

- **Secrets**: Never commit API keys, tokens, or credentials
- **Dependencies**: Automated security scanning via Dependabot
- **Code Analysis**: SonarQube integration for security vulnerabilities
- **Dependency Submission**: Advanced Security component detection in CI

## Additional Resources

- **Main Documentation**: `README.md` - comprehensive usage guide
- **Changelog**: `CHANGELOG.md` - version history and changes
- **Build Scripts**: `scripts/README.md` - PowerShell build system documentation
- **License**: MIT License (`LICENSE.md`)

---

**Note for AI Agents**: When making changes:
1. Follow the established coding standards strictly (tabs for C#, proper headers)
2. Ensure all new code includes XML documentation for public APIs
3. Add or update tests for any logic changes
4. Run `dotnet build` and `dotnet test` before committing
5. Use existing services and patterns rather than introducing new dependencies
6. Consider performance implications for file I/O operations
7. Maintain backward compatibility unless explicitly breaking changes are required
