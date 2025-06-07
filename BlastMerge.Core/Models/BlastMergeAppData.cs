// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System.Collections.Generic;
using ktsu.AppDataStorage;

/// <summary>
/// Main application data storage for BlastMerge, managing all persistent state.
/// </summary>
public class BlastMergeAppData : AppData<BlastMergeAppData>
{
	/// <summary>
	/// Gets or sets the collection of batch configurations.
	/// </summary>
	public Dictionary<string, BatchConfiguration> BatchConfigurations { get; init; } = [];

	/// <summary>
	/// Gets or sets the input history organized by prompt type.
	/// </summary>
	public Dictionary<string, List<string>> InputHistory { get; init; } = [];

	/// <summary>
	/// Gets or sets information about the most recently used batch.
	/// </summary>
	public RecentBatchInfo? RecentBatch { get; set; }

	/// <summary>
	/// Gets or sets application settings and preferences.
	/// </summary>
	public ApplicationSettings Settings { get; set; } = new();
}
