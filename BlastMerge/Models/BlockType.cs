// Copyright (c) 2023-2026 ktsu-dev contributors

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
