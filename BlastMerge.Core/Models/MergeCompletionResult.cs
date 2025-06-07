// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

/// <summary>
/// Represents the completion status of an iterative merge
/// </summary>
/// <param name="IsSuccessful"> Gets whether the merge completed successfully </param>
/// <param name="FinalMergedContent"> Gets the final merged content </param>
/// <param name="FinalLineCount"> Gets the number of lines in the final result </param>
/// <param name="OriginalFileName"> Gets the original filename being merged </param>
public record MergeCompletionResult(bool IsSuccessful, string? FinalMergedContent, int FinalLineCount, string OriginalFileName)
{
	/// <summary>
	/// Gets or sets the total number of merge operations performed
	/// </summary>
	public int TotalMergeOperations { get; init; }

	/// <summary>
	/// Gets or sets the initial number of file groups before merging
	/// </summary>
	public int InitialFileGroups { get; init; }

	/// <summary>
	/// Gets or sets the total number of files that were merged
	/// </summary>
	public int TotalFilesMerged { get; init; }

	/// <summary>
	/// Gets or sets the list of merge operations performed
	/// </summary>
	public IReadOnlyList<MergeOperationSummary> Operations { get; init; } = [];
}
