// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Models;

using System;

/// <summary>
/// Represents information about the most recently used batch configuration.
/// </summary>
public class RecentBatchInfo
{
	/// <summary>
	/// Gets or sets the name of the most recent batch.
	/// </summary>
	public string BatchName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets when the batch was last used.
	/// </summary>
	public DateTime LastUsed { get; set; }
}
