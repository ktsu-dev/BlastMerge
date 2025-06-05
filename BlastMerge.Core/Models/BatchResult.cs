// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System.Collections.ObjectModel;

/// <summary>
/// Represents the result of processing an entire batch
/// </summary>
public class BatchResult
{
	/// <summary>
	/// Gets or sets the name of the batch that was processed
	/// </summary>
	public string BatchName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the batch processing was successful
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets the collection of pattern results
	/// </summary>
	public Collection<PatternResult> PatternResults { get; init; } = [];

	/// <summary>
	/// Gets or sets the total number of patterns processed
	/// </summary>
	public int TotalPatternsProcessed { get; set; }

	/// <summary>
	/// Gets or sets the number of successful patterns
	/// </summary>
	public int SuccessfulPatterns { get; set; }

	/// <summary>
	/// Gets or sets a summary message of the batch processing
	/// </summary>
	public string Summary { get; set; } = string.Empty;
}
