// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using Spectre.Console;

/// <summary>
/// Service for handling user interactions and prompts in the console application.
/// </summary>
public static class UserInteractionService
{
	/// <summary>
	/// Asks the user if they want to continue with the next merge.
	/// </summary>
	/// <returns>True to continue, false to stop.</returns>
	public static bool ConfirmContinueMerge() => AnsiConsole.Confirm("[cyan]Continue with next merge?[/]");

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

		return AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title(title)
				.AddChoices(choices));
	}
}
