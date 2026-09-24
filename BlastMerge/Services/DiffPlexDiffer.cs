// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Model;
using ktsu.BlastMerge.Models;

/// <summary>
/// Provides diffing functionality using DiffPlex library
/// </summary>
public static class DiffPlexDiffer
{
	private static readonly Differ Differ = new();
	private static readonly InlineDiffBuilder InlineDiffBuilder = new(Differ);
	private static readonly SideBySideDiffBuilder SideBySideDiffBuilder = new(Differ);
	private const string FilesNotFoundMessage = "One or both files do not exist";

	/// <summary>
	/// Checks if two files are identical
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>True if files are identical, false otherwise</returns>
	public static bool AreFilesIdentical(string file1, string file2)
	{
		Ensure.NotNull(file1);
		Ensure.NotNull(file2);

		IFileSystem fileSystem = FileSystemProvider.Current;

		if (!fileSystem.File.Exists(file1) || !fileSystem.File.Exists(file2))
		{
			return false;
		}

		(string content1, string content2) = ReadComparableContent(file1, file2);

		DiffResult diff = Differ.CreateDiffs(content1, content2, true, false, new LineChunker());
		return !diff.DiffBlocks.Any();
	}

	/// <summary>
	/// Reads both sides of a comparison in a form that can be diffed as text.
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>The content of each side, or a stand-in for each when either side is binary</returns>
	/// <remarks>
	/// Binary content is never decoded. Each side is represented by its content hash instead, so two
	/// binary files compare equal exactly when their bytes are equal, and a diff between them reports
	/// that they differ without inventing lines that the file never contained. Decoding them as UTF-8
	/// would be worse than useless here: it splits the byte stream wherever a 0x0A happens to fall and
	/// replaces every invalid sequence with U+FFFD, so the "lines" shown would not be in the file.
	/// </remarks>
	private static (string Content1, string Content2) ReadComparableContent(string file1, string file2)
	{
		IFileSystem fileSystem = FileSystemProvider.Current;

		return BinaryContentDetector.IsBinaryFile(file1, fileSystem) || BinaryContentDetector.IsBinaryFile(file2, fileSystem)
			? (DescribeBinaryContent(file1, fileSystem), DescribeBinaryContent(file2, fileSystem))
			: (fileSystem.File.ReadAllText(file1), fileSystem.File.ReadAllText(file2));
	}

	/// <summary>
	/// Describes binary content by its hash, standing in for content that must not be decoded.
	/// </summary>
	/// <param name="filePath">Path to the file to describe</param>
	/// <param name="fileSystem">File system abstraction</param>
	/// <returns>A single-line description of the file's content</returns>
	private static string DescribeBinaryContent(string filePath, IFileSystem fileSystem) =>
		$"Binary content ({FileHasher.ComputeFileHash(filePath, fileSystem)})";

	/// <summary>
	/// Creates a line-by-line diff between two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>DiffResult containing the differences</returns>
	/// <exception cref="ArgumentNullException">Thrown when file1 or file2 is null</exception>
	/// <exception cref="FileNotFoundException">Thrown when one or both files do not exist</exception>
	public static DiffResult CreateLineDiffs(string file1, string file2)
	{
		Ensure.NotNull(file1);
		Ensure.NotNull(file2);

		IFileSystem fileSystem = FileSystemProvider.Current;

		if (!fileSystem.File.Exists(file1) || !fileSystem.File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		(string content1, string content2) = ReadComparableContent(file1, file2);

		return DiffPlexHelper.CreateLineDiffsFromContent(content1, content2);
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
	public static string GenerateUnifiedDiff(string file1, string file2, int contextLines = 3)
	{
		Ensure.NotNull(file1);
		Ensure.NotNull(file2);

		IFileSystem fileSystem = FileSystemProvider.Current;

		if (!fileSystem.File.Exists(file1) || !fileSystem.File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		(string content1, string content2) = ReadComparableContent(file1, file2);

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

		DiffPaneModel diff = InlineDiffBuilder.BuildDiffModel(content1, content2);

		List<string> result =
		[
			$"--- {file1}",
			$"+++ {file2}"
		];

		ProcessDiffLines(diff.Lines, result, contextLines);

		return string.Join(Environment.NewLine, result);
	}

	/// <summary>
	/// Processes diff lines and builds unified diff hunks
	/// </summary>
	/// <param name="lines">Diff lines to process</param>
	/// <param name="result">Result list to add hunks to</param>
	/// <param name="contextLines">Number of context lines</param>
	private static void ProcessDiffLines(IEnumerable<DiffPiece> lines, List<string> result, int contextLines)
	{
		List<DiffEntry> entries = BuildDiffEntries(lines);

		foreach ((int start, int end) in FindHunkRanges(entries, contextLines))
		{
			EmitHunk(entries, start, end, result);
		}
	}

	/// <summary>
	/// Flattens the diff into one entry per unified-diff body line, each carrying the line number it
	/// occupies in the old and/or new file
	/// </summary>
	/// <param name="lines">Diff lines to process</param>
	/// <returns>The diff body lines in order</returns>
	private static List<DiffEntry> BuildDiffEntries(IEnumerable<DiffPiece> lines)
	{
		List<DiffEntry> entries = [];
		int oldLine = 1;
		int newLine = 1;

		foreach (DiffPiece line in lines)
		{
			switch (line.Type)
			{
				case ChangeType.Unchanged:
					entries.Add(new DiffEntry(' ', line.Text, oldLine++, newLine++));
					break;

				case ChangeType.Deleted:
					entries.Add(new DiffEntry('-', line.Text, oldLine++, 0));
					break;

				case ChangeType.Inserted:
					entries.Add(new DiffEntry('+', line.Text, 0, newLine++));
					break;

				case ChangeType.Modified:
					// A modified line is a deletion and an insertion at the same position
					entries.Add(new DiffEntry('-', line.Text, oldLine++, 0));
					entries.Add(new DiffEntry('+', line.Text, 0, newLine++));
					break;

				case ChangeType.Imaginary:
					// Padding only, it occupies no line in either file
					break;

				default:
					entries.Add(new DiffEntry(' ', line.Text, oldLine++, newLine++));
					break;
			}
		}

		return entries;
	}

	/// <summary>
	/// Groups the changed entries into hunk ranges, each padded by up to <paramref name="contextLines"/>
	/// of context and merged with the next group when their context regions would meet or overlap
	/// </summary>
	/// <param name="entries">The diff body lines</param>
	/// <param name="contextLines">Number of context lines</param>
	/// <returns>Inclusive (start, end) index ranges into <paramref name="entries"/></returns>
	private static List<(int Start, int End)> FindHunkRanges(List<DiffEntry> entries, int contextLines)
	{
		int context = Math.Max(0, contextLines);
		List<(int Start, int End)> ranges = [];
		int hunkStart = -1;
		int lastChange = -1;

		for (int i = 0; i < entries.Count; i++)
		{
			if (!entries[i].IsChange)
			{
				continue;
			}

			if (hunkStart < 0)
			{
				hunkStart = i;
			}
			else if (i - lastChange - 1 > context * 2)
			{
				// The gap is wide enough that the two groups get their own hunks
				ranges.Add((Math.Max(0, hunkStart - context), Math.Min(entries.Count - 1, lastChange + context)));
				hunkStart = i;
			}

			lastChange = i;
		}

		if (hunkStart >= 0)
		{
			ranges.Add((Math.Max(0, hunkStart - context), Math.Min(entries.Count - 1, lastChange + context)));
		}

		return ranges;
	}

	/// <summary>
	/// Writes one hunk's header and body
	/// </summary>
	/// <param name="entries">The diff body lines</param>
	/// <param name="start">Inclusive start index of the hunk</param>
	/// <param name="end">Inclusive end index of the hunk</param>
	/// <param name="result">Result list to add the hunk to</param>
	private static void EmitHunk(List<DiffEntry> entries, int start, int end, List<string> result)
	{
		int oldCount = 0;
		int newCount = 0;
		int oldStart = 0;
		int newStart = 0;

		for (int i = start; i <= end; i++)
		{
			DiffEntry entry = entries[i];

			if (entry.Marker != '+')
			{
				oldCount++;
				if (oldStart == 0)
				{
					oldStart = entry.OldLine;
				}
			}

			if (entry.Marker != '-')
			{
				newCount++;
				if (newStart == 0)
				{
					newStart = entry.NewLine;
				}
			}
		}

		// A hunk that only inserts (or only deletes) has no line of its own on that side, so it is
		// anchored after the last line that does exist, which is what a zero length implies
		if (oldCount == 0)
		{
			oldStart = CountLinesBefore(entries, start, '+');
		}

		if (newCount == 0)
		{
			newStart = CountLinesBefore(entries, start, '-');
		}

		result.Add($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@");

		for (int i = start; i <= end; i++)
		{
			result.Add($"{entries[i].Marker}{entries[i].Text}");
		}
	}

	/// <summary>
	/// Counts how many lines one side of the diff has consumed before <paramref name="index"/>
	/// </summary>
	/// <param name="entries">The diff body lines</param>
	/// <param name="index">Exclusive end index</param>
	/// <param name="excludedMarker">The marker belonging to the other side of the diff</param>
	/// <returns>The number of lines on this side before the index</returns>
	private static int CountLinesBefore(List<DiffEntry> entries, int index, char excludedMarker)
	{
		int count = 0;

		for (int i = 0; i < index; i++)
		{
			if (entries[i].Marker != excludedMarker)
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// One line of a unified diff body, with the line number it occupies in each file
	/// </summary>
	/// <param name="Marker">The unified diff marker: a space, '-' or '+'</param>
	/// <param name="Text">The line's text</param>
	/// <param name="OldLine">The 1-based line number in the old file, or 0 if the line is an insertion</param>
	/// <param name="NewLine">The 1-based line number in the new file, or 0 if the line is a deletion</param>
	private sealed record DiffEntry(char Marker, string Text, int OldLine, int NewLine)
	{
		/// <summary>
		/// Gets a value indicating whether this line is a change rather than context
		/// </summary>
		public bool IsChange => Marker != ' ';
	}

	/// <summary>
	/// Generates colored diff lines for display
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Collection of colored diff lines</returns>
	public static Collection<ColoredDiffLine> GenerateColoredDiff(string file1, string file2)
	{
		Ensure.NotNull(file1);
		Ensure.NotNull(file2);

		IFileSystem fileSystem = FileSystemProvider.Current;

		if (!fileSystem.File.Exists(file1) || !fileSystem.File.Exists(file2))
		{
			throw new FileNotFoundException(FilesNotFoundMessage);
		}

		(string content1, string content2) = ReadComparableContent(file1, file2);

		DiffPaneModel diff = InlineDiffBuilder.BuildDiffModel(content1, content2);
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
	public static IReadOnlyCollection<LineDifference> FindDifferences(string file1, string file2)
	{
		Ensure.NotNull(file1);
		Ensure.NotNull(file2);

		IFileSystem fileSystem = FileSystemProvider.Current;

		if (!fileSystem.File.Exists(file1) || !fileSystem.File.Exists(file2))
		{
			throw new FileNotFoundException(FilesNotFoundMessage);
		}

		(string content1, string content2) = ReadComparableContent(file1, file2);

		// Use inline diff for simpler processing
		DiffPaneModel inlineDiff = InlineDiffBuilder.BuildDiffModel(content1, content2);
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
	public static SideBySideDiffModel GenerateSideBySideDiff(string file1, string file2)
	{
		Ensure.NotNull(file1);
		Ensure.NotNull(file2);

		IFileSystem fileSystem = FileSystemProvider.Current;

		if (!fileSystem.File.Exists(file1) || !fileSystem.File.Exists(file2))
		{
			throw new FileNotFoundException(FilesNotFoundMessage);
		}

		(string content1, string content2) = ReadComparableContent(file1, file2);

		return SideBySideDiffBuilder.BuildDiffModel(content1, content2);
	}

	/// <summary>
	/// Generates change summary showing only added/removed lines
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Collection of colored diff lines with only changes</returns>
	public static Collection<ColoredDiffLine> GenerateChangeSummary(string file1, string file2)
	{
		Ensure.NotNull(file1);
		Ensure.NotNull(file2);

		Collection<ColoredDiffLine> coloredDiff = GenerateColoredDiff(file1, file2);

		// Filter to only show additions, deletions, and headers
		List<ColoredDiffLine> filteredLines = [.. coloredDiff.Where(line => line.Color != DiffColor.Default)];

		Collection<ColoredDiffLine> result = [.. filteredLines];

		return result;
	}
}
