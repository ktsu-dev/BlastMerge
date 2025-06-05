// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

/// <summary>
/// Represents the available choices when multiple versions are found for a batch pattern
/// </summary>
public enum BatchActionChoice
{
	/// <summary>
	/// Run iterative merge on the multiple versions
	/// </summary>
	RunIterativeMerge,

	/// <summary>
	/// Skip this pattern and continue with the next one
	/// </summary>
	SkipPattern,

	/// <summary>
	/// Stop the entire batch processing
	/// </summary>
	StopBatchProcessing
}
