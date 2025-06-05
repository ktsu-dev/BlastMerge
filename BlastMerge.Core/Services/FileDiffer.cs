// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using ktsu.BlastMerge.Core.Models;

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

		Dictionary<string, FileGroup> groups = [];

		foreach (string filePath in filePaths)
		{
			string hash = FileHasher.ComputeFileHash(filePath);

			if (!groups.TryGetValue(hash, out FileGroup? group))
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

		SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

		// Get all files from both directories
		HashSet<string> files1 = Directory.Exists(dir1)
			? [.. Directory.GetFiles(dir1, searchPattern, searchOption).Select(f => Path.GetRelativePath(dir1, f))]
			: [];

		HashSet<string> files2 = Directory.Exists(dir2)
			? [.. Directory.GetFiles(dir2, searchPattern, searchOption).Select(f => Path.GetRelativePath(dir2, f))]
			: [];

		List<string> sameFiles = [];
		List<string> modifiedFiles = [];
		List<string> onlyInDir1 = [];
		List<string> onlyInDir2 = [];

		// Find files that exist in both directories
		List<string> commonFiles = [.. files1.Intersect(files2)];

		foreach (string? relativePath in commonFiles)
		{
			string file1Path = Path.Combine(dir1, relativePath);
			string file2Path = Path.Combine(dir2, relativePath);

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
		string gitDiff = DiffPlexDiffer.GenerateUnifiedDiff(file1, file2);

		if (!useColor || string.IsNullOrEmpty(gitDiff))
		{
			return gitDiff;
		}

		// Add color escape sequences for terminal if requested
		Collection<ColoredDiffLine> coloredDiff = DiffPlexDiffer.GenerateColoredDiff(file1, file2);
		StringBuilder sb = new();

		foreach (ColoredDiffLine line in coloredDiff)
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
		Collection<ColoredDiffLine> diffLines = DiffPlexDiffer.GenerateColoredDiff(file1, file2);
		Collection<ColoredDiffLine> result = [.. diffLines];
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
		IReadOnlyCollection<LineDifference> differences = FindDifferences(file1, file2);

		StringBuilder sb = new();
		sb.AppendLine($"Change Summary: {Path.GetFileName(file1)} vs {Path.GetFileName(file2)}");
		sb.AppendLine("----------------------------------------");

		// Count different types of changes
		int modifications = differences.Count(d => d.LineNumber1.HasValue && d.LineNumber2.HasValue);
		int additions = differences.Count(d => !d.LineNumber1.HasValue && d.LineNumber2.HasValue);
		int deletions = differences.Count(d => d.LineNumber1.HasValue && !d.LineNumber2.HasValue);

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
		foreach (LineDifference diff in differences)
		{
			if (diff.LineNumber1.HasValue && diff.LineNumber2.HasValue)
			{
				// Modified line
				string prefix = useColor ? "\u001b[33m" : "";
				string suffix = useColor ? "\u001b[0m" : "";
				sb.AppendLine($"{prefix}Modified line {diff.LineNumber1}: {diff.Content1} → {diff.Content2}{suffix}");
			}
			else if (!diff.LineNumber1.HasValue && diff.LineNumber2.HasValue)
			{
				// Added line
				string prefix = useColor ? "\u001b[32m" : "";
				string suffix = useColor ? "\u001b[0m" : "";
				sb.AppendLine($"{prefix}Added line {diff.LineNumber2}: {diff.Content2}{suffix}");
			}
			else if (diff.LineNumber1.HasValue && !diff.LineNumber2.HasValue)
			{
				// Deleted line
				string prefix = useColor ? "\u001b[31m" : "";
				string suffix = useColor ? "\u001b[0m" : "";
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

		Collection<ColoredDiffLine> result =
		[
			// Add header
			new($"Change Summary: {Path.GetFileName(file1)} vs {Path.GetFileName(file2)}", DiffColor.FileHeader),
			new("----------------------------------------", DiffColor.Default)
		];

		// Use DiffPlex to get differences
		IReadOnlyCollection<LineDifference> differences = FindDifferences(file1, file2);

		// Track added and removed lines
		List<(int, string)> addedLines = [];
		List<(int, string)> removedLines = [];

		foreach (LineDifference diff in differences)
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
			result.Add(new ColoredDiffLine("REMOVED LINES (in version 1 but not in version X):", DiffColor.ChunkHeader));

			foreach ((int lineNum, string content) in removedLines)
			{
				result.Add(new ColoredDiffLine($"- Line {lineNum}: {content}", DiffColor.Deletion));
			}

			result.Add(new ColoredDiffLine("", DiffColor.Default));
		}

		// Output added lines (in version X but not in version 1)
		if (addedLines.Count > 0)
		{
			result.Add(new ColoredDiffLine("ADDED LINES (in version X but not in version 1):", DiffColor.ChunkHeader));

			foreach ((int lineNum, string content) in addedLines)
			{
				result.Add(new ColoredDiffLine($"+ Line {lineNum}: {content}", DiffColor.Addition));
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

		string? targetDir = Path.GetDirectoryName(targetFile);
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

		string[] lines1 = File.ReadAllLines(file1);
		string[] lines2 = File.ReadAllLines(file2);

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
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			// Use DiffPlex to get differences
			IReadOnlyCollection<LineDifference> differences = DiffPlexDiffer.FindDifferences(tempFile1, tempFile2);

			// Calculate similarity based on unchanged lines
			int totalOperations = differences.Count;
			int maxLines = Math.Max(lines1.Length, lines2.Length);

			if (totalOperations == 0)
			{
				return 1.0; // No differences means identical
			}

			// Simple similarity calculation: 1 - (differences / max_lines)
			double similarityRatio = Math.Max(0.0, 1.0 - ((double)totalOperations / maxLines));
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
	/// Calculates a hash for string content
	/// </summary>
	/// <param name="content">The string content to hash</param>
	/// <returns>The hash as a hex string</returns>
	public static string CalculateFileHash(string content) => FileHasher.ComputeContentHash(content);

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

		List<FileGroup> groups = [.. fileGroups];
		FileSimilarity? mostSimilar = null;
		double highestSimilarity = -1.0;

		for (int i = 0; i < groups.Count; i++)
		{
			for (int j = i + 1; j < groups.Count; j++)
			{
				string file1 = groups[i].FilePaths.First();
				string file2 = groups[j].FilePaths.First();
				double similarity = CalculateFileSimilarity(file1, file2);

				if (similarity > highestSimilarity)
				{
					highestSimilarity = similarity;
					mostSimilar = new FileSimilarity(file1, file2, similarity);
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

		string[] lines1 = File.ReadAllLines(file1);
		string[] lines2 = File.ReadAllLines(file2);

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
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			IReadOnlyCollection<LineDifference> differences = DiffPlexDiffer.FindDifferences(tempFile1, tempFile2);
			List<string> mergedLines = [];
			List<MergeConflict> conflicts = [];

			int line1Index = 0;
			int line2Index = 0;

			foreach (LineDifference diff in differences)
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
					conflicts.Add(new MergeConflict(mergedLines.Count + 1, diff.Content1, diff.Content2, null, false));

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
					conflicts.Add(new MergeConflict(mergedLines.Count + 1, diff.Content1, null, null, false));

					// Add conflict marker for deletion
					mergedLines.Add($"<<<<<<< Version 1 (deleted)");
					mergedLines.Add(diff.Content1 ?? "");
					mergedLines.Add("=======");
					mergedLines.Add(">>>>>>> Version 2 (not present)");
				}
				else if (diff.LineNumber2 > 0)
				{
					// Line only in second file - treat as addition, add a conflict
					conflicts.Add(new MergeConflict(mergedLines.Count + 1, null, diff.Content2, null, false));

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

			return new MergeResult(mergedLines.AsReadOnly(), conflicts.AsReadOnly());
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

