// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System.Collections.Generic;

/// <summary>
/// Represents the result of a directory comparison
/// </summary>
public class DirectoryComparisonResult
{
	/// <summary>
	/// Gets the collection of files that are identical in both directories
	/// </summary>
	public IReadOnlyCollection<string> SameFiles { get; init; } = [];

	/// <summary>
	/// Gets the collection of files that exist in both directories but have different content
	/// </summary>
	public IReadOnlyCollection<string> ModifiedFiles { get; init; } = [];

	/// <summary>
	/// Gets the collection of files that exist only in the first directory
	/// </summary>
	public IReadOnlyCollection<string> OnlyInDir1 { get; init; } = [];

	/// <summary>
	/// Gets the collection of files that exist only in the second directory
	/// </summary>
	public IReadOnlyCollection<string> OnlyInDir2 { get; init; } = [];
}

