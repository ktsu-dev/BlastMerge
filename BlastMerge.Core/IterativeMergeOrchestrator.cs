// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Represents the status of an iterative merge process
/// </summary>
public class MergeSessionStatus
{
	/// <summary>
	/// Gets the current iteration number
	/// </summary>
	public int CurrentIteration { get; init; }

	/// <summary>
	/// Gets the number of remaining files to merge
	/// </summary>
	public int RemainingFilesCount { get; init; }

	/// <summary>
	/// Gets the number of completed merges
	/// </summary>
	public int CompletedMergesCount { get; init; }

	/// <summary>
	/// Gets the most similar file pair for this iteration
	/// </summary>
	public FileSimilarity? MostSimilarPair { get; init; }
}

/// <summary>
/// Represents the completion status of an iterative merge
/// </summary>
public class MergeCompletionResult
{
	/// <summary>
	/// Gets whether the merge completed successfully
	/// </summary>
	public bool IsSuccessful { get; init; }

	/// <summary>
	/// Gets the final merged content
	/// </summary>
	public string? FinalMergedContent { get; init; }

	/// <summary>
	/// Gets the number of lines in the final result
	/// </summary>
	public int FinalLineCount { get; init; }

	/// <summary>
	/// Gets the original filename being merged
	/// </summary>
	public required string OriginalFileName { get; init; }
}

/// <summary>
/// Orchestrates iterative merge processes for multiple file versions
/// </summary>
public static class IterativeMergeOrchestrator
{
	/// <summary>
	/// Starts an iterative merge process for multiple file versions
	/// </summary>
	/// <param name="fileGroups">The unique file groups to merge</param>
	/// <param name="mergeCallback">Callback function to perform individual merges</param>
	/// <param name="statusCallback">Callback function to report merge status</param>
	/// <param name="continuationCallback">Callback function to ask whether to continue</param>
	/// <returns>The completion result of the merge process</returns>
	public static MergeCompletionResult StartIterativeMergeProcess(
		IReadOnlyCollection<FileGroup> fileGroups,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);
		ArgumentNullException.ThrowIfNull(mergeCallback);
		ArgumentNullException.ThrowIfNull(statusCallback);
		ArgumentNullException.ThrowIfNull(continuationCallback);

		// Create a working copy of file groups that we'll update as we merge
		var remainingGroups = fileGroups.ToList();
		var mergeCount = 1;

		while (remainingGroups.Count > 1)
		{
			// Find the two most similar groups among remaining groups
			var similarity = FileDiffer.FindMostSimilarFiles(remainingGroups);

			if (similarity == null)
			{
				return new MergeCompletionResult
				{
					IsSuccessful = false,
					FinalMergedContent = null,
					FinalLineCount = 0,
					OriginalFileName = "unknown"
				};
			}

			// Report status to UI
			var status = new MergeSessionStatus
			{
				CurrentIteration = mergeCount,
				RemainingFilesCount = remainingGroups.Sum(g => g.FilePaths.Count),
				CompletedMergesCount = mergeCount - 1,
				MostSimilarPair = similarity
			};
			statusCallback(status);

			// Perform the merge
			var mergeResult = mergeCallback(similarity.FilePath1, similarity.FilePath2, null);

			if (mergeResult == null)
			{
				return new MergeCompletionResult
				{
					IsSuccessful = false,
					FinalMergedContent = null,
					FinalLineCount = 0,
					OriginalFileName = "cancelled"
				};
			}

			// Update all files with the merged result
			var mergedContent = string.Join(Environment.NewLine, mergeResult.MergedLines);

			try
			{
				// Find the groups being merged
				var group1 = remainingGroups.First(g => g.FilePaths.Contains(similarity.FilePath1));
				var group2 = remainingGroups.First(g => g.FilePaths.Contains(similarity.FilePath2));

				// Create a new merged group with all files from both groups
				var mergedGroup = new FileGroup([.. group1.FilePaths, .. group2.FilePaths])
				{
					Hash = FileDiffer.CalculateFileHash(mergedContent)
				};

				// Update all files in both groups with the merged content
				foreach (var filePath in mergedGroup.FilePaths)
				{
					File.WriteAllText(filePath, mergedContent);
				}

				// Remove the original groups and add the merged group
				remainingGroups.Remove(group1);
				remainingGroups.Remove(group2);
				remainingGroups.Add(mergedGroup);
			}
			catch (IOException ex)
			{
				return new MergeCompletionResult
				{
					IsSuccessful = false,
					FinalMergedContent = mergedContent,
					FinalLineCount = mergedContent.Split(Environment.NewLine).Length,
					OriginalFileName = $"error: {ex.Message}"
				};
			}
			catch (UnauthorizedAccessException ex)
			{
				return new MergeCompletionResult
				{
					IsSuccessful = false,
					FinalMergedContent = mergedContent,
					FinalLineCount = mergedContent.Split(Environment.NewLine).Length,
					OriginalFileName = $"access denied: {ex.Message}"
				};
			}

			mergeCount++;

			// Check if user wants to continue (if there are more groups to merge)
			if (remainingGroups.Count > 1 && !continuationCallback())
			{
				return new MergeCompletionResult
				{
					IsSuccessful = false,
					FinalMergedContent = mergedContent,
					FinalLineCount = mergedContent.Split(Environment.NewLine).Length,
					OriginalFileName = "incomplete"
				};
			}
		}

		// Merge completed successfully
		var finalGroup = remainingGroups.First();
		var finalContent = File.ReadAllText(finalGroup.FilePaths.First());
		var finalLines = finalContent.Split(Environment.NewLine);

		return new MergeCompletionResult
		{
			IsSuccessful = true,
			FinalMergedContent = finalContent,
			FinalLineCount = finalLines.Length,
			OriginalFileName = Path.GetFileName(finalGroup.FilePaths.First())
		};
	}

	/// <summary>
	/// Prepares file groups for iterative merging by finding unique versions
	/// </summary>
	/// <param name="directory">Directory containing the files</param>
	/// <param name="fileName">Filename pattern to search for</param>
	/// <returns>Collection of unique file groups, or null if insufficient files found</returns>
	public static IReadOnlyCollection<FileGroup>? PrepareFileGroupsForMerging(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			return null;
		}

		var files = FileFinder.FindFiles(directory, fileName);
		var filesList = files.ToList();

		if (filesList.Count < 2)
		{
			return null; // Need at least 2 versions to merge
		}

		// Group files by hash to find unique versions
		var fileGroups = FileDiffer.GroupFilesByHash(files);
		var uniqueGroups = fileGroups.Where(g => g.FilePaths.Count >= 1).ToList();

		if (uniqueGroups.Count < 2)
		{
			return null; // All files are identical
		}

		return uniqueGroups;
	}

	/// <summary>
	/// Performs a merge with conflict resolution between two files or with existing merged content
	/// </summary>
	/// <param name="file1">First file to merge</param>
	/// <param name="file2">Second file to merge</param>
	/// <param name="existingMergedContent">Existing merged content (if any)</param>
	/// <param name="blockChoiceCallback">Callback function to get user's choice for each block</param>
	/// <returns>The manually merged result, or null if cancelled</returns>
	public static MergeResult? PerformMergeWithConflictResolution(
		string file1,
		string file2,
		string? existingMergedContent,
		Func<DiffBlock, BlockContext, int, BlockChoice> blockChoiceCallback)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);
		ArgumentNullException.ThrowIfNull(blockChoiceCallback);

		string[] lines1;
		string[] lines2;

		if (existingMergedContent != null)
		{
			// Merge with existing content
			lines1 = existingMergedContent.Split(Environment.NewLine);
			lines2 = File.ReadAllLines(file2);
		}
		else
		{
			// Merge two files
			lines1 = File.ReadAllLines(file1);
			lines2 = File.ReadAllLines(file2);
		}

		return BlockMerger.PerformManualBlockSelection(lines1, lines2, blockChoiceCallback);
	}
}
