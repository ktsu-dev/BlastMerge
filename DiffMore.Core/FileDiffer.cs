// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

/// <summary>
/// Represents a group of identical files by hash
/// </summary>
public class FileGroup
{
	private readonly Collection<string> filePaths = [];

	/// <summary>
	/// Gets the hash that identifies this group
	/// </summary>
	public required string Hash { get; init; }

	/// <summary>
	/// Gets the list of file paths in this group
	/// </summary>
	public IReadOnlyCollection<string> FilePaths => filePaths.AsReadOnly();

	/// <summary>
	/// Adds a file path to this group
	/// </summary>
	/// <param name="filePath">The file path to add</param>
	public void AddFilePath(string filePath) => filePaths.Add(filePath);
}

/// <summary>
/// Represents a line difference between two files
/// </summary>
public class LineDifference
{
	/// <summary>
	/// Gets or sets the line number in the first file
	/// </summary>
	public int LineNumber1 { get; set; }

	/// <summary>
	/// Gets or sets the line number in the second file
	/// </summary>
	public int LineNumber2 { get; set; }

	/// <summary>
	/// Gets or sets the content from the first file
	/// </summary>
	public string? Content1 { get; set; }

	/// <summary>
	/// Gets or sets the content from the second file
	/// </summary>
	public string? Content2 { get; set; }
}

/// <summary>
/// Defines colors to use for different parts of the diff output
/// </summary>
public enum DiffColor
{
	/// <summary>
	/// Default console color
	/// </summary>
	Default,

	/// <summary>
	/// Color for deleted lines
	/// </summary>
	Deletion,

	/// <summary>
	/// Color for added lines
	/// </summary>
	Addition,

	/// <summary>
	/// Color for chunk headers
	/// </summary>
	ChunkHeader,

	/// <summary>
	/// Color for file headers
	/// </summary>
	FileHeader
}

/// <summary>
/// Represents a diffed line with formatting information
/// </summary>
public class ColoredDiffLine
{
	/// <summary>
	/// Gets or sets the line content
	/// </summary>
	public required string Content { get; set; }

	/// <summary>
	/// Gets or sets the color for this line
	/// </summary>
	public DiffColor Color { get; set; }
}

/// <summary>
/// Represents an edit operation (used in Myers diff algorithm)
/// </summary>
internal enum EditOperation
{
	Delete,
	Insert,
	Equal
}

/// <summary>
/// Represents the result of a directory comparison
/// </summary>
public class DirectoryComparisonResult
{
	/// <summary>
	/// Gets the collection of files that are identical in both directories
	/// </summary>
	public IReadOnlyCollection<string> SameFiles { get; init; } = [];

	/// <summary>
	/// Gets the collection of files that exist in both directories but have different content
	/// </summary>
	public IReadOnlyCollection<string> ModifiedFiles { get; init; } = [];

	/// <summary>
	/// Gets the collection of files that exist only in the first directory
	/// </summary>
	public IReadOnlyCollection<string> OnlyInDir1 { get; init; } = [];

	/// <summary>
	/// Gets the collection of files that exist only in the second directory
	/// </summary>
	public IReadOnlyCollection<string> OnlyInDir2 { get; init; } = [];
}

/// <summary>
/// Compares file contents to find differences
/// </summary>
public static class FileDiffer
{
	/// <summary>
	/// Groups files by their hash to identify unique versions
	/// </summary>
	/// <param name="filePaths">List of file paths to group</param>
	/// <returns>A collection of file groups where each group contains identical files</returns>
	public static IReadOnlyCollection<FileGroup> GroupFilesByHash(IReadOnlyCollection<string> filePaths)
	{
		ArgumentNullException.ThrowIfNull(filePaths);

		var groups = new Dictionary<string, FileGroup>();

		foreach (var filePath in filePaths)
		{
			var hash = FileHasher.ComputeFileHash(filePath);

			if (!groups.TryGetValue(hash, out var group))
			{
				group = new FileGroup { Hash = hash };
				groups[hash] = group;
			}

			group.AddFilePath(filePath);
		}

		return [.. groups.Values];
	}

	/// <summary>
	/// Finds differences between two text files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>A collection of line differences</returns>
	public static IReadOnlyCollection<LineDifference> FindDifferences(string file1, string file2)
	{
		var rawDifferences = new Collection<LineDifference>();

		// Read all lines from both files
		var lines1 = File.ReadAllLines(file1);
		var lines2 = File.ReadAllLines(file2);

		// Use Myers algorithm to get edit script
		var editScript = GetMyersDiff(lines1, lines2);

		int line1 = 0, line2 = 0;

		foreach (var edit in editScript)
		{
			switch (edit.Item1)
			{
				case EditOperation.Delete:
					rawDifferences.Add(new LineDifference
					{
						LineNumber1 = line1 + 1,
						LineNumber2 = 0, // No corresponding line in file 2
						Content1 = lines1[line1],
						Content2 = null
					});
					line1++;
					break;

				case EditOperation.Insert:
					rawDifferences.Add(new LineDifference
					{
						LineNumber1 = 0, // No corresponding line in file 1
						LineNumber2 = line2 + 1,
						Content1 = null,
						Content2 = lines2[line2]
					});
					line2++;
					break;

				case EditOperation.Equal:
					// No difference to add for equal lines
					line1++;
					line2++;
					break;
				default:
					break;
			}
		}

		// Post-process to merge consecutive delete/insert operations into modifications
		return MergeModifications(rawDifferences);
	}

	/// <summary>
	/// Merges consecutive delete/insert operations into modification operations
	/// </summary>
	/// <param name="rawDifferences">Raw differences from the diff algorithm</param>
	/// <returns>Processed differences with modifications merged</returns>
	private static ReadOnlyCollection<LineDifference> MergeModifications(Collection<LineDifference> rawDifferences)
	{
		var mergedDifferences = new Collection<LineDifference>();

		for (var i = 0; i < rawDifferences.Count; i++)
		{
			var current = rawDifferences[i];

			// Check if this is a delete operation followed by an insert operation
			if (current.LineNumber2 == 0 && current.Content1 != null && // This is a delete
				i + 1 < rawDifferences.Count) // There's a next item
			{
				var next = rawDifferences[i + 1];

				// Check if the next operation is an insert and they should be merged
				// Only merge if they are truly consecutive (no other operations in between)
				if (next.LineNumber1 == 0 && next.Content2 != null) // Next is an insert
				{
					// Merge consecutive delete/insert operations into modifications
					// This handles cases where entire files have changed line-by-line
					mergedDifferences.Add(new LineDifference
					{
						LineNumber1 = current.LineNumber1,
						LineNumber2 = next.LineNumber2,
						Content1 = current.Content1,
						Content2 = next.Content2
					});

					// Skip the next item since we've merged it
					i++;
					continue;
				}
			}

			// If we didn't merge, add the current difference as-is
			mergedDifferences.Add(current);
		}

		return new ReadOnlyCollection<LineDifference>(mergedDifferences);
	}

	/// <summary>
	/// Compares two directories and finds differences between files
	/// </summary>
	/// <param name="dir1">Path to the first directory</param>
	/// <param name="dir2">Path to the second directory</param>
	/// <param name="searchPattern">File search pattern (e.g., "*.txt")</param>
	/// <param name="recursive">Whether to search subdirectories recursively</param>
	/// <returns>A DirectoryComparisonResult containing the comparison results</returns>
	public static DirectoryComparisonResult FindDifferences(string dir1, string dir2, string searchPattern, bool recursive = false)
	{
		ArgumentNullException.ThrowIfNull(dir1);
		ArgumentNullException.ThrowIfNull(dir2);
		ArgumentNullException.ThrowIfNull(searchPattern);

		var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

		// Get all files from both directories
		var files1 = Directory.Exists(dir1)
			? Directory.GetFiles(dir1, searchPattern, searchOption)
				.Select(f => Path.GetRelativePath(dir1, f))
				.ToHashSet()
			: [];

		var files2 = Directory.Exists(dir2)
			? Directory.GetFiles(dir2, searchPattern, searchOption)
				.Select(f => Path.GetRelativePath(dir2, f))
				.ToHashSet()
			: [];

		var sameFiles = new List<string>();
		var modifiedFiles = new List<string>();
		var onlyInDir1 = new List<string>();
		var onlyInDir2 = new List<string>();

		// Find files that exist in both directories
		var commonFiles = files1.Intersect(files2).ToList();

		foreach (var relativePath in commonFiles)
		{
			var file1Path = Path.Combine(dir1, relativePath);
			var file2Path = Path.Combine(dir2, relativePath);

			try
			{
				// Compare file contents using hash
				var hash1 = FileHasher.ComputeFileHash(file1Path);
				var hash2 = FileHasher.ComputeFileHash(file2Path);

				if (hash1 == hash2)
				{
					sameFiles.Add(relativePath);
				}
				else
				{
					modifiedFiles.Add(relativePath);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// If we can't read the files, consider them modified
				modifiedFiles.Add(relativePath);
			}
		}

		// Find files that exist only in dir1
		onlyInDir1.AddRange(files1.Except(files2));

		// Find files that exist only in dir2
		onlyInDir2.AddRange(files2.Except(files1));

		return new DirectoryComparisonResult
		{
			SameFiles = sameFiles.AsReadOnly(),
			ModifiedFiles = modifiedFiles.AsReadOnly(),
			OnlyInDir1 = onlyInDir1.AsReadOnly(),
			OnlyInDir2 = onlyInDir2.AsReadOnly()
		};
	}

	/// <summary>
	/// Generates a git-style diff between two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>A string containing the git-style diff</returns>
	public static string GenerateGitStyleDiff(string file1, string file2) => GenerateGitStyleDiff(file1, file2, false);

	/// <summary>
	/// Generates a git-style diff between two files with optional color
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="useColor">Whether to add color codes to the output</param>
	/// <returns>A string containing the git-style diff</returns>
	public static string GenerateGitStyleDiff(string file1, string file2, bool useColor)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var lines1 = File.ReadAllLines(file1);
		var lines2 = File.ReadAllLines(file2);

		var coloredDiff = GenerateColoredDiff(file1, file2, lines1, lines2);

		var sb = new StringBuilder();

		foreach (var line in coloredDiff)
		{
			if (useColor)
			{
				// Add color escape sequences for terminal
				switch (line.Color)
				{
					case DiffColor.Addition:
						sb.AppendLine($"\u001b[32m{line.Content}\u001b[0m"); // Green
						break;
					case DiffColor.Deletion:
						sb.AppendLine($"\u001b[31m{line.Content}\u001b[0m"); // Red
						break;
					case DiffColor.ChunkHeader:
						sb.AppendLine($"\u001b[36m{line.Content}\u001b[0m"); // Cyan
						break;
					case DiffColor.FileHeader:
						sb.AppendLine($"\u001b[1;34m{line.Content}\u001b[0m"); // Bold blue
						break;
					case DiffColor.Default:
						break;
					default:
						sb.AppendLine(line.Content);
						break;
				}
			}
			else
			{
				sb.AppendLine(line.Content);
			}
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generates a colored diff output between two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="lines1">Contents of the first file</param>
	/// <param name="lines2">Contents of the second file</param>
	/// <returns>List of colored diff lines</returns>
	public static Collection<ColoredDiffLine> GenerateColoredDiff(string file1, string file2, string[] lines1, string[] lines2)
	{
		ArgumentNullException.ThrowIfNull(lines1);
		ArgumentNullException.ThrowIfNull(lines2);

		// Get edit script from Myers algorithm
		var editScript = GetMyersDiff(lines1, lines2);

		// Check if files are identical (no differences)
		if (editScript.All(edit => edit.Item1 == EditOperation.Equal))
		{
			return [];
		}

		var result = new Collection<ColoredDiffLine>
		{
			// Add file headers
			new() {
				Content = $"--- a/{Path.GetFileName(file1)}",
				Color = DiffColor.FileHeader
			},
			new() {
				Content = $"+++ b/{Path.GetFileName(file2)}",
				Color = DiffColor.FileHeader
			}
		};

		// Group edit operations into chunks with context
		var chunks = GetDiffChunks(editScript, lines1, lines2);

		// Generate diff for each chunk
		foreach (var chunk in chunks)
		{
			var (chunkStart1, chunkEnd1, chunkStart2, chunkEnd2) = chunk;

			// Calculate line counts for chunk header
			var line1Count = chunkEnd1 - chunkStart1 + 1;
			var line2Count = chunkEnd2 - chunkStart2 + 1;

			// Add chunk header
			result.Add(new ColoredDiffLine
			{
				Content = $"@@ -{chunkStart1 + 1},{line1Count} +{chunkStart2 + 1},{line2Count} @@",
				Color = DiffColor.ChunkHeader
			});

			// Process the chunk
			var i1 = chunkStart1;
			var i2 = chunkStart2;

			while (i1 <= chunkEnd1 || i2 <= chunkEnd2)
			{
				// Find the corresponding edit operation
				var op = FindEditOperation(editScript, i1, i2);

				switch (op)
				{
					case EditOperation.Equal:
						result.Add(new ColoredDiffLine
						{
							Content = $" {lines1[i1]}",
							Color = DiffColor.Default
						});
						i1++;
						i2++;
						break;

					case EditOperation.Delete:
						result.Add(new ColoredDiffLine
						{
							Content = $"-{lines1[i1]}",
							Color = DiffColor.Deletion
						});
						i1++;
						break;

					case EditOperation.Insert:
						result.Add(new ColoredDiffLine
						{
							Content = $"+{lines2[i2]}",
							Color = DiffColor.Addition
						});
						i2++;
						break;
					default:
						break;
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Finds the edit operation for the given indices in the edit script
	/// </summary>
	private static EditOperation FindEditOperation(Collection<(EditOperation, string)> editScript, int index1, int index2)
	{
		// Count the operations until we reach the desired indices
		int curIndex1 = 0, curIndex2 = 0;

		foreach (var (op, _) in editScript)
		{
			if (curIndex1 == index1 && curIndex2 == index2)
			{
				return op;
			}

			switch (op)
			{
				case EditOperation.Equal:
					curIndex1++;
					curIndex2++;
					break;
				case EditOperation.Delete:
					curIndex1++;
					break;
				case EditOperation.Insert:
					curIndex2++;
					break;
				default:
					break;
			}
		}

		// Default to Equal if not found
		return EditOperation.Equal;
	}

	/// <summary>
	/// Groups the edit script into chunks with context
	/// </summary>
	private static Collection<(int, int, int, int)> GetDiffChunks(Collection<(EditOperation, string)> editScript, string[] lines1, string[] lines2)
	{
		const int contextLines = 3;
		var chunks = new Collection<(int, int, int, int)>();

		// Find all changed lines
		var changedIndices = new Collection<(int, int)>();
		int index1 = 0, index2 = 0;

		foreach (var (op, _) in editScript)
		{
			if (op != EditOperation.Equal)
			{
				changedIndices.Add((index1, index2));
			}

			switch (op)
			{
				case EditOperation.Equal:
					index1++;
					index2++;
					break;
				case EditOperation.Delete:
					index1++;
					break;
				case EditOperation.Insert:
					index2++;
					break;
				default:
					break;
			}
		}

		if (changedIndices.Count == 0)
		{
			return chunks;
		}

		// Group consecutive changed lines into chunks
		var chunkStart1 = Math.Max(0, changedIndices[0].Item1 - contextLines);
		var chunkStart2 = Math.Max(0, changedIndices[0].Item2 - contextLines);
		var chunkEnd1 = changedIndices[0].Item1;
		var chunkEnd2 = changedIndices[0].Item2;

		foreach (var (idx1, idx2) in changedIndices.Skip(1))
		{
			if (idx1 <= chunkEnd1 + (contextLines * 2) || idx2 <= chunkEnd2 + (contextLines * 2))
			{
				// Extend current chunk
				chunkEnd1 = Math.Max(chunkEnd1, idx1);
				chunkEnd2 = Math.Max(chunkEnd2, idx2);
			}
			else
			{
				// End current chunk and start a new one
				chunks.Add((
					chunkStart1,
					Math.Min(lines1.Length - 1, chunkEnd1 + contextLines),
					chunkStart2,
					Math.Min(lines2.Length - 1, chunkEnd2 + contextLines)
				));

				chunkStart1 = Math.Max(0, idx1 - contextLines);
				chunkStart2 = Math.Max(0, idx2 - contextLines);
				chunkEnd1 = idx1;
				chunkEnd2 = idx2;
			}
		}

		// Add the last chunk
		chunks.Add((
			chunkStart1,
			Math.Min(lines1.Length - 1, chunkEnd1 + contextLines),
			chunkStart2,
			Math.Min(lines2.Length - 1, chunkEnd2 + contextLines)
		));

		return chunks;
	}

	/// <summary>
	/// Implements the Myers diff algorithm to find the shortest edit script
	/// </summary>
	private static Collection<(EditOperation, string)> GetMyersDiff(string[] a, string[] b)
	{
		var n = a.Length;
		var m = b.Length;

		// Handle edge cases for empty files
		if (n == 0 && m == 0)
		{
			// Both files are empty, no differences
			return [];
		}

		if (n == 0)
		{
			// First file is empty, all lines from second file are insertions
			var insertResult = new Collection<(EditOperation, string)>();
			for (var i = 0; i < m; i++)
			{
				insertResult.Add((EditOperation.Insert, b[i]));
			}

			return insertResult;
		}

		if (m == 0)
		{
			// Second file is empty, all lines from first file are deletions
			var deleteResult = new Collection<(EditOperation, string)>();
			for (var i = 0; i < n; i++)
			{
				deleteResult.Add((EditOperation.Delete, a[i]));
			}

			return deleteResult;
		}

		var max = n + m;

		var v = new int[(2 * max) + 1];
		var trace = new Collection<int[]>();

		// Find the shortest edit path
		int x = 0, y = 0;

		for (var d = 0; d <= max; d++)
		{
			// Save the state for backtracking
			trace.Add([.. v]);

			for (var k = -d; k <= d; k += 2)
			{
				// Choose the best move: down or right
				if (k == -d || (k != d && v[k - 1 + max] < v[k + 1 + max]))
				{
					x = v[k + 1 + max]; // Move right
				}
				else
				{
					x = v[k - 1 + max] + 1; // Move down
				}

				y = x - k;

				// Follow the diagonal as far as possible
				while (x < n && y < m && a[x] == b[y])
				{
					x++;
					y++;
				}

				v[k + max] = x;

				// Check if we reached the target
				if (x >= n && y >= m)
				{
					// Reconstruct the edit path
					return BacktrackPath(a, b, trace, max);
				}
			}
		}

		// Fallback to a simple edit script if the algorithm fails
		return CreateSimpleDiff(a, b);
	}

	/// <summary>
	/// Backtracks through the edit path to construct the edit script
	/// </summary>
	private static Collection<(EditOperation, string)> BacktrackPath(string[] a, string[] b, Collection<int[]> trace, int max)
	{
		// Use a simpler approach for very large files to avoid index errors
		if (a.Length > 5000 || b.Length > 5000)
		{
			return CreateSimpleDiff(a, b);
		}

		var n = a.Length;
		var m = b.Length;
		var script = new Collection<(EditOperation, string)>();

		var x = n;
		var y = m;

		try
		{
			for (var d = trace.Count - 1; d >= 0 && (x > 0 || y > 0); d--)
			{
				var v = trace[d];
				var k = x - y;

				// Check if k-1+max or k+1+max is out of bounds
				if (k - 1 + max < 0 || k - 1 + max >= v.Length || k + 1 + max < 0 || k + 1 + max >= v.Length)
				{
					// Skip this iteration if indices would be out of bounds
					continue;
				}

				var down = k == -d || (k != d && v[k - 1 + max] < v[k + 1 + max]);

				var kPrev = down ? k + 1 : k - 1;

				// Check if kPrev+max is out of bounds
				if (kPrev + max < 0 || kPrev + max >= v.Length)
				{
					// Skip this iteration if index would be out of bounds
					continue;
				}

				var xPrev = v[kPrev + max];
				var yPrev = xPrev - kPrev;

				// Add diagonal moves (equal elements)
				while (x > xPrev && y > yPrev)
				{
					if (x > 0 && x <= a.Length)
					{
						script.Add((EditOperation.Equal, a[--x]));
						y--;
					}
					else
					{
						// Break if index would be out of bounds
						break;
					}
				}

				if (x == xPrev)
				{
					// Down move (insert from b)
					if (y > 0 && y <= b.Length)
					{
						script.Add((EditOperation.Insert, b[--y]));
					}
				}
				else
				{
					// Right move (delete from a)
					if (x > 0 && x <= a.Length)
					{
						script.Add((EditOperation.Delete, a[--x]));
					}
				}
			}
		}
		catch (IndexOutOfRangeException)
		{
			// Fall back to simple diff if we encounter an index error
			return CreateSimpleDiff(a, b);
		}

		// Reverse the script to get the correct order
		var reversed = new Collection<(EditOperation, string)>();

		for (var i = script.Count - 1; i >= 0; i--)
		{
			reversed.Add(script[i]);
		}

		return reversed;
	}

	/// <summary>
	/// Creates a simple diff by comparing lines sequentially
	/// </summary>
	private static Collection<(EditOperation, string)> CreateSimpleDiff(string[] a, string[] b)
	{
		var result = new Collection<(EditOperation, string)>();
		var commonLength = Math.Min(a.Length, b.Length);

		// First add common prefix
		var prefixLength = 0;
		while (prefixLength < commonLength && a[prefixLength] == b[prefixLength])
		{
			result.Add((EditOperation.Equal, a[prefixLength]));
			prefixLength++;
		}

		// Process remaining lines by interleaving delete/insert pairs for better merging
		var remainingA = a.Length - prefixLength;
		var remainingB = b.Length - prefixLength;
		var maxRemaining = Math.Max(remainingA, remainingB);

		for (var i = 0; i < maxRemaining; i++)
		{
			var indexA = prefixLength + i;
			var indexB = prefixLength + i;

			// Add delete operation if we have more lines in A
			if (indexA < a.Length)
			{
				result.Add((EditOperation.Delete, a[indexA]));
			}

			// Add insert operation if we have more lines in B
			if (indexB < b.Length)
			{
				result.Add((EditOperation.Insert, b[indexB]));
			}
		}

		return result;
	}

	/// <summary>
	/// Generates a change summary diff showing only lines added or removed between versions
	/// </summary>
	/// <param name="file1">Path to the first file (version 1)</param>
	/// <param name="file2">Path to the second file (version X)</param>
	/// <param name="useColor">Whether to add color codes to the output</param>
	/// <returns>A string containing the summarized changes</returns>
	public static string GenerateChangeSummaryDiff(string file1, string file2, bool useColor = false)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		// Get the differences using our main diff method
		var differences = FindDifferences(file1, file2);

		var sb = new StringBuilder();
		sb.AppendLine($"Change Summary: {Path.GetFileName(file1)} vs {Path.GetFileName(file2)}");
		sb.AppendLine("----------------------------------------");

		// Count different types of changes
		var modifications = differences.Count(d => d.LineNumber1 > 0 && d.LineNumber2 > 0);
		var additions = differences.Count(d => d.LineNumber1 == 0 && d.LineNumber2 > 0);
		var deletions = differences.Count(d => d.LineNumber1 > 0 && d.LineNumber2 == 0);

		// Add summary statistics
		if (modifications > 0)
		{
			sb.AppendLine($"{modifications} line(s) modified");
		}

		if (additions > 0)
		{
			sb.AppendLine($"{additions} line(s) added");
		}

		if (deletions > 0)
		{
			sb.AppendLine($"{deletions} line(s) deleted");
		}

		if (modifications == 0 && additions == 0 && deletions == 0)
		{
			sb.AppendLine("No differences found");
		}

		sb.AppendLine();

		// Output detailed changes
		var modifiedLines = differences.Where(d => d.LineNumber1 > 0 && d.LineNumber2 > 0).ToList();
		var addedLines = differences.Where(d => d.LineNumber1 == 0 && d.LineNumber2 > 0).ToList();
		var deletedLines = differences.Where(d => d.LineNumber1 > 0 && d.LineNumber2 == 0).ToList();

		// Output modified lines
		if (modifiedLines.Count > 0)
		{
			sb.AppendLine("MODIFIED LINES:");
			foreach (var diff in modifiedLines)
			{
				if (useColor)
				{
					sb.AppendLine($"\u001b[33m~ Line {diff.LineNumber1}: '{diff.Content1}' -> '{diff.Content2}'\u001b[0m"); // Yellow
				}
				else
				{
					sb.AppendLine($"~ Line {diff.LineNumber1}: '{diff.Content1}' -> '{diff.Content2}'");
				}
			}

			sb.AppendLine();
		}

		// Output added lines
		if (addedLines.Count > 0)
		{
			sb.AppendLine("ADDED LINES:");
			foreach (var diff in addedLines)
			{
				if (useColor)
				{
					sb.AppendLine($"\u001b[32m+ Line {diff.LineNumber2}: {diff.Content2}\u001b[0m"); // Green
				}
				else
				{
					sb.AppendLine($"+ Line {diff.LineNumber2}: {diff.Content2}");
				}
			}

			sb.AppendLine();
		}

		// Output deleted lines
		if (deletedLines.Count > 0)
		{
			sb.AppendLine("DELETED LINES:");
			foreach (var diff in deletedLines)
			{
				if (useColor)
				{
					sb.AppendLine($"\u001b[31m- Line {diff.LineNumber1}: {diff.Content1}\u001b[0m"); // Red
				}
				else
				{
					sb.AppendLine($"- Line {diff.LineNumber1}: {diff.Content1}");
				}
			}
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generates a colored change summary showing added and removed lines
	/// </summary>
	/// <param name="file1">Path to the first file (version 1)</param>
	/// <param name="file2">Path to the second file (version X)</param>
	/// <returns>List of colored diff lines showing only changes</returns>
	public static Collection<ColoredDiffLine> GenerateColoredChangeSummary(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var lines1 = File.ReadAllLines(file1);
		var lines2 = File.ReadAllLines(file2);

		var result = new Collection<ColoredDiffLine>
		{
			// Add header
			new() {
				Content = $"Change Summary: {Path.GetFileName(file1)} vs {Path.GetFileName(file2)}",
				Color = DiffColor.FileHeader
			},
			new() {
				Content = "----------------------------------------",
				Color = DiffColor.Default
			}
		};

		// Generate edit script
		var editScript = GetMyersDiff(lines1, lines2);

		// Track added and removed lines
		var addedLines = new List<(int, string)>();
		var removedLines = new List<(int, string)>();

		int line1 = 0, line2 = 0;

		foreach (var (op, content) in editScript)
		{
			switch (op)
			{
				case EditOperation.Delete:
					removedLines.Add((line1 + 1, lines1[line1]));
					line1++;
					break;

				case EditOperation.Insert:
					addedLines.Add((line2 + 1, lines2[line2]));
					line2++;
					break;

				case EditOperation.Equal:
					line1++;
					line2++;
					break;

				default:
					break;
			}
		}

		// Output removed lines (in version 1 but not in version X)
		if (removedLines.Count > 0)
		{
			result.Add(new ColoredDiffLine
			{
				Content = "REMOVED LINES (in version 1 but not in version X):",
				Color = DiffColor.ChunkHeader
			});

			foreach (var (lineNum, content) in removedLines)
			{
				result.Add(new ColoredDiffLine
				{
					Content = $"- Line {lineNum}: {content}",
					Color = DiffColor.Deletion
				});
			}

			result.Add(new ColoredDiffLine
			{
				Content = "",
				Color = DiffColor.Default
			});
		}

		// Output added lines (in version X but not in version 1)
		if (addedLines.Count > 0)
		{
			result.Add(new ColoredDiffLine
			{
				Content = "ADDED LINES (in version X but not in version 1):",
				Color = DiffColor.ChunkHeader
			});

			foreach (var (lineNum, content) in addedLines)
			{
				result.Add(new ColoredDiffLine
				{
					Content = $"+ Line {lineNum}: {content}",
					Color = DiffColor.Addition
				});
			}
		}

		return result;
	}

	/// <summary>
	/// Copies the content from source file to target file
	/// </summary>
	/// <param name="sourceFile">Path to the source file</param>
	/// <param name="targetFile">Path to the target file</param>
	public static void SyncFile(string sourceFile, string targetFile)
	{
		ArgumentNullException.ThrowIfNull(sourceFile);
		ArgumentNullException.ThrowIfNull(targetFile);

		var targetDir = Path.GetDirectoryName(targetFile);
		if (!string.IsNullOrEmpty(targetDir))
		{
			Directory.CreateDirectory(targetDir);
		}

		File.Copy(sourceFile, targetFile, overwrite: true);
	}
}

