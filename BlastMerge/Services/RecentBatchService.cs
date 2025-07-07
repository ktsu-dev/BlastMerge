// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Threading.Tasks;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services.Base;
using ktsu.PersistenceProvider;
using ktsu.Semantics;

/// <summary>
/// Service for managing recent batch information using PersistenceProvider.
/// </summary>
/// <remarks>
/// Initializes a new instance of the RecentBatchService class.
/// </remarks>
/// <param name="persistenceProvider">The persistence provider for storage operations.</param>
public class RecentBatchService(IPersistenceProvider<string> persistenceProvider)
	: BasePersistenceService<RecentBatchInfo>(persistenceProvider), IRecentBatchService
{
	/// <inheritdoc/>
	protected override string StorageKey => "recent_batch";

	/// <inheritdoc/>
	public async Task RecordBatchUsageAsync(BatchName batchName)
	{
		ArgumentNullException.ThrowIfNull(batchName);

		if (string.IsNullOrWhiteSpace(batchName.ToString()))
		{
			throw new ArgumentException("Batch name cannot be null, empty, or whitespace.", nameof(batchName));
		}

		RecentBatchInfo recentBatch = new()
		{
			BatchName = batchName.ToString(),
			LastUsed = DateTime.UtcNow
		};

		await SaveAsync(recentBatch).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task<RecentBatchInfo?> GetMostRecentBatchAsync()
	{
		RecentBatchInfo recentBatch = await LoadAsync().ConfigureAwait(false);

		// Return null if we got a default (empty) instance
		return string.IsNullOrEmpty(recentBatch.BatchName) ? null : recentBatch;
	}

	/// <inheritdoc/>
	public async Task<BatchName?> GetMostRecentBatchNameAsync()
	{
		RecentBatchInfo? recentBatch = await GetMostRecentBatchAsync().ConfigureAwait(false);
		return !string.IsNullOrEmpty(recentBatch?.BatchName) ? recentBatch.BatchName.As<BatchName>() : null;
	}

	/// <inheritdoc/>
	public async Task ClearRecentBatchAsync() =>
		await RemoveAsync().ConfigureAwait(false);
}
