// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents context information around a diff block
/// </summary>
public record BlockContext
{
	/// <summary>
	/// Gets the context lines before the block from the first file
	/// </summary>
	public required ReadOnlyCollection<string> ContextBefore1 { get; init; }

	/// <summary>
	/// Gets the context lines after the block from the first file
	/// </summary>
	public required ReadOnlyCollection<string> ContextAfter1 { get; init; }

	/// <summary>
	/// Gets the context lines before the block from the second file
	/// </summary>
	public required ReadOnlyCollection<string> ContextBefore2 { get; init; }

	/// <summary>
	/// Gets the context lines after the block from the second file
	/// </summary>
	public required ReadOnlyCollection<string> ContextAfter2 { get; init; }

	/// <summary>
	/// Creates a BlockContext from arrays
	/// </summary>
	/// <param name="contextBefore1">Context before from file 1</param>
	/// <param name="contextAfter1">Context after from file 1</param>
	/// <param name="contextBefore2">Context before from file 2</param>
	/// <param name="contextAfter2">Context after from file 2</param>
	/// <returns>A new BlockContext instance</returns>
	public static BlockContext Create(string[] contextBefore1, string[] contextAfter1,
		string[] contextBefore2, string[] contextAfter2) =>
		new()
		{
			ContextBefore1 = new ReadOnlyCollection<string>(contextBefore1),
			ContextAfter1 = new ReadOnlyCollection<string>(contextAfter1),
			ContextBefore2 = new ReadOnlyCollection<string>(contextBefore2),
			ContextAfter2 = new ReadOnlyCollection<string>(contextAfter2)
		};
}
