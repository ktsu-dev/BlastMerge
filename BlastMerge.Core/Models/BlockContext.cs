// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents context lines around a block for DiffPlex blocks
/// </summary>
public class BlockContext
{
	/// <summary>
	/// Gets or sets the context lines before the block in version 1
	/// </summary>
	public ReadOnlyCollection<string> ContextBefore1 { get; set; } = null!;

	/// <summary>
	/// Gets or sets the context lines after the block in version 1
	/// </summary>
	public ReadOnlyCollection<string> ContextAfter1 { get; set; } = null!;

	/// <summary>
	/// Gets or sets the context lines before the block in version 2
	/// </summary>
	public ReadOnlyCollection<string> ContextBefore2 { get; set; } = null!;

	/// <summary>
	/// Gets or sets the context lines after the block in version 2
	/// </summary>
	public ReadOnlyCollection<string> ContextAfter2 { get; set; } = null!;
}
