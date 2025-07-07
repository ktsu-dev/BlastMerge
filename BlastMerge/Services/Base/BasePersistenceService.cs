// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services.Base;

using System;
using System.Threading.Tasks;
using ktsu.PersistenceProvider;

/// <summary>
/// Base class for services that use persistence provider for data storage.
/// Provides common persistence operations with consistent error handling.
/// </summary>
/// <typeparam name="T">The type of data being persisted.</typeparam>
/// <remarks>
/// Initializes a new instance of the BasePersistenceService class.
/// </remarks>
/// <param name="persistenceProvider">The persistence provider for storage operations.</param>
public abstract class BasePersistenceService<T>(IPersistenceProvider<string> persistenceProvider) where T : class, new()
{
	/// <summary>
	/// Gets the persistence provider for storage operations.
	/// </summary>
	protected IPersistenceProvider<string> PersistenceProvider { get; } = persistenceProvider ?? throw new ArgumentNullException(nameof(persistenceProvider));

	/// <summary>
	/// Gets the storage key used for this service's data.
	/// </summary>
	protected abstract string StorageKey { get; }

	/// <summary>
	/// Loads data from storage, returning a default instance if not found or on error.
	/// </summary>
	/// <returns>The loaded data or a new default instance.</returns>
	protected async Task<T> LoadAsync()
	{
#pragma warning disable CA1031 // Do not catch general exception types
		try
		{
			T? result = await PersistenceProvider.RetrieveAsync<T>(StorageKey).ConfigureAwait(false);
			return result ?? new T();
		}
		catch (Exception)
		{
			// Storage failures should return defaults to allow application to continue
			return new T();
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}

	/// <summary>
	/// Saves data to storage.
	/// </summary>
	/// <param name="data">The data to save.</param>
	/// <returns>True if successful, false otherwise.</returns>
	protected async Task<bool> SaveAsync(T data)
	{
		ArgumentNullException.ThrowIfNull(data);

#pragma warning disable CA1031 // Do not catch general exception types
		try
		{
			await PersistenceProvider.StoreAsync(StorageKey, data).ConfigureAwait(false);
			return true;
		}
		catch (Exception)
		{
			// Storage save failures should be handled gracefully - return false to indicate failure
			return false;
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}

	/// <summary>
	/// Removes data from storage.
	/// </summary>
	/// <returns>True if successful, false otherwise.</returns>
	protected async Task<bool> RemoveAsync()
	{
#pragma warning disable CA1031 // Do not catch general exception types
		try
		{
			await PersistenceProvider.RemoveAsync(StorageKey).ConfigureAwait(false);
			return true;
		}
		catch (Exception)
		{
			// Remove failures should be handled gracefully - return false to indicate failure
			return false;
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}

	/// <summary>
	/// Checks if data exists in storage.
	/// </summary>
	/// <returns>True if data exists, false otherwise.</returns>
	protected async Task<bool> ExistsAsync()
	{
#pragma warning disable CA1031 // Do not catch general exception types
		try
		{
			return await PersistenceProvider.ExistsAsync(StorageKey).ConfigureAwait(false);
		}
		catch (Exception)
		{
			// Existence check failures should default to false
			return false;
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}

	/// <summary>
	/// Executes an operation that doesn't return a value, with consistent error handling.
	/// </summary>
	/// <param name="operation">The operation to execute.</param>
	/// <returns>True if successful, false otherwise.</returns>
	protected async Task<bool> ExecuteNonCriticalOperationAsync(Func<Task> operation)
	{
		ArgumentNullException.ThrowIfNull(operation);

#pragma warning disable CA1031 // Do not catch general exception types
		try
		{
			await operation().ConfigureAwait(false);
			return true;
		}
		catch (Exception)
		{
			// Non-critical operations can fail without affecting main application flow
			return false;
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}

	/// <summary>
	/// Executes an operation that returns a value, with consistent error handling.
	/// </summary>
	/// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
	/// <param name="operation">The operation to execute.</param>
	/// <param name="defaultValue">The default value to return on failure.</param>
	/// <returns>The result of the operation or the default value on failure.</returns>
	protected async Task<TResult> ExecuteNonCriticalOperationAsync<TResult>(Func<Task<TResult>> operation, TResult defaultValue)
	{
		ArgumentNullException.ThrowIfNull(operation);

#pragma warning disable CA1031 // Do not catch general exception types
		try
		{
			return await operation().ConfigureAwait(false);
		}
		catch (Exception)
		{
			// Non-critical operations can fail without affecting main application flow
			return defaultValue;
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}
}

