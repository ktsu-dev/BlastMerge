// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Threading;
using Spectre.Console;

/// <summary>
/// Service for handling user interactions and prompts in the console application.
/// </summary>
public static class UserInteractionService
{
	// Lock object to ensure only one prompt can be shown at a time (prevents conflicts in parallel processing)
	private static readonly Lock PromptLock = new();
	/// <summary>
	/// Asks the user if they want to continue with the next merge.
	/// </summary>
	/// <returns>True to continue, false to stop.</returns>
	public static bool ConfirmContinueMerge()
	{
		lock (PromptLock)
		{
			return AnsiConsole.Confirm("[cyan]Continue with next merge?[/]");
		}
	}

	/// <summary>
	/// Prompts the user to press any key to continue.
	/// </summary>
	/// <param name="message">The message to display.</param>
	public static void PressAnyKeyToContinue(string message = "Press any key to continue...")
	{
		ArgumentNullException.ThrowIfNull(message);
		AnsiConsole.WriteLine(message);
		Console.ReadKey();
	}

	/// <summary>
	/// Asks the user for confirmation with a yes/no prompt.
	/// </summary>
	/// <param name="prompt">The prompt message.</param>
	/// <returns>True if user confirms, false otherwise.</returns>
	public static bool Confirm(string prompt)
	{
		ArgumentNullException.ThrowIfNull(prompt);
		return AnsiConsole.Confirm(prompt);
	}

	/// <summary>
	/// Shows a selection prompt to the user.
	/// </summary>
	/// <param name="title">The title of the selection prompt.</param>
	/// <param name="choices">The available choices.</param>
	/// <returns>The selected choice.</returns>
	public static string ShowSelectionPrompt(string title, string[] choices)
	{
		ArgumentNullException.ThrowIfNull(title);
		ArgumentNullException.ThrowIfNull(choices);

		// Use lock to ensure only one prompt can be shown at a time (prevents conflicts in parallel processing)
		lock (PromptLock)
		{
			// Add safeguards to prevent display issues and infinite loops
			AnsiConsole.WriteLine(); // Add spacing before prompt
			string result = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title(title)
					.PageSize(Math.Max(3, Math.Min(10, choices.Length))) // Ensure minimum page size of 3
					.MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
					.AddChoices(choices));
			AnsiConsole.WriteLine(); // Add spacing after prompt
			return result;
		}
	}
}
