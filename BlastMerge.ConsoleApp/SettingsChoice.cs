// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

/// <summary>
/// Represents the available choices in the settings menu
/// </summary>
public enum SettingsChoice
{
	/// <summary>
	/// View configuration file paths and directories
	/// </summary>
	ViewConfigurationPaths,

	/// <summary>
	/// Clear input history data
	/// </summary>
	ClearInputHistory,

	/// <summary>
	/// View application statistics and memory usage
	/// </summary>
	ViewStatistics,

	/// <summary>
	/// Return to the main menu
	/// </summary>
	BackToMainMenu
}
