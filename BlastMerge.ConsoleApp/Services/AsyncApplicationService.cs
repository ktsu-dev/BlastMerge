// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
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
				filePaths = FileFinder.FindFiles(directory, fileName, progressCallback: path =>
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
				Dictionary<string, IReadOnlyCollection<string>> result = groups.ToDictionary(group => group.Hash, group => group.FilePaths);

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
				string[] files1 = recursive
					? [.. FileFinder.FindFiles(dir1, pattern, fileSystem: null)]
					: GetNonRecursiveFiles(dir1, pattern);

				ctx.Status($"[yellow]Finding files in second directory...[/]");
				string[] files2 = recursive
					? [.. FileFinder.FindFiles(dir2, pattern, fileSystem: null)]
					: GetNonRecursiveFiles(dir2, pattern);

				// Step 2: Compute hashes in parallel for better performance
				ctx.Status($"[yellow]Computing file hashes ({files1.Length + files2.Length} files)...[/]");
				Task<Dictionary<string, string>> hashesTask1 = FileHasher.ComputeFileHashesAsync(
					files1, null, maxDegreeOfParallelism, cancellationToken);
				Task<Dictionary<string, string>> hashesTask2 = FileHasher.ComputeFileHashesAsync(
					files2, null, maxDegreeOfParallelism, cancellationToken);

				Dictionary<string, string>[] hashResults = await Task.WhenAll(hashesTask1, hashesTask2).ConfigureAwait(false);
				Dictionary<string, string> hashes1 = hashResults[0];
				Dictionary<string, string> hashes2 = hashResults[1];

				// Step 3: Create comparison result using pre-computed hashes
				ctx.Status($"[yellow]Creating comparison report...[/]");
				result = CreateDirectoryComparisonResult(dir1, dir2, files1, files2, hashes1, hashes2);
			});

		return result;
	}

	/// <summary>
	/// Gets files from directory non-recursively, or empty collection if directory doesn't exist
	/// </summary>
	/// <param name="directory">Directory path</param>
	/// <param name="pattern">Search pattern</param>
	/// <returns>Collection of file paths</returns>
	private static string[] GetNonRecursiveFiles(string directory, string pattern) =>
		Directory.Exists(directory)
			? Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly)
			: [];

	/// <summary>
	/// Creates a DirectoryComparisonResult using pre-computed file hashes
	/// </summary>
	/// <param name="dir1">First directory path</param>
	/// <param name="dir2">Second directory path</param>
	/// <param name="files1">Files from first directory</param>
	/// <param name="files2">Files from second directory</param>
	/// <param name="hashes1">Pre-computed hashes for files1</param>
	/// <param name="hashes2">Pre-computed hashes for files2</param>
	/// <returns>Directory comparison result</returns>
	private static DirectoryComparisonResult CreateDirectoryComparisonResult(
		string dir1,
		string dir2,
		IReadOnlyCollection<string> files1,
		IReadOnlyCollection<string> files2,
		Dictionary<string, string> hashes1,
		Dictionary<string, string> hashes2)
	{
		// Convert absolute paths to relative paths for comparison
		HashSet<string> relativeFiles1 = Directory.Exists(dir1)
			? [.. files1.Select(f => Path.GetRelativePath(dir1, f))]
			: [];

		HashSet<string> relativeFiles2 = Directory.Exists(dir2)
			? [.. files2.Select(f => Path.GetRelativePath(dir2, f))]
			: [];

		List<string> sameFiles = [];
		List<string> modifiedFiles = [];
		List<string> onlyInDir1 = [];
		List<string> onlyInDir2 = [];

		// Find files that exist in both directories
		List<string> commonFiles = [.. relativeFiles1.Intersect(relativeFiles2)];

		foreach (string relativePath in commonFiles)
		{
			string file1Path = Path.Combine(dir1, relativePath);
			string file2Path = Path.Combine(dir2, relativePath);

			// Use pre-computed hashes to compare files
			bool file1HasHash = hashes1.TryGetValue(file1Path, out string? hash1);
			bool file2HasHash = hashes2.TryGetValue(file2Path, out string? hash2);

			if (file1HasHash && file2HasHash && hash1 == hash2)
			{
				sameFiles.Add(relativePath);
			}
			else
			{
				modifiedFiles.Add(relativePath);
			}
		}

		// Find files that exist only in dir1
		onlyInDir1.AddRange(relativeFiles1.Except(relativeFiles2));

		// Find files that exist only in dir2
		onlyInDir2.AddRange(relativeFiles2.Except(relativeFiles1));

		return new DirectoryComparisonResult
		{
			SameFiles = sameFiles.AsReadOnly(),
			ModifiedFiles = modifiedFiles.AsReadOnly(),
			OnlyInDir1 = onlyInDir1.AsReadOnly(),
			OnlyInDir2 = onlyInDir2.AsReadOnly()
		};
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
