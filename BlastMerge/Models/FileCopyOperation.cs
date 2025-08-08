// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents a file copy operation
/// </summary>
public record FileCopyOperation
{
	/// <summary>
	/// Gets the source file path
	/// </summary>
	public required string Source { get; init; }

	/// <summary>
	/// Gets the target file path
	/// </summary>
	public required string Target { get; init; }
}
