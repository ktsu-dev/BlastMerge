// Copyright (c) 2023-2026 ktsu-dev contributors

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
	/// <para>
	/// ktsu.AppDataStorage holds the singleton in a static read-only <see cref="Lazy{T}"/>, and offers no
	/// way to reset it. The field itself cannot be replaced: the runtime refuses to write a static
	/// read-only field once its type has been initialised. So the cached <see cref="Lazy{T}"/> is reset
	/// in place instead, by copying the state of a freshly constructed, not yet evaluated one over it.
	/// The next call to <see cref="AppData{T}.Get"/> then loads the data again from whichever file
	/// system is configured at that point.
	/// </para>
	/// <para>
	/// This used to clear a static field named <c>_instance</c>, which the base class does not have,
	/// so it silently did nothing and one instance lived for the whole test run.
	/// </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// The base class no longer keeps its singleton where this method expects it.
	/// </exception>
	public static void ResetForTesting()
	{
		PropertyInfo? stateProperty = typeof(AppData<BlastMergeAppData>).GetProperty("InternalState", BindingFlags.NonPublic | BindingFlags.Static);
		if (stateProperty?.GetValue(null) is not Lazy<BlastMergeAppData> cached)
		{
			throw new InvalidOperationException("ktsu.AppDataStorage no longer keeps its singleton in a static Lazy<T> named InternalState; update ResetForTesting.");
		}

		Lazy<BlastMergeAppData> fresh = new(LoadOrCreate, LazyThreadSafetyMode.ExecutionAndPublication);
		foreach (FieldInfo field in typeof(Lazy<BlastMergeAppData>).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
		{
			field.SetValue(cached, field.GetValue(fresh));
		}
	}
}
