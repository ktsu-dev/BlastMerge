// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Represents a block type for manual selection
/// </summary>
public enum BlockType
{
	/// <summary>
	/// Content only exists in version 2 (insertion)
	/// </summary>
	Insert,

	/// <summary>
	/// Content only exists in version 1 (deletion)
	/// </summary>
	Delete,

	/// <summary>
	/// Content differs between versions (replacement)
	/// </summary>
	Replace
}
