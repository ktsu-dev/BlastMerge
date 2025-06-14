// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents statistics about differences between files
/// </summary>
public record DiffStatistics
{
	/// <summary>
	/// Gets the number of added lines
	/// </summary>
	public required int Additions { get; init; }

	/// <summary>
	/// Gets the number of deleted lines
	/// </summary>
	public required int Deletions { get; init; }

	/// <summary>
	/// Gets the number of modified lines
	/// </summary>
	public required int Modifications { get; init; }

	/// <summary>
	/// Gets the total number of changes
	/// </summary>
	public int TotalChanges => Additions + Deletions + Modifications;

	/// <summary>
	/// Gets whether there are any differences
	/// </summary>
	public bool HasDifferences => TotalChanges > 0;
}
