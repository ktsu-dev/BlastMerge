// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

/// <summary>
/// Represents the result of processing a single pattern in a batch operation.
/// </summary>
public class BatchPatternResult
{
	/// <summary>
	/// Gets or sets a value indicating whether the pattern was processed.
	/// </summary>
	public bool WasProcessed { get; set; }

	/// <summary>
	/// Gets or sets the number of files found for the pattern.
	/// </summary>
	public int FilesFound { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether batch processing should stop.
	/// </summary>
	public bool ShouldStop { get; set; }
}
