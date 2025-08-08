// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

using DiffPlex.Model;
using ktsu.BlastMerge.Models;
using ktsu.FileSystemProvider;

/// <summary>
/// Interface for DiffPlex helper operations that provides ProjectDirector-style diff functionality
/// </summary>
public interface IDiffPlexHelper
{
	/// <summary>
	/// Creates a line-based diff result between two files using DiffPlex directly
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="fileSystemProvider">File system abstraction</param>
	/// <returns>DiffPlex DiffResult with DiffBlocks</returns>
	public DiffResult CreateLineDiffs(string file1, string file2, IFileSystemProvider fileSystemProvider);

	/// <summary>
	/// Creates a line-based diff result between two strings using DiffPlex directly
	/// </summary>
	/// <param name="content1">Content of the first version</param>
	/// <param name="content2">Content of the second version</param>
	/// <returns>DiffPlex DiffResult with DiffBlocks</returns>
	public DiffResult CreateLineDiffsFromContent(string content1, string content2);

	/// <summary>
	/// Gets context lines around a diff block (ProjectDirector style)
	/// </summary>
	/// <param name="linesOld">Lines from the old version</param>
	/// <param name="linesNew">Lines from the new version</param>
	/// <param name="block">The diff block</param>
	/// <param name="contextSize">Number of context lines to include</param>
	/// <returns>Context lines before and after the block</returns>
	public BlockContext GetBlockContext(string[] linesOld, string[] linesNew, DiffBlock block, int contextSize = 3);

	/// <summary>
	/// Gets lines in a specific range from an array
	/// </summary>
	/// <param name="lines">The source lines</param>
	/// <param name="start">Start index (inclusive)</param>
	/// <param name="endIndex">End index (exclusive)</param>
	/// <returns>Array of lines in the specified range</returns>
	public string[] GetLinesInRange(string[] lines, int start, int endIndex);

	/// <summary>
	/// Applies a "take left" operation for a specific diff block (ProjectDirector style)
	/// </summary>
	/// <param name="linesOld">Lines from the old version</param>
	/// <param name="linesNew">Lines from the new version</param>
	/// <param name="block">The diff block to apply</param>
	/// <returns>New content with the left side applied</returns>
	public string ApplyTakeLeft(string[] linesOld, string[] linesNew, DiffBlock block);

	/// <summary>
	/// Applies a "take right" operation for a specific diff block (ProjectDirector style)
	/// </summary>
	/// <param name="linesOld">Lines from the old version</param>
	/// <param name="linesNew">Lines from the new version</param>
	/// <param name="block">The diff block to apply</param>
	/// <returns>New content with the right side applied</returns>
	public string ApplyTakeRight(string[] linesOld, string[] linesNew, DiffBlock block);

	/// <summary>
	/// Calculates diff statistics similar to ProjectDirector
	/// </summary>
	/// <param name="diffResult">The DiffPlex DiffResult</param>
	/// <returns>Diff statistics</returns>
	public DiffStatistics CalculateDiffStatistics(DiffResult diffResult);

	/// <summary>
	/// Creates a formatted diff summary similar to ProjectDirector
	/// </summary>
	/// <param name="additions">Number of added lines</param>
	/// <param name="deletions">Number of deleted lines</param>
	/// <param name="modifications">Number of modified sections</param>
	/// <returns>Formatted statistics string</returns>
	public string FormatDiffStatistics(int additions, int deletions, int modifications);
}
