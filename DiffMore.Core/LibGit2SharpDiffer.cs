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
using LibGit2Sharp;

/// <summary>
/// Provides Git-based diffing functionality using LibGit2Sharp for industry-standard diff algorithms
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

		// Check if files exist and handle non-existent files
		var content1 = File.Exists(file1) ? File.ReadAllText(file1) : string.Empty;
		var content2 = File.Exists(file2) ? File.ReadAllText(file2) : string.Empty;

		// If contents are identical, return empty string
		if (content1 == content2)
		{
			return string.Empty;
		}

		// Create a temporary Git repository to use LibGit2Sharp diffing
		var tempDir = Path.Combine(Path.GetTempPath(), $"diffmore-{Guid.NewGuid()}");

		try
		{
			Directory.CreateDirectory(tempDir);
			Repository.Init(tempDir);

			using var repo = new Repository(tempDir);

			// Create signature for commits
			var signature = new Signature("DiffMore", "diffmore@temp.local", DateTimeOffset.Now);

			// Create temp filenames
			var fileName = "comparison.txt";
			var filePath = Path.Combine(tempDir, fileName);

			// Write first version and commit
			File.WriteAllText(filePath, content1);
			Commands.Stage(repo, fileName);
			var commit1 = repo.Commit("First version", signature, signature);

			// Write second version and commit
			File.WriteAllText(filePath, content2);
			Commands.Stage(repo, fileName);
			var commit2 = repo.Commit("Second version", signature, signature);

			// Configure compare options
			var compareOptions = new CompareOptions
			{
				ContextLines = contextLines,
				InterhunkLines = 0
			};

			// Generate patch between the two commits
			var patch = repo.Diff.Compare<Patch>(commit1.Tree, commit2.Tree, compareOptions);

			// Format the patch to match git diff output format
			var result = FormatPatchOutput(patch, Path.GetFileName(file1), Path.GetFileName(file2));
			return result;
		}
		finally
		{
			// Clean up temporary directory
			if (Directory.Exists(tempDir))
			{
				try
				{
					Directory.Delete(tempDir, recursive: true);
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}
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

		// First check file sizes for quick comparison
		var fileInfo1 = new FileInfo(file1);
		var fileInfo2 = new FileInfo(file2);

		if (fileInfo1.Length != fileInfo2.Length)
		{
			return false;
		}

		// For small files, compare content directly
		if (fileInfo1.Length < 1024 * 1024) // 1MB threshold
		{
			var content1 = File.ReadAllText(file1);
			var content2 = File.ReadAllText(file2);
			return content1 == content2;
		}

		// For larger files, use Git's object comparison for efficiency
		var tempDir = Path.Combine(Path.GetTempPath(), $"diffmore-compare-{Guid.NewGuid()}");

		try
		{
			Directory.CreateDirectory(tempDir);
			Repository.Init(tempDir);

			using var repo = new Repository(tempDir);

			var signature = new Signature("DiffMore", "diffmore@temp.local", DateTimeOffset.Now);
			var fileName = "comparison.txt";
			var filePath = Path.Combine(tempDir, fileName);

			// Create blob from first file
			File.Copy(file1, filePath, overwrite: true);
			Commands.Stage(repo, fileName);
			var commit1 = repo.Commit("First version", signature, signature);

			// Create blob from second file
			File.Copy(file2, filePath, overwrite: true);
			Commands.Stage(repo, fileName);
			var commit2 = repo.Commit("Second version", signature, signature);

			// Compare the tree entries
			var tree1 = commit1.Tree;
			var tree2 = commit2.Tree;

			return tree1[fileName].Target.Sha == tree2[fileName].Target.Sha;
		}
		finally
		{
			if (Directory.Exists(tempDir))
			{
				try
				{
					Directory.Delete(tempDir, recursive: true);
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}
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
	/// Finds differences between two files using LibGit2Sharp
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
			else if (line.StartsWith(' '))
			{
				// Context line - increment both counters
				currentOldLine++;
				currentNewLine++;
			}
		}

		return differences.AsReadOnly();
	}

	/// <summary>
	/// Parses a hunk header to extract line number information
	/// </summary>
	/// <param name="hunkHeader">The hunk header line (e.g., "@@ -1,4 +1,5 @@")</param>
	/// <returns>Tuple containing old start line and new start line</returns>
	private static (int oldStart, int newStart) ParseHunkHeader(string hunkHeader)
	{
		try
		{
			// Format: @@ -oldStart,oldCount +newStart,newCount @@
			var parts = hunkHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length >= 3)
			{
				var oldPart = parts[1]; // -oldStart,oldCount
				var newPart = parts[2]; // +newStart,newCount

				var oldStart = int.Parse(oldPart.Split(',')[0][1..]); // Remove '-' and parse
				var newStart = int.Parse(newPart.Split(',')[0][1..]); // Remove '+' and parse

				return (oldStart, newStart);
			}
		}
		catch
		{
			// Fall back to defaults if parsing fails
		}

		return (1, 1);
	}

	/// <summary>
	/// Formats the LibGit2Sharp patch output to match standard git diff format
	/// </summary>
	/// <param name="patch">The patch object from LibGit2Sharp</param>
	/// <param name="fileName1">Name of the first file</param>
	/// <param name="fileName2">Name of the second file</param>
	/// <returns>Formatted git diff string</returns>
	private static string FormatPatchOutput(Patch patch, string fileName1, string fileName2)
	{
		if (patch == null || !patch.Any())
		{
			return string.Empty;
		}

		var sb = new StringBuilder();

		foreach (var filePatch in patch)
		{
			// Add file header
			sb.AppendLine($"--- a/{fileName1}");
			sb.AppendLine($"+++ b/{fileName2}");

			// Add hunks
			foreach (var hunk in filePatch.Hunks)
			{
				// Add hunk header
				sb.AppendLine($"@@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@");

				// Add hunk content
				foreach (var line in hunk.Lines)
				{
					switch (line.Origin)
					{
						case ChangeKind.Added:
							sb.AppendLine($"+{line.Content.TrimEnd('\n')}");
							break;
						case ChangeKind.Deleted:
							sb.AppendLine($"-{line.Content.TrimEnd('\n')}");
							break;
						case ChangeKind.Unmodified:
							sb.AppendLine($" {line.Content.TrimEnd('\n')}");
							break;
					}
				}
			}
		}

		return sb.ToString();
	}
}
