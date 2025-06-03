// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

/// <summary>
/// Represents a merge conflict that needs resolution
/// </summary>
/// <param name="LineNumber"> Gets the line number where the conflict occurs </param>
/// <param name="Content1"> Gets the content from the first file </param>
/// <param name="Content2"> Gets the content from the second file </param>
/// <param name="ResolvedContent"> Gets or sets the resolved content chosen by the user </param>
/// <param name="IsResolved"> Gets or sets whether this conflict has been resolved </param>
public record MergeConflict(int LineNumber, string? Content1, string? Content2, string? ResolvedContent, bool IsResolved)
{
	/// <summary>
	/// Gets or sets the resolved content chosen by the user
	/// </summary>
	public string? ResolvedContent { get; set; } = ResolvedContent;

	/// <summary>
	/// Gets or sets whether this conflict has been resolved
	/// </summary>
	public bool IsResolved { get; set; } = IsResolved;
}

