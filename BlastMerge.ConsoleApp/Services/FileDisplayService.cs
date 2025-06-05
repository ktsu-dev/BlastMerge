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
}
