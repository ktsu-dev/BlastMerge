// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using ktsu.BlastMerge.Models;

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
	/// <param name="fileSystem">File system abstraction (optional)</param>
	/// <returns>The completion result of the merge process</returns>
	public static MergeCompletionResult StartIterativeMergeProcess(
		IReadOnlyCollection<FileGroup> fileGroups,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continuationCallback,
		IFileSystem? fileSystem = null)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);
		ArgumentNullException.ThrowIfNull(mergeCallback);
		ArgumentNullException.ThrowIfNull(statusCallback);
		ArgumentNullException.ThrowIfNull(continuationCallback);

		// Use provided fileSystem or get the default one
		fileSystem ??= FileSystemProvider.Current;

		// Create a working copy of file groups that we'll update as we merge
		List<FileGroup> remainingGroups = [.. fileGroups];
		List<MergeOperationSummary> operations = [];
		int initialFileGroups = fileGroups.Count;
		int totalFilesMerged = fileGroups.Sum(g => g.FilePaths.Count);
		int mergeCount = 1;

		while (remainingGroups.Count > 1)
		{
			// Find the two most similar groups among remaining groups
			FileSimilarity? similarity = FileDiffer.FindMostSimilarFiles(remainingGroups, fileSystem);

			if (similarity == null)
			{
				// No similar files found - this happens when files have different names and our safety check prevents merging
				// This is the intended safe behavior, not an error
				return new MergeCompletionResult(true, null, 0, "All files preserved safely - no merging needed.\nAll files have different names, so they remain separate as intended.")
				{
					TotalMergeOperations = 0,
					InitialFileGroups = initialFileGroups,
					TotalFilesMerged = totalFilesMerged,
					Operations = operations.AsReadOnly()
				};
			}

			// Perform the merge
			MergeResult? mergeResult = mergeCallback(similarity.FilePath1, similarity.FilePath2, null);

			// Report status to UI (after merge UI)
			MergeSessionStatus status = new(mergeCount, remainingGroups.Sum(g => g.FilePaths.Count), mergeCount - 1, similarity);
			statusCallback(status);

			if (mergeResult == null)
			{
				return new MergeCompletionResult(false, null, 0, "cancelled")
				{
					TotalMergeOperations = mergeCount - 1,
					InitialFileGroups = initialFileGroups,
					TotalFilesMerged = totalFilesMerged,
					Operations = operations.AsReadOnly()
				};
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

				// Track this operation
				MergeOperationSummary operation = new()
				{
					OperationNumber = mergeCount,
					FilePath1 = similarity.FilePath1,
					FilePath2 = similarity.FilePath2,
					SimilarityScore = similarity.SimilarityScore,
					FilesAffected = group1.FilePaths.Count + group2.FilePaths.Count,
					ConflictsResolved = mergeResult.Conflicts.Count,
					MergedLineCount = mergeResult.MergedLines.Count
				};
				operations.Add(operation);

				// Update all files in both groups with the merged content
				foreach (string filePath in mergedGroup.FilePaths)
				{
					fileSystem.File.WriteAllText(filePath, mergedContent);
				}

				// Remove the original groups and add the merged group
				remainingGroups.Remove(group1);
				remainingGroups.Remove(group2);
				remainingGroups.Add(mergedGroup);
			}
			catch (IOException ex)
			{
				return new MergeCompletionResult(false, mergedContent, mergedContent.Split(Environment.NewLine).Length, $"error: {ex.Message}")
				{
					TotalMergeOperations = mergeCount - 1,
					InitialFileGroups = initialFileGroups,
					TotalFilesMerged = totalFilesMerged,
					Operations = operations.AsReadOnly()
				};
			}
			catch (UnauthorizedAccessException ex)
			{
				return new MergeCompletionResult(false, mergedContent, mergedContent.Split(Environment.NewLine).Length, $"access denied: {ex.Message}")
				{
					TotalMergeOperations = mergeCount - 1,
					InitialFileGroups = initialFileGroups,
					TotalFilesMerged = totalFilesMerged,
					Operations = operations.AsReadOnly()
				};
			}

			mergeCount++;

			// Check if user wants to continue (if there are more groups to merge)
			if (remainingGroups.Count > 1 && !continuationCallback())
			{
				return new MergeCompletionResult(false, mergedContent, mergedContent.Split(Environment.NewLine).Length, "incomplete")
				{
					TotalMergeOperations = mergeCount - 1,
					InitialFileGroups = initialFileGroups,
					TotalFilesMerged = totalFilesMerged,
					Operations = operations.AsReadOnly()
				};
			}
		}

		// Merge completed successfully
		FileGroup finalGroup = remainingGroups[0];
		string finalContent = fileSystem.File.ReadAllText(finalGroup.FilePaths.First());
		string[] finalLines = finalContent.Split(Environment.NewLine);

		return new MergeCompletionResult(true, finalContent, finalLines.Length, Path.GetFileName(finalGroup.FilePaths.First()))
		{
			TotalMergeOperations = mergeCount - 1,
			InitialFileGroups = initialFileGroups,
			TotalFilesMerged = totalFilesMerged,
			Operations = operations.AsReadOnly()
		};
	}

	/// <summary>
	/// Prepares file groups for iterative merging by finding unique versions
	/// </summary>
	/// <param name="directory">Directory containing the files</param>
	/// <param name="fileName">Filename pattern to search for</param>
	/// <param name="fileSystem">File system abstraction (optional)</param>
	/// <returns>Collection of unique file groups, or null if insufficient files found</returns>
	public static IReadOnlyCollection<FileGroup>? PrepareFileGroupsForMerging(string directory, string fileName, IFileSystem? fileSystem = null)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		// Use provided fileSystem or get the default one
		fileSystem ??= FileSystemProvider.Current;

		if (!fileSystem.Directory.Exists(directory))
		{
			return null;
		}

		IReadOnlyCollection<string> files = FileFinder.FindFiles(directory, fileName, fileSystem);
		List<string> filesList = [.. files];

		if (filesList.Count < 2)
		{
			return null; // Need at least 2 versions to merge
		}

		// Group files by hash to find unique versions
		IReadOnlyCollection<FileGroup> fileGroups = FileDiffer.GroupFilesByHash(files, fileSystem);
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
	/// <param name="fileSystem">File system abstraction (optional)</param>
	/// <returns>The manually merged result, or null if cancelled</returns>
	public static MergeResult? PerformMergeWithConflictResolution(
		string file1,
		string file2,
		string? existingMergedContent,
		Func<DiffPlex.Model.DiffBlock, BlockContext, int, BlockChoice> blockChoiceCallback,
		IFileSystem? fileSystem = null)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);
		ArgumentNullException.ThrowIfNull(blockChoiceCallback);

		// Use provided fileSystem or get the default one
		fileSystem ??= FileSystemProvider.Current;

		string[] lines1;
		string[] lines2;

		if (existingMergedContent != null)
		{
			// Merge with existing content
			lines1 = existingMergedContent.Split(Environment.NewLine);
			lines2 = fileSystem.File.ReadAllLines(file2);
		}
		else
		{
			// Merge two files
			lines1 = fileSystem.File.ReadAllLines(file1);
			lines2 = fileSystem.File.ReadAllLines(file2);
		}

		return BlockMerger.PerformManualBlockSelection(lines1, lines2, blockChoiceCallback);
	}
}
