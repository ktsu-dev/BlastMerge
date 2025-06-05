// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Processes batch operations for multiple file patterns
/// </summary>
public static partial class BatchProcessor
{
	/// <summary>
	/// Represents the result of processing a single pattern in a batch
	/// </summary>
	public class PatternResult
	{
		/// <inheritdoc/>
		public string Pattern { get; set; } = string.Empty;
		/// <inheritdoc/>
		public bool Success { get; set; }
		/// <inheritdoc/>
		public int FilesFound { get; set; }
		/// <inheritdoc/>
		public int UniqueVersions { get; set; }
		/// <inheritdoc/>
		public string Message { get; set; } = string.Empty;
		/// <inheritdoc/>
		public MergeCompletionResult? MergeResult { get; set; }
	}

	/// <summary>
	/// Processes a batch configuration
	/// </summary>
	/// <param name="batch">The batch configuration to process</param>
	/// <param name="directory">The directory to search in</param>
	/// <param name="mergeCallback">Callback function to perform individual merges</param>
	/// <param name="statusCallback">Callback function to report merge status</param>
	/// <param name="continuationCallback">Callback function to ask whether to continue</param>
	/// <param name="patternCallback">Optional callback for each pattern before processing</param>
	/// <returns>The batch processing result</returns>
	public static BatchResult ProcessBatch(
		BatchConfiguration batch,
		string directory,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback,
		Func<string, bool>? patternCallback = null)
	{
		ArgumentNullException.ThrowIfNull(batch);
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(mergeCallback);
		ArgumentNullException.ThrowIfNull(statusCallback);
		ArgumentNullException.ThrowIfNull(continuationCallback);

		BatchResult result = new()
		{
			BatchName = batch.Name,
			Success = true
		};

		if (!Directory.Exists(directory))
		{
			result.Success = false;
			result.Summary = $"Directory does not exist: {directory}";
			return result;
		}

		foreach (string pattern in batch.FilePatterns)
		{
			// Check if user wants to skip this pattern (if callback provided)
			if (batch.PromptBeforeEachPattern && patternCallback != null)
			{
				if (!patternCallback(pattern))
				{
					PatternResult skippedResult = new()
					{
						Pattern = pattern,
						Success = true,
						Message = "Skipped by user"
					};
					result.PatternResults.Add(skippedResult);
					continue;
				}
			}

			PatternResult patternResult = ProcessSinglePattern(
				pattern,
				directory,
				mergeCallback,
				statusCallback,
				continuationCallback);

			result.PatternResults.Add(patternResult);

			// Skip empty patterns if configured to do so
			if (!patternResult.Success && batch.SkipEmptyPatterns && patternResult.FilesFound == 0)
			{
				patternResult.Success = true; // Don't count as failure if we're skipping empty patterns
				patternResult.Message = "No files found (skipped)";
			}

			// Update overall batch success
			if (!patternResult.Success)
			{
				result.Success = false;
			}
		}

		// Generate summary
		result.TotalPatternsProcessed = result.PatternResults.Count;
		result.SuccessfulPatterns = result.PatternResults.Count(r => r.Success);

		if (result.Success)
		{
			result.Summary = $"Batch completed successfully. Processed {result.SuccessfulPatterns}/{result.TotalPatternsProcessed} patterns.";
		}
		else
		{
			int failedPatterns = result.TotalPatternsProcessed - result.SuccessfulPatterns;
			result.Summary = $"Batch completed with {failedPatterns} failed patterns. Processed {result.SuccessfulPatterns}/{result.TotalPatternsProcessed} patterns.";
		}

		return result;
	}

	/// <summary>
	/// Processes a single file pattern
	/// </summary>
	/// <param name="pattern">The file pattern to process</param>
	/// <param name="directory">The directory to search in</param>
	/// <param name="mergeCallback">Callback function to perform individual merges</param>
	/// <param name="statusCallback">Callback function to report merge status</param>
	/// <param name="continuationCallback">Callback function to ask whether to continue</param>
	/// <returns>The pattern processing result</returns>
	public static PatternResult ProcessSinglePattern(
		string pattern,
		string directory,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback)
	{
		ArgumentNullException.ThrowIfNull(pattern);
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(mergeCallback);
		ArgumentNullException.ThrowIfNull(statusCallback);
		ArgumentNullException.ThrowIfNull(continuationCallback);

		PatternResult result = new()
		{
			Pattern = pattern,
			Success = false
		};

		try
		{
			// Find files matching the pattern
			IReadOnlyCollection<string> files = FileFinder.FindFiles(directory, pattern);
			result.FilesFound = files.Count;

			if (files.Count == 0)
			{
				result.Message = "No files found";
				return result;
			}

			if (files.Count == 1)
			{
				result.Success = true;
				result.UniqueVersions = 1;
				result.Message = "Only one file found, no merge needed";
				return result;
			}

			// Group files by hash to find unique versions
			IReadOnlyCollection<FileGroup> fileGroups = FileDiffer.GroupFilesByHash(files);
			result.UniqueVersions = fileGroups.Count;

			if (fileGroups.Count == 1)
			{
				result.Success = true;
				result.Message = "All files are identical";
				return result;
			}

			// Perform iterative merge
			MergeCompletionResult mergeResult = IterativeMergeOrchestrator.StartIterativeMergeProcess(
				fileGroups,
				mergeCallback,
				statusCallback,
				continuationCallback);

			result.MergeResult = mergeResult;
			result.Success = mergeResult.IsSuccessful;
			result.Message = mergeResult.IsSuccessful ? "Merge completed successfully" : $"Merge failed: {mergeResult.OriginalFileName}";

			return result;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			result.Message = $"Error processing pattern: {ex.Message}";
			return result;
		}
	}

	/// <summary>
	/// Creates a custom batch configuration from a list of patterns
	/// </summary>
	/// <param name="name">Name for the batch</param>
	/// <param name="patterns">List of file patterns</param>
	/// <param name="description">Optional description</param>
	/// <returns>A new batch configuration</returns>
	public static BatchConfiguration CreateCustomBatch(string name, IEnumerable<string> patterns, string description = "")
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(patterns);

		return new BatchConfiguration
		{
			Name = name,
			Description = description,
			FilePatterns = [.. patterns.Where(p => !string.IsNullOrWhiteSpace(p))],
			SkipEmptyPatterns = true,
			PromptBeforeEachPattern = false
		};
	}
}
