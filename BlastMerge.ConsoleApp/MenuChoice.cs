// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

/// <summary>
/// Represents the available menu choices in the application
/// </summary>
public enum MenuChoice
{
	/// <summary>
	/// Find and process files
	/// </summary>
	FindFiles,

	/// <summary>
	/// Run iterative merge process
	/// </summary>
	IterativeMerge,

	/// <summary>
	/// Compare files and show differences
	/// </summary>
	CompareFiles,

	/// <summary>
	/// Batch operations and configurations
	/// </summary>
	BatchOperations,

	/// <summary>
	/// Settings and configuration
	/// </summary>
	Settings,

	/// <summary>
	/// Help and information
	/// </summary>
	Help,

	/// <summary>
	/// Exit the application
	/// </summary>
	Exit
}
