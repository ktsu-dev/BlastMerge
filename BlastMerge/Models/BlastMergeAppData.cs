// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ktsu.PersistenceProvider;

/// <summary>
/// Main application data storage for BlastMerge, managing all persistent state.
/// </summary>
public class BlastMergeAppData
{
	private static BlastMergeAppData? _instance;
	private static readonly Lock _lock = new();
	private static IPersistenceProvider<string>? _persistenceProvider;
	private static Timer? _saveTimer;
	private static bool _hasPendingChanges;
	private static readonly Lock _saveTimerLock = new();
	private const string StorageKey = "BlastMergeAppData";

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
	/// Initializes the application data with required providers.
	/// </summary>
	/// <param name="persistenceProvider">The persistence provider for data storage.</param>
	public static void Initialize(IPersistenceProvider<string> persistenceProvider) =>
		_persistenceProvider = persistenceProvider ?? throw new ArgumentNullException(nameof(persistenceProvider));

	/// <summary>
	/// Gets the singleton instance of the application data.
	/// </summary>
	/// <returns>The application data instance.</returns>
	public static BlastMergeAppData Get()
	{
#pragma warning disable CA1508 // Avoid dead conditional code - False positive with lazy initialization pattern
		if (_instance is null)
		{
			lock (_lock)
			{
				_instance ??= LoadFromStorage();
			}
		}
#pragma warning restore CA1508
		return _instance;
	}

	/// <summary>
	/// Saves the current application data to storage.
	/// </summary>
	public void Save()
	{
		if (_persistenceProvider is null)
		{
			throw new InvalidOperationException("BlastMergeAppData must be initialized before use. Call Initialize() first.");
		}

		try
		{
			_persistenceProvider.StoreAsync(StorageKey, this).GetAwaiter().GetResult();
			_hasPendingChanges = false;
		}
		catch (InvalidOperationException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error saving application data: {ex.Message}");
		}
		catch (IOException ex)
		{
			// Log or handle the error as appropriate  
			Console.WriteLine($"Error saving application data: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error saving application data: {ex.Message}");
		}
	}

	/// <summary>
	/// Saves the current application data to storage asynchronously.
	/// </summary>
	/// <returns>A task representing the asynchronous save operation.</returns>
	public async Task SaveAsync()
	{
		if (_persistenceProvider is null)
		{
			throw new InvalidOperationException("BlastMergeAppData must be initialized before use. Call Initialize() first.");
		}

		try
		{
			await _persistenceProvider.StoreAsync(StorageKey, this).ConfigureAwait(false);
			_hasPendingChanges = false;
		}
		catch (InvalidOperationException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error saving application data: {ex.Message}");
		}
		catch (IOException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error saving application data: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error saving application data: {ex.Message}");
		}
	}

	/// <summary>
	/// Queues a save operation to be performed after a delay.
	/// </summary>
	public static void QueueSave()
	{
		lock (_saveTimerLock)
		{
			_hasPendingChanges = true;
			_saveTimer?.Dispose();
			_saveTimer = new Timer(SaveCallback, null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
		}
	}

	/// <summary>
	/// Saves the data if there are pending changes.
	/// </summary>
	public static void SaveIfRequired()
	{
		if (_hasPendingChanges)
		{
			Get().Save();
		}
	}

	/// <summary>
	/// Resets the singleton instance for testing purposes.
	/// This method should only be used in test environments.
	/// </summary>
	public static void ResetForTesting()
	{
		lock (_lock)
		{
			_instance = null;
			_hasPendingChanges = false;
			_saveTimer?.Dispose();
			_saveTimer = null;
		}
	}

	/// <summary>
	/// Loads application data from storage.
	/// </summary>
	/// <returns>The loaded application data or a new instance if loading fails.</returns>
	private static BlastMergeAppData LoadFromStorage()
	{
		if (_persistenceProvider is null)
		{
			throw new InvalidOperationException("BlastMergeAppData must be initialized before use. Call Initialize() first.");
		}

		try
		{
			BlastMergeAppData? result = _persistenceProvider.RetrieveAsync<BlastMergeAppData>(StorageKey).GetAwaiter().GetResult();
			return result ?? new BlastMergeAppData();
		}
		catch (InvalidOperationException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error loading application data: {ex.Message}");
			return new BlastMergeAppData();
		}
		catch (IOException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error loading application data: {ex.Message}");
			return new BlastMergeAppData();
		}
		catch (UnauthorizedAccessException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error loading application data: {ex.Message}");
			return new BlastMergeAppData();
		}
	}

	/// <summary>
	/// Timer callback for saving data.
	/// </summary>
	/// <param name="state">Timer state (unused).</param>
	private static void SaveCallback(object? state)
	{
		try
		{
			Get().Save();
		}
		catch (InvalidOperationException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error in save callback: {ex.Message}");
		}
		catch (IOException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error in save callback: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			// Log or handle the error as appropriate
			Console.WriteLine($"Error in save callback: {ex.Message}");
		}
	}
}
