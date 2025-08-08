// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Contracts;

using System.Collections.Generic;

/// <summary>
/// Service for managing the input history.
/// </summary>
public interface IInputHistoryService
{
	/// <summary>
	/// Adds an entry to the history for a specific prompt.
	/// </summary>
	/// <param name="promptKey">The prompt key.</param>
	/// <param name="value">The value to add.</param>
	public void AddToHistory(string promptKey, string value);

	/// <summary>
	/// Asks the user for input with history.
	/// </summary>
	/// <param name="prompt">The prompt text.</param>
	/// <returns>The user's input.</returns>
	public string AskWithHistory(string prompt);

	/// <summary>
	/// Asks the user for input with history.
	/// </summary>
	/// <param name="prompt">The prompt text.</param>
	/// <param name="defaultValue">The default value to use if no input is provided.</param>
	/// <returns>The user's input.</returns>
	public string AskWithHistory(string prompt, string defaultValue);

	/// <summary>
	/// Clears all history.
	/// </summary>
	public void ClearAllHistory();

	/// <summary>
	/// Gets all history.
	/// </summary>
	/// <returns>The history.</returns>
	public IReadOnlyDictionary<string, IReadOnlyList<string>> GetAllHistory();

	/// <summary>
	/// Gets the last input for a specific prompt.
	/// </summary>
	/// <param name="prompt">The prompt text.</param>
	/// <returns>The last input for the prompt.</returns>
	public string GetLastInput(string prompt);
}
