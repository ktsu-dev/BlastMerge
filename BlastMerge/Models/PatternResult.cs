// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents the result of processing a single pattern in a batch
/// </summary>
public class PatternResult
{
	/// <summary>
	/// Gets or sets the pattern that was processed
	/// </summary>
	public string Pattern { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the actual filename found (when different from pattern)
	/// </summary>
	public string? FileName { get; set; }

	/// <summary>
	/// Gets or sets whether the pattern processing was successful
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets or sets the number of files found for this pattern
	/// </summary>
	public int FilesFound { get; set; }

	/// <summary>
	/// Gets or sets the number of unique versions found
	/// </summary>
	public int UniqueVersions { get; set; }

	/// <summary>
	/// Gets or sets a message describing the result
	/// </summary>
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the merge result if a merge was performed
	/// </summary>
	public MergeCompletionResult? MergeResult { get; set; }

	/// <summary>
	/// Gets the files that were discovered for this pattern but could not be read, and were
	/// therefore excluded from grouping and merging. A non-empty collection always accompanies
	/// <see cref="Success"/> being <see langword="false"/>.
	/// </summary>
	public Collection<FileHashFailure> SkippedFiles { get; init; } = [];

	/// <summary>
	/// Gets the files that were discovered for this pattern but hold binary content, and were
	/// therefore excluded from grouping and merging. Merging them as text would read their bytes as
	/// UTF-8 and write the result back, losing every byte that is not valid UTF-8, so they are left
	/// exactly as they were found and are not counted in <see cref="FilesFound"/>.
	/// </summary>
	public Collection<string> SkippedBinaryFiles { get; init; } = [];
}
