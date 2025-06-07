// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using Spectre.Console;

/// <summary>
/// Service for displaying menus and welcome screens in the console application.
/// </summary>
public static class MenuDisplayService
{
	/// <summary>
	/// Minimum console width required to display the welcome ASCII art.
	/// </summary>
	private const int MinimumWelcomeAsciiWidth = 100;

	/// <summary>
	/// Minimum console width required to display the goodbye ASCII art.
	/// </summary>
	private const int MinimumGoodbyeAsciiWidth = 80;

	/// <summary>
	/// Shows the welcome screen with application information.
	/// </summary>
	public static void ShowWelcomeScreen()
	{
		AnsiConsole.Clear();

		if (CanDisplayWelcomeAscii())
		{
			ShowWelcomeWithAscii();
		}
		else
		{
			ShowWelcomeWithPlainText();
		}
	}

	/// <summary>
	/// Shows the goodbye screen when exiting the application.
	/// </summary>
	public static void ShowGoodbyeScreen()
	{
		AnsiConsole.Clear();

		if (CanDisplayGoodbyeAscii())
		{
			ShowGoodbyeWithAscii();
		}
		else
		{
			ShowGoodbyeWithPlainText();
		}
	}

	/// <summary>
	/// Determines if the console is wide enough to display the welcome ASCII art.
	/// </summary>
	/// <returns>True if the console can display the ASCII art, false otherwise.</returns>
	private static bool CanDisplayWelcomeAscii()
	{
		try
		{
			return Console.WindowWidth >= MinimumWelcomeAsciiWidth;
		}
		catch (IOException)
		{
			// If we can't get console width (e.g., redirected output), default to plain text
			return false;
		}
		catch (ArgumentOutOfRangeException)
		{
			// Console.WindowWidth can throw this exception in some scenarios
			return false;
		}
	}

	/// <summary>
	/// Determines if the console is wide enough to display the goodbye ASCII art.
	/// </summary>
	/// <returns>True if the console can display the ASCII art, false otherwise.</returns>
	private static bool CanDisplayGoodbyeAscii()
	{
		try
		{
			return Console.WindowWidth >= MinimumGoodbyeAsciiWidth;
		}
		catch (IOException)
		{
			// If we can't get console width (e.g., redirected output), default to plain text
			return false;
		}
		catch (ArgumentOutOfRangeException)
		{
			// Console.WindowWidth can throw this exception in some scenarios
			return false;
		}
	}

	/// <summary>
	/// Shows the welcome screen with ASCII art.
	/// </summary>
	private static void ShowWelcomeWithAscii()
	{
		// Epic explosive ASCII art title
		string blastMergeArt = @"
[red]██████  ██       █████   ███████ ████████    ███    ███ ███████ ██████   ██████  ███████[/]
[red]██   ██ ██      ██   ██  ██         ██       ████  ████ ██      ██   ██ ██       ██     [/]
[red]██████  ██      ███████  ███████    ██       ██ ████ ██ █████   ██████  ██   ███ █████  [/]
[red]██   ██ ██      ██   ██       ██    ██       ██  ██  ██ ██      ██   ██ ██    ██ ██     [/]
[red]██████  ███████ ██   ██  ███████    ██       ██      ██ ███████ ██   ██  ██████  ███████[/]

";

		AnsiConsole.Write(new Markup(blastMergeArt));

		ShowWelcomeContent();
	}

	/// <summary>
	/// Shows the welcome screen with plain text title.
	/// </summary>
	private static void ShowWelcomeWithPlainText()
	{
		// Plain text title for narrow consoles
		AnsiConsole.Write(new Rule("[bold red]BLAST MERGE[/]")
		{
			Style = Style.Parse("red"),
			Justification = Justify.Center
		});
		AnsiConsole.WriteLine();

		ShowWelcomeContent();
	}

	/// <summary>
	/// Shows the welcome content panel (common for both ASCII and plain text versions).
	/// </summary>
	private static void ShowWelcomeContent()
	{
		// Create an epic welcome panel with rich content
		Table welcomeTable = new Table()
			.BorderColor(Color.Purple)
			.Border(TableBorder.Rounded)
			.AddColumn(new TableColumn("").Width(80).Centered());

		welcomeTable
		.HideHeaders()
		.AddRow(new Markup(
			"[bold red]💥 CROSS-REPOSITORY FILE SYNCHRONIZATION WEAPON 💥[/]\n\n" +
			"[yellow]🔀 Intelligent iterative merging across repositories[/]\n" +
			"[green]🎯 Smart discovery of multiple file versions[/]\n" +
			"[cyan]🧠 Similarity-based optimal merge progression[/]\n" +
			"[magenta]⚡ Interactive conflict resolution TUI[/]\n" +
			"[red]🚀 Cross-repository file synchronization[/]\n\n" +
			"[bold white on red] FUSION-POWERED BY DIFFPLEX & SPECTRE.CONSOLE [/]\n\n" +
			"[dim]Navigate: [/][yellow]↑ ↓ Arrow Keys[/] [dim]• Select: [/][green]Enter[/] [dim]• Cancel: [/][red]Ctrl+C[/]"
		));

		Panel welcomePanel = new Panel(welcomeTable)
			.Header(new PanelHeader("[bold red]💥 WELCOME TO BLASTMERGE - INTELLIGENT ITERATIVE FILE SYNCHRONIZATION 💥[/]", Justify.Center))
			.Border(BoxBorder.Double)
			.BorderColor(Color.Red);

		AnsiConsole.Write(welcomePanel);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows the goodbye screen with ASCII art.
	/// </summary>
	private static void ShowGoodbyeWithAscii()
	{
		// Epic explosive goodbye ASCII art
		string explosionArt = @"

[red] ██████   ██████   ██████  ██████  ██████  ██    ██ ███████ [/]
[red]██       ██    ██ ██    ██ ██   ██ ██   ██  ██  ██  ██      [/]
[red]██   ███ ██    ██ ██    ██ ██   ██ ██████    ████   █████   [/]
[red]██    ██ ██    ██ ██    ██ ██   ██ ██   ██    ██    ██      [/]
[red] ██████   ██████   ██████  ██████  ██████     ██    ███████ [/]

";

		AnsiConsole.Write(new Markup(explosionArt));

		ShowGoodbyeContent();
	}

	/// <summary>
	/// Shows the goodbye screen with plain text title.
	/// </summary>
	private static void ShowGoodbyeWithPlainText()
	{
		// Plain text title for narrow consoles
		AnsiConsole.Write(new Rule("[bold red]GOODBYE[/]")
		{
			Style = Style.Parse("red"),
			Justification = Justify.Center
		});
		AnsiConsole.WriteLine();

		ShowGoodbyeContent();
	}

	/// <summary>
	/// Shows the goodbye content panel (common for both ASCII and plain text versions).
	/// </summary>
	private static void ShowGoodbyeContent()
	{
		// Create epic goodbye content
		Table goodbyeTable = new Table()
			.BorderColor(Color.Gold1)
			.Border(TableBorder.Heavy)
			.AddColumn(new TableColumn("").Width(70).Centered());

		goodbyeTable
		.HideHeaders()
		.AddRow(new Markup(
			"[bold red]💥 DETONATION SUCCESSFUL! TARGET OBLITERATED! 💥[/]\n\n" +
			"[yellow]🧨 Your merge conflicts have been atomized[/]\n" +
			"[cyan]🔥 Files blasted into perfect harmony[/]\n" +
			"[magenta]💣 Repository chaos neutralized with extreme prejudice[/]\n\n" +
			"[bold white on red] EXPLOSIVE ENGINEERING BY KTSU DEMOLITION SQUAD [/]\n\n" +
			"[dim]Reload for more carnage at:[/]\n" +
			"[link=https://github.com/ktsu-dev/BlastMerge]🌐 https://github.com/ktsu-dev/BlastMerge[/]\n\n" +
			"[bold blue]Until next blast, keep the fireworks coming! 🎆[/]"
		));

		Panel goodbyePanel = new Panel(goodbyeTable)
			.Header(new PanelHeader("[bold gold1]🧨 DEMOLITION COMPLETE - YOU'RE A BLAST MASTER! 🧨[/]", Justify.Center))
			.Border(BoxBorder.Double)
			.BorderColor(Color.Red);

		AnsiConsole.Write(goodbyePanel);

		AnsiConsole.Write(new Markup("[bold yellow]💥 May your merges always go out with a BANG! 💥[/]"));

		// Add some space for dramatic effect
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();

		// Final epic message
		AnsiConsole.Write(new Markup("[dim italic]Exiting in 3... 2... 1... [/][bold red]💥[/]"));
		AnsiConsole.WriteLine();
	}
}
