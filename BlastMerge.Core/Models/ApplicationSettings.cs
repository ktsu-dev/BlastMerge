// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

/// <summary>
/// Represents application-wide settings and preferences.
/// </summary>
public class ApplicationSettings
{
	/// <summary>
	/// Gets or sets the maximum number of history entries to keep per prompt type.
	/// </summary>
	public int MaxHistoryEntriesPerPrompt { get; set; } = 50;

	/// <summary>
	/// Gets or sets whether to automatically save configuration changes.
	/// </summary>
	public bool AutoSaveEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the last configuration schema version.
	/// </summary>
	public string ConfigurationVersion { get; set; } = "1.0.0";
}
