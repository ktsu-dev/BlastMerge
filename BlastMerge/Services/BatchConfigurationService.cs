// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services.Base;
using ktsu.PersistenceProvider;
using ktsu.Semantics;

/// <summary>
/// Service for managing batch configurations using PersistenceProvider.
/// </summary>
/// <remarks>
/// Initializes a new instance of the BatchConfigurationService class.
/// </remarks>
/// <param name="persistenceProvider">The persistence provider for storage operations.</param>
public class BatchConfigurationService(IPersistenceProvider<string> persistenceProvider)
	: BaseKeyedPersistenceService<BatchConfiguration>(persistenceProvider), IBatchConfigurationService
{
	/// <inheritdoc/>
	protected override string StorageKey => "batches"; // Not used by keyed service, but required by base

	/// <inheritdoc/>
	protected override string KeyPrefix => "batch:";

	/// <inheritdoc/>
	protected override string KeyListStorageKey => "batches";

	/// <inheritdoc/>
	public async Task<bool> SaveBatchAsync(BatchConfiguration batch)
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

		batch.LastModified = DateTime.UtcNow;
		return await SaveItemAsync(batch.Name, batch).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task<BatchConfiguration?> LoadBatchAsync(BatchName name)
	{
		ArgumentNullException.ThrowIfNull(name);

		if (string.IsNullOrWhiteSpace(name.ToString()))
		{
			throw new ArgumentException("Name cannot be null, empty, or whitespace.", nameof(name));
		}

		return await LoadItemAsync(name.ToString()).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyCollection<BatchName>> ListBatchesAsync()
	{
		List<string> batchNames = await GetKeyListAsync().ConfigureAwait(false);
		return batchNames.Select(name => name.As<BatchName>()).ToList().AsReadOnly();
	}

	/// <inheritdoc/>
	public async Task<bool> DeleteBatchAsync(BatchName name)
	{
		ArgumentNullException.ThrowIfNull(name);

		if (string.IsNullOrWhiteSpace(name.ToString()))
		{
			throw new ArgumentException("Name cannot be null, empty, or whitespace.", nameof(name));
		}

		return await RemoveItemAsync(name.ToString()).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyCollection<BatchConfiguration>> GetAllBatchesAsync()
	{
		Dictionary<string, BatchConfiguration> allBatches = await GetAllItemsAsync().ConfigureAwait(false);
		return allBatches.Values.OrderBy(b => b.Name).ToList().AsReadOnly();
	}

	/// <inheritdoc/>
	public async Task<bool> CreateDefaultBatchIfNoneExistAsync()
	{
		List<string> batchNames = await GetKeyListAsync().ConfigureAwait(false);
		if (batchNames.Count == 0)
		{
			BatchConfiguration defaultBatch = BatchConfiguration.CreateDefault();
			return await SaveBatchAsync(defaultBatch).ConfigureAwait(false);
		}
		return false;
	}
}
