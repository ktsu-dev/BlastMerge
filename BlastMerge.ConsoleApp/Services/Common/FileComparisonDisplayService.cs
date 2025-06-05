// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.Common;

using System.Collections.ObjectModel;
using System.IO;
using DiffPlex.Model;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralized service for file comparison display functionality.
/// </summary>
public static class FileComparisonDisplayService
{
	/// <summary>
	/// Compares two files and displays the results with user choice of format.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	public static void CompareTwoFiles(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		try
		{
			IReadOnlyCollection<LineDifference> differences = FileDiffer.FindDifferences(file1, file2);

			if (differences.Count == 0)
			{
				UIHelper.ShowSuccess("✅ Files are identical!");
			}
			else
			{
				UIHelper.ShowWarning("📄 Files are different.");
				ShowFileComparisonOptions(file1, file2);
			}
		}
		catch (FileNotFoundException ex)
		{
			UIHelper.ShowError($"File not found: {ex.Message}");
		}
		catch (IOException ex)
		{
			UIHelper.ShowError($"IO error: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			UIHelper.ShowError($"Access denied: {ex.Message}");
		}
	}

	/// <summary>
	/// Shows file comparison options and handles user selection.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	public static void ShowFileComparisonOptions(string file1, string file2)
	{
		string diffFormat = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Choose diff format:[/]")
				.AddChoices(
					"📊 Change Summary (Added/Removed lines only)",
					"🔧 Git-style Diff (Full context)",
					"🎨 Side-by-Side Diff (Rich formatting)",
					"⏭️ Skip comparison"
				));

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
		// Skip if user chose to skip
	}

	/// <summary>
	/// Shows a change summary between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	public static void ShowChangeSummary(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		try
		{
			IReadOnlyCollection<LineDifference> differences = FileDiffer.FindDifferences(file1, file2);

			if (differences.Count == 0)
			{
				UIHelper.ShowSuccess("Files are identical!");
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
		catch (FileNotFoundException ex)
		{
			UIHelper.ShowError($"File not found: {ex.Message}");
		}
		catch (IOException ex)
		{
			UIHelper.ShowError($"IO error: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			UIHelper.ShowError($"Access denied: {ex.Message}");
		}
	}

	/// <summary>
	/// Shows a git-style diff between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	public static void ShowGitStyleDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		try
		{
			string[] lines1 = File.ReadAllLines(file1);
			string[] lines2 = File.ReadAllLines(file2);
			Collection<ColoredDiffLine> coloredDiff = FileDiffer.GenerateColoredDiff(file1, file2, lines1, lines2);

			Panel panel = new(RenderColoredDiff(coloredDiff))
			{
				Header = new PanelHeader("[bold]Git-style Diff[/]"),
				Border = BoxBorder.Rounded
			};

			AnsiConsole.Write(panel);
		}
		catch (FileNotFoundException ex)
		{
			UIHelper.ShowError($"File not found: {ex.Message}");
		}
		catch (IOException ex)
		{
			UIHelper.ShowError($"IO error: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			UIHelper.ShowError($"Access denied: {ex.Message}");
		}
	}

	/// <summary>
	/// Shows a side-by-side diff comparison of two files.
	/// </summary>
	/// <param name="file1">Path to the first file.</param>
	/// <param name="file2">Path to the second file.</param>
	public static void ShowSideBySideDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		try
		{
			// Use core DiffPlex API to get DiffResult with DiffBlocks
			string content1 = File.ReadAllText(file1);
			string content2 = File.ReadAllText(file2);

			DiffResult diffResult = DiffPlex.Differ.Instance.CreateLineDiffs(content1, content2, ignoreWhitespace: false, ignoreCase: false);

			if (!diffResult.DiffBlocks.Any())
			{
				UIHelper.ShowSuccess("Files are identical!");
				return;
			}

			Table table = CreateSideBySideTable(file1, file2);
			PopulateTableWithDiffBlocks(table, diffResult);
			DisplayDiffWithDiffBlockStatistics(table, diffResult);
			ShowDiffLegend();
		}
		catch (FileNotFoundException ex)
		{
			UIHelper.ShowError($"File not found: {ex.Message}");
		}
		catch (IOException ex)
		{
			UIHelper.ShowError($"IO error generating side-by-side diff: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			UIHelper.ShowError($"Access denied: {ex.Message}");
		}
	}

	/// <summary>
	/// Checks if the files are identical based on diff model.
	/// </summary>

	/// <summary>
	/// Creates the side-by-side table structure.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	/// <returns>Configured table for side-by-side display.</returns>
	private static Table CreateSideBySideTable(string file1, string file2)
	{
		(string label1, string label2) = FileDisplayService.GetDistinguishingLabels(file1, file2);

		return new Table()
			.AddColumn(new TableColumn("[dim]#[/]").Width(4))           // Line numbers for file1
			.AddColumn(new TableColumn($"[bold cyan]{label1}[/]"))       // File1 content
			.AddColumn(new TableColumn("[dim]#[/]").Width(4))           // Line numbers for file2
			.AddColumn(new TableColumn($"[bold cyan]{label2}[/]"))       // File2 content
			.Border(TableBorder.Rounded)
			.Expand();
	}

	/// <summary>
	/// Populates the table with diff blocks from both sides.
	/// </summary>
	/// <param name="table">The table to populate.</param>
	/// <param name="diffResult">The diff result with diff blocks.</param>
	private static void PopulateTableWithDiffBlocks(Table table, DiffResult diffResult)
	{
		string[] linesA = [.. diffResult.PiecesOld];
		string[] linesB = [.. diffResult.PiecesNew];

		const int contextLines = 3; // Show 3 lines of context around changes

		foreach (DiffPlex.Model.DiffBlock block in diffResult.DiffBlocks)
		{
			// Calculate context range
			int startA = Math.Max(0, block.DeleteStartA - contextLines);
			int endA = Math.Min(linesA.Length - 1, block.DeleteStartA + block.DeleteCountA + contextLines - 1);

			int startB = Math.Max(0, block.InsertStartB - contextLines);
			int endB = Math.Min(linesB.Length - 1, block.InsertStartB + block.InsertCountB + contextLines - 1);

			// Show context before changes
			int contextStart = Math.Min(startA, startB);
			int preContextLines = Math.Min(contextLines, Math.Min(block.DeleteStartA, block.InsertStartB));

			for (int i = 0; i < preContextLines; i++)
			{
				int lineA = block.DeleteStartA - preContextLines + i;
				int lineB = block.InsertStartB - preContextLines + i;

				if (lineA >= 0 && lineA < linesA.Length && lineB >= 0 && lineB < linesB.Length)
				{
					string leftContent = $"  {linesA[lineA].EscapeMarkup()}";
					string rightContent = $"  {linesB[lineB].EscapeMarkup()}";
					table.AddRow((lineA + 1).ToString(), leftContent, (lineB + 1).ToString(), rightContent);
				}
			}

			// Show the actual changes
			int maxChanges = Math.Max(block.DeleteCountA, block.InsertCountB);

			for (int i = 0; i < maxChanges; i++)
			{
				string leftLineNum = "";
				string leftContent = "";
				string rightLineNum = "";
				string rightContent = "";

				// Handle deleted lines (left side)
				if (i < block.DeleteCountA)
				{
					int lineIndex = block.DeleteStartA + i;
					leftLineNum = (lineIndex + 1).ToString();
					leftContent = $"[red on darkred]- {linesA[lineIndex].EscapeMarkup()}[/]";
				}

				// Handle inserted lines (right side)
				if (i < block.InsertCountB)
				{
					int lineIndex = block.InsertStartB + i;
					rightLineNum = (lineIndex + 1).ToString();
					rightContent = $"[green on darkgreen]+ {linesB[lineIndex].EscapeMarkup()}[/]";
				}

				table.AddRow(leftLineNum, leftContent, rightLineNum, rightContent);
			}

			// Show context after changes
			int postContextLines = contextLines;
			int leftPostStart = block.DeleteStartA + block.DeleteCountA;
			int rightPostStart = block.InsertStartB + block.InsertCountB;

			for (int i = 0; i < postContextLines; i++)
			{
				int lineA = leftPostStart + i;
				int lineB = rightPostStart + i;

				if (lineA < linesA.Length && lineB < linesB.Length)
				{
					string leftContent = $"  {linesA[lineA].EscapeMarkup()}";
					string rightContent = $"  {linesB[lineB].EscapeMarkup()}";
					table.AddRow((lineA + 1).ToString(), leftContent, (lineB + 1).ToString(), rightContent);
				}
			}

			// Add separator between blocks if there are more blocks
			if (block != diffResult.DiffBlocks.Last())
			{
				table.AddEmptyRow();
				table.AddRow("[dim]...[/]", "[dim]...[/]", "[dim]...[/]", "[dim]...[/]");
				table.AddEmptyRow();
			}
		}
	}

	/// <summary>
	/// Displays the diff table with statistics in a panel.
	/// </summary>
	/// <param name="table">The populated table.</param>
	/// <param name="diffResult">The diff result with diff blocks.</param>
	private static void DisplayDiffWithDiffBlockStatistics(Table table, DiffResult diffResult)
	{
		string stats = CalculateDiffStatistics(diffResult);

		Panel panel = new(table)
		{
			Header = new PanelHeader($"[bold]Side-by-Side Diff[/] ({stats})"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(panel);
	}

	/// <summary>
	/// Calculates and formats diff statistics.
	/// </summary>
	/// <param name="diffResult">The diff result with diff blocks.</param>
	/// <returns>Formatted statistics string.</returns>
	private static string CalculateDiffStatistics(DiffResult diffResult)
	{
		int additions = diffResult.DiffBlocks.Sum(b => b.InsertCountB);
		int deletions = diffResult.DiffBlocks.Sum(b => b.DeleteCountA);
		int modifications = diffResult.DiffBlocks.Count(b => b.InsertCountB > 0 && b.DeleteCountA > 0);

		string stats = $"[green]+{additions}[/] [red]-{deletions}[/]";
		if (modifications > 0)
		{
			stats += $" [yellow]~{modifications}[/]";
		}

		return stats;
	}

	/// <summary>
	/// Shows the diff legend panel.
	/// </summary>
	private static void ShowDiffLegend()
	{
		Panel legend = new(
			"[green]+ Added lines[/]   " +
			"[red]- Deleted lines[/]   " +
			"[yellow]~ Modified lines[/]   " +
			"[dim]  Unchanged lines[/]")
		{
			Header = new PanelHeader("[dim]Legend[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(legend);
	}

	/// <summary>
	/// Renders colored diff lines as a formatted string.
	/// </summary>
	/// <param name="coloredDiff">The colored diff lines to render.</param>
	/// <returns>Formatted diff string with markup.</returns>
	private static string RenderColoredDiff(IEnumerable<ColoredDiffLine> coloredDiff)
	{
		System.Text.StringBuilder sb = new();
		foreach (ColoredDiffLine line in coloredDiff)
		{
			string colorMarkup = line.Color switch
			{
				DiffColor.Addition => "[green]",
				DiffColor.Deletion => "[red]",
				DiffColor.ChunkHeader => "[yellow]",
				DiffColor.FileHeader => "[cyan]",
				DiffColor.Default => "",
				_ => ""
			};

			string endMarkup = string.IsNullOrEmpty(colorMarkup) ? "" : "[/]";
			sb.AppendLine($"{colorMarkup}{line.Content.EscapeMarkup()}{endMarkup}");
		}
		return sb.ToString();
	}
}
