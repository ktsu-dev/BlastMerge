// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services.Base;
using ktsu.PersistenceProvider;

/// <summary>
/// Service for managing input history using PersistenceProvider.
/// </summary>
/// <remarks>
/// Initializes a new instance of the InputHistoryService class.
/// </remarks>
/// <param name="persistenceProvider">The persistence provider for storage operations.</param>
/// <param name="settingsService">The application settings service.</param>
public class InputHistoryService(IPersistenceProvider<string> persistenceProvider, IApplicationSettingsService settingsService)
	: BaseKeyedPersistenceService<List<string>>(persistenceProvider), IInputHistoryService
{
	private readonly IApplicationSettingsService _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

	/// <inheritdoc/>
	protected override string StorageKey => "history_keys"; // Not used by keyed service, but required by base

	/// <inheritdoc/>
	protected override string KeyPrefix => "history:";

	/// <inheritdoc/>
	protected override string KeyListStorageKey => "history_keys";

	/// <inheritdoc/>
	public async Task AddToHistoryAsync(PromptKey promptKey, string value)
	{
		ArgumentNullException.ThrowIfNull(promptKey);
		ArgumentNullException.ThrowIfNull(value);

		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}

		await ExecuteNonCriticalOperationAsync(async () =>
		{
			List<string> history = await GetHistoryListAsync(promptKey).ConfigureAwait(false);

			// Remove if already exists (move to end)
			history.Remove(value);

			// Add to end
			history.Add(value);

			// Trim to max size
			ApplicationSettings settings = await _settingsService.GetSettingsAsync().ConfigureAwait(false);
			int maxSize = settings.MaxHistoryEntriesPerPrompt;
			while (history.Count > maxSize)
			{
				history.RemoveAt(0);
			}

			// Store updated history
			await SaveItemAsync(promptKey.ToString(), history).ConfigureAwait(false);
		}).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyList<string>> GetHistoryForPromptAsync(PromptKey promptKey)
	{
		ArgumentNullException.ThrowIfNull(promptKey);

		List<string> history = await GetHistoryListAsync(promptKey).ConfigureAwait(false);
		return history.AsReadOnly();
	}

	/// <inheritdoc/>
	public async Task<int> GetHistoryCountAsync(PromptKey promptKey)
	{
		ArgumentNullException.ThrowIfNull(promptKey);

		List<string> history = await GetHistoryListAsync(promptKey).ConfigureAwait(false);
		return history.Count;
	}

	/// <inheritdoc/>
	public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllHistoryAsync()
	{
		return await ExecuteNonCriticalOperationAsync(async () =>
		{
			Dictionary<string, List<string>> allItems = await GetAllItemsAsync().ConfigureAwait(false);
			Dictionary<string, IReadOnlyList<string>> result = [];

			foreach (KeyValuePair<string, List<string>> kvp in allItems)
			{
				result[kvp.Key] = kvp.Value.AsReadOnly();
			}

			return (IReadOnlyDictionary<string, IReadOnlyList<string>>)result;
		}, defaultValue: new Dictionary<string, IReadOnlyList<string>>()).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async Task ClearAllHistoryAsync() =>
		await ClearAllAsync().ConfigureAwait(false);

	/// <summary>
	/// Gets the history list for a specific prompt key.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <returns>The history list.</returns>
	private async Task<List<string>> GetHistoryListAsync(PromptKey promptKey)
	{
		List<string>? history = await LoadItemAsync(promptKey.ToString()).ConfigureAwait(false);
		return history ?? [];
	}
}
