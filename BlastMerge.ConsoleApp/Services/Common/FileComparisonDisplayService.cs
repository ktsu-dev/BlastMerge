// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.Common;

using System.Collections.ObjectModel;
using System.IO;
using DiffPlex.DiffBuilder.Model;
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
	/// Shows a side-by-side diff between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	public static void ShowSideBySideDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		try
		{
			SideBySideDiffModel sideBySideDiff = DiffPlexDiffer.GenerateSideBySideDiff(file1, file2);

			if (AreFilesIdentical(sideBySideDiff))
			{
				UIHelper.ShowSuccess("Files are identical!");
				return;
			}

			Table table = CreateSideBySideTable(file1, file2);
			PopulateTableWithDiffLines(table, sideBySideDiff);
			DisplayDiffWithStatistics(table, sideBySideDiff);
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
	/// <param name="sideBySideDiff">The side-by-side diff model.</param>
	/// <returns>True if files are identical.</returns>
	private static bool AreFilesIdentical(SideBySideDiffModel sideBySideDiff) =>
		sideBySideDiff.OldText.Lines.All(l => l.Type == ChangeType.Unchanged) &&
		sideBySideDiff.NewText.Lines.All(l => l.Type == ChangeType.Unchanged);

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
	/// Populates the table with diff lines from both sides.
	/// </summary>
	/// <param name="table">The table to populate.</param>
	/// <param name="sideBySideDiff">The side-by-side diff model.</param>
	private static void PopulateTableWithDiffLines(Table table, SideBySideDiffModel sideBySideDiff)
	{
		// Process each side independently to create rows
		List<(string leftLineNum, string leftContent)> leftRows = [];
		List<(string rightLineNum, string rightContent)> rightRows = [];

		int leftLineNumber = 1;
		int rightLineNumber = 1;

		// Process left side (old text)
		foreach (DiffPiece piece in sideBySideDiff.OldText.Lines)
		{
			(string lineNum, string content, int newLineNumber) = ProcessLeftSide(piece, leftLineNumber);
			leftRows.Add((lineNum, content));
			leftLineNumber = newLineNumber;
		}

		// Process right side (new text)
		foreach (DiffPiece piece in sideBySideDiff.NewText.Lines)
		{
			(string lineNum, string content, int newLineNumber) = ProcessRightSide(piece, rightLineNumber);
			rightRows.Add((lineNum, content));
			rightLineNumber = newLineNumber;
		}

		// Now create table rows by combining both sides
		int maxRows = Math.Max(leftRows.Count, rightRows.Count);

		for (int i = 0; i < maxRows; i++)
		{
			string leftLineNum = i < leftRows.Count ? leftRows[i].leftLineNum : "";
			string leftContent = i < leftRows.Count ? leftRows[i].leftContent : "";
			string rightLineNum = i < rightRows.Count ? rightRows[i].rightLineNum : "";
			string rightContent = i < rightRows.Count ? rightRows[i].rightContent : "";

			table.AddRow(leftLineNum, leftContent, rightLineNum, rightContent);
		}
	}

	/// <summary>
	/// Processes the left side (original file) of the diff.
	/// </summary>
	/// <param name="leftLine">The left diff line.</param>
	/// <param name="lineNum1">Current line number for left side.</param>
	/// <returns>Tuple of line number display, content, and updated line number.</returns>
	private static (string lineNum, string content, int newLineNum) ProcessLeftSide(DiffPiece? leftLine, int lineNum1)
	{
		if (leftLine == null)
		{
			return ("", "", lineNum1);
		}

		return leftLine.Type switch
		{
			ChangeType.Unchanged or ChangeType.Modified or ChangeType.Deleted =>
				($"[dim]{lineNum1,3}[/]", FormatDiffLineEnhanced(leftLine, true), lineNum1 + 1),
			ChangeType.Inserted or ChangeType.Imaginary =>
				("[dim]   [/]", "[dim]...[/]", lineNum1),
			_ =>
				($"[dim]{lineNum1,3}[/]", FormatDiffLineEnhanced(leftLine, true), lineNum1 + 1)
		};
	}

	/// <summary>
	/// Processes the right side (modified file) of the diff.
	/// </summary>
	/// <param name="rightLine">The right diff line.</param>
	/// <param name="lineNum2">Current line number for right side.</param>
	/// <returns>Tuple of line number display, content, and updated line number.</returns>
	private static (string lineNum, string content, int newLineNum) ProcessRightSide(DiffPiece? rightLine, int lineNum2)
	{
		if (rightLine == null)
		{
			return ("", "", lineNum2);
		}

		return rightLine.Type switch
		{
			ChangeType.Unchanged or ChangeType.Modified or ChangeType.Inserted =>
				($"[dim]{lineNum2,3}[/]", FormatDiffLineEnhanced(rightLine, false), lineNum2 + 1),
			ChangeType.Deleted or ChangeType.Imaginary =>
				("[dim]   [/]", "[dim]...[/]", lineNum2),
			_ =>
				($"[dim]{lineNum2,3}[/]", FormatDiffLineEnhanced(rightLine, false), lineNum2 + 1)
		};
	}

	/// <summary>
	/// Displays the diff table with statistics in a panel.
	/// </summary>
	/// <param name="table">The populated table.</param>
	/// <param name="sideBySideDiff">The side-by-side diff model for statistics.</param>
	private static void DisplayDiffWithStatistics(Table table, SideBySideDiffModel sideBySideDiff)
	{
		string stats = CalculateDiffStatistics(sideBySideDiff);

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
	/// <param name="sideBySideDiff">The side-by-side diff model.</param>
	/// <returns>Formatted statistics string.</returns>
	private static string CalculateDiffStatistics(SideBySideDiffModel sideBySideDiff)
	{
		int additions = sideBySideDiff.NewText.Lines.Count(l => l.Type == ChangeType.Inserted);
		int deletions = sideBySideDiff.OldText.Lines.Count(l => l.Type == ChangeType.Deleted);
		int modifications = sideBySideDiff.OldText.Lines.Count(l => l.Type == ChangeType.Modified);

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
	/// Formats a diff line for enhanced side-by-side display with appropriate coloring and symbols.
	/// </summary>
	/// <param name="line">The diff line to format.</param>
	/// <param name="isLeftSide">Whether this is the left side (original) of the diff.</param>
	/// <returns>Formatted line with markup.</returns>
	private static string FormatDiffLineEnhanced(DiffPiece line, bool isLeftSide)
	{
		string content = line.Text?.EscapeMarkup() ?? "";

		return line.Type switch
		{
			ChangeType.Inserted when !isLeftSide => $"[green]+ {content}[/]",
			ChangeType.Deleted when isLeftSide => $"[red]- {content}[/]",
			ChangeType.Modified when isLeftSide => $"[yellow]~ {content}[/]",
			ChangeType.Modified when !isLeftSide => $"[yellow]~ {content}[/]",
			ChangeType.Unchanged => $"  {content}",
			ChangeType.Imaginary => "[dim]...[/]",
			_ => content
		};
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
