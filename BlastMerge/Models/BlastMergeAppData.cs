// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Models;

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
	/// Resets the singleton instance for testing purposes.
	/// This method should only be used in test environments.
	/// </summary>
	/// <remarks>
	/// <para>
	/// ktsu.AppDataStorage caches the singleton and offers no way to discard it, so the cached instance is
	/// emptied in place instead: its batches, input history and recent batch are cleared and its settings
	/// are restored to their defaults. A property added to this class needs resetting here too.
	/// </para>
	/// <para>
	/// This used to clear a static field named <c>_instance</c>, which the base class does not have,
	/// so it silently did nothing and one instance's state lived for the whole test run.
	/// </para>
	/// </remarks>
	public static void ResetForTesting()
	{
		BlastMergeAppData appData = Get();
		appData.BatchConfigurations.Clear();
		appData.InputHistory.Clear();
		appData.RecentBatch = null;
		appData.Settings = new();
	}
}
