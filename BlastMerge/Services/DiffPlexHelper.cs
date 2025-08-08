// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using DiffPlex;
using DiffPlex.Model;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.FileSystemProvider;

/// <summary>
/// Helper class that provides ProjectDirector-style diff functionality using DiffPlex directly
/// </summary>
public class DiffPlexHelper : IDiffPlexHelper
{
	/// <summary>
	/// Creates a line-based diff result between two files using DiffPlex directly
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="fileSystemProvider">File system abstraction</param>
	/// <returns>DiffPlex DiffResult with DiffBlocks</returns>
	public DiffResult CreateLineDiffs(string file1, string file2, IFileSystemProvider fileSystemProvider)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);
		ArgumentNullException.ThrowIfNull(fileSystemProvider);

		IFileSystem fileSystem = fileSystemProvider.Current;

		if (!fileSystem.File.Exists(file1) || !fileSystem.File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		string content1 = fileSystem.File.ReadAllText(file1);
		string content2 = fileSystem.File.ReadAllText(file2);

		return Differ.Instance.CreateLineDiffs(content1, content2, ignoreWhitespace: false, ignoreCase: false);
	}

	/// <summary>
	/// Creates a line-based diff result between two strings using DiffPlex directly
	/// </summary>
	/// <param name="content1">Content of the first version</param>
	/// <param name="content2">Content of the second version</param>
	/// <returns>DiffPlex DiffResult with DiffBlocks</returns>
	public DiffResult CreateLineDiffsFromContent(string content1, string content2)
	{
		ArgumentNullException.ThrowIfNull(content1);
		ArgumentNullException.ThrowIfNull(content2);

		return Differ.Instance.CreateLineDiffs(content1, content2, ignoreWhitespace: false, ignoreCase: false);
	}

	/// <summary>
	/// Gets context lines around a diff block (ProjectDirector style)
	/// </summary>
	/// <param name="linesOld">Lines from the old version</param>
	/// <param name="linesNew">Lines from the new version</param>
	/// <param name="block">The diff block</param>
	/// <param name="contextSize">Number of context lines to include</param>
	/// <returns>Context lines before and after the block</returns>
	public BlockContext GetBlockContext(string[] linesOld, string[] linesNew, DiffBlock block, int contextSize = 3)
	{
		ArgumentNullException.ThrowIfNull(linesOld);
		ArgumentNullException.ThrowIfNull(linesNew);
		ArgumentNullException.ThrowIfNull(block);

		// Context before the block
		int startBefore1 = Math.Max(0, block.DeleteStartA - contextSize);
		int endBefore1 = block.DeleteStartA;
		string[] contextBefore1 = GetLinesInRange(linesOld, startBefore1, endBefore1);

		int startBefore2 = Math.Max(0, block.InsertStartB - contextSize);
		int endBefore2 = block.InsertStartB;
		string[] contextBefore2 = GetLinesInRange(linesNew, startBefore2, endBefore2);

		// Context after the block
		int startAfter1 = block.DeleteStartA + block.DeleteCountA;
		int endAfter1 = Math.Min(linesOld.Length, startAfter1 + contextSize);
		string[] contextAfter1 = GetLinesInRange(linesOld, startAfter1, endAfter1);

		int startAfter2 = block.InsertStartB + block.InsertCountB;
		int endAfter2 = Math.Min(linesNew.Length, startAfter2 + contextSize);
		string[] contextAfter2 = GetLinesInRange(linesNew, startAfter2, endAfter2);

		return BlockContext.Create(contextBefore1, contextAfter1, contextBefore2, contextAfter2);
	}

	/// <summary>
	/// Gets lines in a specific range from an array
	/// </summary>
	/// <param name="lines">The source lines</param>
	/// <param name="start">Start index (inclusive)</param>
	/// <param name="endIndex">End index (exclusive)</param>
	/// <returns>Array of lines in the specified range</returns>
	public string[] GetLinesInRange(string[] lines, int start, int endIndex)
	{
		ArgumentNullException.ThrowIfNull(lines);

		if (start >= endIndex || start >= lines.Length || endIndex <= 0)
		{
			return [];
		}

		start = Math.Max(0, start);
		endIndex = Math.Min(lines.Length, endIndex);

		return lines[start..endIndex];
	}

	/// <summary>
	/// Applies a "take left" operation for a specific diff block (ProjectDirector style)
	/// </summary>
	/// <param name="linesOld">Lines from the old version</param>
	/// <param name="linesNew">Lines from the new version</param>
	/// <param name="block">The diff block to apply</param>
	/// <returns>New content with the left side applied</returns>
	public string ApplyTakeLeft(string[] linesOld, string[] linesNew, DiffBlock block)
	{
		ArgumentNullException.ThrowIfNull(linesOld);
		ArgumentNullException.ThrowIfNull(linesNew);
		ArgumentNullException.ThrowIfNull(block);

		List<string> newLines = [];

		// Add prologue (everything before the change in the new version)
		int endIndex = block.InsertStartB;
		newLines.AddRange(linesNew[..endIndex]);

		// Add the deleted lines from the old version
		int startIndex = block.DeleteStartA;
		int endDeleteIndex = startIndex + block.DeleteCountA;
		newLines.AddRange(linesOld[startIndex..endDeleteIndex]);

		// Add epilogue (everything after the change in the new version)
		int startEpilogueIndex = block.InsertStartB + block.InsertCountB;
		newLines.AddRange(linesNew[startEpilogueIndex..]);

		return string.Join(Environment.NewLine, newLines);
	}

	/// <summary>
	/// Applies a "take right" operation for a specific diff block (ProjectDirector style)
	/// </summary>
	/// <param name="linesOld">Lines from the old version</param>
	/// <param name="linesNew">Lines from the new version</param>
	/// <param name="block">The diff block to apply</param>
	/// <returns>New content with the right side applied</returns>
	public string ApplyTakeRight(string[] linesOld, string[] linesNew, DiffBlock block)
	{
		ArgumentNullException.ThrowIfNull(linesOld);
		ArgumentNullException.ThrowIfNull(linesNew);
		ArgumentNullException.ThrowIfNull(block);

		List<string> newLines = [];

		// Add prologue (everything before the change in the old version)
		int endIndex = block.DeleteStartA;
		newLines.AddRange(linesOld[..endIndex]);

		// Add the new lines from the new version
		int startIndex = block.InsertStartB;
		int endInsertIndex = startIndex + block.InsertCountB;
		newLines.AddRange(linesNew[startIndex..endInsertIndex]);

		// Add epilogue (everything after the change in the old version)
		int startEpilogueIndex = block.DeleteStartA + block.DeleteCountA;
		newLines.AddRange(linesOld[startEpilogueIndex..]);

		return string.Join(Environment.NewLine, newLines);
	}

	/// <summary>
	/// Calculates diff statistics similar to ProjectDirector
	/// </summary>
	/// <param name="diffResult">The DiffPlex DiffResult</param>
	/// <returns>Diff statistics</returns>
	public DiffStatistics CalculateDiffStatistics(DiffResult diffResult)
	{
		ArgumentNullException.ThrowIfNull(diffResult);

		int additions = diffResult.DiffBlocks.Sum(b => b.InsertCountB);
		int deletions = diffResult.DiffBlocks.Sum(b => b.DeleteCountA);
		int modifications = diffResult.DiffBlocks.Count(b => b.InsertCountB > 0 && b.DeleteCountA > 0);

		return new DiffStatistics
		{
			Additions = additions,
			Deletions = deletions,
			Modifications = modifications
		};
	}

	/// <summary>
	/// Creates a formatted diff summary similar to ProjectDirector
	/// </summary>
	/// <param name="additions">Number of added lines</param>
	/// <param name="deletions">Number of deleted lines</param>
	/// <param name="modifications">Number of modified sections</param>
	/// <returns>Formatted statistics string</returns>
	public string FormatDiffStatistics(int additions, int deletions, int modifications)
	{
		List<string> parts = [];

		if (additions > 0)
		{
			parts.Add($"+{additions}");
		}

		if (deletions > 0)
		{
			parts.Add($"-{deletions}");
		}

		if (modifications > 0)
		{
			parts.Add($"~{modifications}");
		}

		return string.Join(" ", parts);
	}
}
