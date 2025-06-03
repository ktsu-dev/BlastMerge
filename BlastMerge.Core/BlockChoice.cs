// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

/// <summary>
/// Represents the user's choice for a merge block
/// </summary>
public enum BlockChoice
{
	/// <summary>
	/// Use content from version 1
	/// </summary>
	UseVersion1,

	/// <summary>
	/// Use content from version 2
	/// </summary>
	UseVersion2,

	/// <summary>
	/// Include content from both versions
	/// </summary>
	UseBoth,

	/// <summary>
	/// Skip this content entirely
	/// </summary>
	Skip,

	/// <summary>
	/// Include the addition
	/// </summary>
	Include,

	/// <summary>
	/// Keep the content (don't delete)
	/// </summary>
	Keep,

	/// <summary>
	/// Remove the content (confirm deletion)
	/// </summary>
	Remove
}
