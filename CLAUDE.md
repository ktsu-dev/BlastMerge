# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BlastMerge is a cross-repository file synchronization tool that uses intelligent iterative merging to unify multiple versions of the same file. The application consists of two main projects:

- **BlastMerge** (Core library) - Contains all business logic, services, and models
- **BlastMerge.ConsoleApp** - Interactive TUI application using Spectre.Console
- **BlastMerge.Test** - Empty test project (tests removed after major refactor, to be rebuilt)

## Build and Development Commands

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the console application
dotnet run --project BlastMerge.ConsoleApp

# Format code (must be run before commits)
dotnet format

# Pack NuGet packages
dotnet pack
```

## Architecture

### Core Library Structure
- **Services/** - Main business logic including file operations, diffing, merging, and batch processing
  - **Base/** - Base classes for persistence services (BasePersistenceService, BaseKeyedPersistenceService)
- **Models/** - Data models, DTOs, configuration classes, and semantic types
- **Contracts/** - Interfaces and abstractions

### Console Application Structure
- **CLI/** - Command line argument parsing using CommandLineParser
- **Services/** - UI-specific services for display, menus, and user interaction
  - **Common/** - Shared UI services (ComparisonOperationsService, FileComparisonDisplayService, NavigationStack, UIHelper)
  - **MenuHandlers/** - Individual menu handlers following single responsibility principle
- **Models/** - UI-specific models and enums for menu choices and state management
- **Contracts/** - Console app-specific interfaces
- **Text/** - Display text, messages, and UI constants

### Key Design Principles
- Console app only handles input/display - all business logic resides in the core library
- Uses dependency injection with Microsoft.Extensions.DependencyInjection and ServiceConfiguration
- File system abstraction with ktsu.FileSystemProvider for testability
- Persistence abstraction with ktsu.PersistenceProvider for settings and data storage
- Semantic types using ktsu.Semantics for type safety (BatchName, FilePattern, etc.)
- Base service classes for consistent persistence patterns
- Async operations with progress reporting throughout

## Key Technologies

- **.NET 9.0** with ktsu custom SDKs
- **Spectre.Console** for rich terminal UI
- **DiffPlex** for file diffing algorithms
- **ktsu.FileSystemProvider** for file system abstraction and testing
- **ktsu.PersistenceProvider** for data storage and configuration
- **ktsu.Semantics** for strong-typed identifiers and semantic types
- **Microsoft.Extensions.DependencyInjection** for service registration
- **CommandLineParser** for CLI argument parsing
- **MSTest** framework ready for test rebuilding (currently empty)

## Development Guidelines

### Code Standards
- All files must have MIT copyright headers
- Use `ArgumentNullException.ThrowIfNull` for parameter validation
- Follow Microsoft C# coding conventions with `var` for obvious types
- Use primary constructors and expression bodies where appropriate
- Handle specific exception types rather than general `Exception`
- Constants for repeated string literals
- One class per file, extract nested classes

### Testing
- Test suite to be rebuilt using MSTest after major refactor
- Mock file system using ktsu.FileSystemProvider testing helpers
- Mock persistence using ktsu.PersistenceProvider testing helpers
- Use FluentAssertions for readable assertions
- Performance tests with BenchmarkDotNet

### Architecture Rules
- Business logic must be in core library only
- Console app restricted to input handling and display
- Use dependency injection for service resolution via ServiceConfiguration
- Async operations with proper cancellation support
- File operations through ktsu.FileSystemProvider abstraction
- Data persistence through ktsu.PersistenceProvider abstraction
- Use semantic types for type safety and parameter validation
- Inherit from base persistence services for consistent patterns

## Important Implementation Details

### Iterative Merging Process
1. File discovery across multiple directories/repositories
2. Hash-based grouping to identify identical vs. different versions
3. Similarity analysis to determine optimal merge order
4. Progressive merging of most similar files first
5. Interactive conflict resolution with block-level choices
6. Cross-repository synchronization of final results

### Batch Operations
- Use BatchConfigurationService for persistent batch configurations
- BatchProcessor handles parallel file processing with configurable concurrency
- Support for custom search paths and exclusion patterns
- Progress reporting during long-running operations
- RecentBatchService tracks recently used batch configurations

### UI Patterns
- Rich terminal interface with Spectre.Console
- Menu handler pattern for organized UI logic
- Command history persistence across sessions via InputHistoryService
- Interactive menus with keyboard navigation
- Side-by-side diff display with character-level highlighting
- Whitespace visualization in diffs
- Progress reporting with ProgressReportingService

### Security Considerations
- Use SecureTempFileHelper instead of Path.GetTempFileName()
- Regex timeout protection against ReDoS attacks
- Safe handling of file system operations with proper error handling