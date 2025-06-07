// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

/// <summary>
/// Represents statistics about a diff block
/// </summary>
public record DiffBlockStatistics
{
	/// <summary>
	/// Gets the number of deletions in the diff block
	/// </summary>
	public required int Deletions { get; init; }

	/// <summary>
	/// Gets the number of insertions in the diff block
	/// </summary>
	public required int Insertions { get; init; }

	/// <summary>
	/// Gets the total number of changes in this block
	/// </summary>
	public int TotalChanges => Deletions + Insertions;

	/// <summary>
	/// Gets whether this block has any changes
	/// </summary>
	public bool HasChanges => TotalChanges > 0;
}
