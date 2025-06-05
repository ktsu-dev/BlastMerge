// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.IO;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.Core.Models;
using Spectre.Console;

/// <summary>
/// Service for displaying file information and comparisons in the console.
/// </summary>
public static class FileDisplayService
{
	/// <summary>
	/// Shows a detailed list of files grouped by hash.
	/// </summary>
	/// <param name="fileGroups">The file groups to display.</param>
	public static void ShowDetailedFileList(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);
		AnsiConsole.Clear();
		UIHelper.ShowInfo("[bold cyan]Detailed File List[/]");
		AnsiConsole.WriteLine();

		Tree tree = new("[bold]File Groups[/]");

		int groupIndex = 1;
		foreach (KeyValuePair<string, IReadOnlyCollection<string>> group in fileGroups)
		{
			string groupStatus = group.Value.Count > 1 ? "[yellow]Multiple versions[/]" : "[green]Unique[/]";
			TreeNode groupNode = tree.AddNode($"[cyan]Group {groupIndex}[/] - {groupStatus} ({group.Value.Count} files)");
			groupNode.AddNode($"[dim]Hash: {group.Key[..Math.Min(8, group.Key.Length)]}...[/]");

			foreach (string filePath in group.Value)
			{
				try
				{
					FileInfo fileInfo = new(filePath);
					groupNode.AddNode($"[green]{filePath}[/] [dim]({fileInfo.Length:N0} bytes, {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss})[/]");
				}
				catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or FileNotFoundException or PathTooLongException or ArgumentException or IOException)
				{
					groupNode.AddNode($"[green]{filePath}[/] [dim]({ex.GetType().Name})[/]");
				}
			}

			groupIndex++;
		}

		AnsiConsole.Write(tree);
		AnsiConsole.WriteLine();
		UIHelper.WaitForKeyPress();
	}

	/// <summary>
	/// Shows differences between file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to show differences for.</param>
	public static void ShowDifferences(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);
		// Convert to FileGroup objects for easier handling
		List<FileGroup> groups = [.. fileGroups.Select(g => new FileGroup(g.Value))];

		// Filter to groups with multiple files
		List<FileGroup> groupsWithMultipleFiles = [.. groups.Where(g => g.FilePaths.Count > 1)];

		if (groupsWithMultipleFiles.Count == 0)
		{
			UIHelper.ShowWarning("No groups with multiple files to compare.");
			return;
		}

		foreach (FileGroup group in groupsWithMultipleFiles)
		{
			AnsiConsole.WriteLine();
			Rule rule = new($"[bold]Group with {group.FilePaths.Count} files[/]")
			{
				Style = Style.Parse("blue")
			};
			AnsiConsole.Write(rule);

			// Show all files in the group
			foreach (string file in group.FilePaths)
			{
				AnsiConsole.MarkupLine($"[dim]📁 {file}[/]");
			}

			// Compare first two files in the group
			if (group.FilePaths.Count >= 2)
			{
				string[] firstTwoFiles = [.. group.FilePaths.Take(2)];
				string file1 = firstTwoFiles[0];
				string file2 = firstTwoFiles[1];

				AnsiConsole.WriteLine();
				UIHelper.ShowWarning("Comparing first two files:");

				// Use centralized file comparison service
				FileComparisonDisplayService.ShowFileComparisonOptions(file1, file2);
			}

			AnsiConsole.WriteLine();
			UIHelper.WaitForKeyPress("Press any key to continue to next group...");
		}
	}

	/// <summary>
	/// Gets the relative directory name from a file path.
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <returns>The relative directory name.</returns>
	public static string GetRelativeDirectoryName(string filePath)
	{
		try
		{
			string? directory = Path.GetDirectoryName(filePath);
			if (string.IsNullOrEmpty(directory))
			{
				return Path.GetFileName(filePath);
			}

			string[] parts = directory.Split(Path.DirectorySeparatorChar);
			if (parts.Length >= 2)
			{
				// Return last two directory parts + filename
				return Path.Combine(parts[^2], parts[^1], Path.GetFileName(filePath));
			}
			else if (parts.Length == 1)
			{
				// Return last directory part + filename
				return Path.Combine(parts[^1], Path.GetFileName(filePath));
			}
			else
			{
				return Path.GetFileName(filePath);
			}
		}
		catch (ArgumentException)
		{
			return Path.GetFileName(filePath);
		}
	}

	/// <summary>
	/// Gets distinguishing labels for two file paths by finding the minimal unique parts.
	/// </summary>
	/// <param name="filePath1">First file path.</param>
	/// <param name="filePath2">Second file path.</param>
	/// <returns>A tuple containing distinguishing labels for both files.</returns>
	public static (string label1, string label2) GetDistinguishingLabels(string filePath1, string filePath2)
	{
		try
		{
			// Normalize paths
			string path1 = Path.GetFullPath(filePath1).Replace('\\', '/');
			string path2 = Path.GetFullPath(filePath2).Replace('\\', '/');

			// Split into components
			string[] parts1 = path1.Split('/', StringSplitOptions.RemoveEmptyEntries);
			string[] parts2 = path2.Split('/', StringSplitOptions.RemoveEmptyEntries);

			// Find the distinguishing parts
			(string[] unique1, string[] unique2) = FindDistinguishingParts(parts1, parts2);

			// Create labels with the filename
			string fileName = Path.GetFileName(filePath1); // Should be same for both
			string label1 = unique1.Length > 0 ? $"{string.Join("/", unique1)}/{fileName}" : fileName;
			string label2 = unique2.Length > 0 ? $"{string.Join("/", unique2)}/{fileName}" : fileName;

			return (label1, label2);
		}
		catch (ArgumentException)
		{
			// Fallback to simple filename if path processing fails
			string fileName = Path.GetFileName(filePath1);
			return (filePath1, filePath2);
		}
	}

	/// <summary>
	/// Finds the minimal distinguishing parts of two path component arrays.
	/// </summary>
	/// <param name="parts1">Path components of first file.</param>
	/// <param name="parts2">Path components of second file.</param>
	/// <returns>A tuple containing the distinguishing parts for each path.</returns>
	private static (string[] unique1, string[] unique2) FindDistinguishingParts(string[] parts1, string[] parts2)
	{
		// Find the first different component from the end (excluding filename)
		int len1 = parts1.Length - 1; // Exclude filename
		int len2 = parts2.Length - 1; // Exclude filename

		int lastCommonIndex = -1;
		int minLength = Math.Min(len1, len2);

		// Find common suffix (working backwards from the directory before filename)
		for (int i = 1; i <= minLength; i++)
		{
			int idx1 = len1 - i;
			int idx2 = len2 - i;

			if (idx1 >= 0 && idx2 >= 0 && parts1[idx1] == parts2[idx2])
			{
				lastCommonIndex = i;
			}
			else
			{
				break;
			}
		}

		// Determine how many distinguishing parts to show
		int showParts = Math.Max(1, Math.Min(3, Math.Max(len1, len2) - lastCommonIndex));

		// Extract distinguishing parts from the end
		string[] unique1 = ExtractDistinguishingParts(parts1, len1, showParts);
		string[] unique2 = ExtractDistinguishingParts(parts2, len2, showParts);

		return (unique1, unique2);
	}

	/// <summary>
	/// Extracts distinguishing parts from a path component array.
	/// </summary>
	/// <param name="parts">Path components.</param>
	/// <param name="endIndex">End index (excluding filename).</param>
	/// <param name="maxParts">Maximum parts to extract.</param>
	/// <returns>Array of distinguishing path components.</returns>
	private static string[] ExtractDistinguishingParts(string[] parts, int endIndex, int maxParts)
	{
		if (endIndex <= 0)
		{
			return [];
		}

		int startIndex = Math.Max(0, endIndex - maxParts);
		int count = endIndex - startIndex;

		string[] result = new string[count];
		Array.Copy(parts, startIndex, result, 0, count);

		return result;
	}
}
