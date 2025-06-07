// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Models;

/// <summary>
/// Represents the result of a batch action performed during pattern processing.
/// </summary>
public class BatchActionResult
{
	/// <summary>
	/// Gets or sets a value indicating whether batch processing should stop.
	/// </summary>
	public bool ShouldStop { get; set; }
}
