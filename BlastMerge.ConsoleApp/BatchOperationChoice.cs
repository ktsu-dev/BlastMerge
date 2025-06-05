// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

/// <summary>
/// Represents the available choices in the batch operations menu
/// </summary>
public enum BatchOperationChoice
{
	/// <summary>
	/// Manage batch configurations (create, edit, delete)
	/// </summary>
	ManageBatchConfigurations,

	/// <summary>
	/// Run a specific batch configuration
	/// </summary>
	RunBatchConfiguration,

	/// <summary>
	/// Return to the main menu
	/// </summary>
	BackToMainMenu
}
