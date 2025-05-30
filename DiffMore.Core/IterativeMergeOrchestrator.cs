// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Core;

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

		// Create a list of representative files (one from each group)
		var filesToMerge = fileGroups.Select(g => g.FilePaths.First()).ToList();
		var session = new IterativeMergeSession(filesToMerge);

		var mergeCount = 1;
		string? lastMergedContent = null;
		string? mergedFilePath = null; // Track the file containing merged content

		while (session.RemainingFiles.Count > 1)
		{
			FileSimilarity? similarity;

			if (lastMergedContent != null && mergedFilePath != null)
			{
				// Find the most similar file to our current merged result
				similarity = FindMostSimilarToMergedContent(session.RemainingFiles, lastMergedContent);

				// Update the similarity to use the merged file path as file1
				if (similarity != null)
				{
					similarity = new FileSimilarity
					{
						FilePath1 = mergedFilePath,
						FilePath2 = similarity.FilePath2,
						SimilarityScore = similarity.SimilarityScore
					};
				}
			}
			else
			{
				// Find the two most similar files from remaining files
				var remainingGroups = fileGroups.Where(g => session.RemainingFiles.Contains(g.FilePaths.First())).ToList();
				similarity = FileDiffer.FindMostSimilarFiles(remainingGroups);
			}

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
				RemainingFilesCount = session.RemainingFiles.Count,
				CompletedMergesCount = session.MergedContents.Count,
				MostSimilarPair = similarity
			};
			statusCallback(status);

			// Perform the merge
			var mergeResult = mergeCallback(similarity.FilePath1, similarity.FilePath2, lastMergedContent);

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

			// Update session
			lastMergedContent = string.Join(Environment.NewLine, mergeResult.MergedLines);
			session.AddMergedContent(lastMergedContent);

			// Replace both input files with the merged output
			try
			{
				// Choose which file to keep (use the first one as the target)
				var targetFile = similarity.FilePath1;
				var fileToDelete = similarity.FilePath2;

				// Write merged content to the target file
				File.WriteAllText(targetFile, lastMergedContent);

				// Delete the second file
				if (File.Exists(fileToDelete))
				{
					File.Delete(fileToDelete);
				}

				// Update tracking
				mergedFilePath = targetFile;
				session.RemoveFile(fileToDelete);

				// Keep the target file in the session for the next iteration
				// (it will be automatically part of RemainingFiles since we only removed fileToDelete)
			}
			catch (IOException ex)
			{
				return new MergeCompletionResult
				{
					IsSuccessful = false,
					FinalMergedContent = lastMergedContent,
					FinalLineCount = lastMergedContent?.Split(Environment.NewLine).Length ?? 0,
					OriginalFileName = $"error: {ex.Message}"
				};
			}
			catch (UnauthorizedAccessException ex)
			{
				return new MergeCompletionResult
				{
					IsSuccessful = false,
					FinalMergedContent = lastMergedContent,
					FinalLineCount = lastMergedContent?.Split(Environment.NewLine).Length ?? 0,
					OriginalFileName = $"access denied: {ex.Message}"
				};
			}

			mergeCount++;

			// Check if user wants to continue (if there are more files to merge)
			if (session.RemainingFiles.Count > 1 && !continuationCallback())
			{
				return new MergeCompletionResult
				{
					IsSuccessful = false,
					FinalMergedContent = lastMergedContent,
					FinalLineCount = lastMergedContent?.Split(Environment.NewLine).Length ?? 0,
					OriginalFileName = "incomplete"
				};
			}
		}

		// Merge completed successfully
		var finalLines = lastMergedContent?.Split(Environment.NewLine) ?? [];
		var finalFilePath = mergedFilePath ?? (session.RemainingFiles.Count > 0 ? session.RemainingFiles[0] : "unknown");

		return new MergeCompletionResult
		{
			IsSuccessful = true,
			FinalMergedContent = lastMergedContent,
			FinalLineCount = finalLines.Length,
			OriginalFileName = Path.GetFileName(finalFilePath)
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

	/// <summary>
	/// Finds the file most similar to the current merged content
	/// </summary>
	/// <param name="remainingFiles">List of remaining files to consider</param>
	/// <param name="mergedContent">The current merged content</param>
	/// <returns>A FileSimilarity object with the most similar file</returns>
	private static FileSimilarity? FindMostSimilarToMergedContent(IReadOnlyList<string> remainingFiles, string mergedContent)
	{
		var mergedLines = mergedContent.Split(Environment.NewLine);
		FileSimilarity? mostSimilar = null;
		var highestSimilarity = -1.0;

		foreach (var file in remainingFiles)
		{
			var fileLines = File.ReadAllLines(file);
			var similarity = FileDiffer.CalculateLineSimilarity(mergedLines, fileLines);

			if (similarity > highestSimilarity)
			{
				highestSimilarity = similarity;
				mostSimilar = new FileSimilarity
				{
					FilePath1 = "<merged_content>", // Placeholder - will be updated by caller
					FilePath2 = file,
					SimilarityScore = similarity
				};
			}
		}

		return mostSimilar;
	}
}
