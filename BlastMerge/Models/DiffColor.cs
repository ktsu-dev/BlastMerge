// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Models;

/// <summary>
/// Defines colors to use for different parts of the diff output
/// </summary>
public enum DiffColor
{
	/// <summary>
	/// Default console color
	/// </summary>
	Default,

	/// <summary>
	/// Color for deleted lines
	/// </summary>
	Deletion,

	/// <summary>
	/// Color for added lines
	/// </summary>
	Addition,

	/// <summary>
	/// Color for chunk headers
	/// </summary>
	ChunkHeader,

	/// <summary>
	/// Color for file headers
	/// </summary>
	FileHeader
}

