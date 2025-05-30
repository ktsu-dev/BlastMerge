// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core;

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

		// Use inline diff for simpler processing
		var inlineDiff = InlineDiffBuilder.BuildDiffModel(content1, content2);
		var rawDifferences = new List<LineDifference>();

		var line1Number = 1;
		var line2Number = 1;

		foreach (var line in inlineDiff.Lines)
		{
			switch (line.Type)
			{
				case ChangeType.Deleted:
					rawDifferences.Add(new LineDifference
					{
						LineNumber1 = line1Number,
						LineNumber2 = null,
						Content1 = line.Text,
						Content2 = null,
						Type = LineDifferenceType.Deleted
					});
					line1Number++;
					break;

				case ChangeType.Inserted:
					rawDifferences.Add(new LineDifference
					{
						LineNumber1 = null,
						LineNumber2 = line2Number,
						Content1 = null,
						Content2 = line.Text,
						Type = LineDifferenceType.Added
					});
					line2Number++;
					break;

				case ChangeType.Modified:
					rawDifferences.Add(new LineDifference
					{
						LineNumber1 = line1Number,
						LineNumber2 = line2Number,
						Content1 = line.Text, // Note: In inline diff, this might be the original text
						Content2 = line.Text, // and this might be the modified text
						Type = LineDifferenceType.Modified
					});
					line1Number++;
					line2Number++;
					break;

				case ChangeType.Unchanged:
					// Skip unchanged lines but track line numbers
					line1Number++;
					line2Number++;
					break;

				case ChangeType.Imaginary:
					// Imaginary lines are used for padding, skip them
					break;

				default:
					// Unknown change type, skip but track line numbers
					line1Number++;
					line2Number++;
					break;
			}
		}

		// Post-process to detect modifications (deletions and additions at the same line positions)
		var finalDifferences = new List<LineDifference>();
		var deletions = rawDifferences.Where(d => d.Type == LineDifferenceType.Deleted).ToList();
		var additions = rawDifferences.Where(d => d.Type == LineDifferenceType.Added).ToList();
		var others = rawDifferences.Where(d => d.Type == LineDifferenceType.Modified).ToList();

		// Try to pair deletions with additions at the same line numbers
		var usedAdditions = new HashSet<int>();

		foreach (var deletion in deletions)
		{
			// Look for an addition at the same line number
			var matchingAddition = additions
				.Select((addition, index) => new { addition, index })
				.FirstOrDefault(a => !usedAdditions.Contains(a.index) &&
								   a.addition.LineNumber2 == deletion.LineNumber1);

			if (matchingAddition != null)
			{
				// Convert to modification
				finalDifferences.Add(new LineDifference
				{
					LineNumber1 = deletion.LineNumber1,
					LineNumber2 = matchingAddition.addition.LineNumber2,
					Content1 = deletion.Content1,
					Content2 = matchingAddition.addition.Content2,
					Type = LineDifferenceType.Modified
				});
				usedAdditions.Add(matchingAddition.index);
			}
			else
			{
				// Keep as deletion
				finalDifferences.Add(deletion);
			}
		}

		// Add remaining additions that weren't paired
		for (var i = 0; i < additions.Count; i++)
		{
			if (!usedAdditions.Contains(i))
			{
				finalDifferences.Add(additions[i]);
			}
		}

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
