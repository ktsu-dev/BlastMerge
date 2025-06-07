// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents a work item that requires user resolution during batch processing.
/// </summary>
public record ResolutionItem
{
	/// <summary>
	/// Gets or sets the original pattern that matched these files.
	/// </summary>
	public required string Pattern { get; init; }

	/// <summary>
	/// Gets or sets the filename being processed.
	/// </summary>
	public required string FileName { get; init; }

	/// <summary>
	/// Gets or sets the groups of files with different content that need resolution.
	/// </summary>
	public required ReadOnlyCollection<FileGroup> FileGroups { get; init; }

	/// <summary>
	/// Gets or sets the type of resolution needed.
	/// </summary>
	public required ResolutionType ResolutionType { get; init; }

	/// <summary>
	/// Gets or sets the total number of files involved.
	/// </summary>
	public int TotalFiles => FileGroups.Sum(g => g.FilePaths.Count);

	/// <summary>
	/// Gets or sets the number of unique versions.
	/// </summary>
	public int UniqueVersions => FileGroups.Count;
}

/// <summary>
/// Represents the type of resolution needed for a resolution item.
/// </summary>
public enum ResolutionType
{
	/// <summary>
	/// Multiple different versions that need merging.
	/// </summary>
	Merge,

	/// <summary>
	/// All files are identical, no action needed.
	/// </summary>
	Identical,

	/// <summary>
	/// Only one file found, no action needed.
	/// </summary>
	SingleFile,

	/// <summary>
	/// No files found for the pattern.
	/// </summary>
	Empty
}
