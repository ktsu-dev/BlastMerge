// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.Common;

using System.IO;
using ktsu.BlastMerge.ConsoleApp.Models;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Spectre.Console;

/// <summary>
/// Centralized service for comparison operations to eliminate code duplication.
/// </summary>
/// <param name="fileComparisonDisplayService">File comparison display service</param>
/// <param name="fileDiffer">File differ service</param>
public class ComparisonOperationsService(
	FileComparisonDisplayService fileComparisonDisplayService,
	FileDiffer fileDiffer)
{
	/// <summary>
	/// Shows a menu title with consistent formatting.
	/// </summary>
	/// <param name="title">The menu title.</param>
	private static void ShowMenuTitle(string title)
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine($"[bold cyan]{title}[/]");
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Handles comparing two directories with user input and display.
	/// </summary>
	public void HandleCompareTwoDirectories()
	{
		ShowMenuTitle("Compare Two Directories");

		string dir1 = AppDataHistoryInput.AskWithHistory("[cyan]Enter the first directory path:[/]");
		if (string.IsNullOrWhiteSpace(dir1))
		{
			UIHelper.ShowWarning(UIHelper.OperationCancelledMessage);
			return;
		}

		if (!Directory.Exists(dir1))
		{
			UIHelper.ShowError("Error: First directory does not exist!");
			return;
		}

		string dir2 = AppDataHistoryInput.AskWithHistory("[cyan]Enter the second directory path:[/]");
		if (string.IsNullOrWhiteSpace(dir2))
		{
			UIHelper.ShowWarning(UIHelper.OperationCancelledMessage);
			return;
		}

		if (!Directory.Exists(dir2))
		{
			UIHelper.ShowError("Error: Second directory does not exist!");
			return;
		}

		string pattern = AppDataHistoryInput.AskWithHistory("[cyan]Enter file pattern (e.g., *.txt, *.cs):[/]", "*.*");

		bool recursive = AnsiConsole.Confirm("[cyan]Search subdirectories recursively?[/]", false);

		CompareDirectories(dir1, dir2, pattern, recursive);
		UIHelper.WaitForKeyPress();
	}

	/// <summary>
	/// Handles comparing two specific files with user input and display.
	/// </summary>
	public void HandleCompareTwoSpecificFiles()
	{
		ShowMenuTitle("Compare Two Specific Files");

		string file1 = AppDataHistoryInput.AskWithHistory("[cyan]Enter the first file path:[/]");
		if (string.IsNullOrWhiteSpace(file1))
		{
			UIHelper.ShowWarning(UIHelper.OperationCancelledMessage);
			return;
		}

		if (!File.Exists(file1))
		{
			UIHelper.ShowError("Error: First file does not exist!");
			return;
		}

		string file2 = AppDataHistoryInput.AskWithHistory("[cyan]Enter the second file path:[/]");
		if (string.IsNullOrWhiteSpace(file2))
		{
			UIHelper.ShowWarning(UIHelper.OperationCancelledMessage);
			return;
		}

		if (!File.Exists(file2))
		{
			UIHelper.ShowError("Error: Second file does not exist!");
			return;
		}

		// Use centralized file comparison service
		fileComparisonDisplayService.CompareTwoFiles(file1, file2);
		UIHelper.WaitForKeyPress();
	}

	/// <summary>
	/// Compares two directories and displays results.
	/// </summary>
	/// <param name="dir1">First directory path.</param>
	/// <param name="dir2">Second directory path.</param>
	/// <param name="pattern">File search pattern.</param>
	/// <param name="recursive">Whether to search recursively.</param>
	public void CompareDirectories(string dir1, string dir2, string pattern, bool recursive)
	{
		DirectoryComparisonResult? result = null;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start($"[yellow]Comparing directories...[/]", ctx =>
			{
				result = fileDiffer.FindDifferences(dir1, dir2, pattern, recursive);
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
				fileComparisonDisplayService.ShowChangeSummary(file1, file2);
			}
		}
	}
}
