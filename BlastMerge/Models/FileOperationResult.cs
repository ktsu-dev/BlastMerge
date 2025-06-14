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
