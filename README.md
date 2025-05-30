# ktsu.BlastMerge

> Cross-repository file synchronization through intelligent iterative merging

## Overview

BlastMerge is a revolutionary file synchronization tool that uses **intelligent iterative merging** to unify multiple versions of files across repositories, directories, and codebases. Unlike traditional diff tools, BlastMerge progressively merges file versions by finding the most similar pairs and resolving conflicts interactively, ultimately synchronizing entire file ecosystems into a single, unified version.

## 🚀 Primary Feature: Iterative File Synchronization

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

-   **Hash-Based Comparison**: Fast file comparison using content hashing for instant duplicate detection
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

### 💻 **Interactive Experience**

-   **Rich Terminal Interface**: Colored output and intuitive navigation with Spectre.Console
-   **Command History**: Arrow key navigation through previous inputs with persistent history
-   **Default Values**: Smart defaults based on most recently used inputs
-   **Progress Indicators**: Real-time feedback for long-running operations
-   **Block-Level Control**: Choose how to handle each difference (keep, remove, use version 1/2, use both)

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

# Select "🔀 Iterative Merge Multiple Versions"
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
```

### Interactive Mode Options

1. **🔀 Iterative merge multiple file versions** - **PRIMARY FEATURE** - Cross-repository file synchronization
2. **🔍 Compare files with same name across directories** - Find and compare files with identical names
3. **📁 Compare two directories** - Full directory comparison with file patterns
4. **📄 Compare two specific files** - Direct file-to-file comparison
5. **ℹ️ Show help** - Comprehensive feature overview

### Iterative Merge Deep Dive

1. **File Discovery**: Scans directory tree for matching files
2. **Version Analysis**: Groups identical files, identifies unique versions
3. **Similarity Calculation**: Determines optimal merge sequence
4. **Progressive Merging**: Merges most similar pairs first
5. **Conflict Resolution**: Interactive TUI for each conflict block
6. **Cross-Repository Update**: Writes merged result to all original locations
7. **Verification**: Confirms all locations now contain identical, unified content

## Why Iterative Merging?

Traditional tools merge two files at a time, requiring manual orchestration for multiple versions. BlastMerge's iterative approach:

-   **Minimizes Conflicts**: By merging similar versions first, later merges have fewer conflicts
-   **Optimizes Decision Making**: Present easier decisions first, complex conflicts last
-   **Maintains Context**: Each merge builds on previous decisions
-   **Scales Naturally**: Works equally well with 3 files or 30 files
-   **Preserves Intent**: Interactive resolution ensures human judgment guides the process

## License

MIT License. Copyright (c) ktsu.dev
