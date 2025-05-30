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
/// Handles user input with history functionality using arrow key navigation
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
	/// Asks the user for input with history support using arrow key navigation
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

		// Show the prompt
		AnsiConsole.Markup(prompt);
		if (!string.IsNullOrEmpty(defaultValue))
		{
			AnsiConsole.Markup($" [dim]({defaultValue})[/]");
		}
		if (history.Count > 0)
		{
			AnsiConsole.Markup(" [dim](↑↓ for history)[/]");
		}
		AnsiConsole.Write(": ");

		// Remember where the input starts
		var inputStartColumn = Console.CursorLeft;
		var inputStartRow = Console.CursorTop;

		// Use most recent history as initial input, or default value, or empty
		var initialInput = "";
		if (history.Count > 0 && string.IsNullOrEmpty(defaultValue))
		{
			initialInput = history[0]; // Most recent input
		}
		else if (!string.IsNullOrEmpty(defaultValue))
		{
			initialInput = defaultValue;
		}

		var input = ReadInputWithHistory(history, initialInput, inputStartColumn, inputStartRow);

		// Use default value if input is empty and default is provided
		if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(defaultValue))
		{
			input = defaultValue;
		}

		// Use last history item if input is empty and no default is provided
		if (string.IsNullOrEmpty(input) && history.Count > 0)
		{
			input = history[0]; // Most recent is at index 0
			AnsiConsole.MarkupLine($"[dim]Using: {input}[/]");
		}

		// Add to history if not already present and not empty
		if (!string.IsNullOrEmpty(input) && !history.Contains(input))
		{
			history.Insert(0, input);

			// Keep only the last 20 entries
			if (history.Count > 20)
			{
				history.RemoveAt(history.Count - 1);
			}

			SaveHistory();
		}
		else if (!string.IsNullOrEmpty(input) && history.Contains(input))
		{
			// Move existing item to the front
			history.Remove(input);
			history.Insert(0, input);
			SaveHistory();
		}

		return input;
	}

	/// <summary>
	/// Reads input with arrow key navigation through history
	/// </summary>
	/// <param name="history">The command history for this prompt</param>
	/// <param name="initialInput">Initial input to display</param>
	/// <param name="inputStartColumn">Column where input starts</param>
	/// <param name="inputStartRow">Row where input starts</param>
	/// <returns>The user input</returns>
	private static string ReadInputWithHistory(List<string> history, string initialInput, int inputStartColumn, int inputStartRow)
	{
		var input = initialInput;
		var cursorPos = input.Length;
		var historyIndex = -1; // -1 means current input, 0+ means history items

		// Display initial input
		Console.Write(input);

		while (true)
		{
			var keyInfo = Console.ReadKey(true);

#pragma warning disable IDE0010 // Populate switch - we intentionally don't handle all ConsoleKey values
			switch (keyInfo.Key)
			{
				case ConsoleKey.Enter:
					AnsiConsole.WriteLine();
					return input;

				case ConsoleKey.UpArrow:
					if (history.Count > 0)
					{
						if (historyIndex < history.Count - 1)
						{
							historyIndex++;
							input = history[historyIndex];
							cursorPos = input.Length;
							RedrawInput(input, inputStartColumn, inputStartRow);
						}
					}
					break;

				case ConsoleKey.DownArrow:
					if (historyIndex > 0)
					{
						historyIndex--;
						input = history[historyIndex];
						cursorPos = input.Length;
						RedrawInput(input, inputStartColumn, inputStartRow);
					}
					else if (historyIndex == 0)
					{
						historyIndex = -1;
						input = initialInput; // Go back to initial input
						cursorPos = input.Length;
						RedrawInput(input, inputStartColumn, inputStartRow);
					}
					break;

				case ConsoleKey.LeftArrow:
					if (cursorPos > 0)
					{
						cursorPos--;
						SetCursorPosition(cursorPos, inputStartColumn, inputStartRow);
					}
					break;

				case ConsoleKey.RightArrow:
					if (cursorPos < input.Length)
					{
						cursorPos++;
						SetCursorPosition(cursorPos, inputStartColumn, inputStartRow);
					}
					break;

				case ConsoleKey.Home:
					cursorPos = 0;
					SetCursorPosition(cursorPos, inputStartColumn, inputStartRow);
					break;

				case ConsoleKey.End:
					cursorPos = input.Length;
					SetCursorPosition(cursorPos, inputStartColumn, inputStartRow);
					break;

				case ConsoleKey.Backspace:
					if (cursorPos > 0)
					{
						input = input.Remove(cursorPos - 1, 1);
						cursorPos--;
						historyIndex = -1; // Reset history when editing
						RedrawInput(input, inputStartColumn, inputStartRow);
						SetCursorPosition(cursorPos, inputStartColumn, inputStartRow);
					}
					break;

				case ConsoleKey.Delete:
					if (cursorPos < input.Length)
					{
						input = input.Remove(cursorPos, 1);
						historyIndex = -1; // Reset history when editing
						RedrawInput(input, inputStartColumn, inputStartRow);
						SetCursorPosition(cursorPos, inputStartColumn, inputStartRow);
					}
					break;

				case ConsoleKey.Escape:
					// Clear current input
					input = "";
					cursorPos = 0;
					historyIndex = -1;
					RedrawInput(input, inputStartColumn, inputStartRow);
					break;

				default:
					// Handle regular character input
					if (!char.IsControl(keyInfo.KeyChar))
					{
						input = input.Insert(cursorPos, keyInfo.KeyChar.ToString());
						cursorPos++;
						historyIndex = -1; // Reset history when typing
						RedrawInput(input, inputStartColumn, inputStartRow);
						SetCursorPosition(cursorPos, inputStartColumn, inputStartRow);
					}
					// Ignore other special keys
					break;
			}
#pragma warning restore IDE0010
		}
	}

	/// <summary>
	/// Redraws only the input portion, preserving the prompt
	/// </summary>
	/// <param name="input">Current input text</param>
	/// <param name="inputStartColumn">Column where input starts</param>
	/// <param name="inputStartRow">Row where input starts</param>
	private static void RedrawInput(string input, int inputStartColumn, int inputStartRow)
	{
		// Save current cursor position
		var currentColumn = Console.CursorLeft;
		var currentRow = Console.CursorTop;

		// Go to input start position
		Console.SetCursorPosition(inputStartColumn, inputStartRow);

		// Clear from input start to end of line
		var remainingWidth = Console.WindowWidth - inputStartColumn;
		Console.Write(new string(' ', remainingWidth));

		// Go back to input start and write the new input
		Console.SetCursorPosition(inputStartColumn, inputStartRow);
		Console.Write(input);
	}

	/// <summary>
	/// Sets the cursor position within the input area
	/// </summary>
	/// <param name="cursorPos">Position within the input string</param>
	/// <param name="inputStartColumn">Column where input starts</param>
	/// <param name="inputStartRow">Row where input starts</param>
	private static void SetCursorPosition(int cursorPos, int inputStartColumn, int inputStartRow) =>
		Console.SetCursorPosition(inputStartColumn + cursorPos, inputStartRow);

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

