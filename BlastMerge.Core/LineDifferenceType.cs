// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

/// <summary>
/// Defines the types of line differences
/// </summary>
public enum LineDifferenceType
{
	/// <summary>
	/// Line was added in the second file
	/// </summary>
	Added,

	/// <summary>
	/// Line was deleted from the first file
	/// </summary>
	Deleted,

	/// <summary>
	/// Line was modified between files
	/// </summary>
	Modified
}

