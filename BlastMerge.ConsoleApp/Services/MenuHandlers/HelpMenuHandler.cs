// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for help and information operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class HelpMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	/// <summary>
	/// Handles the help menu operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Help & Information");

		Dictionary<string, HelpChoice> helpChoices = new()
		{
			["📖 Application Overview"] = HelpChoice.ApplicationOverview,
			["🎯 Feature Guide"] = HelpChoice.FeatureGuide,
			["⌨️ Keyboard Shortcuts"] = HelpChoice.KeyboardShortcuts,
			["🔙 Back to Main Menu"] = HelpChoice.BackToMainMenu
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Select help topic:[/]")
				.AddChoices(helpChoices.Keys));

		if (helpChoices.TryGetValue(selection, out HelpChoice choice))
		{
			switch (choice)
			{
				case HelpChoice.ApplicationOverview:
					ShowApplicationOverview();
					break;
				case HelpChoice.FeatureGuide:
					ShowFeatureGuide();
					break;
				case HelpChoice.KeyboardShortcuts:
					ShowKeyboardShortcuts();
					break;
				case HelpChoice.BackToMainMenu:
					// Return to main menu
					break;
				default:
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Shows the application overview.
	/// </summary>
	private static void ShowApplicationOverview()
	{
		ShowMenuTitle("Application Overview");

		Panel overview = new("""
			[bold cyan]BlastMerge[/] is a powerful file comparison and merging tool designed to help you:

			• [green]Find and compare files[/] across directories
			• [green]Identify duplicates and differences[/] between file versions
			• [green]Perform iterative merging[/] of text files with conflict resolution
			• [green]Manage batch operations[/] for processing multiple file sets
			• [green]Synchronize files[/] to maintain consistency

			The tool provides both command-line and interactive modes, making it suitable for
			both scripted automation and manual file management tasks.
			""")
		{
			Header = new PanelHeader("[bold]What is BlastMerge?[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("cyan")
		};

		AnsiConsole.Write(overview);
		WaitForKeyPress();
	}

	/// <summary>
	/// Shows the feature guide.
	/// </summary>
	private static void ShowFeatureGuide()
	{
		ShowMenuTitle("Feature Guide");

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("[bold]Feature[/]")
			.AddColumn("[bold]Description[/]")
			.AddColumn("[bold]Use Case[/]");

		table.AddRow(
			"[green]Find Files[/]",
			"Search for files matching patterns",
			"Locate specific files in large directory trees");

		table.AddRow(
			"[green]Compare Files[/]",
			"Compare files in directories or specific files",
			"Identify differences and duplicates");

		table.AddRow(
			"[green]Iterative Merge[/]",
			"Merge multiple file versions step by step",
			"Resolve conflicts in configuration files");

		table.AddRow(
			"[green]Batch Operations[/]",
			"Process multiple file operations at once",
			"Automate repetitive tasks");

		table.AddRow(
			"[green]Sync Files[/]",
			"Synchronize files to make them identical",
			"Maintain consistency across environments");

		AnsiConsole.Write(table);
		WaitForKeyPress();
	}

	/// <summary>
	/// Shows keyboard shortcuts.
	/// </summary>
	private static void ShowKeyboardShortcuts()
	{
		ShowMenuTitle("Keyboard Shortcuts");

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Yellow)
			.AddColumn("[bold]Key[/]")
			.AddColumn("[bold]Action[/]")
			.AddColumn("[bold]Context[/]");

		table.AddRow("[green]↑ ↓[/]", "Navigate menu options", "All menus");
		table.AddRow("[green]Enter[/]", "Select option", "All menus");
		table.AddRow("[green]↑ ↓[/]", "Browse input history", "Text input");
		table.AddRow("[green]Ctrl+C[/]", "Cancel operation", "Any time");
		table.AddRow("[green]Space[/]", "Toggle selection", "Multi-select menus");
		table.AddRow("[green]Any key[/]", "Continue", "After viewing results");

		AnsiConsole.Write(table);
		WaitForKeyPress();
	}

	/// <summary>
	/// Help menu choices.
	/// </summary>
	private enum HelpChoice
	{
		ApplicationOverview,
		FeatureGuide,
		KeyboardShortcuts,
		BackToMainMenu
	}
}
