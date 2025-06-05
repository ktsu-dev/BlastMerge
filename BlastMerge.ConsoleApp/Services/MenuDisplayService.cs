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
	/// Shows the welcome screen with application information.
	/// </summary>
	public static void ShowWelcomeScreen()
	{
		AnsiConsole.Clear();

		// Epic title with gradient colors
		FigletText figlet = new FigletText("BlastMerge")
			.LeftJustified()
			.Color(Color.DeepSkyBlue1);

		AnsiConsole.Write(figlet);

		// Create an epic welcome panel with rich content
		Table welcomeTable = new Table()
			.BorderColor(Color.Purple)
			.Border(TableBorder.Rounded)
			.AddColumn(new TableColumn("").Width(80).Centered());

		welcomeTable.AddRow(new Markup(
			"[bold red]💥 EXPLOSIVE FILE SYNCHRONIZATION WEAPON 💥[/]\n\n" +
			"[yellow]🧨 Detonates merge conflicts into harmony[/]\n" +
			"[green]💣 Nuclear-powered repository blasting[/]\n" +
			"[cyan]🔥 Incendiary diff visualization[/]\n" +
			"[magenta]⚡ High-voltage whitespace demolition[/]\n" +
			"[red]🚀 Shockwave iterative resolution[/]\n\n" +
			"[bold white on red] FUSION-POWERED BY DIFFPLEX & SPECTRE.CONSOLE [/]\n\n" +
			"[dim]Arm weapon: [/][yellow]↑ ↓ Arrow Keys[/] [dim]• Fire: [/][green]Enter[/] [dim]• Abort: [/][red]Escape[/]"
		));

		Panel welcomePanel = new Panel(welcomeTable)
			.Header(new PanelHeader("[bold red]💥 WELCOME TO THE BLAST ZONE - MERGING NEVER FELT SO EXPLOSIVE 💥[/]", Justify.Center))
			.Border(BoxBorder.Double)
			.BorderColor(Color.Red);

		AnsiConsole.Write(welcomePanel);

		// Add a status bar
		Rule rule = new Rule("[dim]Explosives armed and ready to blast... 🧨[/]")
			.RuleStyle("red")
			.LeftJustified();

		AnsiConsole.Write(rule);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows the goodbye screen when exiting the application.
	/// </summary>
	public static void ShowGoodbyeScreen()
	{
		AnsiConsole.Clear();

		// Epic goodbye with multiple figlet texts
		FigletText thankYou = new FigletText("Thank You!")
			.LeftJustified()
			.Color(Color.Gold1);

		AnsiConsole.Write(thankYou);

		FigletText goodbye = new FigletText("Goodbye!")
			.LeftJustified()
			.Color(Color.DeepPink1);

		AnsiConsole.Write(goodbye);

		// Create epic goodbye content
		Table goodbyeTable = new Table()
			.BorderColor(Color.Gold1)
			.Border(TableBorder.Heavy)
			.AddColumn(new TableColumn("").Width(70).Centered());

		goodbyeTable.AddRow(new Markup(
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

		// Add epic final message
		Rule finalRule = new Rule("[bold yellow]💥 May your merges always go out with a BANG! 💥[/]")
			.RuleStyle("red")
			.Centered();

		AnsiConsole.Write(finalRule);

		// Add some space for dramatic effect
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();

		// Final epic message
		AnsiConsole.Write(new Markup("[dim italic]Exiting in 3... 2... 1... [/][bold red]💥[/]"));
		AnsiConsole.WriteLine();
	}
}
