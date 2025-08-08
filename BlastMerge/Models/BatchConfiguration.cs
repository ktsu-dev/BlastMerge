// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

/// <summary>
/// Represents a batch configuration for processing multiple file patterns
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
public class BatchConfiguration
{
	/// <summary>
	/// Gets or sets the name of the batch configuration
	/// </summary>
	[Required]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the list of file patterns to process
	/// </summary>
	[Required]
	public Collection<string> FilePatterns { get; set; } = [];

	/// <summary>
	/// Gets or sets the search paths (directories) to search in. If empty, uses the directory parameter from ProcessBatch.
	/// </summary>
	public Collection<string> SearchPaths { get; set; } = [];

	/// <summary>
	/// Gets or sets the path exclusion patterns to exclude specific directories or file paths from the search
	/// </summary>
	public Collection<string> PathExclusionPatterns { get; set; } = [];

	/// <summary>
	/// Creates a default batch configuration for common repository files
	/// </summary>
	/// <returns>A batch configuration with common repository file patterns</returns>
	public static BatchConfiguration CreateDefault()
	{
		return new BatchConfiguration
		{
			Name = "Common Repository Files",
			FilePatterns = [
				".gitignore",
				".gitattributes",
				".editorconfig",
				"*.yml",
				"*.yaml",
				".mailmap",
				"LICENSE",
				"LICENSE.md",
				"LICENSE.txt"
			],
		};
	}

	/// <summary>
	/// Creates a comprehensive batch configuration for repository synchronization
	/// </summary>
	/// <returns>A batch configuration with the user's requested patterns</returns>
	public static BatchConfiguration CreateRepositorySyncBatch()
	{
		return new BatchConfiguration
		{
			Name = "Repository Sync Batch",
			FilePatterns = [
				".runsettings",
				".mailmap",
				".gitignore",
				".gitattributes",
				".editorconfig",
				"*.yml",
				"icon.png",
				"*.ps1",
				"*.psm1",
				"*.psd1",
				"LICENSE.template"
			],
		};
	}

	/// <summary>
	/// Validates the batch configuration
	/// </summary>
	/// <returns>True if valid, false otherwise</returns>
	public bool IsValid()
	{
		return !string.IsNullOrWhiteSpace(Name) &&
			   FilePatterns.Count > 0 &&
			   FilePatterns.All(p => !string.IsNullOrWhiteSpace(p));
	}
}
