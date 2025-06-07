// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

/// <summary>
/// Represents a summary of an individual merge operation
/// </summary>
public record MergeOperationSummary
{
	/// <summary>
	/// Gets or sets the operation number in the merge sequence
	/// </summary>
	public int OperationNumber { get; init; }

	/// <summary>
	/// Gets or sets the first file path that was merged
	/// </summary>
	public string FilePath1 { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the second file path that was merged
	/// </summary>
	public string FilePath2 { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the similarity score between the files
	/// </summary>
	public double SimilarityScore { get; init; }

	/// <summary>
	/// Gets or sets the number of files affected by this merge operation
	/// </summary>
	public int FilesAffected { get; init; }

	/// <summary>
	/// Gets or sets the number of conflicts resolved in this operation
	/// </summary>
	public int ConflictsResolved { get; init; }

	/// <summary>
	/// Gets or sets the number of lines in the merged result
	/// </summary>
	public int MergedLineCount { get; init; }
}
