// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

using ktsu.Semantics;

/// <summary>
/// Strong-typed identifier for batch configuration names.
/// </summary>
public sealed record BatchName : SemanticString<BatchName> { }

/// <summary>
/// Strong-typed identifier for file patterns (e.g., "*.txt", "README.md").
/// </summary>
public sealed record FilePattern : SemanticString<FilePattern> { }

/// <summary>
/// Strong-typed identifier for file hash values.
/// </summary>
public sealed record FileHash : SemanticString<FileHash> { }

/// <summary>
/// Strong-typed identifier for input history prompt keys.
/// </summary>
public sealed record PromptKey : SemanticString<PromptKey> { }

/// <summary>
/// Strong-typed identifier for persistence storage keys.
/// </summary>
public sealed record StorageKey : SemanticString<StorageKey> { }

/// <summary>
/// Strong-typed content string for file contents.
/// </summary>
public sealed record FileContent : SemanticString<FileContent> { }

/// <summary>
/// Strong-typed diff content string for merge operations.
/// </summary>
public sealed record DiffContent : SemanticString<DiffContent> { }

/// <summary>
/// Strong-typed search pattern for file exclusions.
/// </summary>
public sealed record ExclusionPattern : SemanticString<ExclusionPattern> { }
