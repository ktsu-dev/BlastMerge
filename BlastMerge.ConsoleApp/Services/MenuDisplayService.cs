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
			"[bold blue]🚀 CROSS-REPOSITORY FILE SYNCHRONIZATION TOOL 🚀[/]\n\n" +
			"[yellow]⚡ Lightning-fast intelligent merging[/]\n" +
			"[green]🔄 Multi-repository synchronization[/]\n" +
			"[cyan]📊 Advanced diff visualization[/]\n" +
			"[magenta]🎯 Whitespace-aware comparisons[/]\n" +
			"[red]🛠️ Iterative merge resolution[/]\n\n" +
			"[bold white on blue] POWERED BY DIFFPLEX & SPECTRE.CONSOLE [/]\n\n" +
			"[dim]Navigate: [/][yellow]↑ ↓ Arrow Keys[/] [dim]• Select: [/][green]Enter[/] [dim]• Exit: [/][red]Escape[/]"
		));

		Panel welcomePanel = new Panel(welcomeTable)
			.Header(new PanelHeader("[bold yellow]🌟 WELCOME TO THE FUTURE OF FILE MERGING 🌟[/]", Justify.Center))
			.Border(BoxBorder.Double)
			.BorderColor(Color.Blue);

		AnsiConsole.Write(welcomePanel);

		// Add a status bar
		Rule rule = new Rule("[dim]Ready to merge the impossible[/]")
			.RuleStyle("cyan")
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
			"[bold green]🎉 MISSION ACCOMPLISHED! 🎉[/]\n\n" +
			"[yellow]✨ Your files have been perfectly synchronized[/]\n" +
			"[cyan]🔥 Changes merged with surgical precision[/]\n" +
			"[magenta]🚀 Repository harmony restored[/]\n\n" +
			"[bold white on purple] POWERED BY THE KTSU DEVELOPMENT TEAM [/]\n\n" +
			"[dim]Visit us for updates and new features:[/]\n" +
			"[link=https://github.com/ktsu-dev/BlastMerge]🌐 https://github.com/ktsu-dev/BlastMerge[/]\n\n" +
			"[bold blue]Until next time, keep merging! 🛡️[/]"
		));

		Panel goodbyePanel = new Panel(goodbyeTable)
			.Header(new PanelHeader("[bold gold1]🏆 SESSION COMPLETE - YOU'RE A MERGE MASTER! 🏆[/]", Justify.Center))
			.Border(BoxBorder.Double)
			.BorderColor(Color.Green);

		AnsiConsole.Write(goodbyePanel);

		// Add epic final message
		Rule finalRule = new Rule("[bold yellow]⭐ May your diffs be ever in your favor ⭐[/]")
			.RuleStyle("gold1")
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
