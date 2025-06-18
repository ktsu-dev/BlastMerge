// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
