// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Model;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.FileSystemProvider;

/// <summary>
/// Provides diffing functionality using DiffPlex library
/// </summary>
/// <param name="fileSystemProvider">File system provider for file operations</param>
/// <param name="diffPlexHelper">Helper for DiffPlex operations</param>
public class DiffPlexDiffer(IFileSystemProvider fileSystemProvider, IDiffPlexHelper diffPlexHelper)
{
	/// <summary>
	/// In-memory API: find differences from arrays of lines
	/// </summary>
	public static IReadOnlyCollection<LineDifference> FindDifferencesFromLines(string[] lines1, string[] lines2)
	{
		ArgumentNullException.ThrowIfNull(lines1);
		ArgumentNullException.ThrowIfNull(lines2);
		Differ d = new();
		DiffResult result = d.CreateLineDiffs(string.Join('\n', lines1), string.Join('\n', lines2), ignoreWhitespace: true);
		return FileDiffer.FindDifferencesFromDiffResult(result, lines1, lines2);
	}

	/// <summary>
	/// In-memory API: find differences from content strings
	/// </summary>
	public static IReadOnlyCollection<LineDifference> FindDifferencesFromContent(string content1, string content2)
	{
		ArgumentNullException.ThrowIfNull(content1);
		ArgumentNullException.ThrowIfNull(content2);
		string[] lines1 = content1.Split('\n');
		string[] lines2 = content2.Split('\n');
		return FindDifferencesFromLines(lines1, lines2);
	}
	private readonly Differ differ = new();
	private const string FilesNotFoundMessage = "One or both files do not exist";

	/// <summary>
	/// Checks if two files are identical
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>True if files are identical, false otherwise</returns>
	public bool AreFilesIdentical(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!fileSystemProvider.Current.File.Exists(file1) || !fileSystemProvider.Current.File.Exists(file2))
		{
			return false;
		}

		string content1 = fileSystemProvider.Current.File.ReadAllText(file1);
		string content2 = fileSystemProvider.Current.File.ReadAllText(file2);

		DiffResult diff = differ.CreateDiffs(content1, content2, ignoreWhiteSpace: true, ignoreCase: false, chunker: new LineChunker());
		return !diff.DiffBlocks.Any();
	}

	/// <summary>
	/// Creates a line-by-line diff between two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>DiffResult containing the differences</returns>
	/// <exception cref="ArgumentNullException">Thrown when file1 or file2 is null</exception>
	/// <exception cref="FileNotFoundException">Thrown when one or both files do not exist</exception>
	public DiffResult CreateLineDiffs(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!fileSystemProvider.Current.File.Exists(file1) || !fileSystemProvider.Current.File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		string content1 = fileSystemProvider.Current.File.ReadAllText(file1);
		string content2 = fileSystemProvider.Current.File.ReadAllText(file2);

		return diffPlexHelper.CreateLineDiffsFromContent(content1, content2);
	}

	/// <summary>
	/// Generates a unified diff between two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="contextLines">Number of context lines to include</param>
	/// <returns>Unified diff as a string</returns>
	/// <exception cref="ArgumentNullException">Thrown when file1 or file2 is null</exception>
	/// <exception cref="FileNotFoundException">Thrown when one or both files do not exist</exception>
	public string GenerateUnifiedDiff(string file1, string file2, int contextLines = 3)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!fileSystemProvider.Current.File.Exists(file1) || !fileSystemProvider.Current.File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		string content1 = fileSystemProvider.Current.File.ReadAllText(file1);
		string content2 = fileSystemProvider.Current.File.ReadAllText(file2);

		return GenerateUnifiedDiffFromContent(content1, content2, file1, file2, contextLines);
	}

	/// <summary>
	/// Generates a unified diff from content strings
	/// </summary>
	/// <param name="content1">Content of the first file</param>
	/// <param name="content2">Content of the second file</param>
	/// <param name="file1">Path to the first file (for header)</param>
	/// <param name="file2">Path to the second file (for header)</param>
	/// <param name="contextLines">Number of context lines to include</param>
	/// <returns>Unified diff as a string</returns>
	private static string GenerateUnifiedDiffFromContent(string content1, string content2, string file1, string file2, int contextLines)
	{
		if (content1 == content2)
		{
			return string.Empty;
		}

		InlineDiffBuilder diffBuilder = new();
		DiffPaneModel diff = diffBuilder.BuildDiffModel(content1, content2);

		List<string> result =
		[
			$"--- {file1}",
			$"+++ {file2}"
		];

		ProcessDiffLines(diff.Lines, result, contextLines);

		return string.Join(Environment.NewLine, result);
	}

	/// <summary>
	/// Processes diff lines and builds unified diff chunks
	/// </summary>
	/// <param name="lines">Diff lines to process</param>
	/// <param name="result">Result list to add chunks to</param>
	/// <param name="contextLines">Number of context lines</param>
	private static void ProcessDiffLines(IEnumerable<DiffPiece> lines, List<string> result, int contextLines)
	{
		int currentLine1 = 1;
		int currentLine2 = 1;
		List<string> pendingLines = [];
		List<string> contextBuffer = [];

		foreach (DiffPiece line in lines)
		{
			ProcessSingleDiffLine(line, ref currentLine1, ref currentLine2, pendingLines, contextBuffer, contextLines);

			// If we have pending changes and hit unchanged text, output the chunk
			if (line.Type == ChangeType.Unchanged && pendingLines.Count > 0)
			{
				OutputDiffChunk(pendingLines, contextBuffer, result, currentLine1, currentLine2, contextLines);
			}
		}

		// Handle any remaining pending changes
		if (pendingLines.Count > 0)
		{
			OutputFinalDiffChunk(pendingLines, result, currentLine1, currentLine2);
		}
	}

	/// <summary>
	/// Processes a single diff line and updates tracking state
	/// </summary>
	/// <param name="line">The diff line to process</param>
	/// <param name="currentLine1">Current line number in first file</param>
	/// <param name="currentLine2">Current line number in second file</param>
	/// <param name="pendingLines">List of pending diff lines</param>
	/// <param name="contextBuffer">Buffer for context lines</param>
	/// <param name="contextLines">Number of context lines</param>
	private static void ProcessSingleDiffLine(DiffPiece line, ref int currentLine1, ref int currentLine2,
		List<string> pendingLines, List<string> contextBuffer, int contextLines)
	{
		switch (line.Type)
		{
			case ChangeType.Unchanged:
				HandleUnchangedLine(line, ref currentLine1, ref currentLine2, contextBuffer, contextLines);
				break;

			case ChangeType.Deleted:
				HandleDeletedLine(line, ref currentLine1, pendingLines, contextBuffer, contextLines);
				break;

			case ChangeType.Inserted:
				HandleInsertedLine(line, ref currentLine2, pendingLines, contextBuffer, contextLines);
				break;

			case ChangeType.Modified:
				HandleModifiedLine(line, ref currentLine1, ref currentLine2, pendingLines, contextBuffer, contextLines);
				break;

			case ChangeType.Imaginary:
				// Handle imaginary lines (used for padding) - no action needed
				break;

			default:
				// Handle any other change types
				currentLine1++;
				currentLine2++;
				break;
		}
	}

	/// <summary>
	/// Handles an unchanged line by updating context buffer
	/// </summary>
	private static void HandleUnchangedLine(DiffPiece line, ref int currentLine1, ref int currentLine2,
		List<string> contextBuffer, int contextLines)
	{
		contextBuffer.Add($" {line.Text}");
		if (contextBuffer.Count > contextLines * 2)
		{
			contextBuffer.RemoveAt(0);
		}
		currentLine1++;
		currentLine2++;
	}

	/// <summary>
	/// Handles a deleted line by adding it to pending changes
	/// </summary>
	private static void HandleDeletedLine(DiffPiece line, ref int currentLine1,
		List<string> pendingLines, List<string> contextBuffer, int contextLines)
	{
		AddContextBeforeChanges(pendingLines, contextBuffer, contextLines);
		pendingLines.Add($"-{line.Text}");
		currentLine1++;
	}

	/// <summary>
	/// Handles an inserted line by adding it to pending changes
	/// </summary>
	private static void HandleInsertedLine(DiffPiece line, ref int currentLine2,
		List<string> pendingLines, List<string> contextBuffer, int contextLines)
	{
		AddContextBeforeChanges(pendingLines, contextBuffer, contextLines);
		pendingLines.Add($"+{line.Text}");
		currentLine2++;
	}

	/// <summary>
	/// Handles a modified line by treating it as deletion + insertion
	/// </summary>
	private static void HandleModifiedLine(DiffPiece line, ref int currentLine1, ref int currentLine2,
		List<string> pendingLines, List<string> contextBuffer, int contextLines)
	{
		AddContextBeforeChanges(pendingLines, contextBuffer, contextLines);
		pendingLines.Add($"-{line.Text}");
		pendingLines.Add($"+{line.Text}");
		currentLine1++;
		currentLine2++;
	}

	/// <summary>
	/// Adds context lines before changes if this is the first change in a chunk
	/// </summary>
	private static void AddContextBeforeChanges(List<string> pendingLines, List<string> contextBuffer, int contextLines)
	{
		if (pendingLines.Count == 0)
		{
			int contextStart = Math.Max(0, contextBuffer.Count - contextLines);
			pendingLines.AddRange(contextBuffer.Skip(contextStart));
		}
	}

	/// <summary>
	/// Outputs a diff chunk with header and context
	/// </summary>
	private static void OutputDiffChunk(List<string> pendingLines, List<string> contextBuffer,
		List<string> result, int currentLine1, int currentLine2, int contextLines)
	{
		// Add context after changes
		List<string> contextAfter = [.. contextBuffer.Take(Math.Min(contextLines, contextBuffer.Count))];
		pendingLines.AddRange(contextAfter);

		// Add chunk header
		(int deletedCount, int addedCount, int contextCount) = CountLineTypes(pendingLines);

		int startLine1 = currentLine1 - deletedCount - (contextCount / 2);
		int startLine2 = currentLine2 - addedCount - (contextCount / 2);

		result.Add($"@@ -{startLine1},{deletedCount + (contextCount / 2)} +{startLine2},{addedCount + (contextCount / 2)} @@");
		result.AddRange(pendingLines);

		pendingLines.Clear();
	}

	/// <summary>
	/// Outputs the final diff chunk for any remaining pending changes
	/// </summary>
	private static void OutputFinalDiffChunk(List<string> pendingLines, List<string> result,
		int currentLine1, int currentLine2)
	{
		(int deletedCount, int addedCount, int contextCount) = CountLineTypes(pendingLines);

		int startLine1 = currentLine1 - deletedCount - contextCount;
		int startLine2 = currentLine2 - addedCount - contextCount;

		result.Add($"@@ -{startLine1},{deletedCount + contextCount} +{startLine2},{addedCount + contextCount} @@");
		result.AddRange(pendingLines);
	}

	/// <summary>
	/// Counts the different types of lines in pending changes
	/// </summary>
	/// <param name="pendingLines">List of pending diff lines</param>
	/// <returns>Tuple of (deleted count, added count, context count)</returns>
	private static (int deletedCount, int addedCount, int contextCount) CountLineTypes(List<string> pendingLines)
	{
		int deletedCount = pendingLines.Count(l => l.StartsWith('-'));
		int addedCount = pendingLines.Count(l => l.StartsWith('+'));
		int contextCount = pendingLines.Count - deletedCount - addedCount;

		return (deletedCount, addedCount, contextCount);
	}

	/// <summary>
	/// Generates colored diff lines for display
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Collection of colored diff lines</returns>
	public Collection<ColoredDiffLine> GenerateColoredDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!fileSystemProvider.Current.File.Exists(file1) || !fileSystemProvider.Current.File.Exists(file2))
		{
			throw new FileNotFoundException(FilesNotFoundMessage);
		}

		string content1 = fileSystemProvider.Current.File.ReadAllText(file1);
		string content2 = fileSystemProvider.Current.File.ReadAllText(file2);

		InlineDiffBuilder diffBuilder = new();
		DiffPaneModel diff = diffBuilder.BuildDiffModel(content1, content2);
		Collection<ColoredDiffLine> result =
		[
			new($"--- {file1}", DiffColor.FileHeader),
			new($"+++ {file2}", DiffColor.FileHeader)
		];

		foreach (DiffPiece? line in diff.Lines)
		{
			DiffColor color = line.Type switch
			{
				ChangeType.Deleted => DiffColor.Deletion,
				ChangeType.Inserted => DiffColor.Addition,
				ChangeType.Modified => DiffColor.ChunkHeader,
				ChangeType.Unchanged => DiffColor.Default,
				ChangeType.Imaginary => DiffColor.Default,
				_ => DiffColor.Default
			};

			string prefix = line.Type switch
			{
				ChangeType.Deleted => "-",
				ChangeType.Inserted => "+",
				ChangeType.Modified => "~",
				ChangeType.Unchanged => " ",
				ChangeType.Imaginary => " ",
				_ => " "
			};

			result.Add(new ColoredDiffLine($"{prefix}{line.Text}", color));
		}

		return result;
	}

	/// <summary>
	/// Finds differences between two files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Collection of line differences</returns>
	public IReadOnlyCollection<LineDifference> FindDifferences(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!fileSystemProvider.Current.File.Exists(file1) || !fileSystemProvider.Current.File.Exists(file2))
		{
			throw new FileNotFoundException(FilesNotFoundMessage);
		}

		string content1 = fileSystemProvider.Current.File.ReadAllText(file1);
		string content2 = fileSystemProvider.Current.File.ReadAllText(file2);

		// Use inline diff for simpler processing
		InlineDiffBuilder inlineDiffBuilder = new();
		DiffPaneModel inlineDiff = inlineDiffBuilder.BuildDiffModel(content1, content2);
		List<LineDifference> rawDifferences = [];

		int line1Number = 1;
		int line2Number = 1;

		foreach (DiffPiece? line in inlineDiff.Lines)
		{
			switch (line.Type)
			{
				case ChangeType.Deleted:
					rawDifferences.Add(new LineDifference(line1Number, null, line.Text, null, LineDifferenceType.Deleted));
					line1Number++;
					break;

				case ChangeType.Inserted:
					rawDifferences.Add(new LineDifference(null, line2Number, null, line.Text, LineDifferenceType.Added));
					line2Number++;
					break;

				case ChangeType.Modified:
					rawDifferences.Add(new LineDifference(line1Number, line2Number, line.Text, line.Text, LineDifferenceType.Modified));
					line1Number++;
					line2Number++;
					break;

				case ChangeType.Imaginary:
					// Imaginary lines are used for padding, skip them
					break;

				default:
					// Skip unchanged lines but track line numbers
					line1Number++;
					line2Number++;
					break;
			}
		}

		// Post-process to detect modifications (deletions and additions at the same line positions)
		List<LineDifference> finalDifferences = [];
		List<LineDifference> deletions = [.. rawDifferences.Where(d => d.Type == LineDifferenceType.Deleted)];
		List<LineDifference> additions = [.. rawDifferences.Where(d => d.Type == LineDifferenceType.Added)];
		List<LineDifference> others = [.. rawDifferences.Where(d => d.Type == LineDifferenceType.Modified)];

		// Try to pair deletions with additions at the same line numbers
		HashSet<int> usedAdditions = [];

		foreach (LineDifference? deletion in deletions)
		{
			// Look for an addition at the same line number
			var matchingAddition = additions
				.Select((addition, index) => new { addition, index })
				.FirstOrDefault(a => !usedAdditions.Contains(a.index) &&
								   a.addition.LineNumber2 == deletion.LineNumber1);

			if (matchingAddition != null)
			{
				// Convert to modification
				finalDifferences.Add(new LineDifference(deletion.LineNumber1, matchingAddition.addition.LineNumber2, deletion.Content1, matchingAddition.addition.Content2, LineDifferenceType.Modified));
				usedAdditions.Add(matchingAddition.index);
			}
			else
			{
				// Keep as deletion
				finalDifferences.Add(deletion);
			}
		}

		// Add remaining additions that weren't paired
		finalDifferences.AddRange(additions.Where((_, index) => !usedAdditions.Contains(index)));

		// Add other types (modifications that were already detected)
		finalDifferences.AddRange(others);

		return new ReadOnlyCollection<LineDifference>(finalDifferences);
	}

	/// <summary>
	/// Generates side-by-side diff model
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Side-by-side diff model</returns>
	public SideBySideDiffModel GenerateSideBySideDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!fileSystemProvider.Current.File.Exists(file1) || !fileSystemProvider.Current.File.Exists(file2))
		{
			throw new FileNotFoundException(FilesNotFoundMessage);
		}

		string content1 = fileSystemProvider.Current.File.ReadAllText(file1);
		string content2 = fileSystemProvider.Current.File.ReadAllText(file2);

		SideBySideDiffBuilder sideBySideDiffBuilder = new();
		return sideBySideDiffBuilder.BuildDiffModel(content1, content2);
	}

	/// <summary>
	/// Generates change summary showing only added/removed lines
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Collection of colored diff lines with only changes</returns>
	public Collection<ColoredDiffLine> GenerateChangeSummary(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		Collection<ColoredDiffLine> coloredDiff = GenerateColoredDiff(file1, file2);

		// Filter to only show additions, deletions, and headers
		List<ColoredDiffLine> filteredLines = [.. coloredDiff.Where(line => line.Color != DiffColor.Default)];

		Collection<ColoredDiffLine> result = [.. filteredLines];

		return result;
	}
}
