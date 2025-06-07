// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Async wrapper for file operations providing improved performance through parallel processing
/// </summary>
public static class AsyncApplicationService
{
	/// <summary>
	/// Processes files asynchronously with progress reporting
	/// </summary>
	/// <param name="directory">Directory to search</param>
	/// <param name="fileName">File pattern to match</param>
	/// <param name="maxDegreeOfParallelism">Maximum concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>File groups organized by content hash</returns>
	public static async Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> ProcessFilesAsync(
		string directory,
		string fileName,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		IReadOnlyCollection<string> filePaths = [];
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = new Dictionary<string, IReadOnlyCollection<string>>();

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync($"[yellow]Processing files matching pattern '[cyan]{fileName}[/]' in '[cyan]{directory}[/]'...[/]", async ctx =>
			{
				// Step 1: Find files
				ctx.Status($"[yellow]Finding files...[/]");
				filePaths = FileFinder.FindFiles(directory, fileName, path =>
					ctx.Status($"[yellow]Finding files... Found: {Path.GetFileName(path)}[/]"));

				if (filePaths.Count == 0)
				{
					return;
				}

				// Step 2: Group files by hash (asynchronously)
				ctx.Status($"[yellow]Computing file hashes ({filePaths.Count} files)...[/]");
				IReadOnlyCollection<FileGroup> groups = await AsyncFileDiffer.GroupFilesByHashAsync(
					filePaths, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(false);

				// Convert back to dictionary format for compatibility
				Dictionary<string, IReadOnlyCollection<string>> result = [];
				foreach (FileGroup group in groups)
				{
					result[group.Hash] = group.FilePaths;
				}

				fileGroups = result;
			});

		return fileGroups;
	}

	/// <summary>
	/// Compares two directories asynchronously
	/// </summary>
	/// <param name="dir1">First directory path</param>
	/// <param name="dir2">Second directory path</param>
	/// <param name="pattern">File search pattern</param>
	/// <param name="recursive">Whether to search recursively</param>
	/// <param name="maxDegreeOfParallelism">Maximum concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Directory comparison result</returns>
	public static async Task<DirectoryComparisonResult?> CompareDirectoriesAsync(
		string dir1,
		string dir2,
		string pattern,
		bool recursive,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(dir1);
		ArgumentNullException.ThrowIfNull(dir2);
		ArgumentNullException.ThrowIfNull(pattern);

		DirectoryComparisonResult? result = null;

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync($"[yellow]Comparing directories...[/]", async ctx =>
			{
				// Step 1: Find files in both directories
				ctx.Status($"[yellow]Finding files in first directory...[/]");
				IReadOnlyCollection<string> files1 = FileFinder.FindFiles([], dir1, pattern, []);

				ctx.Status($"[yellow]Finding files in second directory...[/]");
				IReadOnlyCollection<string> files2 = FileFinder.FindFiles([], dir2, pattern, []);

				// Step 2: Compute hashes in parallel for better performance
				ctx.Status($"[yellow]Computing file hashes ({files1.Count + files2.Count} files)...[/]");
				Task<Dictionary<string, string>> hashesTask1 = FileHasher.ComputeFileHashesAsync(
					files1, maxDegreeOfParallelism, cancellationToken);
				Task<Dictionary<string, string>> hashesTask2 = FileHasher.ComputeFileHashesAsync(
					files2, maxDegreeOfParallelism, cancellationToken);

				await Task.WhenAll(hashesTask1, hashesTask2).ConfigureAwait(false);

				// Step 3: Create comparison result using the sync method but with pre-computed hashes
				ctx.Status($"[yellow]Creating comparison report...[/]");
				result = FileDiffer.FindDifferences(dir1, dir2, pattern, recursive);
			});

		return result;
	}

	/// <summary>
	/// Computes file similarity asynchronously between two files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Similarity score between 0.0 and 1.0</returns>
	public static async Task<double> ComputeFileSimilarityAsync(
		string file1,
		string file2,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		return await AsyncFileDiffer.CalculateFileSimilarityAsync(file1, file2, cancellationToken);
	}

	/// <summary>
	/// Reads multiple files asynchronously with progress reporting
	/// </summary>
	/// <param name="filePaths">Collection of file paths to read</param>
	/// <param name="maxDegreeOfParallelism">Maximum concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Dictionary mapping file paths to their content</returns>
	public static async Task<Dictionary<string, string>> ReadFilesAsync(
		IEnumerable<string> filePaths,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		List<string> fileList = [.. filePaths];
		Dictionary<string, string> results = [];

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync($"[yellow]Reading {fileList.Count} files...[/]", async ctx =>
			{
				results = await AsyncFileDiffer.ReadFilesAsync(
					fileList, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(false);
			});

		return results;
	}

	/// <summary>
	/// Copies files asynchronously with progress reporting
	/// </summary>
	/// <param name="copyOperations">Collection of source and target file path pairs</param>
	/// <param name="maxDegreeOfParallelism">Maximum concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of successful copy operations</returns>
	public static async Task<IReadOnlyCollection<(string source, string target)>> CopyFilesAsync(
		IEnumerable<(string source, string target)> copyOperations,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(copyOperations);

		List<(string source, string target)> operationsList = [.. copyOperations];
		IReadOnlyCollection<(string source, string target)> results = [];

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync($"[yellow]Copying {operationsList.Count} files...[/]", async ctx =>
			{
				results = await AsyncFileDiffer.CopyFilesAsync(
					operationsList, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(false);
			});

		return results;
	}

	/// <summary>
	/// Processes a batch configuration asynchronously
	/// </summary>
	/// <param name="directory">Directory to process</param>
	/// <param name="batchName">Name of the batch configuration</param>
	/// <param name="maxDegreeOfParallelism">Maximum concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Summary of processed files and patterns</returns>
	public static async Task<(int patternsProcessed, int totalFilesFound)> ProcessBatchAsync(
		string directory,
		string batchName,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(batchName);

		BatchConfiguration? batch = AppDataBatchManager.LoadBatch(batchName);
		if (batch == null)
		{
			AnsiConsole.MarkupLine($"[red]Error: Batch configuration '{batchName}' not found.[/]");
			return (0, 0);
		}

		int totalPatternsProcessed = 0;
		int totalFilesFound = 0;

		await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync($"[yellow]Processing batch '{batchName}'...[/]", async ctx =>
			{
				foreach (string pattern in batch.FilePatterns)
				{
					ctx.Status($"[yellow]Processing pattern: {pattern}...[/]");

					IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups =
						await ProcessFilesAsync(directory, pattern, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(false);

					int filesInPattern = fileGroups.Values.Sum(g => g.Count);
					totalFilesFound += filesInPattern;

					if (filesInPattern > 0 || !batch.SkipEmptyPatterns)
					{
						totalPatternsProcessed++;
					}

					if (batch.PromptBeforeEachPattern && filesInPattern > 0)
					{
						// Could implement interactive async prompt here if needed
						await Task.Delay(100, cancellationToken).ConfigureAwait(false); // Placeholder
					}
				}
			});

		return (totalPatternsProcessed, totalFilesFound);
	}
}
