// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;

/// <summary>
/// Manages batch configurations using PersistenceProvider for improved persistence and safety.
/// </summary>
public class AppDataBatchManager(BlastMergePersistenceService persistenceService)
{
	private readonly BlastMergePersistenceService persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));

	/// <summary>
	/// Saves a batch configuration.
	/// </summary>
	/// <param name="batch">The batch configuration to save.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if saved successfully, false otherwise.</returns>
	public async Task<bool> SaveBatchAsync(BatchConfiguration batch, CancellationToken cancellationToken = default)
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
			BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
			appData.BatchConfigurations[batch.Name] = batch;

			await persistenceService.SaveAsync(cancellationToken).ConfigureAwait(false);

			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}

	/// <summary>
	/// Loads a batch configuration by name.
	/// </summary>
	/// <param name="name">The name of the batch to load.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The batch configuration, or null if not found.</returns>
	public async Task<BatchConfiguration?> LoadBatchAsync(string name, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(name);

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Name cannot be null, empty, or whitespace.", nameof(name));
		}

		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		return appData.BatchConfigurations.TryGetValue(name, out BatchConfiguration? batch) ? batch : null;
	}

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A read-only collection of batch configuration names.</returns>
	public async Task<IReadOnlyCollection<string>> ListBatchesAsync(CancellationToken cancellationToken = default)
	{
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		return appData.BatchConfigurations.Keys.OrderBy(name => name).ToList().AsReadOnly();
	}

	/// <summary>
	/// Deletes a batch configuration.
	/// </summary>
	/// <param name="name">The name of the batch to delete.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if deleted successfully, false otherwise.</returns>
	public async Task<bool> DeleteBatchAsync(string name, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(name);

		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Name cannot be null, empty, or whitespace.", nameof(name));
		}

		try
		{
			BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
			bool removed = appData.BatchConfigurations.Remove(name);

			if (removed)
			{
				await persistenceService.SaveAsync(cancellationToken).ConfigureAwait(false);
			}

			return removed;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}

	/// <summary>
	/// Gets all batch configurations with their details.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A read-only collection of all batch configurations.</returns>
	public async Task<IReadOnlyCollection<BatchConfiguration>> GetAllBatchesAsync(CancellationToken cancellationToken = default)
	{
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		return appData.BatchConfigurations.Values
			.OrderBy(b => b.Name)
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Creates and saves a default batch configuration if none exist.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if default was created, false if batches already exist.</returns>
	public async Task<bool> CreateDefaultBatchIfNoneExistAsync(CancellationToken cancellationToken = default)
	{
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		if (appData.BatchConfigurations.Count == 0)
		{
			BatchConfiguration defaultBatch = BatchConfiguration.CreateDefault();
			return await SaveBatchAsync(defaultBatch, cancellationToken).ConfigureAwait(false);
		}
		return false;
	}

	/// <summary>
	/// Records that a batch configuration was used for recent batch tracking.
	/// </summary>
	/// <param name="batchName">The name of the batch configuration that was used.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task RecordBatchUsageAsync(string batchName, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(batchName);

		if (string.IsNullOrWhiteSpace(batchName))
		{
			throw new ArgumentException("Batch name cannot be null, empty, or whitespace.", nameof(batchName));
		}

		try
		{
			BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
			appData.RecentBatch = new RecentBatchInfo
			{
				BatchName = batchName,
				LastUsed = DateTime.UtcNow
			};

			await persistenceService.SaveAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			// Ignore failures - recent batch tracking is not critical
		}
	}

	/// <summary>
	/// Gets the most recently used batch configuration name.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The name of the most recent batch, or null if none found.</returns>
	public async Task<string?> GetMostRecentBatchAsync(CancellationToken cancellationToken = default)
	{
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		return appData.RecentBatch?.BatchName;
	}

	/// <summary>
	/// Forces a save of all pending changes to disk.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task SaveIfRequiredAsync(CancellationToken cancellationToken = default) =>
		await persistenceService.SaveAsync(cancellationToken).ConfigureAwait(false);
}
