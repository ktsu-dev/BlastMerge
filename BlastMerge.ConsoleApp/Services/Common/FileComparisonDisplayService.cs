// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.Common;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
		ShowWhitespaceLegend();
		string diffFormat = PromptForDiffFormat();
		ExecuteDiffFormat(diffFormat, file1, file2);
	}

	/// <summary>
	/// Shows the whitespace legend panel.
	/// </summary>
	private static void ShowWhitespaceLegend()
	{
		AnsiConsole.Write(new Panel(WhitespaceVisualizer.CreateWhitespaceLegend())
		{
			Header = new PanelHeader("[dim]Whitespace Visualization Legend[/]"),
			Border = BoxBorder.Rounded
		});
	}

	/// <summary>
	/// Prompts the user to select a diff format.
	/// </summary>
	/// <returns>The selected diff format.</returns>
	private static string PromptForDiffFormat()
	{
		return AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Choose diff format:[/]")
				.AddChoices(
					MenuNames.DiffFormats.ChangeSummary,
					MenuNames.DiffFormats.GitStyleDiff,
					MenuNames.DiffFormats.SideBySideDiff,
					MenuNames.DiffFormats.SkipComparison
				));
	}

	/// <summary>
	/// Executes the selected diff format.
	/// </summary>
	/// <param name="diffFormat">The selected diff format.</param>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	private static void ExecuteDiffFormat(string diffFormat, string file1, string file2)
	{
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
		(string label1, string label2) = FileDisplayService.MakeDistinguishedPaths(file1, file2);

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

		if (diffResult.DiffBlocks.Count == 0)
		{
			ShowIdenticalFilesPreview(table, linesA, linesB);
			return;
		}

		ProcessDiffBlocks(table, linesA, linesB, diffResult.DiffBlocks);
	}

	/// <summary>
	/// Shows a preview of identical files.
	/// </summary>
	/// <param name="table">The table to populate.</param>
	/// <param name="linesA">Lines from file A.</param>
	/// <param name="linesB">Lines from file B.</param>
	private static void ShowIdenticalFilesPreview(Table table, string[] linesA, string[] linesB)
	{
		int linesToShow = Math.Min(10, Math.Min(linesA.Length, linesB.Length));
		for (int i = 0; i < linesToShow; i++)
		{
			string processedA = WhitespaceVisualizer.ProcessLineForMarkupDisplay(linesA[i]);
			string processedB = WhitespaceVisualizer.ProcessLineForMarkupDisplay(linesB[i]);
			table.AddRow((i + 1).ToString(), $"  {processedA}", (i + 1).ToString(), $"  {processedB}");
		}
	}

	/// <summary>
	/// Processes all diff blocks and adds them to the table.
	/// </summary>
	/// <param name="table">The table to populate.</param>
	/// <param name="linesA">Lines from file A.</param>
	/// <param name="linesB">Lines from file B.</param>
	/// <param name="diffBlocks">The diff blocks to process.</param>
	private static void ProcessDiffBlocks(Table table, string[] linesA, string[] linesB, IList<DiffPlex.Model.DiffBlock> diffBlocks)
	{
		for (int i = 0; i < diffBlocks.Count; i++)
		{
			DiffPlex.Model.DiffBlock block = diffBlocks[i];
			ProcessSingleDiffBlock(table, linesA, linesB, block);

			if (i < diffBlocks.Count - 1)
			{
				table.AddEmptyRow();
			}
		}
	}

	/// <summary>
	/// Processes a single diff block with context.
	/// </summary>
	/// <param name="table">The table to populate.</param>
	/// <param name="linesA">Lines from file A.</param>
	/// <param name="linesB">Lines from file B.</param>
	/// <param name="block">The diff block to process.</param>
	private static void ProcessSingleDiffBlock(Table table, string[] linesA, string[] linesB, DiffPlex.Model.DiffBlock block)
	{
		const int contextSize = 3;
		(int startLineA, int endLineA, int startLineB, int endLineB) = CalculateBlockRanges(block, linesA.Length, linesB.Length, contextSize);

		int minStart = Math.Min(startLineA, startLineB);
		int maxEnd = Math.Max(endLineA, endLineB);

		for (int offset = 0; offset <= (maxEnd - minStart); offset++)
		{
			int lineA = startLineA + offset;
			int lineB = startLineB + offset;

			(string leftLineNum, string leftContent) = ProcessLeftSide(lineA, linesA, block);
			(string rightLineNum, string rightContent) = ProcessRightSide(lineB, linesB, block);

			table.AddRow(leftLineNum, leftContent, rightLineNum, rightContent);
		}
	}

	/// <summary>
	/// Calculates the line ranges for a diff block with context.
	/// </summary>
	/// <param name="block">The diff block.</param>
	/// <param name="lengthA">Length of file A.</param>
	/// <param name="lengthB">Length of file B.</param>
	/// <param name="contextSize">Size of context to show.</param>
	/// <returns>Tuple of start and end line ranges for both files.</returns>
	private static (int startLineA, int endLineA, int startLineB, int endLineB) CalculateBlockRanges(
		DiffPlex.Model.DiffBlock block, int lengthA, int lengthB, int contextSize)
	{
		int startLineA = Math.Max(0, block.DeleteStartA - contextSize);
		int endLineA = Math.Min(lengthA - 1, block.DeleteStartA + block.DeleteCountA + contextSize - 1);
		int startLineB = Math.Max(0, block.InsertStartB - contextSize);
		int endLineB = Math.Min(lengthB - 1, block.InsertStartB + block.InsertCountB + contextSize - 1);

		return (startLineA, endLineA, startLineB, endLineB);
	}

	/// <summary>
	/// Processes the left side of a diff line.
	/// </summary>
	/// <param name="lineA">Line index in file A.</param>
	/// <param name="linesA">All lines from file A.</param>
	/// <param name="block">The diff block.</param>
	/// <returns>Tuple of line number and content for display.</returns>
	private static (string lineNum, string content) ProcessLeftSide(int lineA, string[] linesA, DiffPlex.Model.DiffBlock block)
	{
		if (lineA < 0 || lineA >= linesA.Length)
		{
			return ("", "");
		}

		string lineNum = (lineA + 1).ToString();
		string processedLine = WhitespaceVisualizer.ProcessLineForMarkupDisplay(linesA[lineA]);

		bool isInDeleteRange = lineA >= block.DeleteStartA && lineA < block.DeleteStartA + block.DeleteCountA;
		string content = isInDeleteRange ? $"[red on darkred]- {processedLine}[/]" : $"  {processedLine}";

		return (lineNum, content);
	}

	/// <summary>
	/// Processes the right side of a diff line.
	/// </summary>
	/// <param name="lineB">Line index in file B.</param>
	/// <param name="linesB">All lines from file B.</param>
	/// <param name="block">The diff block.</param>
	/// <returns>Tuple of line number and content for display.</returns>
	private static (string lineNum, string content) ProcessRightSide(int lineB, string[] linesB, DiffPlex.Model.DiffBlock block)
	{
		if (lineB < 0 || lineB >= linesB.Length)
		{
			return ("", "");
		}

		string lineNum = (lineB + 1).ToString();
		string processedLine = WhitespaceVisualizer.ProcessLineForMarkupDisplay(linesB[lineB]);

		bool isInInsertRange = lineB >= block.InsertStartB && lineB < block.InsertStartB + block.InsertCountB;
		string content = isInInsertRange ? $"[green on darkgreen]+ {processedLine}[/]" : $"  {processedLine}";

		return (lineNum, content);
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
			"[dim]  Unchanged lines[/]\n" +
			WhitespaceVisualizer.CreateWhitespaceLegend())
		{
			Header = new PanelHeader("[dim]Legend[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(legend);
	}

	/// <summary>
	/// Renders colored diff lines as a formatted string with character-level highlighting.
	/// </summary>
	/// <param name="coloredDiff">The colored diff lines to render.</param>
	/// <returns>Formatted diff string with markup.</returns>
	private static string RenderColoredDiff(IEnumerable<ColoredDiffLine> coloredDiff)
	{
		StringBuilder sb = new();
		List<ColoredDiffLine> lines = [.. coloredDiff];

		for (int i = 0; i < lines.Count; i++)
		{
			ColoredDiffLine line = lines[i];

			if (ShouldApplyCharacterLevelDiff(line, lines, i))
			{
				string formattedDiff = CharacterLevelDiffer.CreateInlineCharacterDiff(line.Content, lines[i + 1].Content);
				sb.AppendLine(formattedDiff);
				i++; // Skip the next line since we processed both
			}
			else
			{
				string formattedLine = FormatSingleDiffLine(line);
				sb.AppendLine(formattedLine);
			}
		}
		return sb.ToString();
	}

	/// <summary>
	/// Determines if character-level diffing should be applied to consecutive lines.
	/// </summary>
	/// <param name="line">Current line.</param>
	/// <param name="lines">All lines.</param>
	/// <param name="index">Current index.</param>
	/// <returns>True if character-level diffing should be applied.</returns>
	private static bool ShouldApplyCharacterLevelDiff(ColoredDiffLine line, List<ColoredDiffLine> lines, int index)
	{
		return line.Color == DiffColor.Deletion &&
			index + 1 < lines.Count &&
			lines[index + 1].Color == DiffColor.Addition &&
			CharacterLevelDiffer.AreLinesSimilar(line.Content, lines[index + 1].Content);
	}

	/// <summary>
	/// Formats a single diff line with appropriate markup.
	/// </summary>
	/// <param name="line">The line to format.</param>
	/// <returns>Formatted line string.</returns>
	private static string FormatSingleDiffLine(ColoredDiffLine line)
	{
		string colorMarkup = GetColorMarkup(line.Color);
		string endMarkup = string.IsNullOrEmpty(colorMarkup) ? "" : "[/]";

		string processedContent = line.Color is DiffColor.Addition or DiffColor.Deletion
			? WhitespaceVisualizer.ProcessLineForMarkupDisplay(line.Content)
			: line.Content;

		return $"{colorMarkup}{processedContent}{endMarkup}";
	}

	/// <summary>
	/// Gets the appropriate color markup for a diff color.
	/// </summary>
	/// <param name="color">The diff color.</param>
	/// <returns>Markup string for the color.</returns>
	private static string GetColorMarkup(DiffColor color) => color switch
	{
		DiffColor.Addition => "[green]",
		DiffColor.Deletion => "[red]",
		DiffColor.ChunkHeader => "[yellow]",
		DiffColor.FileHeader => "[cyan]",
		DiffColor.Default => "",
		_ => ""
	};

	/// <summary>
	/// Creates a side-by-side comparison display for a DiffPlex block with proper alignment
	/// </summary>
	/// <param name="lines1">Lines from version 1</param>
	/// <param name="lines2">Lines from version 2</param>
	/// <param name="diffBlock">The DiffPlex diff block</param>
	/// <param name="context">Block context with before/after lines</param>
	/// <param name="leftLabel">Label for the left side</param>
	/// <param name="rightLabel">Label for the right side</param>
	/// <returns>A formatted table showing the side-by-side diff</returns>
	public static Table CreateSideBySideDiffTable(string[] lines1, string[] lines2,
		DiffPlex.Model.DiffBlock diffBlock, BlockContext context,
		string leftLabel, string rightLabel)
	{
		ArgumentNullException.ThrowIfNull(lines1);
		ArgumentNullException.ThrowIfNull(lines2);
		ArgumentNullException.ThrowIfNull(diffBlock);
		ArgumentNullException.ThrowIfNull(context);

		Table table = new Table()
			.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
			.AddColumn(new TableColumn($"[bold]{leftLabel}[/]").Width(50))
			.AddColumn(new TableColumn($"[bold]{rightLabel}[/]").Width(50));

		// Add context before
		AddContextLines(table, context.ContextBefore1, context.ContextBefore2,
			GetContextStartLine(diffBlock, context.ContextBefore1.Count));

		// Add the actual diff block content
		AddDiffBlockContent(table, lines1, lines2, diffBlock);

		// Add context after
		AddContextLines(table, context.ContextAfter1, context.ContextAfter2,
			GetContextAfterStartLine(diffBlock));

		return table;
	}

	/// <summary>
	/// Adds context lines to the diff table
	/// </summary>
	private static void AddContextLines(Table table, ReadOnlyCollection<string> contextLeft,
		ReadOnlyCollection<string> contextRight, int startLineNumber)
	{
		int maxLines = Math.Max(contextLeft.Count, contextRight.Count);

		for (int i = 0; i < maxLines; i++)
		{
			string leftLine = i < contextLeft.Count ? contextLeft[i] : "";
			string rightLine = i < contextRight.Count ? contextRight[i] : "";

			// Apply whitespace visualization to context lines
			string leftProcessed = WhitespaceVisualizer.ProcessLineForMarkupDisplay(leftLine);
			string rightProcessed = WhitespaceVisualizer.ProcessLineForMarkupDisplay(rightLine);

			table.AddRow(
				$"[dim]{startLineNumber + i}[/]",
				$"[dim]{leftProcessed}[/]",
				$"[dim]{rightProcessed}[/]"
			);
		}
	}

	/// <summary>
	/// Adds the main diff block content to the table with character-level highlighting
	/// </summary>
	private static void AddDiffBlockContent(Table table, string[] lines1, string[] lines2,
		DiffPlex.Model.DiffBlock diffBlock)
	{
		int maxLines = Math.Max(diffBlock.DeleteCountA, diffBlock.InsertCountB);

		for (int i = 0; i < maxLines; i++)
		{
			(string? originalLeftLine, string? originalRightLine, string lineNumberDisplay) = GetLineContents(lines1, lines2, diffBlock, i);
			(string leftLine, string rightLine) = FormatDiffLines(originalLeftLine, originalRightLine);
			table.AddRow(lineNumberDisplay, leftLine, rightLine);
		}
	}

	/// <summary>
	/// Gets the line contents and line number display for a diff block row.
	/// </summary>
	/// <param name="lines1">Lines from file 1.</param>
	/// <param name="lines2">Lines from file 2.</param>
	/// <param name="diffBlock">The diff block.</param>
	/// <param name="index">The current row index.</param>
	/// <returns>Tuple of left line, right line, and line number display.</returns>
	private static (string? originalLeftLine, string? originalRightLine, string lineNumberDisplay) GetLineContents(
		string[] lines1, string[] lines2, DiffPlex.Model.DiffBlock diffBlock, int index)
	{
		string? originalLeftLine = null;
		string? originalRightLine = null;
		string lineNumberDisplay = "";

		// Get the actual line contents first
		if (index < diffBlock.DeleteCountA && diffBlock.DeleteStartA + index < lines1.Length)
		{
			originalLeftLine = lines1[diffBlock.DeleteStartA + index];
			lineNumberDisplay = $"{diffBlock.DeleteStartA + index + 1}";
		}

		if (index < diffBlock.InsertCountB && diffBlock.InsertStartB + index < lines2.Length)
		{
			originalRightLine = lines2[diffBlock.InsertStartB + index];
			if (string.IsNullOrEmpty(lineNumberDisplay))
			{
				lineNumberDisplay = $"{diffBlock.InsertStartB + index + 1}";
			}
		}

		return (originalLeftLine, originalRightLine, lineNumberDisplay);
	}

	/// <summary>
	/// Formats diff lines with appropriate highlighting.
	/// </summary>
	/// <param name="originalLeftLine">Original left line content.</param>
	/// <param name="originalRightLine">Original right line content.</param>
	/// <returns>Tuple of formatted left and right lines.</returns>
	private static (string leftLine, string rightLine) FormatDiffLines(string? originalLeftLine, string? originalRightLine)
	{
		string leftLine = "";
		string rightLine = "";

		// Apply character-level diffing if both lines exist and are similar
		if (originalLeftLine != null && originalRightLine != null &&
			CharacterLevelDiffer.AreLinesSimilar(originalLeftLine, originalRightLine))
		{
			(string leftHighlighted, string rightHighlighted) = CharacterLevelDiffer.CreateSideBySideCharacterDiff(originalLeftLine, originalRightLine);
			leftLine = $"[red]- {leftHighlighted}[/]";
			rightLine = $"[green]+ {rightHighlighted}[/]";
		}
		else
		{
			leftLine = FormatSingleSideLine(originalLeftLine, "[red]- ", "[/]");
			rightLine = FormatSingleSideLine(originalRightLine, "[green]+ ", "[/]");
		}

		return (leftLine, rightLine);
	}

	/// <summary>
	/// Formats a single side line with markup.
	/// </summary>
	/// <param name="originalLine">Original line content.</param>
	/// <param name="prefix">Markup prefix.</param>
	/// <param name="suffix">Markup suffix.</param>
	/// <returns>Formatted line.</returns>
	private static string FormatSingleSideLine(string? originalLine, string prefix, string suffix)
	{
		if (originalLine == null)
		{
			return "";
		}

		string processedLine = WhitespaceVisualizer.ProcessLineForMarkupDisplay(originalLine);
		return $"{prefix}{processedLine}{suffix}";
	}

	/// <summary>
	/// Gets the starting line number for context before the diff block
	/// </summary>
	private static int GetContextStartLine(DiffPlex.Model.DiffBlock diffBlock, int contextCount) => Math.Max(1, diffBlock.DeleteStartA - contextCount + 1);

	/// <summary>
	/// Gets the starting line number for context after the diff block
	/// </summary>
	private static int GetContextAfterStartLine(DiffPlex.Model.DiffBlock diffBlock) => diffBlock.DeleteStartA + diffBlock.DeleteCountA + 1;

	/// <summary>
	/// Shows a diff block with its context in a side-by-side format
	/// </summary>
	/// <param name="lines1">Lines from version 1</param>
	/// <param name="lines2">Lines from version 2</param>
	/// <param name="diffBlock">The DiffPlex diff block</param>
	/// <param name="context">Block context</param>
	/// <param name="leftLabel">Label for left side</param>
	/// <param name="rightLabel">Label for right side</param>
	public static void ShowDiffBlock(string[] lines1, string[] lines2,
		DiffPlex.Model.DiffBlock diffBlock, BlockContext context,
		string leftLabel, string rightLabel)
	{
		Table table = CreateSideBySideDiffTable(lines1, lines2, diffBlock, context, leftLabel, rightLabel);
		AnsiConsole.Write(table);
	}

	/// <summary>
	/// Gets statistics about the diff block
	/// </summary>
	/// <param name="diffBlock">The DiffPlex diff block</param>
	/// <returns>A tuple containing (deletions, insertions) counts</returns>
	public static (int deletions, int insertions) GetDiffBlockStatistics(DiffPlex.Model.DiffBlock diffBlock)
	{
		ArgumentNullException.ThrowIfNull(diffBlock);
		return (diffBlock.DeleteCountA, diffBlock.InsertCountB);
	}
}
