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
			string groupStatus = group.Value.Count > 1 ? "[yellow]Multiple identical copies[/]" : "[green]Unique[/]";
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

		foreach (IReadOnlyCollection<string> filePaths in groupsWithMultipleFiles.Select(group => group.FilePaths))
		{
			AnsiConsole.WriteLine();
			Rule rule = new($"[bold]Group with {filePaths.Count} files[/]")
			{
				Style = Style.Parse("blue")
			};
			AnsiConsole.Write(rule);

			// Show all files in the group
			foreach (string file in filePaths)
			{
				AnsiConsole.MarkupLine($"[dim]📁 {file}[/]");
			}

			// Compare first two files in the group
			if (filePaths.Count >= 2)
			{
				string[] firstTwoFiles = [.. filePaths.Take(2)];
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
	/// Creates distinguished paths for two file paths by finding the minimal unique parts.
	/// Normalizes paths, removes common leading components, keeps differing components at same depth,
	/// ellipsizes matching internal components, and always keeps the leaf component.
	/// </summary>
	/// <param name="filePath1">First file path.</param>
	/// <param name="filePath2">Second file path.</param>
	/// <returns>A tuple containing distinguished paths for both files.</returns>
	public static (string path1, string path2) MakeDistinguishedPaths(string filePath1, string filePath2)
	{
		ArgumentNullException.ThrowIfNull(filePath1);
		ArgumentNullException.ThrowIfNull(filePath2);

		try
		{
			return ProcessDistinguishedPaths(filePath1, filePath2);
		}
		catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
		{
			return GetFallbackFileNames(filePath1, filePath2);
		}
	}

	/// <summary>
	/// Processes the distinguished paths logic.
	/// </summary>
	/// <param name="filePath1">First file path.</param>
	/// <param name="filePath2">Second file path.</param>
	/// <returns>A tuple containing distinguished paths for both files.</returns>
	private static (string path1, string path2) ProcessDistinguishedPaths(string filePath1, string filePath2)
	{
		string path1 = NormalizePath(filePath1);
		string path2 = NormalizePath(filePath2);

		string[] parts1 = path1.Split('/', StringSplitOptions.RemoveEmptyEntries);
		string[] parts2 = path2.Split('/', StringSplitOptions.RemoveEmptyEntries);

		if (string.Equals(path1, path2, StringComparison.Ordinal))
		{
			return GetIdenticalPathResult(parts1);
		}

		int commonLeadingCount = FindCommonLeadingComponents(parts1, parts2);
		string[] remaining1 = GetRemainingComponents(parts1, commonLeadingCount);
		string[] remaining2 = GetRemainingComponents(parts2, commonLeadingCount);

		if (remaining1.Length == 0 && remaining2.Length == 0)
		{
			return GetFileNameResult(parts1, parts2);
		}

		string distinguishedPath1 = BuildDistinguishingLabel(remaining1, remaining2);
		string distinguishedPath2 = BuildDistinguishingLabel(remaining2, remaining1);

		return (distinguishedPath1, distinguishedPath2);
	}

	/// <summary>
	/// Gets the fallback file names when path processing fails.
	/// </summary>
	/// <param name="filePath1">First file path.</param>
	/// <param name="filePath2">Second file path.</param>
	/// <returns>A tuple containing the file names.</returns>
	private static (string fileName1, string fileName2) GetFallbackFileNames(string filePath1, string filePath2)
	{
		string fileName1 = Path.GetFileName(filePath1);
		string fileName2 = Path.GetFileName(filePath2);
		return (fileName1, fileName2);
	}

	/// <summary>
	/// Gets the result for identical paths.
	/// </summary>
	/// <param name="parts">Path components.</param>
	/// <returns>A tuple containing the file name for both paths.</returns>
	private static (string fileName1, string fileName2) GetIdenticalPathResult(string[] parts)
	{
		string fileName = parts.Length > 0 ? parts[^1] : string.Empty;
		return (fileName, fileName);
	}

	/// <summary>
	/// Finds the number of common leading components between two path arrays.
	/// </summary>
	/// <param name="parts1">First path components.</param>
	/// <param name="parts2">Second path components.</param>
	/// <returns>The number of common leading components.</returns>
	private static int FindCommonLeadingComponents(string[] parts1, string[] parts2)
	{
		int commonLeadingCount = 0;
		int minLength = Math.Min(parts1.Length, parts2.Length);

		for (int i = 0; i < minLength; i++)
		{
			if (string.Equals(parts1[i], parts2[i], StringComparison.OrdinalIgnoreCase))
			{
				commonLeadingCount++;
			}
			else
			{
				break;
			}
		}

		return commonLeadingCount;
	}

	/// <summary>
	/// Gets the remaining components after removing common leading parts.
	/// </summary>
	/// <param name="parts">Original path components.</param>
	/// <param name="commonLeadingCount">Number of common leading components to remove.</param>
	/// <returns>The remaining components.</returns>
	private static string[] GetRemainingComponents(string[] parts, int commonLeadingCount) =>
		parts.Length > commonLeadingCount ? parts[commonLeadingCount..] : [];

	/// <summary>
	/// Gets the file name result when no remaining components exist.
	/// </summary>
	/// <param name="parts1">First path components.</param>
	/// <param name="parts2">Second path components.</param>
	/// <returns>A tuple containing the file names.</returns>
	private static (string fileName1, string fileName2) GetFileNameResult(string[] parts1, string[] parts2)
	{
		string fileName1 = parts1.Length > 0 ? parts1[^1] : string.Empty;
		string fileName2 = parts2.Length > 0 ? parts2[^1] : string.Empty;
		return (fileName1, fileName2);
	}

	/// <summary>
	/// Normalizes a path by handling relative paths and ensuring consistent separators.
	/// Preserves drive letter case and handles both absolute and relative paths.
	/// </summary>
	/// <param name="path">The path to normalize.</param>
	/// <returns>A normalized path with forward slashes.</returns>
	private static string NormalizePath(string path)
	{
		// Replace backslashes with forward slashes
		string normalized = path.Replace('\\', '/');

		// Handle relative paths (don't expand to full path)
		if (!Path.IsPathRooted(path))
		{
			// For relative paths, just normalize path separators
			return normalized;
		}

		// For absolute paths, get full path but then convert case appropriately
		try
		{
			string fullPath = Path.GetFullPath(path).Replace('\\', '/');

			// Convert drive letters to lowercase and remove colon to match test expectations
			if (fullPath.Length >= 2 && fullPath[1] == ':')
			{
				fullPath = char.ToLowerInvariant(fullPath[0]) + fullPath[2..];
			}

			return fullPath;
		}
		catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException or UnauthorizedAccessException)
		{
			// If Path.GetFullPath fails, just use the original normalized path
			return normalized;
		}
	}

	/// <summary>
	/// Builds a distinguishing label from path components, ellipsizing matching internal components.
	/// </summary>
	/// <param name="components">Path components for this path.</param>
	/// <param name="otherComponents">Path components for the other path.</param>
	/// <returns>A distinguishing label string.</returns>
	private static string BuildDistinguishingLabel(string[] components, string[] otherComponents)
	{
		if (components.Length == 0)
		{
			return string.Empty;
		}

		if (components.Length == 1)
		{
			return components[0];
		}

		List<string> labelParts = ProcessLabelComponents(components, otherComponents);
		labelParts.Add(components[^1]); // Always add the leaf component (filename)

		List<string> cleanedParts = CleanConsecutiveEllipses(labelParts);
		return string.Join("/", cleanedParts);
	}

	/// <summary>
	/// Processes the label components excluding the filename.
	/// </summary>
	/// <param name="components">Path components for this path.</param>
	/// <param name="otherComponents">Path components for the other path.</param>
	/// <returns>List of processed label parts.</returns>
	private static List<string> ProcessLabelComponents(string[] components, string[] otherComponents)
	{
		List<string> labelParts = [];

		for (int i = 0; i < components.Length - 1; i++) // Exclude filename from this loop
		{
			string currentComponent = components[i];

			if (ShouldEllipsizeComponent(currentComponent, otherComponents, i))
			{
				AddEllipsisIfNeeded(labelParts);
			}
			else
			{
				labelParts.Add(currentComponent);
			}
		}

		return labelParts;
	}

	/// <summary>
	/// Determines if a component should be ellipsized.
	/// </summary>
	/// <param name="currentComponent">The current component.</param>
	/// <param name="otherComponents">The other path components.</param>
	/// <param name="index">The current index.</param>
	/// <returns>True if the component should be ellipsized.</returns>
	private static bool ShouldEllipsizeComponent(string currentComponent, string[] otherComponents, int index) =>
		index < otherComponents.Length - 1 &&
		string.Equals(currentComponent, otherComponents[index], StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Adds an ellipsis if needed (not already present and not at the beginning).
	/// </summary>
	/// <param name="labelParts">The label parts list.</param>
	private static void AddEllipsisIfNeeded(List<string> labelParts)
	{
		if (labelParts.Count > 0 && labelParts[^1] != "...")
		{
			labelParts.Add("...");
		}
	}

	/// <summary>
	/// Cleans up consecutive ellipses from the label parts.
	/// </summary>
	/// <param name="labelParts">The original label parts.</param>
	/// <returns>Cleaned label parts without consecutive ellipses.</returns>
	private static List<string> CleanConsecutiveEllipses(List<string> labelParts)
	{
		List<string> cleanedParts = [];
		for (int i = 0; i < labelParts.Count; i++)
		{
			if (labelParts[i] == "..." && cleanedParts.Count > 0 && cleanedParts[^1] == "...")
			{
				continue; // Skip consecutive ellipses
			}
			cleanedParts.Add(labelParts[i]);
		}
		return cleanedParts;
	}
}
