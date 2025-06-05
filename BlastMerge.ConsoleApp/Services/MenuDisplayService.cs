// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
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

		FigletText figlet = new FigletText("BlastMerge")
			.LeftJustified()
			.Color(Color.Cyan1);

		AnsiConsole.Write(figlet);

		Panel panel = new Panel(
			new Markup("[bold]Cross-Repository File Synchronization Tool[/]\n\n" +
					  "[dim]Efficiently merge and synchronize files across multiple repositories[/]\n" +
					  "[dim]Navigate using arrow keys, press Enter to select[/]"))
			.Header("Welcome")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Blue);

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows the goodbye screen when exiting the application.
	/// </summary>
	public static void ShowGoodbyeScreen()
	{
		AnsiConsole.Clear();

		FigletText figlet = new FigletText("Goodbye!")
			.Centered()
			.Color(Color.Yellow);

		AnsiConsole.Write(figlet);

		Panel panel = new Panel(
			new Markup("[bold]Thank you for using BlastMerge![/]\n\n" +
					  "[dim]Your files have been processed successfully.[/]\n" +
					  "[dim]Visit us at https://github.com/ktsu-dev/BlastMerge for updates.[/]"))
			.Header("Session Complete")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Green);

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();

		AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
		Console.ReadKey();
	}
}
