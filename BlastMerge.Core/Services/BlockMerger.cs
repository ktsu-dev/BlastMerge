// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Collections.Generic;
using DiffPlex.Model;
using ktsu.BlastMerge.Core.Models;

/// <summary>
/// Provides improved block-based merging functionality using DiffPlex directly
/// </summary>
public static class BlockMerger
{
	/// <summary>
	/// Performs manual block-by-block selection for merging using DiffPlex directly
	/// </summary>
	/// <param name="lines1">Lines from version 1</param>
	/// <param name="lines2">Lines from version 2</param>
	/// <param name="blockChoiceCallback">Callback function to get user's choice for each block</param>
	/// <returns>The manually merged result</returns>
	public static MergeResult PerformManualBlockSelection(string[] lines1, string[] lines2,
		Func<DiffPlex.Model.DiffBlock, BlockContext, int, BlockChoice> blockChoiceCallback)
	{
		ArgumentNullException.ThrowIfNull(lines1);
		ArgumentNullException.ThrowIfNull(lines2);
		ArgumentNullException.ThrowIfNull(blockChoiceCallback);

		string content1 = string.Join(Environment.NewLine, lines1);
		string content2 = string.Join(Environment.NewLine, lines2);

		// Use DiffPlex directly to get proper diff blocks
		DiffResult diffResult = DiffPlexHelper.CreateLineDiffsFromContent(content1, content2);

		List<string> mergedLines = [];
		List<MergeConflict> conflicts = [];
		int blockNumber = 1;

		// Track current position in both files
		int currentPos1 = 0;
		int currentPos2 = 0;

		foreach (DiffPlex.Model.DiffBlock diffBlock in diffResult.DiffBlocks)
		{
			// Add unchanged content before this block
			AddUnchangedContentBeforeBlock(lines1, ref currentPos1, diffBlock, mergedLines);

			// Get context for this block using DiffPlexHelper
			(string[] contextBefore1, string[] contextAfter1, string[] contextBefore2, string[] contextAfter2) =
				DiffPlexHelper.GetBlockContext(lines1, lines2, diffBlock, 3);

			// Create context object for callback
			BlockContext context = new()
			{
				ContextBefore1 = contextBefore1.AsReadOnly(),
				ContextAfter1 = contextAfter1.AsReadOnly(),
				ContextBefore2 = contextBefore2.AsReadOnly(),
				ContextAfter2 = contextAfter2.AsReadOnly()
			};

			// Get user's choice for this block
			BlockChoice choice = blockChoiceCallback(diffBlock, context, blockNumber);

			// Apply the user's choice using DiffPlexHelper
			ApplyDiffBlockChoice(lines1, lines2, diffBlock, choice, mergedLines);

			// Update current positions
			currentPos1 = diffBlock.DeleteStartA + diffBlock.DeleteCountA;
			currentPos2 = diffBlock.InsertStartB + diffBlock.InsertCountB;

			blockNumber++;
		}

		// Add any remaining unchanged content after the last block
		AddRemainingUnchangedContent(lines1, currentPos1, mergedLines);

		return new MergeResult(mergedLines.AsReadOnly(), conflicts.AsReadOnly());
	}

	/// <summary>
	/// Adds unchanged content before a diff block
	/// </summary>
	private static void AddUnchangedContentBeforeBlock(string[] lines1,
		ref int currentPos1, DiffPlex.Model.DiffBlock diffBlock, List<string> mergedLines)
	{
		// Add unchanged lines that appear before this diff block
		int endPos1 = diffBlock.DeleteStartA;

		// The unchanged content should be the same in both files, so we can take from either
		// We'll use the lines from file 1 up to the start of the delete operation
		for (int i = currentPos1; i < endPos1 && i < lines1.Length; i++)
		{
			mergedLines.Add(lines1[i]);
		}
	}

	/// <summary>
	/// Adds remaining unchanged content after the last diff block
	/// </summary>
	private static void AddRemainingUnchangedContent(string[] lines1,
		int currentPos1, List<string> mergedLines)
	{
		// Add any remaining unchanged lines from the end of the file
		// Since the content should be the same, we can take from either file
		// We'll use file 1
		for (int i = currentPos1; i < lines1.Length; i++)
		{
			mergedLines.Add(lines1[i]);
		}
	}

	/// <summary>
	/// Applies the user's choice for a DiffPlex diff block
	/// </summary>
	private static void ApplyDiffBlockChoice(string[] lines1, string[] lines2,
		DiffPlex.Model.DiffBlock diffBlock, BlockChoice choice, List<string> mergedLines)
	{
		switch (choice)
		{
			case BlockChoice.UseVersion1:
				// Take the deleted lines from the left (original) version
				for (int i = diffBlock.DeleteStartA; i < diffBlock.DeleteStartA + diffBlock.DeleteCountA && i < lines1.Length; i++)
				{
					mergedLines.Add(lines1[i]);
				}
				break;

			case BlockChoice.UseVersion2:
				// Take the inserted lines from the right (new) version
				for (int i = diffBlock.InsertStartB; i < diffBlock.InsertStartB + diffBlock.InsertCountB && i < lines2.Length; i++)
				{
					mergedLines.Add(lines2[i]);
				}
				break;

			case BlockChoice.UseBoth:
				// Take deleted lines first, then inserted lines
				for (int i = diffBlock.DeleteStartA; i < diffBlock.DeleteStartA + diffBlock.DeleteCountA && i < lines1.Length; i++)
				{
					mergedLines.Add(lines1[i]);
				}
				for (int i = diffBlock.InsertStartB; i < diffBlock.InsertStartB + diffBlock.InsertCountB && i < lines2.Length; i++)
				{
					mergedLines.Add(lines2[i]);
				}
				break;

			case BlockChoice.Skip:
				// Skip both - don't add any lines from this block
				break;

			case BlockChoice.Include:
			case BlockChoice.Keep:
			case BlockChoice.Remove:
			default:
				// For compatibility with other block types, default to UseVersion2
				for (int i = diffBlock.InsertStartB; i < diffBlock.InsertStartB + diffBlock.InsertCountB && i < lines2.Length; i++)
				{
					mergedLines.Add(lines2[i]);
				}
				break;
		}
	}
}
