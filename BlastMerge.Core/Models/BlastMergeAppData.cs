// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

using System;
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

	/// <summary>
	/// Represents application-wide settings and preferences.
	/// </summary>
	public class ApplicationSettings
	{
		/// <summary>
		/// Gets or sets the maximum number of history entries to keep per prompt type.
		/// </summary>
		public int MaxHistoryEntriesPerPrompt { get; set; } = 50;

		/// <summary>
		/// Gets or sets whether to automatically save configuration changes.
		/// </summary>
		public bool AutoSaveEnabled { get; set; } = true;

		/// <summary>
		/// Gets or sets the last configuration schema version.
		/// </summary>
		public string ConfigurationVersion { get; set; } = "1.0.0";
	}
}
