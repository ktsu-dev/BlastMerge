// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

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
}
