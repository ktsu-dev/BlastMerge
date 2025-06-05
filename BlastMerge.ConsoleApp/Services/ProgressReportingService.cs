// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
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
	public static void ReportMergeInitiation(string leftFile, string rightFile, bool hasExistingContent)
	{
		string leftLabel = FileDisplayService.GetRelativeDirectoryName(leftFile);
		string rightLabel = FileDisplayService.GetRelativeDirectoryName(rightFile);

		AnsiConsole.MarkupLine($"[yellow]🔀 Merging:[/]");
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
}
