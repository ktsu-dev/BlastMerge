// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.PersistenceProvider;

/// <summary>
/// Service for managing the application data.
/// </summary>
/// <param name="persistenceProvider">The persistence provider to use.</param>
public class AppDataService(IPersistenceProvider<string> persistenceProvider) : IAppDataService
{
	/// <summary>
	/// Gets the application data.
	/// </summary>
	public AppData AppData { get; } = persistenceProvider.RetrieveOrCreateAsync<AppData>(nameof(AppData)).Result;

	/// <summary>
	/// Saves the application data.
	/// </summary>
	public async Task SaveAsync() => await persistenceProvider.StoreAsync(nameof(AppData), AppData).ConfigureAwait(false);
}
