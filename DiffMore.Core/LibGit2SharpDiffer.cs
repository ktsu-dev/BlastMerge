// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Provides improved diffing functionality that was originally planned to use LibGit2Sharp
/// but now uses an enhanced text-based algorithm for better reliability
/// </summary>
public static class LibGit2SharpDiffer
{
	/// <summary>
	/// Generates a git-style diff between two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="contextLines">Number of context lines to include (default: 3)</param>
	/// <returns>Git-style diff output</returns>
	public static string GenerateGitStyleDiff(string file1, string file2, int contextLines = 3)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		// Read file contents
		var content1 = File.Exists(file1) ? File.ReadAllText(file1) : string.Empty;
		var content2 = File.Exists(file2) ? File.ReadAllText(file2) : string.Empty;

		// If contents are identical, return empty string
		if (content1 == content2)
		{
			return string.Empty;
		}

		// Generate diff using improved text-based algorithm
		return GenerateTextBasedDiff(file1, file2, content1, content2, contextLines);
	}

	/// <summary>
	/// Compares two files directly using content comparison
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>True if files are identical, false otherwise</returns>
	public static bool AreFilesIdentical(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		if (!File.Exists(file1) || !File.Exists(file2))
		{
			return false;
		}

		// First check file sizes
		var fileInfo1 = new FileInfo(file1);
		var fileInfo2 = new FileInfo(file2);

		if (fileInfo1.Length != fileInfo2.Length)
		{
			return false;
		}

		// Compare content
		var content1 = File.ReadAllText(file1);
		var content2 = File.ReadAllText(file2);

		return content1 == content2;
	}

	/// <summary>
	/// Generates colored diff lines from two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>Collection of colored diff lines</returns>
	public static Collection<ColoredDiffLine> GenerateColoredDiff(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var result = new Collection<ColoredDiffLine>();
		var gitDiff = GenerateGitStyleDiff(file1, file2);

		if (string.IsNullOrEmpty(gitDiff))
		{
			// No differences - return the original file content as context
			if (File.Exists(file1))
			{
				var lines = File.ReadAllLines(file1);
				foreach (var line in lines)
				{
					result.Add(new ColoredDiffLine
					{
						Content = line,
						Color = DiffColor.Default
					});
				}
			}
			return result;
		}

		// Parse git diff output and assign colors
		var diffLines = gitDiff.Split('\n', StringSplitOptions.None);

		foreach (var line in diffLines)
		{
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}

			var color = DiffColor.Default;

			if (line.StartsWith("+++") || line.StartsWith("---"))
			{
				color = DiffColor.FileHeader;
			}
			else if (line.StartsWith("@@"))
			{
				color = DiffColor.ChunkHeader;
			}
			else if (line.StartsWith('+'))
			{
				color = DiffColor.Addition;
			}
			else if (line.StartsWith('-'))
			{
				color = DiffColor.Deletion;
			}

			result.Add(new ColoredDiffLine
			{
				Content = line,
				Color = color
			});
		}

		return result;
	}

	/// <summary>
	/// Finds differences between two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>Collection of line differences</returns>
	public static IReadOnlyCollection<LineDifference> FindDifferences(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var differences = new List<LineDifference>();
		var gitDiff = GenerateGitStyleDiff(file1, file2);

		if (string.IsNullOrEmpty(gitDiff))
		{
			return differences.AsReadOnly();
		}

		// Parse the git diff to extract line differences
		var diffLines = gitDiff.Split('\n', StringSplitOptions.None);
		var currentOldLine = 0;
		var currentNewLine = 0;

		foreach (var line in diffLines)
		{
			if (line.StartsWith("@@"))
			{
				// Parse hunk header to get line numbers
				var (oldStart, newStart) = ParseHunkHeader(line);
				currentOldLine = oldStart;
				currentNewLine = newStart;
			}
			else if (line.StartsWith('-'))
			{
				// Deleted line
				differences.Add(new LineDifference
				{
					LineNumber1 = currentOldLine,
					LineNumber2 = 0,
					Content1 = line.Length > 1 ? line[1..] : string.Empty,
					Content2 = null
				});
				currentOldLine++;
			}
			else if (line.StartsWith('+'))
			{
				// Added line
				differences.Add(new LineDifference
				{
					LineNumber1 = 0,
					LineNumber2 = currentNewLine,
					Content1 = null,
					Content2 = line.Length > 1 ? line[1..] : string.Empty
				});
				currentNewLine++;
			}
			else if (!line.StartsWith("---") && !line.StartsWith("+++") && !string.IsNullOrEmpty(line))
			{
				// Context line (unchanged)
				currentOldLine++;
				currentNewLine++;
			}
		}

		return differences.AsReadOnly();
	}

	/// <summary>
	/// Parses a diff hunk header to extract starting line numbers
	/// </summary>
	/// <param name="hunkHeader">The hunk header line (e.g., "@@ -1,4 +1,6 @@")</param>
	/// <returns>A tuple containing the old start line and new start line</returns>
	private static (int oldStart, int newStart) ParseHunkHeader(string hunkHeader)
	{
		// Example: "@@ -1,4 +1,6 @@"
		var parts = hunkHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		var oldStart = 1;
		var newStart = 1;

		if (parts.Length >= 3)
		{
			// Parse old start (after '-')
			var oldPart = parts[1];
			if (oldPart.StartsWith('-'))
			{
				var oldInfo = oldPart[1..].Split(',');
				if (oldInfo.Length > 0 && int.TryParse(oldInfo[0], out var oldLineNum))
				{
					oldStart = oldLineNum;
				}
			}

			// Parse new start (after '+')
			var newPart = parts[2];
			if (newPart.StartsWith('+'))
			{
				var newInfo = newPart[1..].Split(',');
				if (newInfo.Length > 0 && int.TryParse(newInfo[0], out var newLineNum))
				{
					newStart = newLineNum;
				}
			}
		}

		return (oldStart, newStart);
	}

	/// <summary>
	/// Generates a git-style diff using enhanced text-based algorithm
	/// </summary>
	/// <param name="file1">Path to first file</param>
	/// <param name="file2">Path to second file</param>
	/// <param name="content1">Content of first file</param>
	/// <param name="content2">Content of second file</param>
	/// <param name="contextLines">Number of context lines</param>
	/// <returns>Git-style diff output</returns>
	private static string GenerateTextBasedDiff(string file1, string file2, string content1, string content2, int contextLines)
	{
		var lines1 = content1.Split('\n').ToArray();
		var lines2 = content2.Split('\n').ToArray();

		var sb = new StringBuilder();
		sb.AppendLine($"--- {Path.GetFileName(file1)}");
		sb.AppendLine($"+++ {Path.GetFileName(file2)}");

		// Use a simple LCS-based algorithm to find differences
		var operations = CalculateEditOperations(lines1, lines2);

		if (operations.Count == 0)
		{
			return string.Empty;
		}

		// Generate hunks with context
		var hunks = GenerateHunks(operations, lines1, contextLines);

		foreach (var hunk in hunks)
		{
			sb.AppendLine(hunk);
		}

		return sb.ToString();
	}

	/// <summary>
	/// Calculates edit operations using a simplified LCS algorithm
	/// </summary>
	private static List<EditOperation> CalculateEditOperations(string[] lines1, string[] lines2)
	{
		var operations = new List<EditOperation>();
		var i = 0; // index in lines1
		var j = 0; // index in lines2

		while (i < lines1.Length || j < lines2.Length)
		{
			if (i >= lines1.Length)
			{
				// All remaining lines in lines2 are additions
				operations.Add(new EditOperation { Type = EditType.Add, Line2Index = j, Content = lines2[j] });
				j++;
			}
			else if (j >= lines2.Length)
			{
				// All remaining lines in lines1 are deletions
				operations.Add(new EditOperation { Type = EditType.Delete, Line1Index = i, Content = lines1[i] });
				i++;
			}
			else if (lines1[i] == lines2[j])
			{
				// Lines are equal
				operations.Add(new EditOperation { Type = EditType.Equal, Line1Index = i, Line2Index = j, Content = lines1[i] });
				i++;
				j++;
			}
			else
			{
				// Lines are different - find the best match
				var lookAhead = Math.Min(10, Math.Min(lines1.Length - i, lines2.Length - j));
				var found = false;

				// Look for a match in the next few lines
				for (var k = 1; k <= lookAhead && !found; k++)
				{
					if (i + k < lines1.Length && lines1[i + k] == lines2[j])
					{
						// Found a match by deleting k lines from lines1
						for (var del = 0; del < k; del++)
						{
							operations.Add(new EditOperation { Type = EditType.Delete, Line1Index = i + del, Content = lines1[i + del] });
						}
						i += k;
						found = true;
					}
					else if (j + k < lines2.Length && lines1[i] == lines2[j + k])
					{
						// Found a match by adding k lines from lines2
						for (var add = 0; add < k; add++)
						{
							operations.Add(new EditOperation { Type = EditType.Add, Line2Index = j + add, Content = lines2[j + add] });
						}
						j += k;
						found = true;
					}
				}

				if (!found)
				{
					// No match found, treat as change
					operations.Add(new EditOperation { Type = EditType.Delete, Line1Index = i, Content = lines1[i] });
					operations.Add(new EditOperation { Type = EditType.Add, Line2Index = j, Content = lines2[j] });
					i++;
					j++;
				}
			}
		}

		return operations;
	}

	/// <summary>
	/// Generates diff hunks with context lines
	/// </summary>
	private static List<string> GenerateHunks(List<EditOperation> operations, string[] lines1, int contextLines)
	{
		var hunks = new List<string>();
		var currentHunk = new StringBuilder();
		var hunkStartOld = -1;
		var hunkStartNew = -1;
		var oldCount = 0;
		var newCount = 0;
		var inHunk = false;

		for (var i = 0; i < operations.Count; i++)
		{
			var op = operations[i];

			if (op.Type != EditType.Equal)
			{
				if (!inHunk)
				{
					// Start a new hunk
					hunkStartOld = Math.Max(1, (op.Line1Index ?? op.Line2Index ?? 0) - contextLines + 1);
					hunkStartNew = Math.Max(1, (op.Line2Index ?? op.Line1Index ?? 0) - contextLines + 1);

					// Add context before
					var contextStart = Math.Max(0, (op.Line1Index ?? op.Line2Index ?? 0) - contextLines);
					for (var ctx = contextStart; ctx < (op.Line1Index ?? op.Line2Index ?? 0); ctx++)
					{
						if (ctx < lines1.Length)
						{
							currentHunk.AppendLine($" {lines1[ctx]}");
							oldCount++;
							newCount++;
						}
					}

					inHunk = true;
				}

				// Add the changed line
				switch (op.Type)
				{
					case EditType.Delete:
						currentHunk.AppendLine($"-{op.Content}");
						oldCount++;
						break;
					case EditType.Add:
						currentHunk.AppendLine($"+{op.Content}");
						newCount++;
						break;
					case EditType.Equal:
						// This case is handled above, but included for completeness
						currentHunk.AppendLine($" {op.Content}");
						oldCount++;
						newCount++;
						break;
					default:
						throw new InvalidOperationException($"Unknown edit type: {op.Type}");
				}
			}
			else if (inHunk)
			{
				// Equal line in a hunk (context)
				currentHunk.AppendLine($" {op.Content}");
				oldCount++;
				newCount++;

				// Check if we should end the hunk
				var remainingOps = operations.Skip(i + 1).Take(contextLines * 2).ToList();
				var hasMoreChanges = remainingOps.Any(r => r.Type != EditType.Equal);

				if (!hasMoreChanges)
				{
					// Add remaining context and end hunk
					var contextEnd = Math.Min(i + contextLines, operations.Count - 1);
					for (var ctx = i + 1; ctx <= contextEnd && ctx < operations.Count; ctx++)
					{
						if (operations[ctx].Type == EditType.Equal)
						{
							currentHunk.AppendLine($" {operations[ctx].Content}");
							oldCount++;
							newCount++;
						}
					}

					// Create hunk header and add to hunks
					var hunkHeader = $"@@ -{hunkStartOld},{oldCount} +{hunkStartNew},{newCount} @@";
					hunks.Add(hunkHeader);
					hunks.Add(currentHunk.ToString().TrimEnd());

					// Reset for next hunk
					currentHunk.Clear();
					oldCount = 0;
					newCount = 0;
					inHunk = false;
				}
			}
		}

		// If we're still in a hunk, close it
		if (inHunk && currentHunk.Length > 0)
		{
			var hunkHeader = $"@@ -{hunkStartOld},{oldCount} +{hunkStartNew},{newCount} @@";
			hunks.Add(hunkHeader);
			hunks.Add(currentHunk.ToString().TrimEnd());
		}

		return hunks;
	}

	/// <summary>
	/// Represents an edit operation in the diff
	/// </summary>
	private class EditOperation
	{
		public EditType Type { get; set; }
		public int? Line1Index { get; set; }
		public int? Line2Index { get; set; }
		public string Content { get; set; } = string.Empty;
	}

	/// <summary>
	/// Types of edit operations
	/// </summary>
	private enum EditType
	{
		Equal,
		Delete,
		Add
	}
}
