// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.ConsoleApp.Models;

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
