// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;
using ktsu.PersistenceProvider;

/// <summary>
/// Manages persistent application data using PersistenceProvider.
/// </summary>
public class BlastMergePersistenceService(IPersistenceProvider<string> persistenceProvider) : IDisposable
{
	private readonly IPersistenceProvider<string> persistenceProvider = persistenceProvider ?? throw new ArgumentNullException(nameof(persistenceProvider));
	private BlastMergeAppData? cachedData;
	private readonly SemaphoreSlim cacheLock = new(1, 1);
	private bool disposed;

	/// <summary>
	/// Gets the application data, loading it if necessary.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The application data.</returns>
	public async Task<BlastMergeAppData> GetAsync(CancellationToken cancellationToken = default)
	{
		await cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			cachedData ??= await persistenceProvider.RetrieveOrCreateAsync<BlastMergeAppData>("AppData", cancellationToken).ConfigureAwait(false);
			return cachedData;
		}
		finally
		{
			cacheLock.Release();
		}
	}

	/// <summary>
	/// Saves the application data.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task SaveAsync(CancellationToken cancellationToken = default)
	{
		await cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (cachedData != null)
			{
				await persistenceProvider.StoreAsync("AppData", cachedData, cancellationToken).ConfigureAwait(false);
			}
		}
		finally
		{
			cacheLock.Release();
		}
	}

	/// <summary>
	/// Resets the cached data for testing purposes.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	internal async Task ResetForTestingAsync(CancellationToken cancellationToken = default)
	{
		await cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			cachedData = new BlastMergeAppData();
			await persistenceProvider.StoreAsync("AppData", cachedData, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			cacheLock.Release();
		}
	}

	/// <summary>
	/// Loads the application data from persistent storage.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task LoadAsync(CancellationToken cancellationToken = default)
	{
		await cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			cachedData = await persistenceProvider.RetrieveOrCreateAsync<BlastMergeAppData>("AppData", cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			cacheLock.Release();
		}
	}

	/// <summary>
	/// Releases the unmanaged resources used by the BlastMergePersistenceService and optionally releases the managed resources.
	/// </summary>
	/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				cacheLock?.Dispose();
			}
			disposed = true;
		}
	}

	/// <summary>
	/// Releases all resources used by the BlastMergePersistenceService.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
