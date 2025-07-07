// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

using System.Threading.Tasks;
using ktsu.BlastMerge.Models;

/// <summary>
/// Service for managing recent batch information with persistent storage.
/// </summary>
public interface IRecentBatchService
{
	/// <summary>
	/// Records that a batch configuration was used.
	/// </summary>
	/// <param name="batchName">The name of the batch configuration that was used.</param>
	public Task RecordBatchUsageAsync(BatchName batchName);

	/// <summary>
	/// Gets the most recently used batch information.
	/// </summary>
	/// <returns>The recent batch information, or null if none found.</returns>
	public Task<RecentBatchInfo?> GetMostRecentBatchAsync();

	/// <summary>
	/// Gets the most recently used batch configuration name.
	/// </summary>
	/// <returns>The name of the most recent batch, or null if none found.</returns>
	public Task<BatchName?> GetMostRecentBatchNameAsync();

	/// <summary>
	/// Clears the recent batch information.
	/// </summary>
	public Task ClearRecentBatchAsync();
}
