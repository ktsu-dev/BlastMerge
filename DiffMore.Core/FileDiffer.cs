// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Linq;

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
	public int? LineNumber1 { get; set; }

	/// <summary>
	/// Gets or sets the line number in the second file
	/// </summary>
	public int? LineNumber2 { get; set; }

	/// <summary>
	/// Gets or sets the content from the first file
	/// </summary>
	public string? Content1 { get; set; }

	/// <summary>
	/// Gets or sets the content from the second file
	/// </summary>
	public string? Content2 { get; set; }

	/// <summary>
	/// Gets or sets the type of difference
	/// </summary>
	public LineDifferenceType Type { get; set; }
}

/// <summary>
/// Defines the types of line differences
/// </summary>
public enum LineDifferenceType
{
	/// <summary>
	/// Line was added in the second file
	/// </summary>
	Added,

	/// <summary>
	/// Line was deleted from the first file
	/// </summary>
	Deleted,

	/// <summary>
	/// Line was modified between files
	/// </summary>
	Modified
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
/// Represents the result of a file similarity calculation
/// </summary>
public class FileSimilarity
{
	/// <summary>
	/// Gets the path to the first file
	/// </summary>
	public required string FilePath1 { get; init; }

	/// <summary>
	/// Gets the path to the second file
	/// </summary>
	public required string FilePath2 { get; init; }

	/// <summary>
	/// Gets the similarity score between 0.0 (completely different) and 1.0 (identical)
	/// </summary>
	public double SimilarityScore { get; init; }
}

/// <summary>
/// Represents a merge conflict that needs resolution
/// </summary>
public class MergeConflict
{
	/// <summary>
	/// Gets the line number where the conflict occurs
	/// </summary>
	public int LineNumber { get; init; }

	/// <summary>
	/// Gets the content from the first file
	/// </summary>
	public string? Content1 { get; init; }

	/// <summary>
	/// Gets the content from the second file
	/// </summary>
	public string? Content2 { get; init; }

	/// <summary>
	/// Gets or sets the resolved content chosen by the user
	/// </summary>
	public string? ResolvedContent { get; set; }

	/// <summary>
	/// Gets or sets whether this conflict has been resolved
	/// </summary>
	public bool IsResolved { get; set; }
}

/// <summary>
/// Represents the result of a merge operation
/// </summary>
public class MergeResult
{
	/// <summary>
	/// Gets the merged file content as lines
	/// </summary>
	public required IReadOnlyList<string> MergedLines { get; init; }

	/// <summary>
	/// Gets the conflicts that were encountered during merge
	/// </summary>
	public required IReadOnlyCollection<MergeConflict> Conflicts { get; init; }

	/// <summary>
	/// Gets whether all conflicts were successfully resolved
	/// </summary>
	public bool IsFullyResolved => Conflicts.All(c => c.IsResolved);
}

/// <summary>
/// Represents an iterative merge session for multiple file versions
/// </summary>
public class IterativeMergeSession(IEnumerable<string> filePaths)
{
	private readonly List<string> _filePaths = [.. filePaths];
	private readonly List<string> _mergedContents = [];

	/// <summary>
	/// Gets the remaining files to be merged
	/// </summary>
	public IReadOnlyList<string> RemainingFiles => _filePaths.AsReadOnly();

	/// <summary>
	/// Gets the current merged content (if any)
	/// </summary>
	public IReadOnlyList<string> MergedContents => _mergedContents.AsReadOnly();

	/// <summary>
	/// Adds a merged result to the session
	/// </summary>
	/// <param name="mergedContent">The merged content to add</param>
	public void AddMergedContent(string mergedContent) => _mergedContents.Add(mergedContent);

	/// <summary>
	/// Removes a file from the remaining files list
	/// </summary>
	/// <param name="filePath">The file path to remove</param>
	public void RemoveFile(string filePath) => _filePaths.Remove(filePath);

	/// <summary>
	/// Gets whether the merge session is complete
	/// </summary>
	public bool IsComplete => _filePaths.Count <= 1 && _mergedContents.Count > 0;
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
	/// Finds differences between two files using DiffPlex
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	/// <returns>Collection of line differences</returns>
	public static IReadOnlyCollection<LineDifference> FindDifferences(string file1, string file2) =>
		// Use DiffPlex implementation for better performance and accuracy
		DiffPlexDiffer.FindDifferences(file1, file2);

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
				// Use DiffPlex for better file comparison
				if (DiffPlexDiffer.AreFilesIdentical(file1Path, file2Path))
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

		// Use DiffPlex for git-style diff generation
		var gitDiff = DiffPlexDiffer.GenerateUnifiedDiff(file1, file2);

		if (!useColor || string.IsNullOrEmpty(gitDiff))
		{
			return gitDiff;
		}

		// Add color escape sequences for terminal if requested
		var coloredDiff = DiffPlexDiffer.GenerateColoredDiff(file1, file2);
		var sb = new StringBuilder();

		foreach (var line in coloredDiff)
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
				default:
					sb.AppendLine(line.Content);
					break;
			}
		}

		return sb.ToString();
	}

	/// <summary>
	/// Generates a colored diff output between two files
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="lines1">Contents of the first file (unused - kept for API compatibility)</param>
	/// <param name="lines2">Contents of the second file (unused - kept for API compatibility)</param>
	/// <returns>List of colored diff lines</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Maintaining API compatibility")]
	public static Collection<ColoredDiffLine> GenerateColoredDiff(string file1, string file2, string[] lines1, string[] lines2)
	{
		// Use DiffPlex implementation
		var diffLines = DiffPlexDiffer.GenerateColoredDiff(file1, file2);
		var result = new Collection<ColoredDiffLine>();
		foreach (var line in diffLines)
		{
			result.Add(line);
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

		// Get the differences using DiffPlex
		var differences = FindDifferences(file1, file2);

		var sb = new StringBuilder();
		sb.AppendLine($"Change Summary: {Path.GetFileName(file1)} vs {Path.GetFileName(file2)}");
		sb.AppendLine("----------------------------------------");

		// Count different types of changes
		var modifications = differences.Count(d => d.LineNumber1.HasValue && d.LineNumber2.HasValue);
		var additions = differences.Count(d => !d.LineNumber1.HasValue && d.LineNumber2.HasValue);
		var deletions = differences.Count(d => d.LineNumber1.HasValue && !d.LineNumber2.HasValue);

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
			sb.AppendLine("No differences found.");
			return sb.ToString();
		}

		sb.AppendLine();

		// Show details of changes
		foreach (var diff in differences)
		{
			if (diff.LineNumber1.HasValue && diff.LineNumber2.HasValue)
			{
				// Modified line
				var prefix = useColor ? "\u001b[33m" : "";
				var suffix = useColor ? "\u001b[0m" : "";
				sb.AppendLine($"{prefix}Modified line {diff.LineNumber1}: {diff.Content1} → {diff.Content2}{suffix}");
			}
			else if (!diff.LineNumber1.HasValue && diff.LineNumber2.HasValue)
			{
				// Added line
				var prefix = useColor ? "\u001b[32m" : "";
				var suffix = useColor ? "\u001b[0m" : "";
				sb.AppendLine($"{prefix}Added line {diff.LineNumber2}: {diff.Content2}{suffix}");
			}
			else if (diff.LineNumber1.HasValue && !diff.LineNumber2.HasValue)
			{
				// Deleted line
				var prefix = useColor ? "\u001b[31m" : "";
				var suffix = useColor ? "\u001b[0m" : "";
				sb.AppendLine($"{prefix}Deleted line {diff.LineNumber1}: {diff.Content1}{suffix}");
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

		// Use DiffPlex to get differences
		var differences = FindDifferences(file1, file2);

		// Track added and removed lines
		var addedLines = new List<(int, string)>();
		var removedLines = new List<(int, string)>();

		foreach (var diff in differences)
		{
			if (diff.LineNumber1.HasValue && !diff.LineNumber2.HasValue)
			{
				// Deletion
				removedLines.Add((diff.LineNumber1.Value, diff.Content1 ?? ""));
			}
			else if (!diff.LineNumber1.HasValue && diff.LineNumber2.HasValue)
			{
				// Addition
				addedLines.Add((diff.LineNumber2.Value, diff.Content2 ?? ""));
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

	/// <summary>
	/// Calculates similarity score between two files based on line matching
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>A similarity score between 0.0 (completely different) and 1.0 (identical)</returns>
	public static double CalculateFileSimilarity(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var lines1 = File.ReadAllLines(file1);
		var lines2 = File.ReadAllLines(file2);

		return CalculateLineSimilarity(lines1, lines2);
	}

	/// <summary>
	/// Calculates similarity score between two sets of lines
	/// </summary>
	/// <param name="lines1">Lines from the first file</param>
	/// <param name="lines2">Lines from the second file</param>
	/// <returns>A similarity score between 0.0 (completely different) and 1.0 (identical)</returns>
	public static double CalculateLineSimilarity(string[] lines1, string[] lines2)
	{
		ArgumentNullException.ThrowIfNull(lines1);
		ArgumentNullException.ThrowIfNull(lines2);

		if (lines1.Length == 0 && lines2.Length == 0)
		{
			return 1.0; // Both empty, completely similar
		}

		if (lines1.Length == 0 || lines2.Length == 0)
		{
			return 0.0; // One empty, completely different
		}

		// Create temporary files to use DiffPlex for similarity calculation
		var tempFile1 = Path.GetTempFileName();
		var tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			// Use DiffPlex to get differences
			var differences = DiffPlexDiffer.FindDifferences(tempFile1, tempFile2);

			// Calculate similarity based on unchanged lines
			var totalOperations = differences.Count;
			var maxLines = Math.Max(lines1.Length, lines2.Length);

			if (totalOperations == 0)
			{
				return 1.0; // No differences means identical
			}

			// Simple similarity calculation: 1 - (differences / max_lines)
			var similarityRatio = Math.Max(0.0, 1.0 - ((double)totalOperations / maxLines));
			return similarityRatio;
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
	/// Finds the two most similar files from a collection of unique file groups
	/// </summary>
	/// <param name="fileGroups">Collection of file groups with different content</param>
	/// <returns>A FileSimilarity object with the most similar pair, or null if less than 2 groups</returns>
	public static FileSimilarity? FindMostSimilarFiles(IReadOnlyCollection<FileGroup> fileGroups)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);

		if (fileGroups.Count < 2)
		{
			return null;
		}

		var groups = fileGroups.ToList();
		FileSimilarity? mostSimilar = null;
		var highestSimilarity = -1.0;

		for (var i = 0; i < groups.Count; i++)
		{
			for (var j = i + 1; j < groups.Count; j++)
			{
				var file1 = groups[i].FilePaths.First();
				var file2 = groups[j].FilePaths.First();
				var similarity = CalculateFileSimilarity(file1, file2);

				if (similarity > highestSimilarity)
				{
					highestSimilarity = similarity;
					mostSimilar = new FileSimilarity
					{
						FilePath1 = file1,
						FilePath2 = file2,
						SimilarityScore = similarity
					};
				}
			}
		}

		return mostSimilar;
	}

	/// <summary>
	/// Performs a three-way merge between two files, detecting conflicts
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>A MergeResult containing the merged content and any conflicts</returns>
	public static MergeResult MergeFiles(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var lines1 = File.ReadAllLines(file1);
		var lines2 = File.ReadAllLines(file2);

		return MergeLines(lines1, lines2);
	}

	/// <summary>
	/// Performs a merge between two sets of lines, detecting conflicts
	/// </summary>
	/// <param name="lines1">Lines from the first file</param>
	/// <param name="lines2">Lines from the second file</param>
	/// <returns>A MergeResult containing the merged content and any conflicts</returns>
	public static MergeResult MergeLines(string[] lines1, string[] lines2)
	{
		ArgumentNullException.ThrowIfNull(lines1);
		ArgumentNullException.ThrowIfNull(lines2);

		// Create temporary files to use DiffPlex
		var tempFile1 = Path.GetTempFileName();
		var tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			var differences = DiffPlexDiffer.FindDifferences(tempFile1, tempFile2);
			var mergedLines = new List<string>();
			var conflicts = new List<MergeConflict>();

			var line1Index = 0;
			var line2Index = 0;

			foreach (var diff in differences)
			{
				// Add unchanged lines before this difference
				while (line1Index < diff.LineNumber1 - 1 && line2Index < diff.LineNumber2 - 1)
				{
					mergedLines.Add(lines1[line1Index]);
					line1Index++;
					line2Index++;
				}

				// Handle the difference
				if (diff.LineNumber1 > 0 && diff.LineNumber2 > 0)
				{
					// Both files have content at this line - this is a conflict
					conflicts.Add(new MergeConflict
					{
						LineNumber = mergedLines.Count + 1,
						Content1 = diff.Content1,
						Content2 = diff.Content2,
						IsResolved = false
					});

					// For now, add a conflict marker
					mergedLines.Add($"<<<<<<< Version 1");
					mergedLines.Add(diff.Content1 ?? "");
					mergedLines.Add("=======");
					mergedLines.Add(diff.Content2 ?? "");
					mergedLines.Add(">>>>>>> Version 2");
				}
				else if (diff.LineNumber1 > 0)
				{
					// Line only in first file - treat as deletion, add a conflict
					conflicts.Add(new MergeConflict
					{
						LineNumber = mergedLines.Count + 1,
						Content1 = diff.Content1,
						Content2 = null,
						IsResolved = false
					});

					// Add conflict marker for deletion
					mergedLines.Add($"<<<<<<< Version 1 (deleted)");
					mergedLines.Add(diff.Content1 ?? "");
					mergedLines.Add("=======");
					mergedLines.Add(">>>>>>> Version 2 (not present)");
				}
				else if (diff.LineNumber2 > 0)
				{
					// Line only in second file - treat as addition, add a conflict
					conflicts.Add(new MergeConflict
					{
						LineNumber = mergedLines.Count + 1,
						Content1 = null,
						Content2 = diff.Content2,
						IsResolved = false
					});

					// Add conflict marker for addition
					mergedLines.Add($"<<<<<<< Version 1 (not present)");
					mergedLines.Add("=======");
					mergedLines.Add(diff.Content2 ?? "");
					mergedLines.Add(">>>>>>> Version 2 (added)");
				}

				// Update indices based on difference type
				if (diff.LineNumber1.HasValue && diff.LineNumber1 > 0)
				{
					line1Index = diff.LineNumber1.Value;
				}

				if (diff.LineNumber2.HasValue && diff.LineNumber2 > 0)
				{
					line2Index = diff.LineNumber2.Value;
				}
			}

			// Add any remaining unchanged lines
			while (line1Index < lines1.Length && line2Index < lines2.Length)
			{
				mergedLines.Add(lines1[line1Index]);
				line1Index++;
				line2Index++;
			}

			// Add remaining lines from either file
			while (line1Index < lines1.Length)
			{
				mergedLines.Add(lines1[line1Index]);
				line1Index++;
			}

			while (line2Index < lines2.Length)
			{
				mergedLines.Add(lines2[line2Index]);
				line2Index++;
			}

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
}

