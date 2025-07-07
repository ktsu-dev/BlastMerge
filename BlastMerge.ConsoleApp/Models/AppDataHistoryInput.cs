// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Spectre.Console;

/// <summary>
/// Handles user input with history functionality using PersistenceProvider and arrow key navigation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the AppDataHistoryInput class.
/// </remarks>
/// <param name="persistenceService">The persistence service to use.</param>
public class AppDataHistoryInput(BlastMergePersistenceService persistenceService)
{
	private readonly BlastMergePersistenceService persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));

	/// <summary>
	/// Asks the user for input with history support.
	/// </summary>
	/// <param name="prompt">The prompt to display to the user.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The user's input.</returns>
	public async Task<string> AskWithHistoryAsync(string prompt, CancellationToken cancellationToken = default) =>
		await AskWithHistoryAsync(prompt, string.Empty, cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Asks the user for input with history support and a default value.
	/// </summary>
	/// <param name="prompt">The prompt to display to the user.</param>
	/// <param name="defaultValue">The default value to use if input is empty.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The user's input or default value if empty.</returns>
	public async Task<string> AskWithHistoryAsync(string prompt, string defaultValue, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(prompt);
		ArgumentNullException.ThrowIfNull(defaultValue);

		string promptKey = GetPromptKey(prompt);

		// Use Spectre.Console's TextPrompt with custom validation
		TextPrompt<string> textPrompt = new TextPrompt<string>(prompt)
			.AllowEmpty()
			.Validate(input => ValidationResult.Success());

		// Set default value if provided
		if (!string.IsNullOrEmpty(defaultValue))
		{
			textPrompt = textPrompt.DefaultValue(defaultValue);
		}

		string result = AnsiConsole.Prompt(textPrompt);

		// Use default value if result is empty
		if (string.IsNullOrWhiteSpace(result) && !string.IsNullOrEmpty(defaultValue))
		{
			result = defaultValue;
		}

		// Add to history if not empty and not already the most recent
		if (!string.IsNullOrWhiteSpace(result))
		{
			await AddToHistoryAsync(promptKey, result, cancellationToken).ConfigureAwait(false);
		}

		return result;
	}

	/// <summary>
	/// Clears all input history.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task ClearAllHistoryAsync(CancellationToken cancellationToken = default)
	{
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		appData.InputHistory.Clear();

		await persistenceService.SaveAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets the history for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The history list for the prompt.</returns>
	private async Task<List<string>> GetHistoryForPromptAsync(string promptKey, CancellationToken cancellationToken = default)
	{
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		if (!appData.InputHistory.TryGetValue(promptKey, out List<string>? history))
		{
			history = [];
			appData.InputHistory[promptKey] = history;
		}

		return history;
	}

	/// <summary>
	/// Adds an entry to the history for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <param name="value">The value to add.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task AddToHistoryAsync(string promptKey, string value, CancellationToken cancellationToken = default)
	{
		List<string> history = await GetHistoryForPromptAsync(promptKey, cancellationToken).ConfigureAwait(false);

		// Remove if already exists (move to end)
		history.Remove(value);

		// Add to end
		history.Add(value);

		// Trim to max size
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		int maxSize = appData.Settings.MaxHistoryEntriesPerPrompt;
		while (history.Count > maxSize)
		{
			history.RemoveAt(0);
		}

		// Save changes
		await persistenceService.SaveAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Generates a key for the prompt to organize history.
	/// </summary>
	/// <param name="prompt">The prompt text.</param>
	/// <returns>A key for organizing history.</returns>
	private static string GetPromptKey(string prompt)
	{
		// Clean the prompt text for use as a key
		string key = prompt.Replace("[", "").Replace("]", "").Replace("/", "");

		// Extract the main part of the prompt
		if (key.Contains("Enter "))
		{
			string afterEnter = key[(key.IndexOf("Enter ") + 6)..];
			key = afterEnter.Contains(' ') ? afterEnter[..afterEnter.IndexOf(' ')] : afterEnter;
		}

		return key.Trim();
	}

	/// <summary>
	/// Gets the count of history entries for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The number of history entries.</returns>
	public async Task<int> GetHistoryCountAsync(string promptKey, CancellationToken cancellationToken = default)
	{
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		return appData.InputHistory.TryGetValue(promptKey, out List<string>? history) ? history.Count : 0;
	}

	/// <summary>
	/// Gets all history entries for debugging or inspection.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A read-only dictionary of all history entries.</returns>
	public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllHistoryAsync(CancellationToken cancellationToken = default)
	{
		BlastMergeAppData appData = await persistenceService.GetAsync(cancellationToken).ConfigureAwait(false);
		return appData.InputHistory.ToDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
	}
}
