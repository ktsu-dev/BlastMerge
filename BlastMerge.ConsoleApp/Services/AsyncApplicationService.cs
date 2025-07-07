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
using System.Collections.Concurrent;
using System.IO.Abstractions;

/// <summary>
/// Provides asynchronous file processing operations with progress reporting
/// </summary>
public static class AsyncApplicationService
{
	/// <summary>
	/// Reads multiple files asynchronously
	/// </summary>
	/// <param name="filePaths">File paths to read</param>
	/// <param name="maxDegreeOfParallelism">Maximum concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Dictionary mapping file paths to their contents</returns>
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

		ConcurrentDictionary<string, string> results = new();

		await Parallel.ForEachAsync(filePaths, new ParallelOptions
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism,
			CancellationToken = cancellationToken
		}, async (filePath, ct) =>
		{
			try
			{
				string content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
				results.TryAdd(filePath, content);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// Log error but continue processing other files
				results.TryAdd(filePath, $"Error reading file: {ex.Message}");
			}
		}).ConfigureAwait(false);

		return new Dictionary<string, string>(results);
	}

	/// <summary>
	/// Copies files asynchronously with progress reporting
	/// </summary>
	/// <param name="copyOperations">Source and target file path pairs</param>
	/// <param name="maxDegreeOfParallelism">Maximum concurrent operations</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Collection of completed copy operations</returns>
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

		ConcurrentBag<(string source, string target)> completed = [];

		await Parallel.ForEachAsync(copyOperations, new ParallelOptions
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism,
			CancellationToken = cancellationToken
		}, async (operation, ct) =>
		{
			try
			{
				string? targetDirectory = Path.GetDirectoryName(operation.target);
				if (!string.IsNullOrEmpty(targetDirectory))
				{
					Directory.CreateDirectory(targetDirectory);
				}

				await using FileStream source = File.OpenRead(operation.source);
				await using FileStream target = File.Create(operation.target);
				await source.CopyToAsync(target, ct).ConfigureAwait(false);

				completed.Add(operation);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// Log error but continue processing other operations
				AnsiConsole.MarkupLine($"[red]Error copying {operation.source} to {operation.target}: {ex.Message}[/]");
			}
		}).ConfigureAwait(false);

		return [.. completed];
	}
}
