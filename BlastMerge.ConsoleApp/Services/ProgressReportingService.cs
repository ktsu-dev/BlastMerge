// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Core.Models;
using Spectre.Console;

/// <summary>
/// Service for handling progress reporting and status updates in the console application.
/// </summary>
public static class ProgressReportingService
{
	/// <summary>
	/// Reports merge status to the console.
	/// </summary>
	/// <param name="status">Current merge session status.</param>
	public static void ReportMergeStatus(MergeSessionStatus status)
	{
		ArgumentNullException.ThrowIfNull(status);
		AnsiConsole.MarkupLine($"[yellow]Merge {status.CurrentIteration}: {status.MostSimilarPair?.FilePath1} <-> {status.MostSimilarPair?.FilePath2}[/]");
		AnsiConsole.MarkupLine($"[dim]Similarity: {status.MostSimilarPair?.SimilarityScore:F1} | Remaining files: {status.RemainingFilesCount}[/]");
	}

	/// <summary>
	/// Reports merge completion result.
	/// </summary>
	/// <param name="result">The completion result.</param>
	public static void ReportCompletionResult(MergeCompletionResult result)
	{
		ArgumentNullException.ThrowIfNull(result);
		if (result.IsSuccessful)
		{
			AnsiConsole.MarkupLine($"[green]Merge completed successfully. Final file: {result.OriginalFileName}[/]");
		}
		else
		{
			AnsiConsole.MarkupLine($"[red]Merge failed or was cancelled: {result.OriginalFileName}[/]");
		}
	}

	/// <summary>
	/// Displays a detailed summary of all merge operations performed.
	/// </summary>
	/// <param name="result">The merge completion result containing operation details.</param>
	public static void ShowDetailedMergeSummary(MergeCompletionResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[bold blue]═══════════════════════════════════════════════════════════════════════════════════════[/]");
		AnsiConsole.MarkupLine($"[bold blue]                           {OutputDisplay.MergeOperationsSummary}                                   [/]");
		AnsiConsole.MarkupLine("[bold blue]═══════════════════════════════════════════════════════════════════════════════════════[/]");
		AnsiConsole.WriteLine();

		// Overall statistics
		Table overallTable = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Blue)
			.AddColumn("[bold]Metric[/]")
			.AddColumn("[bold]Value[/]");

		overallTable.AddRow("Initial File Groups", result.InitialFileGroups.ToString());
		overallTable.AddRow("Total Files Processed", result.TotalFilesMerged.ToString());
		overallTable.AddRow("Merge Operations", result.TotalMergeOperations.ToString());
		overallTable.AddRow("Final Result", result.IsSuccessful ? "[green]Success[/]" : "[red]Failed/Cancelled[/]");
		if (result.FinalLineCount > 0)
		{
			overallTable.AddRow("Final Line Count", result.FinalLineCount.ToString());
		}

		AnsiConsole.Write(overallTable);

		// Individual operations (only show if there were actual operations)
		if (result.Operations.Count > 0)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[bold yellow]📋 Individual Operations:[/]");
			AnsiConsole.WriteLine();

			Table operationsTable = new Table()
				.Border(TableBorder.Rounded)
				.BorderColor(Color.Yellow)
				.AddColumn("[bold]Op #[/]")
				.AddColumn("[bold]Files Merged[/]")
				.AddColumn("[bold]Similarity[/]")
				.AddColumn("[bold]Files Affected[/]")
				.AddColumn("[bold]Conflicts[/]")
				.AddColumn("[bold]Lines[/]");

			foreach (MergeOperationSummary operation in result.Operations)
			{
				// Create short labels for the file paths
				(string label1, string label2) = FileDisplayService.MakeDistinguishedPaths(
					operation.FilePath1, operation.FilePath2);

				string mergedFiles = $"{label1} ↔ {label2}";
				string similarity = $"{operation.SimilarityScore:F1}%";
				string conflicts = operation.ConflictsResolved > 0
					? $"[red]{operation.ConflictsResolved}[/]"
					: "[green]0[/]";

				operationsTable.AddRow(
					operation.OperationNumber.ToString(),
					mergedFiles,
					similarity,
					operation.FilesAffected.ToString(),
					conflicts,
					operation.MergedLineCount.ToString()
				);
			}

			AnsiConsole.Write(operationsTable);
		}
		else
		{
			// Show message when no operations were needed
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[bold green]📝 No merge operations required - all files were already synchronized.[/]");
		}

		// Final summary message
		AnsiConsole.WriteLine();
		if (result.TotalMergeOperations > 0)
		{
			if (result.IsSuccessful)
			{
				AnsiConsole.MarkupLine($"[green]✅ Successfully merged {result.InitialFileGroups} file groups into a single result through {result.TotalMergeOperations} operations.[/]");
			}
			else
			{
				AnsiConsole.MarkupLine($"[yellow]⚠️ Merge process was {result.OriginalFileName} after {result.TotalMergeOperations} operations.[/]");
			}
		}
		else
		{
			AnsiConsole.MarkupLine($"[green]✅ All {result.TotalFilesMerged} files were already identical - no merging required.[/]");
		}

		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Reports successful merge step.
	/// </summary>
	public static void ReportMergeStepSuccess()
	{
		AnsiConsole.MarkupLine($"[green]✅ Merged successfully! Versions reduced by 1.[/]");
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Reports merge initiation.
	/// </summary>
	/// <param name="leftFile">The left file path.</param>
	/// <param name="rightFile">The right file path.</param>
	/// <param name="hasExistingContent"></param>
	/// <param name="deletions">Number of deletions (-) from the diff.</param>
	/// <param name="insertions">Number of insertions (+) from the diff.</param>
	public static void ReportMergeInitiation(string leftFile, string rightFile, bool hasExistingContent, int deletions = 0, int insertions = 0)
	{
		(string leftLabel, string rightLabel) = FileDisplayService.MakeDistinguishedPaths(leftFile, rightFile);

		// Create the header with diff stats and tug of war visualization on same line
		string diffStats = CreateDiffStatsDisplay(deletions, insertions);
		string tugOfWar = "";

		// Add tug of war visualization to the same line if there are changes
		if (deletions > 0 || insertions > 0)
		{
			tugOfWar = " " + CreateTugOfWarVisualization(deletions, insertions);
		}

		AnsiConsole.MarkupLine($"[yellow]🔀 Merging{diffStats}:{tugOfWar}[/]");

		if (hasExistingContent)
		{
			AnsiConsole.MarkupLine($"[dim]  📋 <existing merged content> → {leftLabel}[/]");
		}
		else
		{
			AnsiConsole.MarkupLine($"[dim]  📁 {leftLabel}[/]");
		}
		AnsiConsole.MarkupLine($"[dim]  📁 {rightLabel}[/]");
		AnsiConsole.MarkupLine($"[green]  ➡️  Result will replace both files[/]");

		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Creates a diff stats display string.
	/// </summary>
	/// <param name="deletions">Number of deletions.</param>
	/// <param name="insertions">Number of insertions.</param>
	/// <returns>Formatted diff stats string.</returns>
	private static string CreateDiffStatsDisplay(int deletions, int insertions)
	{
		if (deletions == 0 && insertions == 0)
		{
			return "";
		}

		List<string> parts = [];

		if (deletions > 0)
		{
			parts.Add($"[red]−{deletions}[/]");
		}

		if (insertions > 0)
		{
			parts.Add($"[green]+{insertions}[/]");
		}

		return $" ({string.Join(" ", parts)})";
	}

	/// <summary>
	/// Creates a git-style "tug of war" visualization showing relative proportion of additions vs deletions.
	/// Uses one character per change unless that would make it too long, in which case scales proportionally.
	/// </summary>
	/// <param name="deletions">Number of deletions.</param>
	/// <param name="insertions">Number of insertions.</param>
	/// <returns>Git-style visualization string.</returns>
	private static string CreateTugOfWarVisualization(int deletions, int insertions)
	{
		int total = deletions + insertions;

		if (total == 0)
		{
			return "";
		}

		int maxWidth = GetMaxTugOfWarWidth();
		(int deleteChars, int insertChars) = CalculateCharacterCounts(deletions, insertions, maxWidth);

		return FormatVisualizationParts(deletions, insertions, deleteChars, insertChars);
	}

	/// <summary>
	/// Calculates the number of characters to use for delete and insert visualization.
	/// </summary>
	private static (int deleteChars, int insertChars) CalculateCharacterCounts(int deletions, int insertions, int maxWidth)
	{
		int total = deletions + insertions;
		int deleteChars = deletions;
		int insertChars = insertions;

		if (total <= maxWidth)
		{
			return (deleteChars, insertChars);
		}

		// Scale proportionally
		double ratio = (double)maxWidth / total;
		deleteChars = Math.Max(deletions > 0 ? 1 : 0, (int)(deletions * ratio));
		insertChars = Math.Max(insertions > 0 ? 1 : 0, (int)(insertions * ratio));

		return AdjustCharacterCounts(deleteChars, insertChars, maxWidth);
	}

	/// <summary>
	/// Adjusts character counts to ensure they don't exceed the maximum width.
	/// </summary>
	private static (int deleteChars, int insertChars) AdjustCharacterCounts(int deleteChars, int insertChars, int maxWidth)
	{
		if (deleteChars + insertChars <= maxWidth)
		{
			return (deleteChars, insertChars);
		}

		// Both have changes - proportionally reduce both
		if (deleteChars > 0 && insertChars > 0)
		{
			double adjustRatio = (double)maxWidth / (deleteChars + insertChars);
			deleteChars = Math.Max(1, (int)(deleteChars * adjustRatio));
			insertChars = Math.Max(1, (int)(insertChars * adjustRatio));
		}
		// Only one has changes - cap it
		else if (deleteChars > maxWidth)
		{
			deleteChars = maxWidth;
		}
		else if (insertChars > maxWidth)
		{
			insertChars = maxWidth;
		}

		return (deleteChars, insertChars);
	}

	/// <summary>
	/// Formats the visualization parts into the final string.
	/// </summary>
	private static string FormatVisualizationParts(int deletions, int insertions, int deleteChars, int insertChars)
	{
		string deletePart = deletions > 0 ? $"[red]{new string('−', deleteChars)}[/]" : "";
		string insertPart = insertions > 0 ? $"[green]{new string('+', insertChars)}[/]" : "";
		return $"{deletePart}{insertPart}";
	}

	/// <summary>
	/// Gets the maximum width available for the tug of war visualization.
	/// </summary>
	/// <returns>Maximum width in characters.</returns>
	private static int GetMaxTugOfWarWidth()
	{
		try
		{
			// Get console width and leave buffer for the rest of the merge line
			// Format: "🔀 Merging (-X +Y): [tugofwar]"
			// Reserve about 50 characters for the prefix and some margin
			int consoleWidth = Console.WindowWidth;
			int reservedWidth = 50; // Buffer for "🔀 Merging (-999 +999): " plus margin
			int maxWidth = Math.Max(10, consoleWidth - reservedWidth); // Minimum of 10 characters

			// Cap at a reasonable maximum to prevent extremely long visualizations
			return Math.Min(maxWidth, 100);
		}
		catch (IOException)
		{
			// If we can't get console width (e.g., redirected output), use a conservative default
			return 30;
		}
		catch (ArgumentOutOfRangeException)
		{
			// Console.WindowWidth can throw this exception in some scenarios
			return 30;
		}
	}
}
