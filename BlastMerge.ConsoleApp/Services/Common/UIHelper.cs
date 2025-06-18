// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.Common;

using Spectre.Console;

/// <summary>
/// Helper class for common UI operations and standardized display methods.
/// </summary>
public static class UIHelper
{
	/// <summary>
	/// Common message for operation cancelled.
	/// </summary>
	public const string OperationCancelledMessage = "Operation cancelled.";
	/// <summary>
	/// Shows an error message in red color.
	/// </summary>
	/// <param name="message">The error message to display.</param>
	public static void ShowError(string message) => AnsiConsole.MarkupLine($"[red]{message}[/]");

	/// <summary>
	/// Shows a warning message in yellow color.
	/// </summary>
	/// <param name="message">The warning message to display.</param>
	public static void ShowWarning(string message) => AnsiConsole.MarkupLine($"[yellow]{message}[/]");

	/// <summary>
	/// Shows a success message in green color.
	/// </summary>
	/// <param name="message">The success message to display.</param>
	public static void ShowSuccess(string message) => AnsiConsole.MarkupLine($"[green]{message}[/]");

	/// <summary>
	/// Shows an info message in cyan color.
	/// </summary>
	/// <param name="message">The info message to display.</param>
	public static void ShowInfo(string message) => AnsiConsole.MarkupLine($"[cyan]{message}[/]");

	/// <summary>
	/// Shows a dimmed message and waits for key press.
	/// </summary>
	/// <param name="message">The message to display. Defaults to standard continue message.</param>
	public static void WaitForKeyPress(string message = "Press any key to continue...")
	{
		AnsiConsole.MarkupLine($"[dim]{message}[/]");
		Console.ReadKey();
	}

	/// <summary>
	/// Shows an error message and waits for key press.
	/// </summary>
	/// <param name="message">The error message to display.</param>
	public static void ShowErrorAndWait(string message)
	{
		ShowError(message);
		WaitForKeyPress();
	}

	/// <summary>
	/// Shows a warning message and waits for key press.
	/// </summary>
	/// <param name="message">The warning message to display.</param>
	public static void ShowWarningAndWait(string message)
	{
		ShowWarning(message);
		WaitForKeyPress();
	}

	/// <summary>
	/// Shows a success message and waits for key press.
	/// </summary>
	/// <param name="message">The success message to display.</param>
	public static void ShowSuccessAndWait(string message)
	{
		ShowSuccess(message);
		WaitForKeyPress();
	}
}
