// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Base class for menu handlers providing common functionality.
/// </summary>
/// <param name="applicationService">The application service.</param>
public abstract class BaseMenuHandler(ApplicationService applicationService) : IMenuHandler
{
	/// <summary>
	/// Common message for press any key prompts.
	/// </summary>
	protected const string PressAnyKeyMessage = "Press any key to continue...";

	/// <summary>
	/// Gets the application service.
	/// </summary>
	protected ApplicationService ApplicationService { get; } = applicationService ?? throw new ArgumentNullException(nameof(applicationService));

	/// <summary>
	/// Handles the menu operation and user interaction.
	/// </summary>
	public abstract void Handle();

	/// <summary>
	/// Shows a press any key prompt and waits for input.
	/// </summary>
	protected static void WaitForKeyPress()
	{
		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Clears the console and shows a menu title.
	/// </summary>
	/// <param name="title">The menu title.</param>
	protected static void ShowMenuTitle(string title)
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine($"[bold cyan]{title}[/]");
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows an error message and waits for user input.
	/// </summary>
	/// <param name="message">The error message.</param>
	protected static void ShowError(string message)
	{
		AnsiConsole.MarkupLine($"[red]{message}[/]");
		WaitForKeyPress();
	}

	/// <summary>
	/// Shows a success message.
	/// </summary>
	/// <param name="message">The success message.</param>
	protected static void ShowSuccess(string message) => AnsiConsole.MarkupLine($"[green]{message}[/]");

	/// <summary>
	/// Shows a warning message.
	/// </summary>
	/// <param name="message">The warning message.</param>
	protected static void ShowWarning(string message) => AnsiConsole.MarkupLine($"[yellow]{message}[/]");
}
