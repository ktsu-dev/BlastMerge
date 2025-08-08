// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents the result of a file hash operation
/// </summary>
public record FileHashResult
{
	/// <summary>
	/// Gets the file path
	/// </summary>
	public required string FilePath { get; init; }

	/// <summary>
	/// Gets the computed hash
	/// </summary>
	public required string Hash { get; init; }
}
