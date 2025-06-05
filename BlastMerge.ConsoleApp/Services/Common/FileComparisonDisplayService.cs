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
			string[] lines1 = File.ReadAllLines(file1);
			string[] lines2 = File.ReadAllLines(file2);
			SideBySideDiffModel sideBySideDiff = DiffPlexDiffer.GenerateSideBySideDiff(file1, file2);

			// Check if files are identical
			if (sideBySideDiff.OldText.Lines.All(l => l.Type == ChangeType.Unchanged) &&
				sideBySideDiff.NewText.Lines.All(l => l.Type == ChangeType.Unchanged))
			{
				UIHelper.ShowSuccess("Files are identical!");
				return;
			}

			// Get file names for headers
			string fileName1 = Path.GetFileName(file1);
			string fileName2 = Path.GetFileName(file2);

			// Create enhanced side-by-side table with line numbers
			Table table = new Table()
				.AddColumn(new TableColumn("[dim]#[/]").Width(4))           // Line numbers for file1
				.AddColumn(new TableColumn($"[bold cyan]{fileName1}[/]"))    // File1 content
				.AddColumn(new TableColumn("[dim]#[/]").Width(4))           // Line numbers for file2
				.AddColumn(new TableColumn($"[bold cyan]{fileName2}[/]"))    // File2 content
				.Border(TableBorder.Rounded)
				.Expand();

			// Process the diff and add rows
			int lineNum1 = 1;
			int lineNum2 = 1;
			int maxLines = Math.Max(sideBySideDiff.OldText.Lines.Count, sideBySideDiff.NewText.Lines.Count);

			for (int i = 0; i < maxLines; i++)
			{
				DiffPiece? leftLine = i < sideBySideDiff.OldText.Lines.Count ? sideBySideDiff.OldText.Lines[i] : null;
				DiffPiece? rightLine = i < sideBySideDiff.NewText.Lines.Count ? sideBySideDiff.NewText.Lines[i] : null;

				string leftLineNum = "";
				string rightLineNum = "";
				string leftContent = "";
				string rightContent = "";

				// Process left side (original file)
				if (leftLine != null)
				{
					switch (leftLine.Type)
					{
						case ChangeType.Unchanged:
						case ChangeType.Modified:
						case ChangeType.Deleted:
							leftLineNum = $"[dim]{lineNum1,3}[/]";
							leftContent = FormatDiffLineEnhanced(leftLine, true);
							lineNum1++;
							break;
						case ChangeType.Inserted:
							leftLineNum = "[dim]   [/]";
							leftContent = "[dim]...[/]";
							break;
						case ChangeType.Imaginary:
							leftLineNum = "[dim]   [/]";
							leftContent = "[dim]...[/]";
							break;
						default:
							leftLineNum = $"[dim]{lineNum1,3}[/]";
							leftContent = FormatDiffLineEnhanced(leftLine, true);
							lineNum1++;
							break;
					}
				}

				// Process right side (modified file)
				if (rightLine != null)
				{
					switch (rightLine.Type)
					{
						case ChangeType.Unchanged:
						case ChangeType.Modified:
						case ChangeType.Inserted:
							rightLineNum = $"[dim]{lineNum2,3}[/]";
							rightContent = FormatDiffLineEnhanced(rightLine, false);
							lineNum2++;
							break;
						case ChangeType.Deleted:
							rightLineNum = "[dim]   [/]";
							rightContent = "[dim]...[/]";
							break;
						case ChangeType.Imaginary:
							rightLineNum = "[dim]   [/]";
							rightContent = "[dim]...[/]";
							break;
						default:
							rightLineNum = $"[dim]{lineNum2,3}[/]";
							rightContent = FormatDiffLineEnhanced(rightLine, false);
							lineNum2++;
							break;
					}
				}

				table.AddRow(leftLineNum, leftContent, rightLineNum, rightContent);
			}

			// Show statistics
			int additions = sideBySideDiff.NewText.Lines.Count(l => l.Type == ChangeType.Inserted);
			int deletions = sideBySideDiff.OldText.Lines.Count(l => l.Type == ChangeType.Deleted);
			int modifications = sideBySideDiff.OldText.Lines.Count(l => l.Type == ChangeType.Modified);

			string stats = $"[green]+{additions}[/] [red]-{deletions}[/]";
			if (modifications > 0)
			{
				stats += $" [yellow]~{modifications}[/]";
			}

			Panel panel = new(table)
			{
				Header = new PanelHeader($"[bold]Side-by-Side Diff[/] ({stats})"),
				Border = BoxBorder.Rounded
			};

			AnsiConsole.Write(panel);

			// Show legend
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
