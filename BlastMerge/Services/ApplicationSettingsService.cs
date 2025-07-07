// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System.Threading.Tasks;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services.Base;
using ktsu.PersistenceProvider;

/// <summary>
/// Service for managing application settings using PersistenceProvider.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ApplicationSettingsService class.
/// </remarks>
/// <param name="persistenceProvider">The persistence provider for storage operations.</param>
public class ApplicationSettingsService(IPersistenceProvider<string> persistenceProvider)
	: BasePersistenceService<ApplicationSettings>(persistenceProvider), IApplicationSettingsService
{
	/// <inheritdoc/>
	protected override string StorageKey => "settings";

	/// <inheritdoc/>
	public async Task<ApplicationSettings> GetSettingsAsync() =>
		await LoadAsync().ConfigureAwait(false);

	/// <inheritdoc/>
	public async Task SaveSettingsAsync(ApplicationSettings settings) =>
		await SaveAsync(settings).ConfigureAwait(false);

	/// <inheritdoc/>
	public async Task ResetToDefaultsAsync()
	{
		ApplicationSettings defaultSettings = new();
		await SaveAsync(defaultSettings).ConfigureAwait(false);
	}
}
