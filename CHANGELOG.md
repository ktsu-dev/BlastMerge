## v1.0.0

Initial release of **BlastMerge** - focused on cross-repository file synchronization through intelligent iterative merging:

### Primary Feature: Iterative File Synchronization

-   **Cross-Repository Synchronization**: Unify multiple versions of files across repositories, directories, and codebases
-   **Intelligent Merge Ordering**: Advanced similarity algorithms determine optimal merge sequence to minimize conflicts
-   **Progressive Conflict Reduction**: Each merge simplifies subsequent merges by reducing version complexity
-   **Interactive Conflict Resolution**: Visual TUI for resolving merge conflicts block-by-block with full context
-   **Smart File Discovery**: Automatically finds and groups all file versions by content hash
-   **Version Unification**: Updates all file locations with the final merged result

### Supporting Features

-   **Rich Interactive TUI**: Spectre.Console-powered terminal interface with colored output and intuitive navigation
-   **Multiple Comparison Modes**: Directory comparison, specific file comparison, and cross-directory file matching
-   **Advanced File Analysis**: Hash-based grouping, content similarity scoring, and intelligent duplicate detection
-   **Multiple Diff Formats**: Change summary, git-style diff, and rich colored diff with side-by-side display
-   **Command History & Defaults**: Arrow key navigation with persistent input history and smart defaults
-   **Progress Indicators**: Real-time visual feedback for long-running operations
-   **Safe Operations**: Built-in error handling and rollback capabilities for file operations

### Technical Foundation

-   Built on .NET 9.0 with modern C# features and cross-platform compatibility
-   DiffPlex integration for high-performance difference calculation and analysis
-   Comprehensive test coverage with MSTest framework and mock filesystem testing
-   Robust file system abstractions for reliable cross-platform operation

## v1.0.1

Initial release or repository with no prior history.
