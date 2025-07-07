// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Main application data storage for BlastMerge, managing all persistent state.
/// </summary>
public class BlastMergeAppData
{
	/// <summary>
	/// Initializes a new instance of the BlastMergeAppData class.
	/// </summary>
	public BlastMergeAppData()
	{
		// Initialize collections
		BatchConfigurations = [];
		InputHistory = [];
		LastBatchPatterns = [];
	}

	/// <summary>
	/// Gets the collection of batch configurations.
	/// </summary>
	public Dictionary<string, BatchConfiguration> BatchConfigurations { get; }

	/// <summary>
	/// Gets the input history organized by prompt type.
	/// </summary>
	public Dictionary<string, List<string>> InputHistory { get; }

	/// <summary>
	/// Gets or sets information about the most recently used batch.
	/// </summary>
	public RecentBatchInfo? RecentBatch { get; set; }

	/// <summary>
	/// Gets or sets application settings and preferences.
	/// </summary>
	public ApplicationSettings Settings { get; set; } = new();

	/// <summary>
	/// Gets or sets the last used source file path.
	/// </summary>
	public string LastSourceFilePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the last used target file path.
	/// </summary>
	public string LastTargetFilePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the last used output file path.
	/// </summary>
	public string LastOutputFilePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets the last used batch operation patterns.
	/// </summary>
	public Collection<string> LastBatchPatterns { get; }

	/// <summary>
	/// Gets or sets whether to show line numbers in diff output.
	/// </summary>
	public bool ShowLineNumbers { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to highlight whitespace differences.
	/// </summary>
	public bool HighlightWhitespace { get; set; } = true;

	/// <summary>
	/// Resets all data to default values for testing purposes.
	/// </summary>
	internal void ResetForTesting()
	{
		BatchConfigurations.Clear();
		InputHistory.Clear();
		RecentBatch = null;
		Settings = new ApplicationSettings();
		LastSourceFilePath = string.Empty;
		LastTargetFilePath = string.Empty;
		LastOutputFilePath = string.Empty;
		LastBatchPatterns.Clear();
		ShowLineNumbers = true;
		HighlightWhitespace = true;
	}
}
