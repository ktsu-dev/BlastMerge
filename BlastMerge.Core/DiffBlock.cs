// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
/// Represents a diff block for manual selection
/// </summary>
/// <param name="Type"> Gets or sets the type of this block </param>
public record DiffBlock(BlockType Type)
{
	/// <summary>
	/// Gets the lines from version 1
	/// </summary>
	public Collection<string> Lines1 { get; } = [];

	/// <summary>
	/// Gets the lines from version 2
	/// </summary>
	public Collection<string> Lines2 { get; } = [];

	/// <summary>
	/// Gets the line numbers from version 1
	/// </summary>
	public Collection<int> LineNumbers1 { get; } = [];

	/// <summary>
	/// Gets the line numbers from version 2
	/// </summary>
	public Collection<int> LineNumbers2 { get; } = [];

	/// <summary>
	/// Gets the last line number from version 1
	/// </summary>
	public int LastLineNumber1 => LineNumbers1.Count > 0 ? LineNumbers1.Last() : 0;

	/// <summary>
	/// Gets the last line number from version 2
	/// </summary>
	public int LastLineNumber2 => LineNumbers2.Count > 0 ? LineNumbers2.Last() : 0;
}
