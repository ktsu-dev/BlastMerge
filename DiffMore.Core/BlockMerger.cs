// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

/// <summary>
/// Represents a block type for manual selection
/// </summary>
public enum BlockType
{
	/// <summary>
	/// Content only exists in version 2 (insertion)
	/// </summary>
	Insert,

	/// <summary>
	/// Content only exists in version 1 (deletion)
	/// </summary>
	Delete,

	/// <summary>
	/// Content differs between versions (replacement)
	/// </summary>
	Replace
}

/// <summary>
/// Represents a diff block for manual selection
/// </summary>
public class DiffBlock
{
	/// <summary>
	/// Gets or sets the type of this block
	/// </summary>
	public BlockType Type { get; set; }

	/// <summary>
	/// Gets the lines from version 1
	/// </summary>
	public Collection<string> Lines1 { get; } = [];

	/// <summary>
	/// Gets the lines from version 2
	/// </summary>
	public Collection<string> Lines2 { get; } = [];

	/// <summary>
	/// Gets the line numbers from version 1
	/// </summary>
	public Collection<int> LineNumbers1 { get; } = [];

	/// <summary>
	/// Gets the line numbers from version 2
	/// </summary>
	public Collection<int> LineNumbers2 { get; } = [];

	/// <summary>
	/// Gets the last line number from version 1
	/// </summary>
	public int LastLineNumber1 => LineNumbers1.Count > 0 ? LineNumbers1.Last() : 0;

	/// <summary>
	/// Gets the last line number from version 2
	/// </summary>
	public int LastLineNumber2 => LineNumbers2.Count > 0 ? LineNumbers2.Last() : 0;
}

/// <summary>
/// Represents context lines around a block
/// </summary>
public class BlockContext
{
	/// <summary>
	/// Gets or sets the context lines before the block in version 1
	/// </summary>
	public IReadOnlyCollection<string> ContextBefore1 { get; set; } = [];

	/// <summary>
	/// Gets or sets the context lines after the block in version 1
	/// </summary>
	public IReadOnlyCollection<string> ContextAfter1 { get; set; } = [];

	/// <summary>
	/// Gets or sets the context lines before the block in version 2
	/// </summary>
	public IReadOnlyCollection<string> ContextBefore2 { get; set; } = [];

	/// <summary>
	/// Gets or sets the context lines after the block in version 2
	/// </summary>
	public IReadOnlyCollection<string> ContextAfter2 { get; set; } = [];
}

/// <summary>
/// Represents the user's choice for a merge block
/// </summary>
public enum BlockChoice
{
	/// <summary>
	/// Use content from version 1
	/// </summary>
	UseVersion1,

	/// <summary>
	/// Use content from version 2
	/// </summary>
	UseVersion2,

	/// <summary>
	/// Include content from both versions
	/// </summary>
	UseBoth,

	/// <summary>
	/// Skip this content entirely
	/// </summary>
	Skip,

	/// <summary>
	/// Include the addition
	/// </summary>
	Include,

	/// <summary>
	/// Keep the content (don't delete)
	/// </summary>
	Keep,

	/// <summary>
	/// Remove the content (confirm deletion)
	/// </summary>
	Remove
}

/// <summary>
/// Provides block-based merging functionality
/// </summary>
public static class BlockMerger
{
	/// <summary>
	/// Performs manual block-by-block selection for merging
	/// </summary>
	/// <param name="lines1">Lines from version 1</param>
	/// <param name="lines2">Lines from version 2</param>
	/// <param name="blockChoiceCallback">Callback function to get user's choice for each block</param>
	/// <returns>The manually merged result</returns>
	public static MergeResult PerformManualBlockSelection(string[] lines1, string[] lines2,
		Func<DiffBlock, BlockContext, int, BlockChoice> blockChoiceCallback)
	{
		ArgumentNullException.ThrowIfNull(lines1);
		ArgumentNullException.ThrowIfNull(lines2);
		ArgumentNullException.ThrowIfNull(blockChoiceCallback);

		// Create temporary files to use with FileDiffer
		var tempFile1 = Path.GetTempFileName();
		var tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			var differences = FileDiffer.FindDifferences(tempFile1, tempFile2);
			var mergedLines = new List<string>();
			var conflicts = new List<MergeConflict>();

			var blockNumber = 1;
			var lastProcessedLine1 = 0;
			var lastProcessedLine2 = 0;

			// Convert differences to our custom block format
			var blocks = ConvertDifferencesToBlocks(differences);

			foreach (var block in blocks)
			{
				// Add unchanged lines between blocks (equal content)
				AddEqualLinesBetweenBlocks(lines1, lines2, ref lastProcessedLine1, ref lastProcessedLine2,
					block, mergedLines);

				// Get context for this block
				var context = GetContextLines(lines1, lines2, block, 3);

				// Get user's choice for this block
				var choice = blockChoiceCallback(block, context, blockNumber);

				// Apply the user's choice
				ApplyBlockChoice(block, choice, mergedLines);

				// Update last processed line numbers
				if (block.LineNumbers1.Count > 0)
				{
					lastProcessedLine1 = block.LineNumbers1.Max();
				}
				if (block.LineNumbers2.Count > 0)
				{
					lastProcessedLine2 = block.LineNumbers2.Max();
				}

				blockNumber++;
			}

			// Add any remaining equal lines after the last block
			AddRemainingEqualLines(lines1, lines2, lastProcessedLine1, lastProcessedLine2, mergedLines);

			return new MergeResult
			{
				MergedLines = mergedLines.AsReadOnly(),
				Conflicts = conflicts.AsReadOnly()
			};
		}
		finally
		{
			// Clean up temporary files
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}
			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}

	/// <summary>
	/// Converts line differences to blocks for manual selection
	/// </summary>
	/// <param name="differences">The line differences to convert</param>
	/// <returns>A collection of diff blocks</returns>
	public static Collection<DiffBlock> ConvertDifferencesToBlocks(IReadOnlyCollection<LineDifference> differences)
	{
		ArgumentNullException.ThrowIfNull(differences);

		var blocks = new Collection<DiffBlock>();
		var sortedDifferences = differences.OrderBy(d => Math.Max(d.LineNumber1 ?? 0, d.LineNumber2 ?? 0)).ToList();

		if (sortedDifferences.Count == 0)
		{
			return blocks;
		}

		var currentBlock = new DiffBlock();
		var isFirstDifference = true;

		foreach (var diff in sortedDifferences)
		{
			// If this is the first difference or it's contiguous with the previous one
			if (isFirstDifference || IsContiguousDifference(currentBlock, diff))
			{
				AddDifferenceToBlock(currentBlock, diff);
				isFirstDifference = false;
			}
			else
			{
				// Finalize the current block and start a new one
				if (currentBlock.Lines1.Count > 0 || currentBlock.Lines2.Count > 0)
				{
					blocks.Add(currentBlock);
				}

				currentBlock = new DiffBlock();
				AddDifferenceToBlock(currentBlock, diff);
			}
		}

		// Add the final block
		if (currentBlock.Lines1.Count > 0 || currentBlock.Lines2.Count > 0)
		{
			blocks.Add(currentBlock);
		}

		return blocks;
	}

	/// <summary>
	/// Gets context lines around a block
	/// </summary>
	/// <param name="lines1">Lines from version 1</param>
	/// <param name="lines2">Lines from version 2</param>
	/// <param name="block">The diff block</param>
	/// <param name="contextSize">Number of context lines to include</param>
	/// <returns>Block context containing context lines</returns>
	public static BlockContext GetContextLines(string[] lines1, string[] lines2, DiffBlock block, int contextSize = 3)
	{
		ArgumentNullException.ThrowIfNull(lines1);
		ArgumentNullException.ThrowIfNull(lines2);
		ArgumentNullException.ThrowIfNull(block);

		var startLine1 = block.LineNumbers1.Count > 0 ? block.LineNumbers1.Min() - 1 : 0;
		var endLine1 = block.LineNumbers1.Count > 0 ? block.LineNumbers1.Max() - 1 : 0;
		var startLine2 = block.LineNumbers2.Count > 0 ? block.LineNumbers2.Min() - 1 : 0;
		var endLine2 = block.LineNumbers2.Count > 0 ? block.LineNumbers2.Max() - 1 : 0;

		// Context before
		var contextBefore1 = GetLinesInRange(lines1, Math.Max(0, startLine1 - contextSize), startLine1);
		var contextAfter1 = GetLinesInRange(lines1, endLine1 + 1, Math.Min(lines1.Length, endLine1 + 1 + contextSize));

		var contextBefore2 = GetLinesInRange(lines2, Math.Max(0, startLine2 - contextSize), startLine2);
		var contextAfter2 = GetLinesInRange(lines2, endLine2 + 1, Math.Min(lines2.Length, endLine2 + 1 + contextSize));

		return new BlockContext
		{
			ContextBefore1 = contextBefore1.AsReadOnly(),
			ContextAfter1 = contextAfter1.AsReadOnly(),
			ContextBefore2 = contextBefore2.AsReadOnly(),
			ContextAfter2 = contextAfter2.AsReadOnly()
		};
	}

	/// <summary>
	/// Gets lines in a specific range from an array
	/// </summary>
	/// <param name="lines">The source lines</param>
	/// <param name="start">Start index (inclusive)</param>
	/// <param name="end">End index (exclusive)</param>
	/// <returns>Array of lines in the specified range</returns>
	public static string[] GetLinesInRange(string[] lines, int start, int end)
	{
		ArgumentNullException.ThrowIfNull(lines);

		if (start >= end || start >= lines.Length || end <= 0)
		{
			return [];
		}

		var actualStart = Math.Max(0, start);
		var actualEnd = Math.Min(lines.Length, end);
		var result = new string[actualEnd - actualStart];

		Array.Copy(lines, actualStart, result, 0, actualEnd - actualStart);
		return result;
	}

	/// <summary>
	/// Adds unchanged lines between blocks
	/// </summary>
	private static void AddEqualLinesBetweenBlocks(string[] lines1, string[] lines2, ref int lastProcessedLine1,
		ref int lastProcessedLine2, DiffBlock block, List<string> mergedLines)
	{
		var nextLine1 = block.LineNumbers1.Count > 0 ? block.LineNumbers1.Min() : lines1.Length + 1;
		var nextLine2 = block.LineNumbers2.Count > 0 ? block.LineNumbers2.Min() : lines2.Length + 1;

		// Add equal lines from the last processed line to the current block
		var equalLinesEnd1 = Math.Min(nextLine1 - 1, lines1.Length);
		var equalLinesEnd2 = Math.Min(nextLine2 - 1, lines2.Length);

		for (var i = lastProcessedLine1; i < equalLinesEnd1 && i < equalLinesEnd2; i++)
		{
			if (i < lines1.Length && i < lines2.Length && lines1[i] == lines2[i])
			{
				mergedLines.Add(lines1[i]);
			}
		}

		// Update the last processed line for version 2 as well
		lastProcessedLine2 = Math.Max(lastProcessedLine2, equalLinesEnd2);
	}

	/// <summary>
	/// Adds remaining equal lines after all blocks have been processed
	/// </summary>
	private static void AddRemainingEqualLines(string[] lines1, string[] lines2, int lastProcessedLine1,
		int lastProcessedLine2, List<string> mergedLines)
	{
		var remainingLines = Math.Min(lines1.Length - lastProcessedLine1, lines2.Length - lastProcessedLine2);

		for (var i = 0; i < remainingLines; i++)
		{
			var line1Index = lastProcessedLine1 + i;
			var line2Index = lastProcessedLine2 + i;

			if (line1Index < lines1.Length && line2Index < lines2.Length &&
				lines1[line1Index] == lines2[line2Index])
			{
				mergedLines.Add(lines1[line1Index]);
			}
		}
	}

	/// <summary>
	/// Checks if a difference is contiguous with the current block
	/// </summary>
	private static bool IsContiguousDifference(DiffBlock currentBlock, LineDifference diff)
	{
		if (currentBlock.Lines1.Count == 0 && currentBlock.Lines2.Count == 0)
		{
			return true;
		}

		// Get the last line numbers from the current block
		var lastLine1 = currentBlock.LastLineNumber1;
		var lastLine2 = currentBlock.LastLineNumber2;

		// Consider differences contiguous if they're within 1 line of each other
		var isContiguous1 = !diff.LineNumber1.HasValue || lastLine1 <= 0 || Math.Abs((diff.LineNumber1 ?? 0) - lastLine1) <= 1;
		var isContiguous2 = !diff.LineNumber2.HasValue || lastLine2 <= 0 || Math.Abs((diff.LineNumber2 ?? 0) - lastLine2) <= 1;

		return isContiguous1 && isContiguous2;
	}

	/// <summary>
	/// Adds a difference to the current block
	/// </summary>
	private static void AddDifferenceToBlock(DiffBlock currentBlock, LineDifference diff)
	{
		if (diff.LineNumber1.HasValue && diff.LineNumber1 > 0 && !string.IsNullOrEmpty(diff.Content1))
		{
			currentBlock.Lines1.Add(diff.Content1);
			currentBlock.LineNumbers1.Add(diff.LineNumber1.Value);
		}

		if (diff.LineNumber2.HasValue && diff.LineNumber2 > 0 && !string.IsNullOrEmpty(diff.Content2))
		{
			currentBlock.Lines2.Add(diff.Content2);
			currentBlock.LineNumbers2.Add(diff.LineNumber2.Value);
		}

		// Determine block type
		if (currentBlock.Lines1.Count > 0 && currentBlock.Lines2.Count > 0)
		{
			currentBlock.Type = BlockType.Replace;
		}
		else if (currentBlock.Lines1.Count > 0)
		{
			currentBlock.Type = BlockType.Delete;
		}
		else if (currentBlock.Lines2.Count > 0)
		{
			currentBlock.Type = BlockType.Insert;
		}
	}

	/// <summary>
	/// Applies the user's block choice to the merged lines
	/// </summary>
	private static void ApplyBlockChoice(DiffBlock block, BlockChoice choice, List<string> mergedLines)
	{
		switch (block.Type)
		{
			case BlockType.Insert:
				ApplyInsertChoice(block, choice, mergedLines);
				break;
			case BlockType.Delete:
				ApplyDeleteChoice(block, choice, mergedLines);
				break;
			case BlockType.Replace:
				ApplyReplaceChoice(block, choice, mergedLines);
				break;
			default:
				throw new InvalidOperationException($"Unknown block type: {block.Type}");
		}
	}

	/// <summary>
	/// Applies choice for insertion blocks
	/// </summary>
	private static void ApplyInsertChoice(DiffBlock block, BlockChoice choice, List<string> mergedLines)
	{
		switch (choice)
		{
			case BlockChoice.Include:
				mergedLines.AddRange(block.Lines2);
				break;
			case BlockChoice.Skip:
				// Don't add anything
				break;
			case BlockChoice.UseVersion1:
			case BlockChoice.UseVersion2:
			case BlockChoice.UseBoth:
			case BlockChoice.Keep:
			case BlockChoice.Remove:
			default:
				throw new InvalidOperationException($"Invalid choice {choice} for insertion block");
		}
	}

	/// <summary>
	/// Applies choice for deletion blocks
	/// </summary>
	private static void ApplyDeleteChoice(DiffBlock block, BlockChoice choice, List<string> mergedLines)
	{
		switch (choice)
		{
			case BlockChoice.Keep:
				mergedLines.AddRange(block.Lines1);
				break;
			case BlockChoice.Remove:
				// Don't add anything
				break;
			case BlockChoice.UseVersion1:
			case BlockChoice.UseVersion2:
			case BlockChoice.UseBoth:
			case BlockChoice.Include:
			case BlockChoice.Skip:
			default:
				throw new InvalidOperationException($"Invalid choice {choice} for deletion block");
		}
	}

	/// <summary>
	/// Applies choice for replacement blocks
	/// </summary>
	private static void ApplyReplaceChoice(DiffBlock block, BlockChoice choice, List<string> mergedLines)
	{
		switch (choice)
		{
			case BlockChoice.UseVersion1:
				mergedLines.AddRange(block.Lines1);
				break;
			case BlockChoice.UseVersion2:
				mergedLines.AddRange(block.Lines2);
				break;
			case BlockChoice.UseBoth:
				mergedLines.AddRange(block.Lines1);
				mergedLines.AddRange(block.Lines2);
				break;
			case BlockChoice.Skip:
				// Don't add anything
				break;
			case BlockChoice.Include:
			case BlockChoice.Keep:
			case BlockChoice.Remove:
			default:
				throw new InvalidOperationException($"Invalid choice {choice} for replacement block");
		}
	}
}
