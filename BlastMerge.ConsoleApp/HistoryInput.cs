// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Spectre.Console;

/// <summary>
/// Exception thrown when user cancels input with escape key
/// </summary>
public class InputCancelledException : Exception
{
	/// <summary>
	/// Initializes a new instance of the InputCancelledException class.
	/// </summary>
	public InputCancelledException() : base("Input was cancelled by user") { }

	/// <summary>
	/// Initializes a new instance of the InputCancelledException class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public InputCancelledException(string message) : base(message) { }

	/// <summary>
	/// Initializes a new instance of the InputCancelledException class with a specified error message and a reference to the inner exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception.</param>
	public InputCancelledException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Handles user input with history functionality using arrow key navigation
/// </summary>
public static class HistoryInput
{
	private static readonly string HistoryFile = GetHistoryFilePath();

	private static Dictionary<string, List<string>> inputHistory = [];

	private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

	static HistoryInput() => LoadHistory();

	/// <summary>
	/// Gets the history file path, with fallback to temp directory if ApplicationData is not accessible
	/// </summary>
	/// <returns>Path to history file</returns>
	private static string GetHistoryFilePath()
	{
		try
		{
			string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			if (!string.IsNullOrEmpty(appDataPath))
			{
				return Path.Combine(appDataPath, "BlastMerge", "input_history.json");
			}
		}
		catch (Exception e) when (e is System.Security.SecurityException or PlatformNotSupportedException)
		{
			// Fall through
		}

		return Path.Combine(".", "blastmerge_history.json");
	}

	/// <summary>
	/// Asks the user for input with history support using arrow key navigation
	/// </summary>
	/// <param name="prompt">The prompt to show</param>
	/// <param name="defaultValue">Default value if provided</param>
	/// <returns>User input</returns>
	/// <exception cref="InputCancelledException">Thrown when user presses escape to cancel</exception>
	public static string AskWithHistory(string prompt, string defaultValue = "")
	{
		ArgumentNullException.ThrowIfNull(prompt);

		List<string> history = GetOrCreateHistory(prompt);
		ShowPromptWithDecorations(prompt, defaultValue, history);

		(int inputStartColumn, int inputStartRow) = GetInputPosition();
		string initialInput = GetInitialInput(history, defaultValue);
		string input = ReadInputWithHistory(history, initialInput, inputStartColumn, inputStartRow);

		input = ApplyDefaultIfEmpty(input, defaultValue, history);
		UpdateHistoryWithInput(input, history);

		return input;
	}

	/// <summary>
	/// Gets or creates history for the given prompt
	/// </summary>
	/// <param name="prompt">The prompt to get history for</param>
	/// <returns>History list for the prompt</returns>
	private static List<string> GetOrCreateHistory(string prompt)
	{
		string historyKey = prompt.Replace("[cyan]", "").Replace("[/]", "").Replace(":", "").Trim();

		if (!inputHistory.TryGetValue(historyKey, out List<string>? history))
		{
			history = [];
			inputHistory[historyKey] = history;
		}

		return history;
	}

	/// <summary>
	/// Shows the prompt with default value and history hints
	/// </summary>
	/// <param name="prompt">The prompt text</param>
	/// <param name="defaultValue">Default value if any</param>
	/// <param name="history">History list</param>
	private static void ShowPromptWithDecorations(string prompt, string defaultValue, List<string> history)
	{
		AnsiConsole.Markup(prompt);
		if (!string.IsNullOrEmpty(defaultValue))
		{
			AnsiConsole.Markup($" [dim]({defaultValue})[/]");
		}
		if (history.Count > 0)
		{
			AnsiConsole.Markup(" [dim](↑↓ for history, Esc to back)[/]");
		}
		else
		{
			AnsiConsole.Markup(" [dim](Esc to back)[/]");
		}
		AnsiConsole.Write(": ");
	}

	/// <summary>
	/// Gets the current cursor position for input tracking
	/// </summary>
	/// <returns>Tuple of column and row position</returns>
	private static (int column, int row) GetInputPosition() => (Console.CursorLeft, Console.CursorTop);

	/// <summary>
	/// Determines the initial input based on history and default value
	/// </summary>
	/// <param name="history">History list</param>
	/// <param name="defaultValue">Default value</param>
	/// <returns>Initial input string</returns>
	private static string GetInitialInput(List<string> history, string defaultValue)
	{
		if (history.Count > 0 && string.IsNullOrEmpty(defaultValue))
		{
			return history[0]; // Most recent input
		}

		return !string.IsNullOrEmpty(defaultValue) ? defaultValue : "";
	}

	/// <summary>
	/// Applies default value or last history item if input is empty
	/// </summary>
	/// <param name="input">Current input</param>
	/// <param name="defaultValue">Default value</param>
	/// <param name="history">History list</param>
	/// <returns>Final input value</returns>
	private static string ApplyDefaultIfEmpty(string input, string defaultValue, List<string> history)
	{
		if (!string.IsNullOrEmpty(input))
		{
			return input;
		}

		if (!string.IsNullOrEmpty(defaultValue))
		{
			return defaultValue;
		}

		if (history.Count > 0)
		{
			string historyInput = history[0]; // Most recent is at index 0
			AnsiConsole.MarkupLine($"[dim]Using: {historyInput}[/]");
			return historyInput;
		}

		return input;
	}

	/// <summary>
	/// Updates history with the new input value
	/// </summary>
	/// <param name="input">Input to add to history</param>
	/// <param name="history">History list to update</param>
	private static void UpdateHistoryWithInput(string input, List<string> history)
	{
		if (string.IsNullOrEmpty(input))
		{
			return;
		}

		// Remove existing item if present, then add to front
		history.Remove(input);
		history.Insert(0, input);

		// Keep only the last 20 entries
		if (history.Count > 20)
		{
			history.RemoveAt(history.Count - 1);
		}

		SaveHistory();
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
		InputState state = new(initialInput);
		Console.Write(state.Input);

		while (true)
		{
			ConsoleKeyInfo keyInfo = Console.ReadKey(true);

			if (keyInfo.Key == ConsoleKey.Enter)
			{
				AnsiConsole.WriteLine();
				return state.Input;
			}

			ProcessKeyInput(keyInfo, state, history, initialInput, inputStartColumn, inputStartRow);
		}
	}

	/// <summary>
	/// Processes a single key input and updates the input state
	/// </summary>
	/// <param name="keyInfo">The key information</param>
	/// <param name="state">Current input state</param>
	/// <param name="history">Command history</param>
	/// <param name="initialInput">Initial input value</param>
	/// <param name="inputStartColumn">Column where input starts</param>
	/// <param name="inputStartRow">Row where input starts</param>
	private static void ProcessKeyInput(ConsoleKeyInfo keyInfo, InputState state, List<string> history,
		string initialInput, int inputStartColumn, int inputStartRow)
	{
#pragma warning disable IDE0010 // Populate switch - we intentionally don't handle all ConsoleKey values
		switch (keyInfo.Key)
		{
			case ConsoleKey.UpArrow:
			case ConsoleKey.DownArrow:
				HandleHistoryNavigation(keyInfo.Key, state, history, initialInput, inputStartColumn, inputStartRow);
				break;

			case ConsoleKey.LeftArrow:
			case ConsoleKey.RightArrow:
			case ConsoleKey.Home:
			case ConsoleKey.End:
				HandleCursorNavigation(keyInfo.Key, state, inputStartColumn, inputStartRow);
				break;

			case ConsoleKey.Backspace:
			case ConsoleKey.Delete:
				HandleTextDeletion(keyInfo.Key, state, inputStartColumn, inputStartRow);
				break;

			case ConsoleKey.Escape:
				throw new InputCancelledException();

			default:
				HandleCharacterInput(keyInfo, state, inputStartColumn, inputStartRow);
				break;
		}
#pragma warning restore IDE0010
	}

	/// <summary>
	/// Handles up/down arrow navigation through history
	/// </summary>
	private static void HandleHistoryNavigation(ConsoleKey key, InputState state, List<string> history,
		string initialInput, int inputStartColumn, int inputStartRow)
	{
		if (key == ConsoleKey.UpArrow && history.Count > 0 && state.HistoryIndex < history.Count - 1)
		{
			state.HistoryIndex++;
			state.Input = history[state.HistoryIndex];
			state.CursorPos = state.Input.Length;
			RedrawInput(state.Input, inputStartColumn, inputStartRow);
		}
		else if (key == ConsoleKey.DownArrow)
		{
			if (state.HistoryIndex > 0)
			{
				state.HistoryIndex--;
				state.Input = history[state.HistoryIndex];
				state.CursorPos = state.Input.Length;
				RedrawInput(state.Input, inputStartColumn, inputStartRow);
			}
			else if (state.HistoryIndex == 0)
			{
				state.HistoryIndex = -1;
				state.Input = initialInput;
				state.CursorPos = state.Input.Length;
				RedrawInput(state.Input, inputStartColumn, inputStartRow);
			}
		}
	}

	/// <summary>
	/// Handles cursor navigation keys
	/// </summary>
	private static void HandleCursorNavigation(ConsoleKey key, InputState state, int inputStartColumn, int inputStartRow)
	{
#pragma warning disable IDE0010 // Populate switch - we intentionally don't handle all ConsoleKey values
		switch (key)
		{
			case ConsoleKey.LeftArrow when state.CursorPos > 0:
				state.CursorPos--;
				SetCursorPosition(state.CursorPos, inputStartColumn, inputStartRow);
				break;
			case ConsoleKey.RightArrow when state.CursorPos < state.Input.Length:
				state.CursorPos++;
				SetCursorPosition(state.CursorPos, inputStartColumn, inputStartRow);
				break;
			case ConsoleKey.Home:
				state.CursorPos = 0;
				SetCursorPosition(state.CursorPos, inputStartColumn, inputStartRow);
				break;
			case ConsoleKey.End:
				state.CursorPos = state.Input.Length;
				SetCursorPosition(state.CursorPos, inputStartColumn, inputStartRow);
				break;
		}
#pragma warning restore IDE0010
	}

	/// <summary>
	/// Handles text deletion (backspace/delete)
	/// </summary>
	private static void HandleTextDeletion(ConsoleKey key, InputState state, int inputStartColumn, int inputStartRow)
	{
		bool modified = false;

		if (key == ConsoleKey.Backspace && state.CursorPos > 0)
		{
			state.Input = state.Input.Remove(state.CursorPos - 1, 1);
			state.CursorPos--;
			modified = true;
		}
		else if (key == ConsoleKey.Delete && state.CursorPos < state.Input.Length)
		{
			state.Input = state.Input.Remove(state.CursorPos, 1);
			modified = true;
		}

		if (modified)
		{
			state.HistoryIndex = -1;
			RedrawInput(state.Input, inputStartColumn, inputStartRow);
			SetCursorPosition(state.CursorPos, inputStartColumn, inputStartRow);
		}
	}

	/// <summary>
	/// Handles regular character input
	/// </summary>
	private static void HandleCharacterInput(ConsoleKeyInfo keyInfo, InputState state, int inputStartColumn, int inputStartRow)
	{
		if (!char.IsControl(keyInfo.KeyChar))
		{
			state.Input = state.Input.Insert(state.CursorPos, keyInfo.KeyChar.ToString());
			state.CursorPos++;
			state.HistoryIndex = -1;
			RedrawInput(state.Input, inputStartColumn, inputStartRow);
			SetCursorPosition(state.CursorPos, inputStartColumn, inputStartRow);
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
		// Go to input start position
		Console.SetCursorPosition(inputStartColumn, inputStartRow);

		// Clear from input start to end of line
		int remainingWidth = Console.WindowWidth - inputStartColumn;
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
			if (!string.IsNullOrEmpty(HistoryFile) && File.Exists(HistoryFile))
			{
				string json = File.ReadAllText(HistoryFile);
				if (!string.IsNullOrWhiteSpace(json))
				{
					inputHistory = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? [];
				}
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
		catch (System.Security.SecurityException)
		{
			// If security exception, start with empty history
			inputHistory = [];
		}

		// Ensure inputHistory is never null
		inputHistory ??= [];
	}

	/// <summary>
	/// Saves input history to file
	/// </summary>
	private static void SaveHistory()
	{
		try
		{
			if (string.IsNullOrEmpty(HistoryFile))
			{
				return; // Can't save if we don't have a valid path
			}

			string? directory = Path.GetDirectoryName(HistoryFile);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			string json = JsonSerializer.Serialize(inputHistory, jsonOptions);
			File.WriteAllText(HistoryFile, json);
		}
		catch (IOException)
		{
			// Ignore file I/O failures - history is not critical
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access denied failures - history is not critical
		}
		catch (JsonException)
		{
			// Ignore JSON serialization failures - history is not critical
		}
		catch (System.Security.SecurityException)
		{
			// Ignore security exceptions - history is not critical
		}
	}
}

