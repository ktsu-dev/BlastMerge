// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;
using ktsu.FileSystemProvider;

/// <summary>
/// Provides asynchronous file diffing and comparison functionality with parallel processing
/// </summary>
/// <param name="fileSystemProvider">File system provider for file operations</param>
/// <param name="fileHasher">File hasher service for computing file hashes</param>
public class AsyncFileDiffer(IFileSystemProvider fileSystemProvider, FileHasher fileHasher)
{
	/// <summary>
	/// Groups files by their hash asynchronously with parallel processing
	/// </summary>
	/// <param name="filePaths">List of file paths to group</param>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A collection of file groups where each group contains identical files</returns>
	public async Task<IReadOnlyCollection<FileGroup>> GroupFilesByHashAsync(
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
		Dictionary<string, string> fileHashes = await fileHasher.ComputeFileHashesAsync(
			filePaths, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(false);

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
	public async Task<IReadOnlyCollection<FileGroup>> GroupFilesByFilenameAndHashAsync(
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
	private async Task<List<FileGroup>> ProcessFilenameGroupAsync(
		List<string> filesWithSameName,
		int maxDegreeOfParallelism,
		CancellationToken cancellationToken)
	{
		if (filesWithSameName.Count == 1)
		{
			// Single file with this name - create a group for it
			string hash = await fileHasher.ComputeFileHashAsync(filesWithSameName[0], cancellationToken).ConfigureAwait(false);
			FileGroup group = new() { Hash = hash };
			group.AddFilePath(filesWithSameName[0]);
			return [group];
		}

		// Multiple files with same name - group by content hash
		Dictionary<string, string> fileHashes = await fileHasher.ComputeFileHashesAsync(
			filesWithSameName, maxDegreeOfParallelism, cancellationToken).ConfigureAwait(false);

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
	public async Task<Dictionary<string, string>> ReadFilesAsync(
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
	private async Task<(string filePath, string content)> ReadFileWithSemaphore(
		string filePath,
		SemaphoreSlim semaphore,
		CancellationToken cancellationToken)
	{
		await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			string content = await fileSystemProvider.Current.File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
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
	public async Task<double> CalculateFileSimilarityAsync(
		string file1,
		string file2,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		string[] lines1 = await fileSystemProvider.Current.File.ReadAllLinesAsync(file1, cancellationToken).ConfigureAwait(false);
		string[] lines2 = await fileSystemProvider.Current.File.ReadAllLinesAsync(file2, cancellationToken).ConfigureAwait(false);

		return FileDiffer.CalculateLineSimilarity(lines1, lines2);
	}

	/// <summary>
	/// Copies files asynchronously in parallel
	/// </summary>
	/// <param name="operations">Collection of (source, target) file pairs to copy</param>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of (source, target) pairs that were successfully copied</returns>
	public async Task<IReadOnlyCollection<(string source, string target)>> CopyFilesAsync(
		IEnumerable<(string source, string target)> operations,
		int maxDegreeOfParallelism = 0,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(operations);

		if (maxDegreeOfParallelism <= 0)
		{
			maxDegreeOfParallelism = Environment.ProcessorCount;
		}

		using SemaphoreSlim semaphore = new(maxDegreeOfParallelism);
		List<Task<(string source, string target, bool success)>> tasks = [];

		foreach ((string source, string target) in operations)
		{
			tasks.Add(CopyFileWithSemaphore(source, target, semaphore, cancellationToken));
		}

		(string source, string target, bool success)[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
		return results.Where(r => r.success).Select(r => (r.source, r.target)).ToList().AsReadOnly();
	}

	/// <summary>
	/// Helper method to copy file with semaphore throttling
	/// </summary>
	private async Task<(string source, string target, bool success)> CopyFileWithSemaphore(
		string source,
		string target,
		SemaphoreSlim semaphore,
		CancellationToken cancellationToken)
	{
		await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			// Ensure target directory exists
			string? targetDirectory = Path.GetDirectoryName(target);
			if (!string.IsNullOrEmpty(targetDirectory) && !fileSystemProvider.Current.Directory.Exists(targetDirectory))
			{
				fileSystemProvider.Current.Directory.CreateDirectory(targetDirectory);
			}

			using Stream sourceStream = fileSystemProvider.Current.File.OpenRead(source);
			using Stream targetStream = fileSystemProvider.Current.File.Create(target);
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
