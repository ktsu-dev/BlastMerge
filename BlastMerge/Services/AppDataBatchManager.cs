// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.Models;

/// <summary>
/// Manages batch configurations using AppDataStorage for improved persistence and safety.
/// </summary>
public static class AppDataBatchManager
{
	/// <summary>
	/// Gets the application data storage instance.
	/// </summary>
	private static BlastMergeAppData AppData => BlastMergeAppData.Get();

	/// <summary>
	/// Saves a batch configuration.
	/// </summary>
	/// <param name="batch">The batch configuration to save.</param>
	/// <returns>True if saved successfully, false otherwise.</returns>
	public static bool SaveBatch(BatchConfiguration batch)
	{
		ArgumentNullException.ThrowIfNull(batch);

		if (string.IsNullOrWhiteSpace(batch.Name))
		{
			throw new ArgumentException("Batch name cannot be null, empty, or whitespace.", nameof(batch));
		}

		if (!batch.IsValid())
		{
			return false;
		}

		try
		{
			batch.LastModified = DateTime.UtcNow;
			AppData.BatchConfigurations[batch.Name] = batch;

			if (AppData.Settings.AutoSaveEnabled)
			{
				AppData.Save();
			}
			else
			{
				BlastMergeAppData.QueueSave();
			}

			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	/// <summary>
	/// Loads a batch configuration by name.
	/// </summary>
	/// <param name="name">The name of the batch to load.</param>
	/// <returns>The batch configuration, or null if not found.</returns>
	public static BatchConfiguration? LoadBatch(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Name cannot be null, empty, or whitespace.", nameof(name));
		}

		return AppData.BatchConfigurations.TryGetValue(name, out BatchConfiguration? batch) ? batch : null;
	}

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	/// <returns>A read-only collection of batch configuration names.</returns>
	public static IReadOnlyCollection<string> ListBatches() =>
		AppData.BatchConfigurations.Keys.OrderBy(name => name).ToList().AsReadOnly();

	/// <summary>
	/// Deletes a batch configuration.
	/// </summary>
	/// <param name="name">The name of the batch to delete.</param>
	/// <returns>True if deleted successfully, false otherwise.</returns>
	public static bool DeleteBatch(string name)
	{
		ArgumentNullException.ThrowIfNull(name);

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Name cannot be null, empty, or whitespace.", nameof(name));
		}

		try
		{
			bool removed = AppData.BatchConfigurations.Remove(name);

			if (removed)
			{
				if (AppData.Settings.AutoSaveEnabled)
				{
					AppData.Save();
				}
				else
				{
					BlastMergeAppData.QueueSave();
				}
			}

			return removed;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	/// <summary>
	/// Gets all batch configurations with their details.
	/// </summary>
	/// <returns>A read-only collection of all batch configurations.</returns>
	public static IReadOnlyCollection<BatchConfiguration> GetAllBatches()
	{
		return AppData.BatchConfigurations.Values
			.OrderBy(b => b.Name)
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Creates and saves a default batch configuration if none exist.
	/// </summary>
	/// <returns>True if default was created, false if batches already exist.</returns>
	public static bool CreateDefaultBatchIfNoneExist()
	{
		if (AppData.BatchConfigurations.Count == 0)
		{
			BatchConfiguration defaultBatch = BatchConfiguration.CreateDefault();
			return SaveBatch(defaultBatch);
		}
		return false;
	}

	/// <summary>
	/// Records that a batch configuration was used for recent batch tracking.
	/// </summary>
	/// <param name="batchName">The name of the batch configuration that was used.</param>
	public static void RecordBatchUsage(string batchName)
	{
		ArgumentNullException.ThrowIfNull(batchName);

		if (string.IsNullOrWhiteSpace(batchName))
		{
			throw new ArgumentException("Batch name cannot be null, empty, or whitespace.", nameof(batchName));
		}

		try
		{
			AppData.RecentBatch = new RecentBatchInfo
			{
				BatchName = batchName,
				LastUsed = DateTime.UtcNow
			};

			if (AppData.Settings.AutoSaveEnabled)
			{
				AppData.Save();
			}
			else
			{
				BlastMergeAppData.QueueSave();
			}
		}
		catch (InvalidOperationException)
		{
			// Ignore failures - recent batch tracking is not critical
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore failures - recent batch tracking is not critical
		}
		catch (IOException)
		{
			// Ignore failures - recent batch tracking is not critical
		}
	}

	/// <summary>
	/// Gets the most recently used batch configuration name.
	/// </summary>
	/// <value>The name of the most recent batch, or null if none found.</value>
	public static string? MostRecentBatch => AppData.RecentBatch?.BatchName;

	/// <summary>
	/// Forces a save of all pending changes to disk.
	/// </summary>
	public static void SaveIfRequired() => BlastMergeAppData.SaveIfRequired();
}
