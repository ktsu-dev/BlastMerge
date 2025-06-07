// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Models;

/// <summary>
/// Represents the available choices for compare operations
/// </summary>
public enum CompareChoice
{
	/// <summary>
	/// Compare files in a directory
	/// </summary>
	CompareFilesInDirectory,

	/// <summary>
	/// Compare two directories
	/// </summary>
	CompareTwoDirectories,

	/// <summary>
	/// Compare two specific files
	/// </summary>
	CompareTwoSpecificFiles,

	/// <summary>
	/// Return to the main menu
	/// </summary>
	BackToMainMenu
}
