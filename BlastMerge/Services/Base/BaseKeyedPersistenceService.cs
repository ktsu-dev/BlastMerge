// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services.Base;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ktsu.PersistenceProvider;

/// <summary>
/// Base class for services that store multiple items with string keys.
/// Provides common keyed persistence operations with consistent error handling.
/// </summary>
/// <typeparam name="T">The type of data being persisted.</typeparam>
/// <remarks>
/// Initializes a new instance of the BaseKeyedPersistenceService class.
/// </remarks>
/// <param name="persistenceProvider">The persistence provider for storage operations.</param>
public abstract class BaseKeyedPersistenceService<T>(IPersistenceProvider<string> persistenceProvider) : BasePersistenceService<T>(persistenceProvider) where T : class, new()
{
	/// <summary>
	/// Gets the prefix used for individual item keys.
	/// </summary>
	protected abstract string KeyPrefix { get; }

	/// <summary>
	/// Gets the key used to store the list of all item keys.
	/// </summary>
	protected abstract string KeyListStorageKey { get; }

	/// <summary>
	/// Loads an item by its key.
	/// </summary>
	/// <param name="key">The key of the item to load.</param>
	/// <returns>The loaded item or null if not found.</returns>
	protected async Task<T?> LoadItemAsync(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException("Key cannot be null, empty, or whitespace.", nameof(key));
		}

		return await ExecuteNonCriticalOperationAsync(async () =>
		{
			string itemKey = KeyPrefix + key;
			return await PersistenceProvider.RetrieveAsync<T>(itemKey).ConfigureAwait(false);
		}, defaultValue: null).ConfigureAwait(false);
	}

	/// <summary>
	/// Saves an item with the specified key.
	/// </summary>
	/// <param name="key">The key to save the item under.</param>
	/// <param name="item">The item to save.</param>
	/// <returns>True if successful, false otherwise.</returns>
	protected async Task<bool> SaveItemAsync(string key, T item)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException("Key cannot be null, empty, or whitespace.", nameof(key));
		}

		ArgumentNullException.ThrowIfNull(item);

		string itemKey = KeyPrefix + key;
		bool saveSuccess = await ExecuteNonCriticalOperationAsync(async () => await PersistenceProvider.StoreAsync(itemKey, item).ConfigureAwait(false)).ConfigureAwait(false);

		if (saveSuccess)
		{
			// Update the key list if save was successful
			await AddToKeyListAsync(key).ConfigureAwait(false);
		}

		return saveSuccess;
	}

	/// <summary>
	/// Removes an item by its key.
	/// </summary>
	/// <param name="key">The key of the item to remove.</param>
	/// <returns>True if successful, false otherwise.</returns>
	protected async Task<bool> RemoveItemAsync(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException("Key cannot be null, empty, or whitespace.", nameof(key));
		}

		string itemKey = KeyPrefix + key;

		// Check if item exists first
		bool exists = await ExecuteNonCriticalOperationAsync(async () => await PersistenceProvider.ExistsAsync(itemKey).ConfigureAwait(false), defaultValue: false).ConfigureAwait(false);

		if (!exists)
		{
			return false;
		}

		// Remove the item
		bool removeSuccess = await ExecuteNonCriticalOperationAsync(async () => await PersistenceProvider.RemoveAsync(itemKey).ConfigureAwait(false)).ConfigureAwait(false);

		if (removeSuccess)
		{
			// Update the key list if removal was successful
			await RemoveFromKeyListAsync(key).ConfigureAwait(false);
		}

		return removeSuccess;
	}

	/// <summary>
	/// Checks if an item exists by its key.
	/// </summary>
	/// <param name="key">The key to check.</param>
	/// <returns>True if the item exists, false otherwise.</returns>
	protected async Task<bool> ItemExistsAsync(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}

		string itemKey = KeyPrefix + key;
		return await ExecuteNonCriticalOperationAsync(async () => await PersistenceProvider.ExistsAsync(itemKey).ConfigureAwait(false), defaultValue: false).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets the list of all stored keys.
	/// </summary>
	/// <returns>A list of all keys.</returns>
	protected async Task<List<string>> GetKeyListAsync()
	{
		return await ExecuteNonCriticalOperationAsync(async () =>
		{
			List<string>? keys = await PersistenceProvider.RetrieveAsync<List<string>>(KeyListStorageKey).ConfigureAwait(false);
			return keys ?? [];
		}, defaultValue: []).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets all stored items.
	/// </summary>
	/// <returns>A dictionary of key-item pairs.</returns>
	protected async Task<Dictionary<string, T>> GetAllItemsAsync()
	{
		List<string> keys = await GetKeyListAsync().ConfigureAwait(false);
		Dictionary<string, T> result = [];

		foreach (string key in keys)
		{
			T? item = await LoadItemAsync(key).ConfigureAwait(false);
			if (item != null)
			{
				result[key] = item;
			}
		}

		return result;
	}

	/// <summary>
	/// Clears all stored items and keys.
	/// </summary>
	/// <returns>True if successful, false otherwise.</returns>
	protected async Task<bool> ClearAllAsync()
	{
		List<string> keys = await GetKeyListAsync().ConfigureAwait(false);

		// Remove all individual items
		foreach (string key in keys)
		{
			await RemoveItemAsync(key).ConfigureAwait(false);
		}

		// Clear the key list
		return await ExecuteNonCriticalOperationAsync(async () => await PersistenceProvider.RemoveAsync(KeyListStorageKey).ConfigureAwait(false)).ConfigureAwait(false);
	}

	/// <summary>
	/// Adds a key to the key list if not already present.
	/// </summary>
	/// <param name="key">The key to add.</param>
	private async Task AddToKeyListAsync(string key)
	{
		await ExecuteNonCriticalOperationAsync(async () =>
		{
			List<string> keys = await GetKeyListAsync().ConfigureAwait(false);
			if (!keys.Contains(key))
			{
				keys.Add(key);
				keys.Sort(); // Keep keys sorted for consistency
				await PersistenceProvider.StoreAsync(KeyListStorageKey, keys).ConfigureAwait(false);
			}
		}).ConfigureAwait(false);
	}

	/// <summary>
	/// Removes a key from the key list.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	private async Task RemoveFromKeyListAsync(string key)
	{
		await ExecuteNonCriticalOperationAsync(async () =>
		{
			List<string> keys = await GetKeyListAsync().ConfigureAwait(false);
			if (keys.Remove(key))
			{
				await PersistenceProvider.StoreAsync(KeyListStorageKey, keys).ConfigureAwait(false);
			}
		}).ConfigureAwait(false);
	}
}

