# ktsu.BlastMerge

> Cross-repository file synchronization through intelligent iterative merging

## Overview

BlastMerge is a revolutionary file synchronization tool that uses **intelligent iterative merging** to unify multiple versions of files across repositories, directories, and codebases. Unlike traditional diff tools, BlastMerge progressively merges file versions by finding the most similar pairs and resolving conflicts interactively, ultimately synchronizing entire file ecosystems into a single, unified version.

## 🚀 Iterative File Synchronization

### **The Problem**

When working across multiple repositories, branches, or environments, the same files often diverge into multiple versions with overlapping changes. Traditional merge tools handle two-way merges, but when you have 3, 5, or 10+ versions of the same file scattered across different locations, manual synchronization becomes a nightmare.

### **The Solution: Iterative Merging**

BlastMerge solves this by:

1. **Smart Discovery**: Automatically finds all versions of a file across directories/repositories
2. **Hash-Based Grouping**: Groups identical files and identifies unique versions
3. **Similarity Analysis**: Calculates similarity scores between all version pairs
4. **Optimal Merge Order**: Progressively merges the most similar versions first to minimize conflicts
5. **Interactive Resolution**: Visual TUI for resolving conflicts block-by-block
6. **Cross-Repository Sync**: Updates all file locations with the final merged result

### **Real-World Use Cases**

-   **Multi-Repository Synchronization**: Sync the same configuration files across microservices
-   **Branch Consolidation**: Merge scattered feature branch changes before cleanup
-   **Environment Alignment**: Unify deployment scripts across dev/staging/prod environments
-   **Code Migration**: Consolidate similar files when merging codebases
-   **Documentation Sync**: Align README files across related projects

## Features

### 🔄 **Iterative Merging Engine**

-   **Smart File Detection**: Automatically finds and groups identical file versions by hash
-   **Similarity-Based Progression**: Uses advanced algorithms to find the most similar files and merge them in optimal order
-   **Cross-Repository Updates**: Synchronizes all file locations with the merged result
-   **Progressive Conflict Reduction**: Each merge reduces complexity for subsequent merges
-   **Interactive Conflict Resolution**: Visual TUI for resolving merge conflicts block-by-block
-   **Version Tracking**: Maintains awareness of which files need updating across all locations

### 📊 **Advanced File Analysis**

-   **Hash-Based Comparison**: Fast file comparison using FNV-1a content hashing for instant duplicate detection
-   **Content Similarity Scoring**: Sophisticated algorithms to determine merge order
-   **Multiple Diff Formats**:
    -   Change Summary (added/removed lines only)
    -   Git-style diff (full context)
    -   Rich colored diff (visual formatting)
-   **Side-by-Side Display**: Context-aware diff visualization with intelligent file ordering

### 🔧 **Repository & Directory Operations**

-   **Directory Comparison**: Compare entire directories with customizable file patterns
-   **Recursive Search**: Deep directory traversal to find all file versions
-   **Batch Synchronization**: Update multiple file locations simultaneously
-   **Safe Operations**: Built-in error handling and rollback capabilities

### ⚡ **Performance & Optimization**

-   **Parallel Processing**: Multi-threaded file hashing and discovery for improved performance
-   **Async Operations**: Non-blocking file operations with progress reporting
-   **Optimized Hashing**: Fast FNV-1a algorithm with 4KB buffers for efficient file comparison
-   **Memory Management**: Smart buffering and resource management for large file sets
-   **Throttled Parallelism**: Configurable concurrency limits to prevent system overload

### 🎯 **Batch Operations & Automation**

-   **Batch Configurations**: Save and reuse complex file processing setups
-   **Custom Search Paths**: Define multiple directories to search across repositories
-   **Path Exclusion Patterns**: Skip unwanted directories (node_modules, bin, obj, .git, etc.)
-   **Pre-defined Templates**: Ready-made configurations for common repository synchronization tasks
-   **Discrete Processing Phases**: Separate gathering, hashing, grouping, and resolution phases for better UX
-   **Pattern-based Processing**: Support for wildcards and complex file matching

### 💻 **Interactive Experience**

-   **Rich Terminal Interface**: Colored output and intuitive navigation with Spectre.Console
-   **Command History**: Arrow key navigation through previous inputs with persistent history across sessions
-   **Smart Defaults**: Intelligent defaults based on most recently used inputs
-   **Progress Indicators**: Real-time feedback for long-running operations with detailed phase reporting
-   **Block-Level Control**: Choose how to handle each difference (keep, remove, use version 1/2, use both)
-   **Keyboard Shortcuts**: Comprehensive keyboard navigation for all operations

## Installation

Add the NuGet package:

```bash
dotnet add package ktsu.BlastMerge
```

## Usage

### Primary Workflow: Iterative File Synchronization

```bash
# Launch interactive mode for iterative merging
BlastMerge.ConsoleApp

# Select "🔀 Iterative Merge"
# 1. Specify the directory containing your repositories/projects
# 2. Enter the filename pattern (e.g., "README.md", "config.json", "*.yml")
# 3. Watch as BlastMerge finds all versions and merges them optimally
# 4. Resolve conflicts interactively when needed
# 5. All file locations are automatically updated with the unified result
```

### Command Line Mode

```bash
# Quick comparison for files with the same name across directories
BlastMerge.ConsoleApp <directory> <filename>

# Run a saved batch configuration
BlastMerge.ConsoleApp <directory> -b <batch-name>

# List available batch configurations
BlastMerge.ConsoleApp -l

# Show version information
BlastMerge.ConsoleApp -v

# Display help
BlastMerge.ConsoleApp -h
```

### Interactive Mode Options

1. **🔀 Iterative Merge** - Cross-repository file synchronization
2. **🔍 Compare files with same name across directories** - Find and compare files with identical names
3. **📁 Compare two directories** - Full directory comparison with file patterns
4. **📄 Compare two specific files** - Direct file-to-file comparison
5. **📦 Batch Operations** - Create, edit, and run batch configurations for complex workflows
6. **🔎 Find Files** - Advanced file discovery with pattern matching
7. **⚙️ Settings** - Configure application preferences and view system information
8. **ℹ️ Help** - Comprehensive feature overview and keyboard shortcuts

### Advanced: Batch Configurations

Create reusable batch configurations for complex synchronization tasks:

```bash
# Interactive batch creation
1. Select "📦 Batch Operations" → "Create New Batch"
2. Define file patterns: *.yml, .gitignore, LICENSE.md
3. Set search paths: /path/to/repo1, /path/to/repo2, /path/to/repo3
4. Add exclusions: */node_modules/*, */bin/*, .git/*
5. Save as "Repository Sync Batch"

# Run the batch across all configured paths
BlastMerge.ConsoleApp . -b "Repository Sync Batch"
```

**Pre-built Templates:**

-   **Common Repository Files**: .gitignore, .editorconfig, LICENSE files
-   **Repository Sync Batch**: Comprehensive configuration for multi-repo synchronization

### Iterative Merge Deep Dive

1. **File Discovery**: Parallel scanning across multiple search paths for matching files
2. **Version Analysis**: Groups identical files by hash, identifies unique versions
3. **Similarity Calculation**: Determines optimal merge sequence using content analysis
4. **Progressive Merging**: Merges most similar pairs first to minimize conflicts
5. **Conflict Resolution**: Interactive TUI for each conflict block with multiple resolution options
6. **Cross-Repository Update**: Writes merged result to all original locations
7. **Verification**: Confirms all locations now contain identical, unified content

### Performance Features

-   **Parallel File Discovery**: Multiple search paths processed simultaneously
-   **Concurrent Hashing**: Configurable parallelism for optimal CPU utilization
-   **Progress Tracking**: Real-time updates on file discovery, hashing, and processing phases
-   **Memory Optimization**: Efficient handling of large file sets with controlled resource usage

## Why Iterative Merging?

Traditional tools merge two files at a time, requiring manual orchestration for multiple versions. BlastMerge's iterative approach:

-   **Minimizes Conflicts**: By merging similar versions first, later merges have fewer conflicts
-   **Optimizes Decision Making**: Present easier decisions first, complex conflicts last
-   **Maintains Context**: Each merge builds on previous decisions
-   **Scales Naturally**: Works equally well with 3 files or 30 files
-   **Preserves Intent**: Interactive resolution ensures human judgment guides the process

## Technical Features

-   **Fast Hashing**: FNV-1a algorithm with optimized 4KB buffer processing
-   **Smart Memory Management**: Controlled resource usage with configurable parallelism
-   **Persistent History**: Command history saved across sessions with intelligent organization
-   **Async Operations**: Non-blocking operations with comprehensive progress reporting
-   **Cross-Platform**: Works on Windows, macOS, and Linux

## License

MIT License. Copyright (c) ktsu.dev
