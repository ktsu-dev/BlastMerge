// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using System.IO;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for compare files operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class CompareFilesMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	// Menu display text to command mappings for compare operations
	private static readonly Dictionary<string, CompareChoice> CompareChoices = new()
	{
		["🔍 Compare Files in Directory"] = CompareChoice.CompareFilesInDirectory,
		["📁 Compare Two Directories"] = CompareChoice.CompareTwoDirectories,
		["📄 Compare Two Specific Files"] = CompareChoice.CompareTwoSpecificFiles,
		["🔙 Back to Main Menu"] = CompareChoice.BackToMainMenu
	};

	/// <summary>
	/// Handles the compare files operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Compare Files");

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Select comparison type:[/]")
				.AddChoices(CompareChoices.Keys));

		if (CompareChoices.TryGetValue(selection, out CompareChoice choice))
		{
			switch (choice)
			{
				case CompareChoice.CompareFilesInDirectory:
					HandleCompareFilesInDirectory();
					break;
				case CompareChoice.CompareTwoDirectories:
					HandleCompareTwoDirectories();
					break;
				case CompareChoice.CompareTwoSpecificFiles:
					HandleCompareTwoSpecificFiles();
					break;
				case CompareChoice.BackToMainMenu:
					// Return to main menu
					break;
				default:
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Handles comparing files in a directory.
	/// </summary>
	private void HandleCompareFilesInDirectory()
	{
		ShowMenuTitle("Compare Files in Directory");

		string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			UIHelper.ShowWarning("Operation cancelled.");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			UIHelper.ShowWarning("Operation cancelled.");
			return;
		}

		ApplicationService.ProcessFiles(directory, fileName);
	}

	/// <summary>
	/// Handles comparing two directories.
	/// </summary>
	private static void HandleCompareTwoDirectories()
	{
		ShowMenuTitle("Compare Two Directories");

		string dir1 = HistoryInput.AskWithHistory("[cyan]Enter the first directory path:[/]");
		if (string.IsNullOrWhiteSpace(dir1))
		{
			UIHelper.ShowWarning("Operation cancelled.");
			return;
		}

		if (!Directory.Exists(dir1))
		{
			UIHelper.ShowError("Error: First directory does not exist!");
			return;
		}

		string dir2 = HistoryInput.AskWithHistory("[cyan]Enter the second directory path:[/]");
		if (string.IsNullOrWhiteSpace(dir2))
		{
			UIHelper.ShowWarning("Operation cancelled.");
			return;
		}

		if (!Directory.Exists(dir2))
		{
			UIHelper.ShowError("Error: Second directory does not exist!");
			return;
		}

		string pattern = HistoryInput.AskWithHistory("[cyan]Enter file pattern (e.g., *.txt, *.cs):[/]", "*.*");
		bool recursive = AnsiConsole.Confirm("[cyan]Search subdirectories recursively?[/]", false);

		CompareDirectories(dir1, dir2, pattern, recursive);
		UIHelper.WaitForKeyPress();
	}

	/// <summary>
	/// Handles comparing two specific files.
	/// </summary>
	private static void HandleCompareTwoSpecificFiles()
	{
		ShowMenuTitle("Compare Two Specific Files");

		string file1 = HistoryInput.AskWithHistory("[cyan]Enter the first file path:[/]");
		if (string.IsNullOrWhiteSpace(file1))
		{
			UIHelper.ShowWarning("Operation cancelled.");
			return;
		}

		if (!File.Exists(file1))
		{
			UIHelper.ShowError("Error: First file does not exist!");
			return;
		}

		string file2 = HistoryInput.AskWithHistory("[cyan]Enter the second file path:[/]");
		if (string.IsNullOrWhiteSpace(file2))
		{
			UIHelper.ShowWarning("Operation cancelled.");
			return;
		}

		if (!File.Exists(file2))
		{
			UIHelper.ShowError("Error: Second file does not exist!");
			return;
		}

		// Use centralized file comparison service
		FileComparisonDisplayService.CompareTwoFiles(file1, file2);
		UIHelper.WaitForKeyPress();
	}

	/// <summary>
	/// Compares two directories.
	/// </summary>
	/// <param name="dir1">First directory path.</param>
	/// <param name="dir2">Second directory path.</param>
	/// <param name="pattern">File search pattern.</param>
	/// <param name="recursive">Whether to search recursively.</param>
	private static void CompareDirectories(string dir1, string dir2, string pattern, bool recursive)
	{
		DirectoryComparisonResult? result = null;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start($"[yellow]Comparing directories...[/]", ctx =>
			{
				result = FileDiffer.FindDifferences(dir1, dir2, pattern, recursive);
				ctx.Status("[yellow]Creating comparison report...[/]");
			});

		// Display results (outside the status context)
		Table table = new Table()
			.AddColumn("[bold]Category[/]")
			.AddColumn("[bold]Count[/]")
			.AddColumn("[bold]Examples[/]")
			.Border(TableBorder.Rounded);

		table.AddRow(
			"[green]Identical Files[/]",
			$"[cyan]{result!.SameFiles.Count}[/]",
			string.Join(", ", result.SameFiles.Take(3).Select(Path.GetFileName))
		);

		table.AddRow(
			"[yellow]Modified Files[/]",
			$"[cyan]{result.ModifiedFiles.Count}[/]",
			string.Join(", ", result.ModifiedFiles.Take(3).Select(Path.GetFileName))
		);

		table.AddRow(
			"[red]Only in First Directory[/]",
			$"[cyan]{result.OnlyInDir1.Count}[/]",
			string.Join(", ", result.OnlyInDir1.Take(3).Select(Path.GetFileName))
		);

		table.AddRow(
			"[red]Only in Second Directory[/]",
			$"[cyan]{result.OnlyInDir2.Count}[/]",
			string.Join(", ", result.OnlyInDir2.Take(3).Select(Path.GetFileName))
		);

		AnsiConsole.Write(table);

		// Show detailed differences for modified files if requested (outside status context)
		if (result.ModifiedFiles.Count > 0 &&
			AnsiConsole.Confirm("[cyan]Show detailed differences for modified files?[/]", false))
		{
			foreach (string? file in result.ModifiedFiles.Take(5)) // Limit to first 5
			{
				string file1 = Path.Combine(dir1, file);
				string file2 = Path.Combine(dir2, file);

				AnsiConsole.WriteLine();
				Rule rule = new($"[bold]Differences: {file}[/]")
				{
					Style = Style.Parse("blue")
				};
				AnsiConsole.Write(rule);

				// Use centralized file comparison service
				FileComparisonDisplayService.ShowChangeSummary(file1, file2);
			}
		}
	}
}
