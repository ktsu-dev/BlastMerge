// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
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
	protected static void WaitForKeyPress() => UIHelper.WaitForKeyPress();

	/// <summary>
	/// Shows a press any key prompt with custom message and waits for input.
	/// </summary>
	/// <param name="message">The custom message to display.</param>
	protected static void WaitForKeyPress(string message) => UIHelper.WaitForKeyPress(message);

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
	protected static void ShowError(string message) => UIHelper.ShowErrorAndWait(message);

	/// <summary>
	/// Shows a success message.
	/// </summary>
	/// <param name="message">The success message.</param>
	protected static void ShowSuccess(string message) => UIHelper.ShowSuccess(message);

	/// <summary>
	/// Shows a warning message.
	/// </summary>
	/// <param name="message">The warning message.</param>
	protected static void ShowWarning(string message) => UIHelper.ShowWarning(message);
}
