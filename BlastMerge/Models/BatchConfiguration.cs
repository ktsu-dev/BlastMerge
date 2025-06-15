// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

/// <summary>
/// Represents a batch configuration for processing multiple file patterns
/// </summary>
public class BatchConfiguration
{
	/// <summary>
	/// Gets or sets the name of the batch configuration
	/// </summary>
	[Required]
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the description of the batch configuration
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the list of file patterns to process
	/// </summary>
	[Required]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
	public Collection<string> FilePatterns { get; set; } = [];

	/// <summary>
	/// Gets or sets the search paths (directories) to search in. If empty, uses the directory parameter from ProcessBatch.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
	public Collection<string> SearchPaths { get; set; } = [];

	/// <summary>
	/// Gets or sets the path exclusion patterns to exclude specific directories or file paths from the search
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "<Pending>")]
	public Collection<string> PathExclusionPatterns { get; set; } = [];

	/// <summary>
	/// Gets or sets the created date
	/// </summary>
	public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Gets or sets the last modified date
	/// </summary>
	public DateTime LastModified { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// Gets or sets whether to skip patterns that don't find any files
	/// </summary>
	public bool SkipEmptyPatterns { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to prompt before processing each pattern
	/// </summary>
	public bool PromptBeforeEachPattern { get; set; }

	/// <summary>
	/// Creates a default batch configuration for common repository files
	/// </summary>
	/// <returns>A batch configuration with common repository file patterns</returns>
	public static BatchConfiguration CreateDefault()
	{
		return new BatchConfiguration
		{
			Name = "Common Repository Files",
			Description = "Standard configuration files commonly synchronized across repositories",
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
			SkipEmptyPatterns = true,
			PromptBeforeEachPattern = false
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
			Description = "Comprehensive batch for synchronizing configuration files across repositories",
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
			SkipEmptyPatterns = true,
			PromptBeforeEachPattern = false
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
