// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.ObjectModel;
using System.IO;
using DiffPlex.DiffBuilder.Model;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Service for displaying file information and comparisons in the console.
/// </summary>
public static class FileDisplayService
{
	/// <summary>
	/// Shows a detailed list of files grouped by hash.
	/// </summary>
	/// <param name="fileGroups">The file groups to display.</param>
	public static void ShowDetailedFileList(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Detailed File List[/]");
		AnsiConsole.WriteLine();

		Tree tree = new("[bold]File Groups[/]");

		int groupIndex = 1;
		foreach (KeyValuePair<string, IReadOnlyCollection<string>> group in fileGroups)
		{
			string groupStatus = group.Value.Count > 1 ? "[yellow]Multiple versions[/]" : "[green]Unique[/]";
			TreeNode groupNode = tree.AddNode($"[cyan]Group {groupIndex}[/] - {groupStatus} ({group.Value.Count} files)");
			groupNode.AddNode($"[dim]Hash: {group.Key[..Math.Min(8, group.Key.Length)]}...[/]");

			foreach (string filePath in group.Value)
			{
				try
				{
					FileInfo fileInfo = new(filePath);
					groupNode.AddNode($"[green]{filePath}[/] [dim]({fileInfo.Length:N0} bytes, {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss})[/]");
				}
				catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or FileNotFoundException or PathTooLongException or ArgumentException or IOException)
				{
					groupNode.AddNode($"[green]{filePath}[/] [dim]({ex.GetType().Name})[/]");
				}
			}

			groupIndex++;
		}

		AnsiConsole.Write(tree);
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
		Console.ReadKey();
	}

	/// <summary>
	/// Shows differences between file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to show differences for.</param>
	public static void ShowDifferences(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);
		// Convert to FileGroup objects for easier handling
		List<FileGroup> groups = [.. fileGroups.Select(g => new FileGroup(g.Value))];

		// Filter to groups with multiple files
		List<FileGroup> groupsWithMultipleFiles = [.. groups.Where(g => g.FilePaths.Count > 1)];

		if (groupsWithMultipleFiles.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No groups with multiple files to compare.[/]");
			return;
		}

		foreach (FileGroup group in groupsWithMultipleFiles)
		{
			AnsiConsole.WriteLine();
			Rule rule = new($"[bold]Group with {group.FilePaths.Count} files[/]")
			{
				Style = Style.Parse("blue")
			};
			AnsiConsole.Write(rule);

			// Show all files in the group
			foreach (string file in group.FilePaths)
			{
				AnsiConsole.MarkupLine($"[dim]📁 {file}[/]");
			}

			// Compare first two files in the group
			if (group.FilePaths.Count >= 2)
			{
				string file1 = group.FilePaths.First();
				string file2 = group.FilePaths.Skip(1).First();

				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine($"[yellow]Comparing first two files:[/]");

				string diffFormat = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("[cyan]Choose diff format:[/]")
						.AddChoices([
							"📊 Change Summary (Added/Removed lines only)",
							"🔧 Git-style Diff (Full context)",
							"🎨 Side-by-Side Diff (Rich formatting)",
							"⏭️ Skip this group"
						]));

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

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[dim]Press any key to continue to next group...[/]");
			Console.ReadKey();
		}
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
	public static void ShowGitStyleDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);
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

			// Use relative directory names for headers
			string header1 = GetRelativeDirectoryName(file1);
			string header2 = GetRelativeDirectoryName(file2);

			Table table = new Table()
				.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
				.AddColumn(new TableColumn($"[bold]{header1}[/]").Width(50))
				.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
				.AddColumn(new TableColumn($"[bold]{header2}[/]").Width(50))
				.Border(TableBorder.Rounded)
				.Expand();

			int maxLines = Math.Max(sideBySideDiff.OldText.Lines.Count, sideBySideDiff.NewText.Lines.Count);
			int displayedLines = 0;
			const int maxDisplayLines = 100; // Limit for TUI performance

			for (int i = 0; i < maxLines && displayedLines < maxDisplayLines; i++)
			{
				DiffPiece? oldLine = i < sideBySideDiff.OldText.Lines.Count ? sideBySideDiff.OldText.Lines[i] : null;
				DiffPiece? newLine = i < sideBySideDiff.NewText.Lines.Count ? sideBySideDiff.NewText.Lines[i] : null;

				string? lineNum1 = "";
				string content1 = "";
				string? lineNum2 = "";
				string content2 = "";

				if (oldLine != null)
				{
					lineNum1 = (oldLine.Position + 1).ToString();
					content1 = FormatDiffLine(oldLine);
				}

				if (newLine != null)
				{
					lineNum2 = (newLine.Position + 1).ToString();
					content2 = FormatDiffLine(newLine);
				}

				table.AddRow(lineNum1 ?? "", content1 ?? "", lineNum2 ?? "", content2 ?? "");
				displayedLines++;
			}

			if (maxLines > maxDisplayLines)
			{
				table.AddRow("[dim]...[/]", "[dim]...[/]", "[dim]...[/]", $"[dim]... and {maxLines - maxDisplayLines} more lines[/]");
			}

			AnsiConsole.Write(table);
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
	/// Formats a diff line with appropriate color and prefix.
	/// </summary>
	/// <param name="line">The diff line to format.</param>
	/// <returns>Formatted line with color markup.</returns>
	private static string FormatDiffLine(DiffPiece line)
	{
		string text = line.Text?.EscapeMarkup() ?? "";

		return line.Type switch
		{
			ChangeType.Deleted => $"[red on darkred]- {text}[/]",
			ChangeType.Inserted => $"[green on darkgreen]+ {text}[/]",
			ChangeType.Modified => $"[yellow on darkorange]~ {text}[/]",
			ChangeType.Imaginary => "[dim]   [/]",
			ChangeType.Unchanged => $"  {text}",
			_ => $"  {text}"
		};
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

	/// <summary>
	/// Gets the relative directory name from a file path.
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <returns>The relative directory name.</returns>
	private static string GetRelativeDirectoryName(string filePath)
	{
		try
		{
			string? directory = Path.GetDirectoryName(filePath);
			if (string.IsNullOrEmpty(directory))
			{
				return Path.GetFileName(filePath);
			}

			string[] parts = directory.Split(Path.DirectorySeparatorChar);
			if (parts.Length >= 2)
			{
				// Return last two directory parts + filename
				return Path.Combine(parts[^2], parts[^1], Path.GetFileName(filePath));
			}
			else if (parts.Length == 1)
			{
				// Return last directory part + filename
				return Path.Combine(parts[^1], Path.GetFileName(filePath));
			}
			else
			{
				return Path.GetFileName(filePath);
			}
		}
		catch (ArgumentException)
		{
			return Path.GetFileName(filePath);
		}
	}
}
