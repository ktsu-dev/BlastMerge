// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;

/// <summary>
/// Console-specific implementation of the application service that adds UI functionality.
/// </summary>
public class ConsoleApplicationService : ApplicationService
{
	/// <summary>
	/// Processes files in a directory with a specified filename pattern.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public override void ProcessFiles(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName);
		// Process files as needed
	}

	/// <summary>
	/// Processes a batch configuration in a specified directory.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batchName">The name of the batch configuration.</param>
	public override void ProcessBatch(string directory, string batchName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(batchName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		// Process batch configuration
	}

	/// <summary>
	/// Compares files in a directory and returns file groups.
	/// </summary>
	/// <param name="directory">The directory containing files to compare.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <returns>Dictionary of file groups organized by hash.</returns>
	public override IReadOnlyDictionary<string, IReadOnlyCollection<string>> CompareFiles(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName);
		return (IReadOnlyDictionary<string, IReadOnlyCollection<string>>)FileDiffer.GroupFilesByHash(filePaths);
	}

	/// <summary>
	/// Runs the iterative merge process on files in a directory.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public override void RunIterativeMerge(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		// Prepare file groups for merging
		IReadOnlyCollection<FileGroup>? fileGroups = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(directory, fileName);

		if (fileGroups == null)
		{
			Console.WriteLine("No files found or insufficient unique versions to merge.");
			return;
		}

		// Start iterative merge process
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			PerformMergeCallback,
			ReportMergeStatus,
			ContinueMergeCallback);

		// Handle result
		if (result.IsSuccessful)
		{
			Console.WriteLine($"Merge completed successfully. Final file: {result.OriginalFileName}");
		}
		else
		{
			Console.WriteLine($"Merge failed or was cancelled: {result.OriginalFileName}");
		}
	}

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	public override void ListBatches()
	{
		IReadOnlyCollection<string> batchNames = BatchManager.ListBatches();
		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();

		Console.WriteLine("Available batch configurations:");

		if (batchNames.Count == 0)
		{
			Console.WriteLine("  No batch configurations found.");
			Console.WriteLine("  Default configurations can be created automatically.");
			return;
		}

		foreach (BatchConfiguration batch in allBatches)
		{
			Console.WriteLine($"  - {batch.Name}");
			if (!string.IsNullOrEmpty(batch.Description))
			{
				Console.WriteLine($"    {batch.Description}");
			}
			Console.WriteLine($"    Patterns: {batch.FilePatterns.Count}");
		}
	}

	/// <summary>
	/// Starts the interactive mode for user input with console UI.
	/// </summary>
	public override void StartInteractiveMode()
	{
		Console.WriteLine("BlastMerge - Cross-Repository File Synchronization Tool");
		Console.WriteLine("Interactive mode started. Use Ctrl+C to exit.");

		while (true)
		{
			try
			{
				string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
				if (string.IsNullOrWhiteSpace(directory))
				{
					break;
				}

				string fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
				if (string.IsNullOrWhiteSpace(fileName))
				{
					break;
				}

				RunIterativeMergeWithConsoleOutput(directory, fileName);
			}
			catch (DirectoryNotFoundException ex)
			{
				Console.WriteLine($"Directory not found: {ex.Message}");
			}
			catch (UnauthorizedAccessException ex)
			{
				Console.WriteLine($"Access denied: {ex.Message}");
			}
			catch (IOException ex)
			{
				Console.WriteLine($"File I/O error: {ex.Message}");
			}
			catch (ArgumentException ex)
			{
				Console.WriteLine($"Invalid input: {ex.Message}");
			}
			catch (InvalidOperationException ex)
			{
				Console.WriteLine($"Operation error: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Runs iterative merge with console output and user interaction.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	private void RunIterativeMergeWithConsoleOutput(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		// Prepare file groups for merging
		IReadOnlyCollection<FileGroup>? fileGroups = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(directory, fileName);

		if (fileGroups == null)
		{
			Console.WriteLine("No files found or insufficient unique versions to merge.");
			return;
		}

		// Start iterative merge process with console callbacks
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			ConsoleMergeCallback,
			ConsoleStatusCallback,
			ConsoleContinueCallback);

		// Handle result
		if (result.IsSuccessful)
		{
			Console.WriteLine($"Merge completed successfully. Final file: {result.OriginalFileName}");
		}
		else
		{
			Console.WriteLine($"Merge failed or was cancelled: {result.OriginalFileName}");
		}
	}

	/// <summary>
	/// Console-specific callback to perform merge operation between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	/// <param name="existingContent">Existing merged content.</param>
	/// <returns>Merge result or null if cancelled.</returns>
	private MergeResult? ConsoleMergeCallback(string file1, string file2, string? existingContent)
	{
		// Console app performs automatic merge with user notification
		Console.WriteLine($"Merging: {file1} <-> {file2}");
		return IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			file1,
			file2,
			existingContent,
			(block, context, index) => BlockChoice.UseVersion1); // Default to version 1 choice
	}

	/// <summary>
	/// Console-specific callback to report merge status.
	/// </summary>
	/// <param name="status">Current merge session status.</param>
	private void ConsoleStatusCallback(MergeSessionStatus status)
	{
		Console.WriteLine($"Merge {status.CurrentIteration}: {status.MostSimilarPair?.FilePath1} <-> {status.MostSimilarPair?.FilePath2}");
		Console.WriteLine($"Similarity: {status.MostSimilarPair?.SimilarityScore:F1} | Remaining files: {status.RemainingFilesCount}");
	}

	/// <summary>
	/// Console-specific callback to ask if the user wants to continue merging.
	/// </summary>
	/// <returns>True to continue, false to stop.</returns>
	private bool ConsoleContinueCallback()
	{
		Console.Write("Continue with next merge? (y/n): ");
		string? response = Console.ReadLine();
		return response?.ToLowerInvariant() is "y" or "yes";
	}

	/// <summary>
	/// Callback to perform merge operation between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	/// <param name="existingContent">Existing merged content.</param>
	/// <returns>Merge result or null if cancelled.</returns>
	private MergeResult? PerformMergeCallback(string file1, string file2, string? existingContent)
	{
		// For console app, perform automatic merge or prompt user
		// This is a simplified implementation - in a real app you'd want user interaction
		return IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			file1,
			file2,
			existingContent,
			(block, context, index) => BlockChoice.UseVersion1); // Default to version 1 choice
	}

	/// <summary>
	/// Callback to report merge status.
	/// </summary>
	/// <param name="status">Current merge session status.</param>
	private void ReportMergeStatus(MergeSessionStatus status)
	{
		Console.WriteLine($"Merge {status.CurrentIteration}: {status.MostSimilarPair?.FilePath1} <-> {status.MostSimilarPair?.FilePath2}");
		Console.WriteLine($"Similarity: {status.MostSimilarPair?.SimilarityScore:F1} | Remaining files: {status.RemainingFilesCount}");
	}

	/// <summary>
	/// Callback to ask if the user wants to continue merging.
	/// </summary>
	/// <returns>True to continue, false to stop.</returns>
	private bool ContinueMergeCallback()
	{
		Console.Write("Continue with next merge? (y/n): ");
		string? response = Console.ReadLine();
		return response?.ToLowerInvariant() is "y" or "yes";
	}
}
