// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.CLI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;

/// <summary>
/// Main program class for the DiffMore CLI
/// </summary>
public static class Program
{
	/// <summary>
	/// Entry point for the application
	/// </summary>
	/// <param name="args">Command line arguments</param>
	public static void Main(string[] args)
	{
		try
		{
			// Validate args parameter is non-null
			ArgumentNullException.ThrowIfNull(args);

			if (args.Length < 2)
			{
				PrintUsage();
				return;
			}

			var directory = args[0];
			var fileName = args[1];

			if (!Directory.Exists(directory))
			{
				Console.WriteLine($"Error: Directory '{directory}' does not exist.");
				return;
			}

			// Find all files with the specified name
			Console.WriteLine($"Searching for '{fileName}' in '{directory}'...");
			var files = FileFinder.FindFiles(directory, fileName);
			var filesList = files.ToList();

			if (filesList.Count == 0)
			{
				Console.WriteLine($"No files with name '{fileName}' found.");
				return;
			}

			Console.WriteLine($"Found {filesList.Count} files.");

			// Group files by hash
			var fileGroups = FileDiffer.GroupFilesByHash(files);
			var fileGroupsList = fileGroups.ToList();

			// Sort groups by number of files (descending)
			fileGroupsList.Sort((a, b) => b.FilePaths.Count.CompareTo(a.FilePaths.Count));

			// Display groups
			Console.WriteLine($"\nFound {fileGroupsList.Count} unique versions:");

			for (var i = 0; i < fileGroupsList.Count; i++)
			{
				var group = fileGroupsList[i];
				Console.WriteLine($"\nVersion {i + 1} (Hash: {group.Hash}):");

				foreach (var file in group.FilePaths)
				{
					Console.WriteLine($"  {file}");
				}
			}

			// If there's only one version, no need to compare
			if (fileGroupsList.Count <= 1)
			{
				Console.WriteLine("\nAll files are identical.");
				return;
			}

			// Show differences between versions using git-style diff
			Console.WriteLine("\nDifferences between versions:");

			var group1 = fileGroupsList[0]; // Version 1 (most common version)
			var file1 = group1.FilePaths.First();

			// Compare Version 1 with all other versions
			for (var j = 1; j < fileGroupsList.Count; j++)
			{
				var group2 = fileGroupsList[j];
				var file2 = group2.FilePaths.First();

				Console.WriteLine($"\n=== Diff between Version 1 and Version {j + 1} ===");
				Console.WriteLine($"--- {file1}");
				Console.WriteLine($"+++ {file2}");

				// Generate and display git-style diff
				var gitDiff = FileDiffer.GenerateGitStyleDiff(file1, file2, true);
				Console.WriteLine(gitDiff);
			}

			// Sync option
			Console.WriteLine("\nDo you want to sync one version to others? (y/n)");
			var response = Console.ReadLine()?.ToLower();

			if (response is "y" or "yes")
			{
				SyncFiles(fileGroupsList);
			}
		}
		catch (IOException ex)
		{
			Console.WriteLine($"I/O Error: {ex.Message}");
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.WriteLine($"Access Error: {ex.Message}");
		}
		catch (ArgumentException ex)
		{
			Console.WriteLine($"Argument Error: {ex.Message}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Unexpected error: {ex.Message}");
			// Rethrow unexpected exceptions for debugging
			throw;
		}
	}

	/// <summary>
	/// Syncs files based on user selection
	/// </summary>
	/// <param name="fileGroups">The file groups to sync</param>
	private static void SyncFiles(List<FileGroup> fileGroups)
	{
		Console.WriteLine($"\nSelect a version to sync to (1-{fileGroups.Count}):");

		if (!int.TryParse(Console.ReadLine(), out var selectedVersion) ||
			selectedVersion < 1 ||
			selectedVersion > fileGroups.Count)
		{
			Console.WriteLine("Invalid selection.");
			return;
		}

		var sourceGroup = fileGroups[selectedVersion - 1];
		var sourceFile = sourceGroup.FilePaths.First();

		Console.WriteLine($"\nSyncing from: {sourceFile}");

		// For each other group, ask which files to sync
		var otherGroups = fileGroups.Where((g, i) => i != selectedVersion - 1).ToList();

		foreach (var group in otherGroups)
		{
			var groupFiles = group.FilePaths.ToList();
			Console.WriteLine($"\nFiles with hash {group.Hash}:");

			for (var i = 0; i < groupFiles.Count; i++)
			{
				Console.WriteLine($"  {i + 1}. {groupFiles[i]}");
			}

			Console.WriteLine("Enter numbers of files to sync (comma-separated), or 'all' for all files:");
			var input = Console.ReadLine()?.Trim();

			if (string.IsNullOrEmpty(input))
			{
				continue;
			}

			List<string> filesToSync = [];

			if (string.Equals(input, "all", StringComparison.OrdinalIgnoreCase))
			{
				filesToSync = groupFiles;
			}
			else
			{
				var indices = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(s => s.Trim())
					.Where(s => int.TryParse(s, out var idx) && idx >= 1 && idx <= groupFiles.Count)
					.Select(s => int.Parse(s) - 1)
					.ToList();

				filesToSync = [.. indices.Select(i => groupFiles[i])];
			}

			foreach (var file in filesToSync)
			{
				try
				{
					FileDiffer.SyncFile(sourceFile, file);
					Console.WriteLine($"  Synced to: {file}");
				}
				catch (IOException ex)
				{
					Console.WriteLine($"  Failed to sync to {file}: I/O Error - {ex.Message}");
				}
				catch (UnauthorizedAccessException ex)
				{
					Console.WriteLine($"  Failed to sync to {file}: Access Error - {ex.Message}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  Failed to sync to {file}: {ex.Message}");
					// Rethrow unexpected exceptions for debugging
					throw;
				}
			}
		}

		Console.WriteLine("\nSync complete.");
	}

	/// <summary>
	/// Prints usage instructions
	/// </summary>
	private static void PrintUsage()
	{
		Console.WriteLine("Usage: DiffMore.CLI <directory> <filename>");
		Console.WriteLine("  <directory> - The root directory to search in");
		Console.WriteLine("  <filename>  - The filename to search for");
	}
}
