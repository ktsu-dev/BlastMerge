// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.ConsoleApp.Models;

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
