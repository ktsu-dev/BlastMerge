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
	// Merge conflict marker constants
	private const string ConflictMarkerStart = "<<<<<<< Version 1";
	private const string ConflictMarkerSeparator = "=======";
	private const string ConflictMarkerEnd = ">>>>>>> Version 2";
	private const string ConflictMarkerDeleted = "<<<<<<< Version 1 (deleted)";
	private const string ConflictMarkerDeletedEnd = ">>>>>>> Version 2 (not present)";
	private const string ConflictMarkerAdded = "<<<<<<< Version 1 (not present)";
	private const string ConflictMarkerAddedEnd = ">>>>>>> Version 2 (added)";
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
			string coloredLine = line.Color switch
			{
				DiffColor.Addition => $"\u001b[32m{line.Content}\u001b[0m", // Green
				DiffColor.Deletion => $"\u001b[31m{line.Content}\u001b[0m", // Red
				DiffColor.ChunkHeader => $"\u001b[36m{line.Content}\u001b[0m", // Cyan
				DiffColor.FileHeader => $"\u001b[1;34m{line.Content}\u001b[0m", // Bold blue
				DiffColor.Default => line.Content,
				_ => line.Content
			};
			sb.AppendLine(coloredLine);
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

		IReadOnlyCollection<LineDifference> differences = FindDifferences(file1, file2);
		StringBuilder sb = new();

		BuildChangeSummaryHeader(sb, file1, file2);
		BuildStatisticsSummary(sb, differences);

		if (HasNoDifferences(differences))
		{
			sb.AppendLine("No differences found.");
			return sb.ToString();
		}

		sb.AppendLine();
		BuildDetailedChanges(sb, differences, useColor);

		return sb.ToString();
	}

	/// <summary>
	/// Builds the header section of the change summary.
	/// </summary>
	/// <param name="sb">StringBuilder to append to.</param>
	/// <param name="file1">Path to the first file.</param>
	/// <param name="file2">Path to the second file.</param>
	private static void BuildChangeSummaryHeader(StringBuilder sb, string file1, string file2)
	{
		sb.AppendLine($"Change Summary: {Path.GetFileName(file1)} vs {Path.GetFileName(file2)}");
		sb.AppendLine("----------------------------------------");
	}

	/// <summary>
	/// Builds the statistics summary section.
	/// </summary>
	/// <param name="sb">StringBuilder to append to.</param>
	/// <param name="differences">Collection of line differences.</param>
	private static void BuildStatisticsSummary(StringBuilder sb, IReadOnlyCollection<LineDifference> differences)
	{
		(int modifications, int additions, int deletions) = CountDifferenceTypes(differences);

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
	}

	/// <summary>
	/// Counts the different types of changes in the differences.
	/// </summary>
	/// <param name="differences">Collection of line differences.</param>
	/// <returns>Tuple of modification, addition, and deletion counts.</returns>
	private static (int modifications, int additions, int deletions) CountDifferenceTypes(IReadOnlyCollection<LineDifference> differences)
	{
		int modifications = differences.Count(d => d.LineNumber1.HasValue && d.LineNumber2.HasValue);
		int additions = differences.Count(d => !d.LineNumber1.HasValue && d.LineNumber2.HasValue);
		int deletions = differences.Count(d => d.LineNumber1.HasValue && !d.LineNumber2.HasValue);

		return (modifications, additions, deletions);
	}

	/// <summary>
	/// Checks if there are no differences found.
	/// </summary>
	/// <param name="differences">Collection of line differences.</param>
	/// <returns>True if no differences exist.</returns>
	private static bool HasNoDifferences(IReadOnlyCollection<LineDifference> differences)
	{
		(int modifications, int additions, int deletions) = CountDifferenceTypes(differences);
		return modifications == 0 && additions == 0 && deletions == 0;
	}

	/// <summary>
	/// Builds the detailed changes section.
	/// </summary>
	/// <param name="sb">StringBuilder to append to.</param>
	/// <param name="differences">Collection of line differences.</param>
	/// <param name="useColor">Whether to use color codes.</param>
	private static void BuildDetailedChanges(StringBuilder sb, IReadOnlyCollection<LineDifference> differences, bool useColor)
	{
		foreach (LineDifference diff in differences)
		{
			string diffLine = FormatDifferenceDetail(diff, useColor);
			if (diffLine.Length > 0)
			{
				sb.AppendLine(diffLine);
			}
		}
	}

	/// <summary>
	/// Formats a single difference for detailed display.
	/// </summary>
	/// <param name="diff">The line difference to format.</param>
	/// <param name="useColor">Whether to use color codes.</param>
	/// <returns>Formatted difference string.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3358:Ternary operators should not be nested", Justification = "<Pending>")]
	private static string FormatDifferenceDetail(LineDifference diff, bool useColor)
	{
		return diff.LineNumber1.HasValue && diff.LineNumber2.HasValue
			? FormatModifiedLine(diff, useColor)
			: !diff.LineNumber1.HasValue && diff.LineNumber2.HasValue
			? FormatAddedLine(diff, useColor)
			: diff.LineNumber1.HasValue && !diff.LineNumber2.HasValue
			? FormatDeletedLine(diff, useColor)
			: string.Empty;
	}

	/// <summary>
	/// Formats a modified line with optional color.
	/// </summary>
	/// <param name="diff">The line difference.</param>
	/// <param name="useColor">Whether to use color codes.</param>
	/// <returns>Formatted modified line string.</returns>
	private static string FormatModifiedLine(LineDifference diff, bool useColor)
	{
		(string prefix, string suffix) = GetColorCodes(useColor, "\u001b[33m"); // Yellow for modified
		return $"{prefix}Modified line {diff.LineNumber1}: {diff.Content1} → {diff.Content2}{suffix}";
	}

	/// <summary>
	/// Formats an added line with optional color.
	/// </summary>
	/// <param name="diff">The line difference.</param>
	/// <param name="useColor">Whether to use color codes.</param>
	/// <returns>Formatted added line string.</returns>
	private static string FormatAddedLine(LineDifference diff, bool useColor)
	{
		(string prefix, string suffix) = GetColorCodes(useColor, "\u001b[32m"); // Green for added
		return $"{prefix}Added line {diff.LineNumber2}: {diff.Content2}{suffix}";
	}

	/// <summary>
	/// Formats a deleted line with optional color.
	/// </summary>
	/// <param name="diff">The line difference.</param>
	/// <param name="useColor">Whether to use color codes.</param>
	/// <returns>Formatted deleted line string.</returns>
	private static string FormatDeletedLine(LineDifference diff, bool useColor)
	{
		(string prefix, string suffix) = GetColorCodes(useColor, "\u001b[31m"); // Red for deleted
		return $"{prefix}Deleted line {diff.LineNumber1}: {diff.Content1}{suffix}";
	}

	/// <summary>
	/// Gets color codes for formatting.
	/// </summary>
	/// <param name="useColor">Whether to use color codes.</param>
	/// <param name="colorCode">The ANSI color code to use.</param>
	/// <returns>Tuple of prefix and suffix color codes.</returns>
	private static (string prefix, string suffix) GetColorCodes(bool useColor, string colorCode) =>
		useColor ? (colorCode, "\u001b[0m") : ("", "");

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
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

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
			SecureTempFileHelper.SafeDeleteTempFiles(tempFile1, tempFile2);
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

		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

		try
		{
			return PerformMergeWithTempFiles(lines1, lines2, tempFile1, tempFile2);
		}
		finally
		{
			SecureTempFileHelper.SafeDeleteTempFiles(tempFile1, tempFile2);
		}
	}

	/// <summary>
	/// Performs the actual merge operation using temporary files
	/// </summary>
	private static MergeResult PerformMergeWithTempFiles(string[] lines1, string[] lines2, string tempFile1, string tempFile2)
	{
		File.WriteAllLines(tempFile1, lines1);
		File.WriteAllLines(tempFile2, lines2);

		IReadOnlyCollection<LineDifference> differences = DiffPlexDiffer.FindDifferences(tempFile1, tempFile2);
		List<string> mergedLines = [];
		List<MergeConflict> conflicts = [];

		return ProcessDifferences(differences, lines1, lines2, mergedLines, conflicts);
	}

	/// <summary>
	/// Processes all differences and builds the merged result
	/// </summary>
	private static MergeResult ProcessDifferences(IReadOnlyCollection<LineDifference> differences, string[] lines1, string[] lines2, List<string> mergedLines, List<MergeConflict> conflicts)
	{
		int line1Index = 0;
		int line2Index = 0;

		foreach (LineDifference diff in differences)
		{
			AddUnchangedLinesBeforeDifference(lines1, mergedLines, ref line1Index, ref line2Index, diff);
			ProcessSingleDifference(diff, mergedLines, conflicts, ref line1Index, ref line2Index);
		}

		AddRemainingLines(lines1, lines2, mergedLines, line1Index, line2Index);
		return new MergeResult(mergedLines.AsReadOnly(), conflicts.AsReadOnly());
	}

	/// <summary>
	/// Adds unchanged lines before processing a difference
	/// </summary>
	private static void AddUnchangedLinesBeforeDifference(string[] lines1, List<string> mergedLines, ref int line1Index, ref int line2Index, LineDifference diff)
	{
		while (line1Index < diff.LineNumber1 - 1 && line2Index < diff.LineNumber2 - 1)
		{
			mergedLines.Add(lines1[line1Index]);
			line1Index++;
			line2Index++;
		}
	}

	/// <summary>
	/// Processes a single difference and updates merge state
	/// </summary>
	private static void ProcessSingleDifference(LineDifference diff, List<string> mergedLines, List<MergeConflict> conflicts, ref int line1Index, ref int line2Index)
	{
		if (diff.LineNumber1 > 0 && diff.LineNumber2 > 0)
		{
			HandleBothFilesConflict(diff, mergedLines, conflicts);
		}
		else if (diff.LineNumber1 > 0)
		{
			HandleDeletionConflict(diff, mergedLines, conflicts);
		}
		else if (diff.LineNumber2 > 0)
		{
			HandleAdditionConflict(diff, mergedLines, conflicts);
		}

		UpdateLineIndices(diff, ref line1Index, ref line2Index);
	}

	/// <summary>
	/// Handles conflicts where both files have different content
	/// </summary>
	private static void HandleBothFilesConflict(LineDifference diff, List<string> mergedLines, List<MergeConflict> conflicts)
	{
		conflicts.Add(new MergeConflict(mergedLines.Count + 1, diff.Content1, diff.Content2, null, false));
		mergedLines.Add(ConflictMarkerStart);
		mergedLines.Add(diff.Content1 ?? "");
		mergedLines.Add(ConflictMarkerSeparator);
		mergedLines.Add(diff.Content2 ?? "");
		mergedLines.Add(ConflictMarkerEnd);
	}

	/// <summary>
	/// Handles conflicts for deleted lines
	/// </summary>
	private static void HandleDeletionConflict(LineDifference diff, List<string> mergedLines, List<MergeConflict> conflicts)
	{
		conflicts.Add(new MergeConflict(mergedLines.Count + 1, diff.Content1, null, null, false));
		mergedLines.Add(ConflictMarkerDeleted);
		mergedLines.Add(diff.Content1 ?? "");
		mergedLines.Add(ConflictMarkerSeparator);
		mergedLines.Add(ConflictMarkerDeletedEnd);
	}

	/// <summary>
	/// Handles conflicts for added lines
	/// </summary>
	private static void HandleAdditionConflict(LineDifference diff, List<string> mergedLines, List<MergeConflict> conflicts)
	{
		conflicts.Add(new MergeConflict(mergedLines.Count + 1, null, diff.Content2, null, false));
		mergedLines.Add(ConflictMarkerAdded);
		mergedLines.Add(ConflictMarkerSeparator);
		mergedLines.Add(diff.Content2 ?? "");
		mergedLines.Add(ConflictMarkerAddedEnd);
	}

	/// <summary>
	/// Updates line indices based on difference type
	/// </summary>
	private static void UpdateLineIndices(LineDifference diff, ref int line1Index, ref int line2Index)
	{
		if (diff.LineNumber1.HasValue && diff.LineNumber1 > 0)
		{
			line1Index = diff.LineNumber1.Value;
		}

		if (diff.LineNumber2.HasValue && diff.LineNumber2 > 0)
		{
			line2Index = diff.LineNumber2.Value;
		}
	}

	/// <summary>
	/// Adds any remaining lines from both files
	/// </summary>
	private static void AddRemainingLines(string[] lines1, string[] lines2, List<string> mergedLines, int line1Index, int line2Index)
	{
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
	}
}

