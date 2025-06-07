// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.Core.Models;

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
		List<FileGroup> remainingGroups = [.. fileGroups];
		int mergeCount = 1;

		while (remainingGroups.Count > 1)
		{
			// Find the two most similar groups among remaining groups
			FileSimilarity? similarity = FileDiffer.FindMostSimilarFiles(remainingGroups);

			if (similarity == null)
			{
				// No similar files found - this happens when files have different names and our safety check prevents merging
				// This is the intended safe behavior, not an error
				return new MergeCompletionResult(true, null, 0, "All files preserved safely - no merging needed.\nAll files have different names, so they remain separate as intended.");
			}

			// Report status to UI
			MergeSessionStatus status = new(mergeCount, remainingGroups.Sum(g => g.FilePaths.Count), mergeCount - 1, similarity);
			statusCallback(status);

			// Perform the merge
			MergeResult? mergeResult = mergeCallback(similarity.FilePath1, similarity.FilePath2, null);

			if (mergeResult == null)
			{
				return new MergeCompletionResult(false, null, 0, "cancelled");
			}

			// Update all files with the merged result
			string mergedContent = string.Join(Environment.NewLine, mergeResult.MergedLines);

			try
			{
				// Find the groups being merged
				FileGroup group1 = remainingGroups.First(g => g.FilePaths.Contains(similarity.FilePath1));
				FileGroup group2 = remainingGroups.First(g => g.FilePaths.Contains(similarity.FilePath2));

				// Create a new merged group with all files from both groups
				FileGroup mergedGroup = new([.. group1.FilePaths, .. group2.FilePaths])
				{
					Hash = FileDiffer.CalculateFileHash(mergedContent)
				};

				// Update all files in both groups with the merged content
				foreach (string filePath in mergedGroup.FilePaths)
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
				return new MergeCompletionResult(false, mergedContent, mergedContent.Split(Environment.NewLine).Length, $"error: {ex.Message}");
			}
			catch (UnauthorizedAccessException ex)
			{
				return new MergeCompletionResult(false, mergedContent, mergedContent.Split(Environment.NewLine).Length, $"access denied: {ex.Message}");
			}

			mergeCount++;

			// Check if user wants to continue (if there are more groups to merge)
			if (remainingGroups.Count > 1 && !continuationCallback())
			{
				return new MergeCompletionResult(false, mergedContent, mergedContent.Split(Environment.NewLine).Length, "incomplete");
			}
		}

		// Merge completed successfully
		FileGroup finalGroup = remainingGroups[0];
		string finalContent = File.ReadAllText(finalGroup.FilePaths.First());
		string[] finalLines = finalContent.Split(Environment.NewLine);

		return new MergeCompletionResult(true, finalContent, finalLines.Length, Path.GetFileName(finalGroup.FilePaths.First()));
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

		IReadOnlyCollection<string> files = FileFinder.FindFiles(directory, fileName);
		List<string> filesList = [.. files];

		if (filesList.Count < 2)
		{
			return null; // Need at least 2 versions to merge
		}

		// Group files by hash to find unique versions
		IReadOnlyCollection<FileGroup> fileGroups = FileDiffer.GroupFilesByHash(files);
		List<FileGroup> uniqueGroups = [.. fileGroups.Where(g => g.FilePaths.Count >= 1)];

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
		Func<DiffPlex.Model.DiffBlock, BlockContext, int, BlockChoice> blockChoiceCallback)
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
