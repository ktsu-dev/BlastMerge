// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using System.IO;
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
			ShowWarning("Operation cancelled.");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			ShowWarning("Operation cancelled.");
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
			ShowWarning("Operation cancelled.");
			return;
		}

		if (!Directory.Exists(dir1))
		{
			ShowError("Error: First directory does not exist!");
			return;
		}

		string dir2 = HistoryInput.AskWithHistory("[cyan]Enter the second directory path:[/]");
		if (string.IsNullOrWhiteSpace(dir2))
		{
			ShowWarning("Operation cancelled.");
			return;
		}

		if (!Directory.Exists(dir2))
		{
			ShowError("Error: Second directory does not exist!");
			return;
		}

		string pattern = HistoryInput.AskWithHistory("[cyan]Enter file pattern (e.g., *.txt, *.cs):[/]", "*.*");
		bool recursive = AnsiConsole.Confirm("[cyan]Search subdirectories recursively?[/]", false);

		CompareDirectories(dir1, dir2, pattern, recursive);
		WaitForKeyPress();
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
			ShowWarning("Operation cancelled.");
			return;
		}

		if (!File.Exists(file1))
		{
			ShowError("Error: First file does not exist!");
			return;
		}

		string file2 = HistoryInput.AskWithHistory("[cyan]Enter the second file path:[/]");
		if (string.IsNullOrWhiteSpace(file2))
		{
			ShowWarning("Operation cancelled.");
			return;
		}

		if (!File.Exists(file2))
		{
			ShowError("Error: Second file does not exist!");
			return;
		}

		CompareTwoFiles(file1, file2);
		WaitForKeyPress();
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

				ShowChangeSummary(file1, file2);
			}
		}
	}

	/// <summary>
	/// Compares two specific files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	private static void CompareTwoFiles(string file1, string file2)
	{
		bool areSame = false;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start("[yellow]Comparing files...[/]", ctx =>
			{
				areSame = DiffPlexDiffer.AreFilesIdentical(file1, file2);
			});

		if (areSame)
		{
			ShowSuccess("✅ Files are identical!");
			return;
		}

		ShowWarning("📄 Files are different.");

		string diffFormat = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Choose diff format:[/]")
				.AddChoices([
					"📊 Change Summary (Added/Removed lines only)",
					"🔧 Git-style Diff (Full context)",
					"🎨 Side-by-Side Diff (Rich formatting)"
				]));

		AnsiConsole.WriteLine();

		if (diffFormat.Contains("📊"))
		{
			ShowChangeSummary(file1, file2);
		}
		else if (diffFormat.Contains("🔧"))
		{
			ShowGitStyleDiff(file1, file2);
		}
		else if (diffFormat.Contains("🎨"))
		{
			ShowSideBySideDiff(file1, file2);
		}
	}

	/// <summary>
	/// Shows a change summary between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	private static void ShowChangeSummary(string file1, string file2)
	{
		IReadOnlyCollection<LineDifference> differences = FileDiffer.FindDifferences(file1, file2);

		if (differences.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]Files are identical![/]");
			return;
		}

		Panel panel = new(
			$"[yellow]Found {differences.Count} differences[/]\n" +
			$"[green]+ Additions: {differences.Count(d => d.Type == LineDifferenceType.Added)}[/]\n" +
			$"[red]- Deletions: {differences.Count(d => d.Type == LineDifferenceType.Deleted)}[/]\n" +
			$"[yellow]~ Modifications: {differences.Count(d => d.Type == LineDifferenceType.Modified)}[/]")
		{
			Header = new PanelHeader("[bold]Change Summary[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(panel);
	}

	/// <summary>
	/// Shows a git-style diff between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	private static void ShowGitStyleDiff(string file1, string file2)
	{
		string[] lines1 = File.ReadAllLines(file1);
		string[] lines2 = File.ReadAllLines(file2);
		System.Collections.ObjectModel.Collection<ColoredDiffLine> coloredDiff = FileDiffer.GenerateColoredDiff(file1, file2, lines1, lines2);

		Panel panel = new(RenderColoredDiff(coloredDiff))
		{
			Header = new PanelHeader("[bold]Git-style Diff[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(panel);
	}

	/// <summary>
	/// Shows a side-by-side diff between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	private static void ShowSideBySideDiff(string file1, string file2)
	{
		try
		{
			// TODO: Implement actual side-by-side diff using file1 and file2
			// For now, show a placeholder message
			AnsiConsole.MarkupLine($"[yellow]Side-by-side diff implementation pending for:[/]");
			AnsiConsole.MarkupLine($"[dim]File 1: {file1}[/]");
			AnsiConsole.MarkupLine($"[dim]File 2: {file2}[/]");
			AnsiConsole.MarkupLine("[yellow]DiffPlex integration coming soon...[/]");
		}
		catch (FileNotFoundException ex)
		{
			AnsiConsole.MarkupLine($"[red]File not found: {ex.Message}[/]");
		}
		catch (IOException ex)
		{
			AnsiConsole.MarkupLine($"[red]IO error generating side-by-side diff: {ex.Message}[/]");
		}
		catch (UnauthorizedAccessException ex)
		{
			AnsiConsole.MarkupLine($"[red]Access denied: {ex.Message}[/]");
		}
	}

	/// <summary>
	/// Renders colored diff for display in panels.
	/// </summary>
	/// <param name="coloredDiff">The colored diff lines.</param>
	/// <returns>Rendered markup string.</returns>
	private static string RenderColoredDiff(IEnumerable<ColoredDiffLine> coloredDiff)
	{
		IEnumerable<string> lines = coloredDiff.Take(50).Select(line => line.Color switch
		{
			DiffColor.Addition => $"[green]{line.Content.EscapeMarkup()}[/]",
			DiffColor.Deletion => $"[red]{line.Content.EscapeMarkup()}[/]",
			DiffColor.ChunkHeader => $"[cyan]{line.Content.EscapeMarkup()}[/]",
			DiffColor.FileHeader => $"[bold blue]{line.Content.EscapeMarkup()}[/]",
			DiffColor.Default => line.Content.EscapeMarkup(),
			_ => line.Content.EscapeMarkup()
		});

		return string.Join("\n", lines);
	}
}
