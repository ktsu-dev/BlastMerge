// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.BlastMerge.Core.Models;
using Spectre.Console;

/// <summary>
/// Handles user input with history functionality using AppDataStorage and arrow key navigation.
/// </summary>
public static class AppDataHistoryInput
{
	/// <summary>
	/// Gets the application data storage instance.
	/// </summary>
	private static BlastMergeAppData AppData => BlastMergeAppData.Get();

	/// <summary>
	/// Asks the user for input with history support.
	/// </summary>
	/// <param name="prompt">The prompt to display to the user.</param>
	/// <returns>The user's input.</returns>
	public static string AskWithHistory(string prompt) => AskWithHistory(prompt, string.Empty);

	/// <summary>
	/// Asks the user for input with history support and a default value.
	/// </summary>
	/// <param name="prompt">The prompt to display to the user.</param>
	/// <param name="defaultValue">The default value to use if input is empty.</param>
	/// <returns>The user's input or default value if empty.</returns>
	public static string AskWithHistory(string prompt, string defaultValue)
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
	public static void ClearAllHistory()
	{
		AppData.InputHistory.Clear();

		if (AppData.Settings.AutoSaveEnabled)
		{
			AppData.Save();
		}
		else
		{
			BlastMergeAppData.QueueSave();
		}
	}

	/// <summary>
	/// Gets the history for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <returns>The history list for the prompt.</returns>
	private static List<string> GetHistoryForPrompt(string promptKey)
	{
		if (!AppData.InputHistory.TryGetValue(promptKey, out List<string>? history))
		{
			history = [];
			AppData.InputHistory[promptKey] = history;
		}

		return history;
	}

	/// <summary>
	/// Adds an entry to the history for a specific prompt type.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <param name="value">The value to add.</param>
	private static void AddToHistory(string promptKey, string value)
	{
		List<string> history = GetHistoryForPrompt(promptKey);

		// Remove if already exists (move to end)
		history.Remove(value);

		// Add to end
		history.Add(value);

		// Trim to max size
		int maxSize = AppData.Settings.MaxHistoryEntriesPerPrompt;
		while (history.Count > maxSize)
		{
			history.RemoveAt(0);
		}

		// Save changes
		if (AppData.Settings.AutoSaveEnabled)
		{
			AppData.Save();
		}
		else
		{
			BlastMergeAppData.QueueSave();
		}
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
	/// <returns>The number of history entries.</returns>
	public static int GetHistoryCount(string promptKey) =>
		AppData.InputHistory.TryGetValue(promptKey, out List<string>? history) ? history.Count : 0;

	/// <summary>
	/// Gets all history entries for debugging or inspection.
	/// </summary>
	/// <returns>A read-only dictionary of all history entries.</returns>
	public static IReadOnlyDictionary<string, IReadOnlyList<string>> GetAllHistory()
	{
		return AppData.InputHistory.ToDictionary(
			kvp => kvp.Key,
			kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());
	}
}
