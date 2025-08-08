// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents the result of a file content read operation
/// </summary>
public record FileContentResult
{
	/// <summary>
	/// Gets the file path
	/// </summary>
	public required string FilePath { get; init; }

	/// <summary>
	/// Gets the file content
	/// </summary>
	public required string Content { get; init; }
}
