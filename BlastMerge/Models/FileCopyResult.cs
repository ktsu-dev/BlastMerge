// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents the result of a file copy operation
/// </summary>
public record FileCopyResult
{
	/// <summary>
	/// Gets the source file path
	/// </summary>
	public required string Source { get; init; }

	/// <summary>
	/// Gets the target file path
	/// </summary>
	public required string Target { get; init; }

	/// <summary>
	/// Gets whether the copy operation was successful
	/// </summary>
	public required bool Success { get; init; }
}
