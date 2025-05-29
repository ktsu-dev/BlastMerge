// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.CLI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Spectre.Console;

/// <summary>
/// Handles user input with history functionality
/// </summary>
public static class HistoryInput
{
	private static readonly string HistoryFile = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"DiffMore",
		"input_history.json");

	private static Dictionary<string, List<string>> inputHistory = [];

	private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

	static HistoryInput() => LoadHistory();

	/// <summary>
	/// Asks the user for input with history support
	/// </summary>
	/// <param name="prompt">The prompt to show</param>
	/// <param name="defaultValue">Default value if provided</param>
	/// <returns>User input</returns>
	public static string AskWithHistory(string prompt, string defaultValue = "")
	{
		ArgumentNullException.ThrowIfNull(prompt);

		var historyKey = prompt.Replace("[cyan]", "").Replace("[/]", "").Replace(":", "").Trim();

		if (!inputHistory.TryGetValue(historyKey, out var history))
		{
			history = [];
			inputHistory[historyKey] = history;
		}

		var textPrompt = new TextPrompt<string>(prompt);

		if (!string.IsNullOrEmpty(defaultValue))
		{
			textPrompt.DefaultValue(defaultValue);
		}

		if (history.Count > 0)
		{
			textPrompt.AddChoices(history);
			textPrompt.ShowChoices(false);
		}

		var input = AnsiConsole.Prompt(textPrompt);

		// Add to history if not already present
		if (!string.IsNullOrEmpty(input) && !history.Contains(input))
		{
			history.Insert(0, input);

			// Keep only the last 10 entries
			if (history.Count > 10)
			{
				history.RemoveAt(history.Count - 1);
			}

			SaveHistory();
		}

		return input;
	}

	/// <summary>
	/// Loads input history from file
	/// </summary>
	private static void LoadHistory()
	{
		try
		{
			if (File.Exists(HistoryFile))
			{
				var json = File.ReadAllText(HistoryFile);
				inputHistory = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? [];
			}
		}
		catch (JsonException)
		{
			// If JSON parsing fails, start with empty history
			inputHistory = [];
		}
		catch (IOException)
		{
			// If file reading fails, start with empty history
			inputHistory = [];
		}
		catch (UnauthorizedAccessException)
		{
			// If access denied, start with empty history
			inputHistory = [];
		}
	}

	/// <summary>
	/// Saves input history to file
	/// </summary>
	private static void SaveHistory()
	{
		try
		{
			var directory = Path.GetDirectoryName(HistoryFile);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var json = JsonSerializer.Serialize(inputHistory, jsonOptions);
			File.WriteAllText(HistoryFile, json);
		}
		catch (IOException)
		{
			// Ignore file I/O failures
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access denied failures
		}
		catch (JsonException)
		{
			// Ignore JSON serialization failures
		}
	}
}

