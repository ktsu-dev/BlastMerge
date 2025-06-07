// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.Core.Models;

/// <summary>
/// Provides asynchronous file diffing and comparison functionality with parallel processing
/// </summary>
public static class AsyncFileDiffer
{
	/// <summary>
	/// Groups files by their hash asynchronously with parallel processing
	/// </summary>
	/// <param name="filePaths">List of file paths to group</param>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A collection of file groups where each group contains identical files</returns>
	public static async Task<IReadOnlyCollection<FileGroup>> GroupFilesByHashAsync(
		IReadOnlyCollection<string> filePaths,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		if (maxDegreeOfParallelism <= 0)
		{
			maxDegreeOfParallelism = Environment.ProcessorCount;
		}

		// Compute hashes in parallel
		Dictionary<string, string> fileHashes = await FileHasher.ComputeFileHashesAsync(
			filePaths, null, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(false);

		// Group by hash
		Dictionary<string, FileGroup> groups = [];
		foreach (KeyValuePair<string, string> kvp in fileHashes)
		{
			if (!groups.TryGetValue(kvp.Value, out FileGroup? group))
			{
				group = new FileGroup { Hash = kvp.Value };
				groups[kvp.Value] = group;
			}
			group.AddFilePath(kvp.Key);
		}

		return [.. groups.Values];
	}

	/// <summary>
	/// Groups files by their filename (without path) first, then by content hash within each filename group asynchronously
	/// </summary>
	/// <param name="filePaths">List of file paths to group</param>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A collection of file groups where each group contains files with the same name and identical content</returns>
	public static async Task<IReadOnlyCollection<FileGroup>> GroupFilesByFilenameAndHashAsync(
		IReadOnlyCollection<string> filePaths,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		if (maxDegreeOfParallelism <= 0)
		{
			maxDegreeOfParallelism = Environment.ProcessorCount;
		}

		// First group by filename (basename without path)
		Dictionary<string, List<string>> filenameGroups = filePaths
			.GroupBy(Path.GetFileName)
			.ToDictionary(g => g.Key!, g => g.ToList());

		// Then group by content hash within each filename group (in parallel)
		Task<List<FileGroup>>[] groupTasks = [.. filenameGroups.Values
			.Select(filesWithSameName => ProcessFilenameGroupAsync(filesWithSameName, maxDegreeOfParallelism, cancellationToken))];

		List<FileGroup>[] groupResults = await Task.WhenAll(groupTasks).ConfigureAwait(false);
		List<FileGroup> allGroups = [.. groupResults.SelectMany(groups => groups)];

		return allGroups.AsReadOnly();
	}

	/// <summary>
	/// Processes a filename group asynchronously
	/// </summary>
	private static async Task<List<FileGroup>> ProcessFilenameGroupAsync(
		List<string> filesWithSameName,
		int maxDegreeOfParallelism,
		CancellationToken cancellationToken)
	{
		if (filesWithSameName.Count == 1)
		{
			// Single file with this name - create a group for it
			string hash = await FileHasher.ComputeFileHashAsync(filesWithSameName[0], null, cancellationToken).ConfigureAwait(false);
			FileGroup group = new() { Hash = hash };
			group.AddFilePath(filesWithSameName[0]);
			return [group];
		}

		// Multiple files with same name - group by content hash
		Dictionary<string, string> fileHashes = await FileHasher.ComputeFileHashesAsync(
			filesWithSameName, null, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(false);

		Dictionary<string, FileGroup> hashGroups = [];
		foreach (KeyValuePair<string, string> kvp in fileHashes)
		{
			if (!hashGroups.TryGetValue(kvp.Value, out FileGroup? group))
			{
				group = new FileGroup { Hash = kvp.Value };
				hashGroups[kvp.Value] = group;
			}
			group.AddFilePath(kvp.Key);
		}

		return [.. hashGroups.Values];
	}

	/// <summary>
	/// Reads multiple files asynchronously in parallel
	/// </summary>
	/// <param name="filePaths">File paths to read</param>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Dictionary mapping file paths to their content</returns>
	public static async Task<Dictionary<string, string>> ReadFilesAsync(
		IEnumerable<string> filePaths,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		if (maxDegreeOfParallelism <= 0)
		{
			maxDegreeOfParallelism = Environment.ProcessorCount;
		}

		using SemaphoreSlim semaphore = new(maxDegreeOfParallelism);
		List<Task<(string filePath, string content)>> tasks = [];

		foreach (string filePath in filePaths)
		{
			tasks.Add(ReadFileWithSemaphore(filePath, semaphore, cancellationToken));
		}

		(string filePath, string content)[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
		return results.ToDictionary(r => r.filePath, r => r.content);
	}

	/// <summary>
	/// Helper method to read file with semaphore throttling
	/// </summary>
	private static async Task<(string filePath, string content)> ReadFileWithSemaphore(
		string filePath,
		SemaphoreSlim semaphore,
		CancellationToken cancellationToken)
	{
		await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			string content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
			return (filePath, content);
		}
		finally
		{
			semaphore.Release();
		}
	}

	/// <summary>
	/// Calculates similarity score between two files asynchronously
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A similarity score between 0.0 (completely different) and 1.0 (identical)</returns>
	public static async Task<double> CalculateFileSimilarityAsync(
		string file1,
		string file2,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		string[] lines1 = await File.ReadAllLinesAsync(file1, cancellationToken).ConfigureAwait(false);
		string[] lines2 = await File.ReadAllLinesAsync(file2, cancellationToken).ConfigureAwait(false);

		return FileDiffer.CalculateLineSimilarity(lines1, lines2);
	}

	/// <summary>
	/// Copies files asynchronously in parallel
	/// </summary>
	/// <param name="copyOperations">Collection of source and target file path pairs</param>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of successful copy operations</returns>
	public static async Task<IReadOnlyCollection<(string source, string target)>> CopyFilesAsync(
		IEnumerable<(string source, string target)> copyOperations,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(copyOperations);

		if (maxDegreeOfParallelism <= 0)
		{
			maxDegreeOfParallelism = Environment.ProcessorCount;
		}

		using SemaphoreSlim semaphore = new(maxDegreeOfParallelism);
		List<Task<(string source, string target, bool success)>> tasks = [];

		foreach ((string source, string target) in copyOperations)
		{
			tasks.Add(CopyFileWithSemaphore(source, target, semaphore, cancellationToken));
		}

		(string source, string target, bool success)[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
		return results.Where(r => r.success).Select(r => (r.source, r.target)).ToList().AsReadOnly();
	}

	/// <summary>
	/// Helper method to copy file with semaphore throttling
	/// </summary>
	private static async Task<(string source, string target, bool success)> CopyFileWithSemaphore(
		string source,
		string target,
		SemaphoreSlim semaphore,
		CancellationToken cancellationToken)
	{
		await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			string? targetDir = Path.GetDirectoryName(target);
			if (!string.IsNullOrEmpty(targetDir))
			{
				Directory.CreateDirectory(targetDir);
			}

			using FileStream sourceStream = File.OpenRead(source);
			using FileStream targetStream = File.Create(target);
			await sourceStream.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);

			return (source, target, true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return (source, target, false);
		}
		finally
		{
			semaphore.Release();
		}
	}
}
