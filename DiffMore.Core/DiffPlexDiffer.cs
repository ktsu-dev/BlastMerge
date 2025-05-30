// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Chunkers;

/// <summary>
/// Provides diffing functionality using DiffPlex library
/// </summary>
public static class DiffPlexDiffer
{
	private static readonly Differ Differ = new();
	private static readonly InlineDiffBuilder InlineDiffBuilder = new(Differ);
	private static readonly SideBySideDiffBuilder SideBySideDiffBuilder = new(Differ);

	/// <summary>
	/// Checks if two files are identical
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>True if files are identical, false otherwise</returns>
	public static bool AreFilesIdentical(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!File.Exists(file1) || !File.Exists(file2))
		{
			return false;
		}

		var content1 = File.ReadAllText(file1);
		var content2 = File.ReadAllText(file2);

		var diff = Differ.CreateDiffs(content1, content2, true, false, new LineChunker());
		return !diff.DiffBlocks.Any();
	}

	/// <summary>
	/// Generates a unified diff between two files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <param name="contextLines">Number of context lines around changes (default: 3)</param>
	/// <returns>Unified diff as string</returns>
	public static string GenerateUnifiedDiff(string file1, string file2, int contextLines = 3)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!File.Exists(file1) || !File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		var content1 = File.ReadAllText(file1);
		var content2 = File.ReadAllText(file2);

		// If files are identical, return empty string
		if (content1 == content2)
		{
			return string.Empty;
		}

		var diff = InlineDiffBuilder.BuildDiffModel(content1, content2);

		var result = new List<string>
		{
			$"--- {file1}",
			$"+++ {file2}"
		};

		var currentLine1 = 1;
		var currentLine2 = 1;
		var pendingLines = new List<string>();
		var contextBuffer = new List<string>();

		foreach (var line in diff.Lines)
		{
			switch (line.Type)
			{
				case ChangeType.Unchanged:
					contextBuffer.Add($" {line.Text}");
					if (contextBuffer.Count > contextLines * 2)
					{
						contextBuffer.RemoveAt(0);
					}
					currentLine1++;
					currentLine2++;
					break;

				case ChangeType.Deleted:
					if (pendingLines.Count == 0)
					{
						// Add context before changes
						var contextStart = Math.Max(0, contextBuffer.Count - contextLines);
						pendingLines.AddRange(contextBuffer.Skip(contextStart));
					}
					pendingLines.Add($"-{line.Text}");
					currentLine1++;
					break;

				case ChangeType.Inserted:
					if (pendingLines.Count == 0)
					{
						// Add context before changes
						var contextStart = Math.Max(0, contextBuffer.Count - contextLines);
						pendingLines.AddRange(contextBuffer.Skip(contextStart));
					}
					pendingLines.Add($"+{line.Text}");
					currentLine2++;
					break;

				case ChangeType.Modified:
					// Handle modified lines as deletion + insertion
					if (pendingLines.Count == 0)
					{
						var contextStart = Math.Max(0, contextBuffer.Count - contextLines);
						pendingLines.AddRange(contextBuffer.Skip(contextStart));
					}
					pendingLines.Add($"-{line.Text}");
					pendingLines.Add($"+{line.Text}");
					currentLine1++;
					currentLine2++;
					break;

				case ChangeType.Imaginary:
					// Handle imaginary lines (used for padding)
					break;

				default:
					// Handle any other change types
					currentLine1++;
					currentLine2++;
					break;
			}

			// If we have pending changes and hit unchanged text, output the chunk
			if (line.Type == ChangeType.Unchanged && pendingLines.Count > 0)
			{
				// Add context after changes
				var contextAfter = contextBuffer.Take(Math.Min(contextLines, contextBuffer.Count)).ToList();
				pendingLines.AddRange(contextAfter);

				// Add chunk header
				var deletedCount = pendingLines.Count(l => l.StartsWith('-'));
				var addedCount = pendingLines.Count(l => l.StartsWith('+'));
				var contextCount = pendingLines.Count - deletedCount - addedCount;

				var startLine1 = currentLine1 - deletedCount - (contextCount / 2);
				var startLine2 = currentLine2 - addedCount - (contextCount / 2);

				result.Add($"@@ -{startLine1},{deletedCount + (contextCount / 2)} +{startLine2},{addedCount + (contextCount / 2)} @@");
				result.AddRange(pendingLines);

				pendingLines.Clear();
			}
		}

		// Handle any remaining pending changes
		if (pendingLines.Count > 0)
		{
			var deletedCount = pendingLines.Count(l => l.StartsWith('-'));
			var addedCount = pendingLines.Count(l => l.StartsWith('+'));
			var contextCount = pendingLines.Count - deletedCount - addedCount;

			var startLine1 = currentLine1 - deletedCount - contextCount;
			var startLine2 = currentLine2 - addedCount - contextCount;

			result.Add($"@@ -{startLine1},{deletedCount + contextCount} +{startLine2},{addedCount + contextCount} @@");
			result.AddRange(pendingLines);
		}

		return string.Join(Environment.NewLine, result);
	}

	/// <summary>
	/// Generates colored diff lines for display
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Collection of colored diff lines</returns>
	public static Collection<ColoredDiffLine> GenerateColoredDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!File.Exists(file1) || !File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		var content1 = File.ReadAllText(file1);
		var content2 = File.ReadAllText(file2);

		var diff = InlineDiffBuilder.BuildDiffModel(content1, content2);
		var result = new Collection<ColoredDiffLine>
		{
			new() { Content = $"--- {file1}", Color = DiffColor.FileHeader },
			new() { Content = $"+++ {file2}", Color = DiffColor.FileHeader }
		};

		foreach (var line in diff.Lines)
		{
			var color = line.Type switch
			{
				ChangeType.Deleted => DiffColor.Deletion,
				ChangeType.Inserted => DiffColor.Addition,
				ChangeType.Modified => DiffColor.ChunkHeader,
				ChangeType.Unchanged => DiffColor.Default,
				ChangeType.Imaginary => DiffColor.Default,
				_ => DiffColor.Default
			};

			var prefix = line.Type switch
			{
				ChangeType.Deleted => "-",
				ChangeType.Inserted => "+",
				ChangeType.Modified => "~",
				ChangeType.Unchanged => " ",
				ChangeType.Imaginary => " ",
				_ => " "
			};

			result.Add(new ColoredDiffLine
			{
				Content = $"{prefix}{line.Text}",
				Color = color
			});
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
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!File.Exists(file1) || !File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		var content1 = File.ReadAllText(file1);
		var content2 = File.ReadAllText(file2);

		// Use side-by-side diff for better handling of modifications
		var sideBySideDiff = SideBySideDiffBuilder.BuildDiffModel(content1, content2);
		var differences = new List<LineDifference>();

		var oldLines = sideBySideDiff.OldText.Lines;
		var newLines = sideBySideDiff.NewText.Lines;

		var oldIndex = 0;
		var newIndex = 0;

		while (oldIndex < oldLines.Count || newIndex < newLines.Count)
		{
			var oldLine = oldIndex < oldLines.Count ? oldLines[oldIndex] : null;
			var newLine = newIndex < newLines.Count ? newLines[newIndex] : null;

			if (oldLine != null && newLine != null)
			{
				if (oldLine.Type == ChangeType.Deleted && newLine.Type == ChangeType.Inserted)
				{
					// This is a modification - both lines exist and are changed
					differences.Add(new LineDifference
					{
						LineNumber1 = oldLine.Position.HasValue ? oldLine.Position.Value + 1 : oldIndex + 1,
						LineNumber2 = newLine.Position.HasValue ? newLine.Position.Value + 1 : newIndex + 1,
						Content1 = oldLine.Text,
						Content2 = newLine.Text,
						Type = LineDifferenceType.Modified
					});
					oldIndex++;
					newIndex++;
				}
				else if (oldLine.Type == ChangeType.Unchanged && newLine.Type == ChangeType.Unchanged)
				{
					// Lines are the same, skip both
					oldIndex++;
					newIndex++;
				}
				else if (oldLine.Type == ChangeType.Deleted)
				{
					// Line was deleted from old file
					differences.Add(new LineDifference
					{
						LineNumber1 = oldLine.Position.HasValue ? oldLine.Position.Value + 1 : oldIndex + 1,
						LineNumber2 = null,
						Content1 = oldLine.Text,
						Content2 = null,
						Type = LineDifferenceType.Deleted
					});
					oldIndex++;
				}
				else if (newLine.Type == ChangeType.Inserted)
				{
					// Line was added to new file
					differences.Add(new LineDifference
					{
						LineNumber1 = null,
						LineNumber2 = newLine.Position.HasValue ? newLine.Position.Value + 1 : newIndex + 1,
						Content1 = null,
						Content2 = newLine.Text,
						Type = LineDifferenceType.Added
					});
					newIndex++;
				}
				else
				{
					// Both lines exist but neither is a change (shouldn't happen)
					oldIndex++;
					newIndex++;
				}
			}
			else if (oldLine != null)
			{
				// Only old line exists (deletion)
				if (oldLine.Type == ChangeType.Deleted)
				{
					differences.Add(new LineDifference
					{
						LineNumber1 = oldLine.Position.HasValue ? oldLine.Position.Value + 1 : oldIndex + 1,
						LineNumber2 = null,
						Content1 = oldLine.Text,
						Content2 = null,
						Type = LineDifferenceType.Deleted
					});
				}
				oldIndex++;
			}
			else if (newLine != null)
			{
				// Only new line exists (addition)
				if (newLine.Type == ChangeType.Inserted)
				{
					differences.Add(new LineDifference
					{
						LineNumber1 = null,
						LineNumber2 = newLine.Position.HasValue ? newLine.Position.Value + 1 : newIndex + 1,
						Content1 = null,
						Content2 = newLine.Text,
						Type = LineDifferenceType.Added
					});
				}
				newIndex++;
			}
		}

		return new ReadOnlyCollection<LineDifference>(differences);
	}

	/// <summary>
	/// Generates side-by-side diff model
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Side-by-side diff model</returns>
	public static SideBySideDiffModel GenerateSideBySideDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!File.Exists(file1) || !File.Exists(file2))
		{
			throw new FileNotFoundException("One or both files do not exist");
		}

		var content1 = File.ReadAllText(file1);
		var content2 = File.ReadAllText(file2);

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
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var coloredDiff = GenerateColoredDiff(file1, file2);

		// Filter to only show additions, deletions, and headers
		var filteredLines = coloredDiff
			.Where(line => line.Color != DiffColor.Default)
			.ToList();

		var result = new Collection<ColoredDiffLine>();
		foreach (var line in filteredLines)
		{
			result.Add(line);
		}

		return result;
	}
}
