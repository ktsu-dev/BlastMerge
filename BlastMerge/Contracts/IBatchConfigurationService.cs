// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

using System.Collections.Generic;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;

/// <summary>
/// Service for managing batch configurations with persistent storage.
/// </summary>
public interface IBatchConfigurationService
{
	/// <summary>
	/// Saves a batch configuration.
	/// </summary>
	/// <param name="batch">The batch configuration to save.</param>
	/// <returns>True if saved successfully, false otherwise.</returns>
	public Task<bool> SaveBatchAsync(BatchConfiguration batch);

	/// <summary>
	/// Loads a batch configuration by name.
	/// </summary>
	/// <param name="name">The name of the batch to load.</param>
	/// <returns>The batch configuration, or null if not found.</returns>
	public Task<BatchConfiguration?> LoadBatchAsync(BatchName name);

	/// <summary>
	/// Lists all available batch configuration names.
	/// </summary>
	/// <returns>A read-only collection of batch configuration names.</returns>
	public Task<IReadOnlyCollection<BatchName>> ListBatchesAsync();

	/// <summary>
	/// Deletes a batch configuration.
	/// </summary>
	/// <param name="name">The name of the batch to delete.</param>
	/// <returns>True if deleted successfully, false otherwise.</returns>
	public Task<bool> DeleteBatchAsync(BatchName name);

	/// <summary>
	/// Gets all batch configurations with their details.
	/// </summary>
	/// <returns>A read-only collection of all batch configurations.</returns>
	public Task<IReadOnlyCollection<BatchConfiguration>> GetAllBatchesAsync();

	/// <summary>
	/// Creates and saves a default batch configuration if none exist.
	/// </summary>
	/// <returns>True if default was created, false if batches already exist.</returns>
	public Task<bool> CreateDefaultBatchIfNoneExistAsync();
}
