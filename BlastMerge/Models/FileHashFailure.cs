// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents a file that was discovered by a batch but could not be read while its hash was computed,
/// and was therefore excluded from grouping and merging.
/// </summary>
/// <param name="FilePath">The full path of the file that could not be hashed.</param>
/// <param name="ErrorMessage">The error reported while reading the file.</param>
public record FileHashFailure(string FilePath, string ErrorMessage);
