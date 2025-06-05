// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

/// <summary>
/// Represents the available choices when processing found files
/// </summary>
public enum ProcessFileActionChoice
{
	/// <summary>
	/// View detailed file list showing all files in each group
	/// </summary>
	ViewDetailedFileList,

	/// <summary>
	/// Show differences between file versions
	/// </summary>
	ShowDifferences,

	/// <summary>
	/// Run iterative merge on duplicate files
	/// </summary>
	RunIterativeMergeOnDuplicates,

	/// <summary>
	/// Sync files to make them identical
	/// </summary>
	SyncFiles,

	/// <summary>
	/// Return to the main menu
	/// </summary>
	ReturnToMainMenu
}
