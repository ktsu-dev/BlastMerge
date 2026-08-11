// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.ConsoleApp.Contracts;

/// <summary>
/// Sync operation choices.
/// </summary>
internal enum SyncChoice
{
	/// <summary>
	/// Synchronize to the newest version.
	/// </summary>
	SyncToNewest,

	/// <summary>
	/// Choose a reference file manually.
	/// </summary>
	ChooseReference,

	/// <summary>
	/// Go back to previous menu.
	/// </summary>
	Back
}
