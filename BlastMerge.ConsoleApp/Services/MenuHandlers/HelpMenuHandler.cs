// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for help and information operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class HelpMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => "Help";
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
			[GetBackMenuText()] = HelpChoice.BackToMainMenu
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
					GoBack();
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
			[bold cyan]BlastMerge[/] is a revolutionary file synchronization tool that uses [bold yellow]intelligent iterative merging[/]
			to unify multiple versions of files across repositories, directories, and codebases.

			[bold red]🚀 PRIMARY FEATURE: Cross-Repository File Synchronization[/]
			• [green]Smart Discovery[/]: Automatically finds all versions of a file across directories/repositories
			• [green]Hash-Based Grouping[/]: Groups identical files and identifies unique versions
			• [green]Similarity Analysis[/]: Calculates similarity scores between all version pairs
			• [green]Optimal Merge Order[/]: Progressively merges the most similar versions first to minimize conflicts
			• [green]Interactive Resolution[/]: Visual TUI for resolving conflicts block-by-block
			• [green]Cross-Repository Sync[/]: Updates all file locations with the final merged result

			[bold]Real-World Use Cases:[/]
			• Multi-repository synchronization (config files across microservices)
			• Branch consolidation (merge scattered feature branch changes)
			• Environment alignment (unify deployment scripts across dev/staging/prod)
			• Code migration (consolidate similar files when merging codebases)
			• Documentation sync (align README files across related projects)

			Unlike traditional diff tools that handle two-way merges, BlastMerge excels when you have
			3, 5, or 10+ versions of the same file scattered across different locations.
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
			"[red]🔀 Iterative Merge[/] [bold](PRIMARY)[/]",
			"Cross-repository file synchronization using intelligent iterative merging with optimal similarity-based progression",
			"Sync config files across microservices, consolidate feature branches, align environment files");

		table.AddRow(
			"[green]🔍 Find Files[/]",
			"Advanced file discovery with pattern matching across directory trees",
			"Locate specific files in large repository structures");

		table.AddRow(
			"[green]📊 Compare Files[/]",
			"Comprehensive file comparison with multiple diff formats (Change Summary, Git-style, Side-by-Side)",
			"Identify differences and duplicates with rich visual formatting");

		table.AddRow(
			"[green]📦 Batch Operations[/]",
			"Automate multiple file operations with configurable patterns and actions",
			"Process multiple file sets across repositories systematically");

		table.AddRow(
			"[green]🔁 Sync Files[/]",
			"Synchronize files to make them identical with version selection options",
			"Maintain consistency across development environments");

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
}
