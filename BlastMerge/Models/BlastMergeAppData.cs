// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

using System.Reflection;
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
	/// This method uses reflection to clear the internal singleton cache
	/// of the AppData base class to enable proper test isolation.
	/// </remarks>
	public static void ResetForTesting()
	{
		// Use reflection to access the static instance field in the base class
		// This is necessary because AppData<T> doesn't provide a public reset method
		Type baseType = typeof(AppData<BlastMergeAppData>);
		FieldInfo? instanceField = baseType.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);

		instanceField?.SetValue(null, null);
	}
}
