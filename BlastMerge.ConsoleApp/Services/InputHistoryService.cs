// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.Contracts;
using Spectre.Console;

/// <summary>
/// Handles user input with history functionality using AppDataStorage and arrow key navigation.
/// </summary>
public class InputHistoryService(IAppDataService appDataService) : IInputHistoryService
{
	/// <summary>
	/// Asks the user for input with history support.
	/// </summary>
	/// <param name="prompt">The prompt to display to the user.</param>
	/// <returns>The user's input.</returns>
	public string AskWithHistory(string prompt) => AskWithHistory(prompt, string.Empty);

	/// <summary>
	/// Asks the user for input with history support and a default value.
	/// </summary>
	/// <param name="prompt">The prompt to display to the user.</param>
	/// <param name="defaultValue">The default value to use if input is empty.</param>
	/// <returns>The user's input or default value if empty.</returns>
	public string AskWithHistory(string prompt, string defaultValue)
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
			AddToHistory(promptKey, result);
		}

		return result;
	}

	/// <summary>
	/// Clears all input history.
	/// </summary>
	public void ClearAllHistory()
	{
		appDataService.AppData.InputHistory.Clear();
		appDataService.SaveAsync().Wait();
	}

	/// <summary>
	/// Gets the history for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <returns>The history list for the prompt.</returns>
	private Collection<string> GetHistoryForPrompt(string promptKey)
	{
		if (!appDataService.AppData.InputHistory.TryGetValue(promptKey, out Collection<string>? history))
		{
			history = [];
			appDataService.AppData.InputHistory[promptKey] = history;
			appDataService.SaveAsync().Wait();
		}

		return history;
	}

	/// <summary>
	/// Adds an entry to the history for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <param name="value">The value to add.</param>
	public void AddToHistory(string promptKey, string value)
	{
		Collection<string> history = GetHistoryForPrompt(promptKey);

		history.Remove(value);
		history.Add(value);

		appDataService.SaveAsync().Wait();
	}

	/// <summary>
	/// Gets the last input for a specific prompt.
	/// </summary>
	/// <param name="prompt">The prompt text.</param>
	/// <returns>The last input for the prompt.</returns>
	public string GetLastInput(string prompt)
	{
		ArgumentNullException.ThrowIfNull(prompt);
		string promptKey = GetPromptKey(prompt);
		Collection<string> history = GetHistoryForPrompt(promptKey);
		return history.LastOrDefault() ?? string.Empty;
	}

	/// <summary>
	/// Generates a key for the prompt to organize history.
	/// </summary>
	/// <param name="prompt">The prompt text.</param>
	/// <returns>A key for organizing history.</returns>
	[Pure]
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
	/// Gets all history entries for debugging or inspection.
	/// </summary>
	/// <returns>A read-only dictionary of all history entries.</returns>
	public IReadOnlyDictionary<string, IReadOnlyList<string>> GetAllHistory()
	{
		return appDataService.AppData.InputHistory.ToDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
	}
}
