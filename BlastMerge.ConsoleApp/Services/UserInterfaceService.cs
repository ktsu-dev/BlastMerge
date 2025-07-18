// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Models;
using Spectre.Console;

/// <summary>
/// Service for user interface and display operations.
/// </summary>
public class UserInterfaceService : IUserInterfaceService
{
	// Table column names
	private const string GroupColumnName = "Group";
	private const string FilesColumnName = "Files";
	private const string StatusColumnName = "Status";
	private const string FilenameColumnName = "Filename";
	private const string HashColumnName = "Hash";

	/// <summary>
	/// Shows a file group summary table.
	/// </summary>
	/// <param name="fileGroups">The file groups to display.</param>
	/// <param name="directory">The base directory for relative path calculation.</param>
	/// <param name="totalFiles">Total number of files found.</param>
	public void ShowFileGroupSummaryTable(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups, string directory, int totalFiles)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);
		ArgumentNullException.ThrowIfNull(directory);

		AnsiConsole.MarkupLine($"[green]Found {totalFiles} files in {fileGroups.Count} groups:[/]");
		AnsiConsole.WriteLine();

		// Sort fileGroups by the first filename in each group for better organization
		List<KeyValuePair<string, IReadOnlyCollection<string>>> sortedFileGroups = [.. fileGroups
			.OrderBy(g => Path.GetFileName(g.Value.FirstOrDefault() ?? string.Empty))];

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Blue)
			.AddColumn(GroupColumnName)
			.AddColumn(FilesColumnName)
			.AddColumn(StatusColumnName)
			.AddColumn(FilenameColumnName)
			.AddColumn(HashColumnName);

		foreach ((KeyValuePair<string, IReadOnlyCollection<string>> group, int groupIndex) in sortedFileGroups.Select((group, index) => (group, index + 1)))
		{
			string status = group.Value.Count > 1 ? "[yellow]Multiple identical copies[/]" : "[green]Unique[/]";

			// Get unique filenames in this group
			IEnumerable<string> uniqueFilenames = group.Value
				.Select(Path.GetFileName)
				.OfType<string>()
				.Where(f => !string.IsNullOrEmpty(f))
				.Distinct()
				.OrderBy(f => f);

			string filenamesDisplay = string.Join("; ", uniqueFilenames);
			if (filenamesDisplay.Length > 50)
			{
				filenamesDisplay = filenamesDisplay[..47] + "...";
			}

			// Show first 8 characters of hash
			string shortHash = group.Key.Length > 8 ? group.Key[..8] + "..." : group.Key;

			table.AddRow(
				$"[cyan]{groupIndex}[/]",
				$"[dim]{group.Value.Count}[/]",
				status,
				$"[dim]{filenamesDisplay}[/]",
				$"[dim]{shortHash}[/]");
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows the welcome screen.
	/// </summary>
	public void ShowWelcomeScreen() => MenuDisplayService.ShowWelcomeScreen();

	/// <summary>
	/// Shows the goodbye screen.
	/// </summary>
	public void ShowGoodbyeScreen() => MenuDisplayService.ShowGoodbyeScreen();

	/// <summary>
	/// Shows batch processing completion message.
	/// </summary>
	/// <param name="totalPatternsProcessed">Total patterns processed.</param>
	/// <param name="totalFilesFound">Total files found.</param>
	/// <param name="batchResult">The batch result containing merge details.</param>
	public void ShowBatchCompletion(int totalPatternsProcessed, int totalFilesFound, BatchResult? batchResult = null)
	{
		AnsiConsole.MarkupLine($"[green]Batch processing completed![/]");
		AnsiConsole.MarkupLine($"[dim]Processed {totalPatternsProcessed} patterns, found {totalFilesFound} total files.[/]");

		// Show detailed summary for all patterns processed
		if (batchResult != null)
		{
			ShowBatchDetailedSummary(batchResult);
		}
	}

	/// <summary>
	/// Shows batch processing header information.
	/// </summary>
	/// <param name="batch">The batch configuration.</param>
	/// <param name="directory">The directory being processed.</param>
	public void ShowBatchHeader(BatchConfiguration batch, string directory)
	{
		ArgumentNullException.ThrowIfNull(batch);
		ArgumentNullException.ThrowIfNull(directory);

		AnsiConsole.MarkupLine($"[cyan]Processing batch configuration '[yellow]{batch.Name}[/]' in '[yellow]{directory}[/]'[/]");
		AnsiConsole.WriteLine();

		AnsiConsole.MarkupLine($"[green]Found batch configuration: {batch.Name}[/]");
		if (!string.IsNullOrEmpty(batch.Description))
		{
			AnsiConsole.MarkupLine($"[dim]{batch.Description}[/]");
		}
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows error message.
	/// </summary>
	/// <param name="message">The error message to display.</param>
	public void ShowError(string message) => UIHelper.ShowError(message);

	/// <summary>
	/// Shows warning message.
	/// </summary>
	/// <param name="message">The warning message to display.</param>
	public void ShowWarning(string message) => UIHelper.ShowWarning(message);

	/// <summary>
	/// Shows informational message.
	/// </summary>
	/// <param name="message">The informational message to display.</param>
	public void ShowInfo(string message) => UIHelper.ShowInfo(message);

	/// <summary>
	/// Shows success message.
	/// </summary>
	/// <param name="message">The success message to display.</param>
	public void ShowSuccess(string message) => UIHelper.ShowSuccess(message);

	/// <summary>
	/// Shows detailed summary for all patterns in the batch, including those without merge operations.
	/// </summary>
	/// <param name="batchResult">The batch result containing pattern results.</param>
	private static void ShowBatchDetailedSummary(BatchResult batchResult)
	{
		if (batchResult.PatternResults.Count == 0)
		{
			return; // No patterns to show
		}

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[bold cyan]{OutputDisplay.BatchProcessingSummary}[/]");
		AnsiConsole.WriteLine();

		// Create summary table
		Table summaryTable = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Blue)
			.AddColumn("[bold]Pattern[/]")
			.AddColumn("[bold]Files Found[/]")
			.AddColumn("[bold]Unique Versions[/]")
			.AddColumn("[bold]Status[/]")
			.AddColumn("[bold]Result[/]");

		foreach (PatternResult patternResult in batchResult.PatternResults)
		{
			string status = patternResult.Success ? "[green]✓[/]" : "[red]✗[/]";
			string result = GetPatternResultDescription(patternResult);
			string displayName = GetDisplayName(patternResult);

			summaryTable.AddRow(
				$"[yellow]{displayName}[/]",
				patternResult.FilesFound.ToString(),
				patternResult.UniqueVersions.ToString(),
				status,
				result
			);
		}

		AnsiConsole.Write(summaryTable);

		// Show detailed merge summaries for patterns that had actual merge operations
		List<PatternResult> mergeResults = [.. batchResult.PatternResults
			.Where(pr => pr.MergeResult != null)];

		if (mergeResults.Count > 0)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[bold cyan]{OutputDisplay.DetailedMergeOperations}[/]");
			AnsiConsole.WriteLine();

			foreach (PatternResult patternResult in mergeResults)
			{
				if (patternResult.MergeResult != null)
				{
					AnsiConsole.MarkupLine($"[bold yellow]Pattern: {patternResult.Pattern}[/]");
					ProgressReportingService.ShowDetailedMergeSummary(patternResult.MergeResult);
				}
			}
		}
	}

	/// <summary>
	/// Gets a descriptive result for a pattern result.
	/// </summary>
	/// <param name="patternResult">The pattern result.</param>
	/// <returns>A formatted description of the result.</returns>
	private static string GetPatternResultDescription(PatternResult patternResult)
	{
		if (!patternResult.Success)
		{
			return $"[red]{patternResult.Message}[/]";
		}

		if (patternResult.FilesFound == 0)
		{
			return "[dim]No files[/]";
		}

		if (patternResult.FilesFound == 1)
		{
			return "[green]Single file[/]";
		}

		if (patternResult.UniqueVersions == 1)
		{
			return "[green]Identical[/]";
		}

		if (patternResult.MergeResult == null)
		{
			return "[yellow]Multiple versions[/]";
		}

		return patternResult.MergeResult.IsSuccessful
			? "[green]Merged[/]"
			: "[red]Failed[/]";
	}

	/// <summary>
	/// Gets the display name for a pattern result, showing filename with pattern in parentheses if it's a glob.
	/// </summary>
	/// <param name="patternResult">The pattern result.</param>
	/// <returns>A formatted display name.</returns>
	private static string GetDisplayName(PatternResult patternResult)
	{
		// If no filename is available, fall back to pattern
		if (string.IsNullOrEmpty(patternResult.FileName))
		{
			return patternResult.Pattern;
		}

		// If pattern equals filename, it's not a glob pattern
		if (patternResult.Pattern.Equals(patternResult.FileName, StringComparison.OrdinalIgnoreCase))
		{
			return patternResult.FileName;
		}

		// Check if pattern contains glob characters
		bool isGlobPattern = patternResult.Pattern.Contains('*') ||
			patternResult.Pattern.Contains('?') ||
			patternResult.Pattern.Contains('[') ||
			patternResult.Pattern.Contains('{');

		return isGlobPattern ? $"{patternResult.FileName} ({patternResult.Pattern})" : patternResult.FileName;
	}
}
