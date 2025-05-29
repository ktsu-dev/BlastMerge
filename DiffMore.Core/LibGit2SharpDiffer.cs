// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Core;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using LibGit2Sharp;

/// <summary>
/// Provides Git-based diffing functionality using LibGit2Sharp
/// </summary>
public static class LibGit2SharpDiffer
{
	/// <summary>
	/// Generates a git-style diff between two files using LibGit2Sharp
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="contextLines">Number of context lines to include (default: 3)</param>
	/// <returns>Git-style diff output</returns>
	public static string GenerateGitStyleDiff(string file1, string file2, int contextLines = 3)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		// Create a temporary repository to use LibGit2Sharp diffing
		var tempRepoPath = Path.Combine(Path.GetTempPath(), $"diffmore_temp_{Guid.NewGuid()}");

		try
		{
			Directory.CreateDirectory(tempRepoPath);
			Repository.Init(tempRepoPath);

			using var repo = new Repository(tempRepoPath);

			// Copy files to temp repo with consistent names
			var tempFile1 = Path.Combine(tempRepoPath, "file_version1.txt");
			var tempFile2 = Path.Combine(tempRepoPath, "file_version2.txt");

			File.Copy(file1, tempFile1);
			File.Copy(file2, tempFile2);

			// Stage and commit first version
			Commands.Stage(repo, "file_version1.txt");
			var signature = new Signature("DiffMore", "diffmore@temp.local", DateTimeOffset.Now);
			var commit1 = repo.Commit("First version", signature, signature);

			// Replace with second version and stage
			File.Delete(tempFile1);
			File.Move(tempFile2, tempFile1);
			Commands.Stage(repo, "file_version1.txt");
			var commit2 = repo.Commit("Second version", signature, signature);

			// Generate diff between commits
			var compareOptions = new CompareOptions
			{
				ContextLines = contextLines,
				InterhunkLines = 1
			};

			var patch = repo.Diff.Compare<Patch>(commit1.Tree, commit2.Tree, compareOptions);

			return patch.Content;
		}
		finally
		{
			// Clean up temporary repository
			if (Directory.Exists(tempRepoPath))
			{
				Directory.Delete(tempRepoPath, recursive: true);
			}
		}
	}

	/// <summary>
	/// Generates colored diff lines using LibGit2Sharp for Git-standard processing
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <param name="contextLines">Number of context lines to include</param>
	/// <returns>Collection of colored diff lines</returns>
	public static Collection<ColoredDiffLine> GenerateColoredDiff(string file1, string file2, int contextLines = 3)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var result = new Collection<ColoredDiffLine>();
		var gitDiff = GenerateGitStyleDiff(file1, file2, contextLines);

		if (string.IsNullOrEmpty(gitDiff))
		{
			return result;
		}

		var lines = gitDiff.Split('\n');

		foreach (var line in lines)
		{
			var color = DetermineLineColor(line);
			result.Add(new ColoredDiffLine
			{
				Content = line,
				Color = color
			});
		}

		return result;
	}

	/// <summary>
	/// Converts LibGit2Sharp patch to DiffMore LineDifference format
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>Collection of line differences compatible with existing DiffMore API</returns>
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

		// Parse the git diff output to extract line differences
		var lines = gitDiff.Split('\n');
		var currentHunk = ParseDiffHunk(lines);

		differences.AddRange(currentHunk);

		return differences.AsReadOnly();
	}

	/// <summary>
	/// Compares two files directly using LibGit2Sharp blob comparison
	/// </summary>
	/// <param name="file1">Path to the first file</param>
	/// <param name="file2">Path to the second file</param>
	/// <returns>True if files are identical, false otherwise</returns>
	public static bool AreFilesIdentical(string file1, string file2)
	{
		ArgumentNullException.ThrowIfNull(file1);
		ArgumentNullException.ThrowIfNull(file2);

		var tempRepoPath = Path.Combine(Path.GetTempPath(), $"diffmore_compare_{Guid.NewGuid()}");

		try
		{
			Directory.CreateDirectory(tempRepoPath);
			Repository.Init(tempRepoPath);

			using var repo = new Repository(tempRepoPath);

			// Create blobs for both files
			var blob1 = repo.ObjectDatabase.CreateBlob(file1);
			var blob2 = repo.ObjectDatabase.CreateBlob(file2);

			return blob1.Equals(blob2);
		}
		finally
		{
			if (Directory.Exists(tempRepoPath))
			{
				Directory.Delete(tempRepoPath, recursive: true);
			}
		}
	}

	/// <summary>
	/// Determines the appropriate color for a diff line based on Git diff format
	/// </summary>
	/// <param name="line">The diff line to analyze</param>
	/// <returns>The appropriate DiffColor for the line</returns>
	private static DiffColor DetermineLineColor(string line)
	{
		return string.IsNullOrEmpty(line) ? DiffColor.Default : line[0] switch
		{
			'+' when line.StartsWith("+++") => DiffColor.FileHeader,
			'+' => DiffColor.Addition,
			'-' when line.StartsWith("---") => DiffColor.FileHeader,
			'-' => DiffColor.Deletion,
			'@' when line.StartsWith("@@") => DiffColor.ChunkHeader,
			'd' when line.StartsWith("diff --git") => DiffColor.FileHeader,
			'i' when line.StartsWith("index") => DiffColor.FileHeader,
			_ => DiffColor.Default
		};
	}

	/// <summary>
	/// Parses a git diff hunk to extract line differences
	/// </summary>
	/// <param name="diffLines">Lines from the git diff output</param>
	/// <returns>Collection of LineDifference objects</returns>
	private static List<LineDifference> ParseDiffHunk(string[] diffLines)
	{
		var differences = new List<LineDifference>();
		var line1Number = 0;
		var line2Number = 0;
		var inHunk = false;

		foreach (var line in diffLines)
		{
			if (line.StartsWith("@@"))
			{
				// Parse hunk header to get starting line numbers
				var (startLine1, startLine2) = ParseHunkHeader(line);
				line1Number = startLine1;
				line2Number = startLine2;
				inHunk = true;
				continue;
			}

			if (!inHunk || string.IsNullOrEmpty(line))
			{
				continue;
			}

			switch (line[0])
			{
				case '+':
					differences.Add(new LineDifference
					{
						LineNumber1 = 0,
						LineNumber2 = line2Number++,
						Content1 = null,
						Content2 = line[1..] // Remove the '+' prefix
					});
					break;

				case '-':
					differences.Add(new LineDifference
					{
						LineNumber1 = line1Number++,
						LineNumber2 = 0,
						Content1 = line[1..], // Remove the '-' prefix
						Content2 = null
					});
					break;

				case ' ':
					// Context line - increment both counters but don't add as difference
					line1Number++;
					line2Number++;
					break;

				default:
					// Handle other git diff metadata lines
					break;
			}
		}

		return differences;
	}

	/// <summary>
	/// Parses a git diff hunk header to extract starting line numbers
	/// </summary>
	/// <param name="hunkHeader">The hunk header line (e.g., "@@ -1,4 +1,6 @@")</param>
	/// <returns>Tuple containing starting line numbers for both files</returns>
	private static (int startLine1, int startLine2) ParseHunkHeader(string hunkHeader)
	{
		// Format: @@ -oldStart,oldCount +newStart,newCount @@
		var parts = hunkHeader.Split(' ');

		if (parts.Length < 3)
		{
			return (1, 1);
		}

		var oldPart = parts[1][1..]; // Remove '-' prefix
		var newPart = parts[2][1..]; // Remove '+' prefix

		var oldStartStr = oldPart.Split(',')[0];
		var newStartStr = newPart.Split(',')[0];

		var oldStart = int.TryParse(oldStartStr, out var old) ? old : 1;
		var newStart = int.TryParse(newStartStr, out var @new) ? @new : 1;

		return (oldStart, newStart);
	}
}
