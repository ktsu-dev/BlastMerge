// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System.Collections.Generic;

/// <summary>
/// Represents context lines around a block
/// </summary>
public class BlockContext
{
	/// <summary>
	/// Gets or sets the context lines before the block in version 1
	/// </summary>
	public IReadOnlyCollection<string> ContextBefore1 { get; set; } = [];

	/// <summary>
	/// Gets or sets the context lines after the block in version 1
	/// </summary>
	public IReadOnlyCollection<string> ContextAfter1 { get; set; } = [];

	/// <summary>
	/// Gets or sets the context lines before the block in version 2
	/// </summary>
	public IReadOnlyCollection<string> ContextBefore2 { get; set; } = [];

	/// <summary>
	/// Gets or sets the context lines after the block in version 2
	/// </summary>
	public IReadOnlyCollection<string> ContextAfter2 { get; set; } = [];
}
