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
		List<(string lineNum, string content, string style)> leftPieces = [];
		List<(string lineNum, string content, string style)> rightPieces = [];

		// This follows the same approach as DiffPlex's SideBySideDiffBuilder
		int aPos = 0; // Position in PiecesOld
		int bPos = 0; // Position in PiecesNew

		foreach (DiffPlex.Model.DiffBlock diffBlock in diffResult.DiffBlocks)
		{
			// Add unchanged pieces before this diff block
			while (bPos < diffBlock.InsertStartB && aPos < diffBlock.DeleteStartA)
			{
				leftPieces.Add(((aPos + 1).ToString(), diffResult.PiecesOld[aPos], "white"));
				rightPieces.Add(((bPos + 1).ToString(), diffResult.PiecesNew[bPos], "white"));
				aPos++;
				bPos++;
			}

			// Process the paired changes (modified lines)
			int i = 0;
			for (; i < Math.Min(diffBlock.DeleteCountA, diffBlock.InsertCountB); i++)
			{
				// Add deleted line from left side
				leftPieces.Add(((aPos + 1).ToString(), diffResult.PiecesOld[i + diffBlock.DeleteStartA], "red on darkred"));
				// Add inserted line to right side
				rightPieces.Add(((bPos + 1).ToString(), diffResult.PiecesNew[i + diffBlock.InsertStartB], "green on darkgreen"));
				aPos++;
				bPos++;
			}

			// Handle remaining deletions (more deletions than insertions)
			if (diffBlock.DeleteCountA > diffBlock.InsertCountB)
			{
				for (; i < diffBlock.DeleteCountA; i++)
				{
					leftPieces.Add(((aPos + 1).ToString(), diffResult.PiecesOld[i + diffBlock.DeleteStartA], "red on darkred"));
					rightPieces.Add(("", "", "white")); // Empty placeholder on right
					aPos++;
				}
			}
			// Handle remaining insertions (more insertions than deletions)
			else
			{
				for (; i < diffBlock.InsertCountB; i++)
				{
					leftPieces.Add(("", "", "white")); // Empty placeholder on left
					rightPieces.Add(((bPos + 1).ToString(), diffResult.PiecesNew[i + diffBlock.InsertStartB], "green on darkgreen"));
					bPos++;
				}
			}
		}

		// Add any remaining unchanged pieces after all diff blocks
		while (bPos < diffResult.PiecesNew.Count && aPos < diffResult.PiecesOld.Count)
		{
			leftPieces.Add(((aPos + 1).ToString(), diffResult.PiecesOld[aPos], "white"));
			rightPieces.Add(((bPos + 1).ToString(), diffResult.PiecesNew[bPos], "white"));
			aPos++;
			bPos++;
		}

		// Ensure both sides have the same number of rows
		int maxRows = Math.Max(leftPieces.Count, rightPieces.Count);
		while (leftPieces.Count < maxRows)
		{
			leftPieces.Add(("", "", "white"));
		}
		while (rightPieces.Count < maxRows)
		{
			rightPieces.Add(("", "", "white"));
		}

		// Add rows to table
		for (int i = 0; i < maxRows; i++)
		{
			(string leftLineNum, string leftContent, string leftStyle) = leftPieces[i];
			(string rightLineNum, string rightContent, string rightStyle) = rightPieces[i];

			// Format content with markup
			string leftContentFormatted = string.IsNullOrEmpty(leftContent) ? "" : $"[{leftStyle}]{leftContent.EscapeMarkup()}[/]";
			string rightContentFormatted = string.IsNullOrEmpty(rightContent) ? "" : $"[{rightStyle}]{rightContent.EscapeMarkup()}[/]";

			// Add prefixes for change types
			if (!string.IsNullOrEmpty(leftContent) && leftStyle.Contains("red"))
			{
				leftContentFormatted = "- " + leftContentFormatted;
			}
			else if (!string.IsNullOrEmpty(leftContent))
			{
				leftContentFormatted = "  " + leftContentFormatted;
			}

			if (!string.IsNullOrEmpty(rightContent) && rightStyle.Contains("green"))
			{
				rightContentFormatted = "+ " + rightContentFormatted;
			}
			else if (!string.IsNullOrEmpty(rightContent))
			{
				rightContentFormatted = "  " + rightContentFormatted;
			}

			table.AddRow(leftLineNum, leftContentFormatted, rightLineNum, rightContentFormatted);
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
