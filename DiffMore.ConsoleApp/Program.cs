// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.CLI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;
using Spectre.Console;

/// <summary>
/// Main program class for the DiffMore CLI TUI
/// </summary>
public static class Program
{
	/// <summary>
	/// Entry point for the application
	/// </summary>
	/// <param name="args">Command line arguments</param>
	public static void Main(string[] args)
	{
		ArgumentNullException.ThrowIfNull(args);

		Console.OutputEncoding = System.Text.Encoding.UTF8;

		try
		{
			ShowBanner();

			// If args are provided, try to use them directly
			if (args.Length >= 2)
			{
				var directory = args[0];
				var fileName = args[1];

				if (Directory.Exists(directory))
				{
					ProcessFiles(directory, fileName);
					return;
				}
			}

			// Interactive TUI mode
			RunInteractiveMode();
		}
		catch (IOException ex)
		{
			AnsiConsole.WriteException(ex);
		}
		catch (UnauthorizedAccessException ex)
		{
			AnsiConsole.WriteException(ex);
		}
		catch (ArgumentException ex)
		{
			AnsiConsole.WriteException(ex);
		}
	}

	/// <summary>
	/// Shows the application banner
	/// </summary>
	private static void ShowBanner()
	{
		var rule = new Rule("[bold yellow]DiffMore[/] - File Comparison Tool")
		{
			Style = Style.Parse("blue")
		};
		AnsiConsole.Write(rule);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Runs the interactive TUI mode
	/// </summary>
	private static void RunInteractiveMode()
	{
		while (true)
		{
			var choice = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("[green]What would you like to do?[/]")
					.AddChoices([
						"🔍 Compare Files in Directory",
						"📁 Compare Two Directories",
						"📄 Compare Two Specific Files",
						"🔀 Iterative Merge Multiple Versions",
						"ℹ️  Show Help",
						"❌ Exit"
					]));

			if (choice.Contains("Compare Files in Directory"))
			{
				CompareFilesInDirectory();
			}
			else if (choice.Contains("Compare Two Directories"))
			{
				CompareTwoDirectories();
			}
			else if (choice.Contains("Compare Two Specific Files"))
			{
				CompareTwoSpecificFiles();
			}
			else if (choice.Contains("Iterative Merge Multiple Versions"))
			{
				RunIterativeMerge();
			}
			else if (choice.Contains("Show Help"))
			{
				ShowHelp();
			}
			else if (choice.Contains("Exit"))
			{
				return;
			}

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
			Console.ReadKey(true);
			AnsiConsole.Clear();
			ShowBanner();
		}
	}

	/// <summary>
	/// Compares files with the same name in a directory
	/// </summary>
	private static void CompareFilesInDirectory()
	{
		var directory = AnsiConsole.Ask<string>("[cyan]Enter the directory path:[/]");

		if (!Directory.Exists(directory))
		{
			AnsiConsole.MarkupLine("[red]Error: Directory does not exist![/]");
			return;
		}

		var fileName = AnsiConsole.Ask<string>("[cyan]Enter the filename to search for:[/]");

		ProcessFiles(directory, fileName);
	}

	/// <summary>
	/// Compares two directories
	/// </summary>
	private static void CompareTwoDirectories()
	{
		var dir1 = AnsiConsole.Ask<string>("[cyan]Enter the first directory path:[/]");

		if (!Directory.Exists(dir1))
		{
			AnsiConsole.MarkupLine("[red]Error: First directory does not exist![/]");
			return;
		}

		var dir2 = AnsiConsole.Ask<string>("[cyan]Enter the second directory path:[/]");

		if (!Directory.Exists(dir2))
		{
			AnsiConsole.MarkupLine("[red]Error: Second directory does not exist![/]");
			return;
		}

		var pattern = AnsiConsole.Ask("[cyan]Enter file pattern (e.g., *.txt, *.cs):[/]", "*.*");
		var recursive = AnsiConsole.Confirm("[cyan]Search subdirectories recursively?[/]", false);

		CompareDirectories(dir1, dir2, pattern, recursive);
	}

	/// <summary>
	/// Compares two specific files
	/// </summary>
	private static void CompareTwoSpecificFiles()
	{
		var file1 = AnsiConsole.Ask<string>("[cyan]Enter the first file path:[/]");

		if (!File.Exists(file1))
		{
			AnsiConsole.MarkupLine("[red]Error: First file does not exist![/]");
			return;
		}

		var file2 = AnsiConsole.Ask<string>("[cyan]Enter the second file path:[/]");

		if (!File.Exists(file2))
		{
			AnsiConsole.MarkupLine("[red]Error: Second file does not exist![/]");
			return;
		}

		CompareTwoFiles(file1, file2);
	}

	/// <summary>
	/// Shows help information
	/// </summary>
	private static void ShowHelp()
	{
		var panel = new Panel("""
		[bold]DiffMore - File Comparison Tool[/]

		[yellow]Features:[/]
		• Compare files with the same name across directories
		• Compare two directories with file patterns
		• Compare two specific files
		• [cyan]Iterative merge multiple file versions[/]
		• View differences in multiple formats (git-style, change summary)
		• Sync files to make them identical

		[yellow]Command Line Usage:[/]
		DiffMore.CLI <directory> <filename>

		[yellow]Interactive Mode:[/]
		Run without arguments to use the interactive TUI interface.

		[yellow]Iterative Merge:[/]
		• Automatically finds the most similar files and merges them step by step
		• Interactive conflict resolution with visual TUI
		• Optimal merge order based on similarity calculation
		• Save final merged result to a new file
		""")
		{
			Header = new PanelHeader("[bold blue]Help[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(panel);
	}

	/// <summary>
	/// Processes files with the same name in a directory
	/// </summary>
	/// <param name="directory">The directory to search</param>
	/// <param name="fileName">The filename to search for</param>
	private static void ProcessFiles(string directory, string fileName)
	{
		// Show progress while searching
		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start($"[yellow]Searching for '{fileName}' in '{directory}'...[/]", ctx =>
			{
				var files = FileFinder.FindFiles(directory, fileName);
				var filesList = files.ToList();

				ctx.Status($"[yellow]Found {filesList.Count} files. Analyzing...[/]");

				if (filesList.Count == 0)
				{
					AnsiConsole.MarkupLine($"[red]No files with name '{fileName}' found.[/]");
					return;
				}

				// Group files by hash
				var fileGroups = FileDiffer.GroupFilesByHash(files);
				var fileGroupsList = fileGroups.ToList();

				// Sort groups by number of files (descending)
				fileGroupsList.Sort((a, b) => b.FilePaths.Count.CompareTo(a.FilePaths.Count));

				ShowFileGroups(fileGroupsList, filesList.Count);

				if (fileGroupsList.Count <= 1)
				{
					AnsiConsole.MarkupLine("[green]All files are identical.[/]");
					return;
				}

				ShowDifferences(fileGroupsList);
				OfferSyncOptions(fileGroupsList);
			});
	}

	/// <summary>
	/// Shows file groups in a formatted table
	/// </summary>
	/// <param name="fileGroups">The file groups to display</param>
	/// <param name="totalFiles">Total number of files found</param>
	private static void ShowFileGroups(List<FileGroup> fileGroups, int totalFiles)
	{
		AnsiConsole.MarkupLine($"[green]Found {totalFiles} files in {fileGroups.Count} unique versions:[/]");
		AnsiConsole.WriteLine();

		var table = new Table()
			.AddColumn("[bold]Version[/]")
			.AddColumn("[bold]Files Count[/]")
			.AddColumn("[bold]Hash[/]")
			.AddColumn("[bold]Files[/]")
			.Border(TableBorder.Rounded);

		for (var i = 0; i < fileGroups.Count; i++)
		{
			var group = fileGroups[i];
			var filesDisplay = string.Join("\n", group.FilePaths.Take(3));
			if (group.FilePaths.Count > 3)
			{
				filesDisplay += $"\n[dim]... and {group.FilePaths.Count - 3} more[/]";
			}

			table.AddRow(
				$"[yellow]{i + 1}[/]",
				$"[cyan]{group.FilePaths.Count}[/]",
				$"[dim]{group.Hash[..8]}...[/]",
				filesDisplay
			);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows differences between file versions
	/// </summary>
	/// <param name="fileGroups">The file groups to compare</param>
	private static void ShowDifferences(List<FileGroup> fileGroups)
	{
		if (!AnsiConsole.Confirm("[cyan]Would you like to see the differences between versions?[/]", true))
		{
			return;
		}

		var diffFormat = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Choose diff format:[/]")
				.AddChoices([
					"📊 Change Summary (Added/Removed lines only)",
					"🔧 Git-style Diff (Full context)",
					"🎨 Colored Diff (Rich formatting)"
				]));

		var group1 = fileGroups[0]; // Most common version
		var file1 = group1.FilePaths.First();

		for (var j = 1; j < fileGroups.Count; j++)
		{
			var group2 = fileGroups[j];
			var file2 = group2.FilePaths.First();

			var rule = new Rule($"[bold]Version 1 vs Version {j + 1}[/]")
			{
				Style = Style.Parse("blue")
			};
			AnsiConsole.Write(rule);

			AnsiConsole.MarkupLine($"[dim]Comparing:[/]");
			AnsiConsole.MarkupLine($"[dim]  📁 {file1}[/]");
			AnsiConsole.MarkupLine($"[dim]  📁 {file2}[/]");
			AnsiConsole.WriteLine();

			if (diffFormat.Contains("📊"))
			{
				ShowChangeSummary(file1, file2);
			}
			else if (diffFormat.Contains("🔧"))
			{
				ShowGitStyleDiff(file1, file2);
			}
			else if (diffFormat.Contains("🎨"))
			{
				ShowColoredDiff(file1, file2);
			}

			AnsiConsole.WriteLine();
		}
	}

	/// <summary>
	/// Shows a change summary between two files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	private static void ShowChangeSummary(string file1, string file2)
	{
		var coloredSummary = FileDiffer.GenerateColoredChangeSummary(file1, file2);

		foreach (var line in coloredSummary)
		{
			var markup = line.Color switch
			{
				DiffColor.Addition => $"[green]{line.Content.EscapeMarkup()}[/]",
				DiffColor.Deletion => $"[red]{line.Content.EscapeMarkup()}[/]",
				DiffColor.ChunkHeader => $"[yellow]{line.Content.EscapeMarkup()}[/]",
				DiffColor.FileHeader => $"[bold blue]{line.Content.EscapeMarkup()}[/]",
				DiffColor.Default => line.Content.EscapeMarkup(),
				_ => line.Content.EscapeMarkup()
			};
			AnsiConsole.MarkupLine(markup);
		}
	}

	/// <summary>
	/// Shows a git-style diff between two files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	private static void ShowGitStyleDiff(string file1, string file2)
	{
		var lines1 = File.ReadAllLines(file1);
		var lines2 = File.ReadAllLines(file2);
		var coloredDiff = FileDiffer.GenerateColoredDiff(file1, file2, lines1, lines2);

		var panel = new Panel(RenderColoredDiff(coloredDiff))
		{
			Header = new PanelHeader("[bold]Git-style Diff[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(panel);
	}

	/// <summary>
	/// Shows a colored diff between two files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	private static void ShowColoredDiff(string file1, string file2)
	{
		var lines1 = File.ReadAllLines(file1);
		var lines2 = File.ReadAllLines(file2);
		var coloredDiff = FileDiffer.GenerateColoredDiff(file1, file2, lines1, lines2);

		AnsiConsole.WriteLine();
		foreach (var line in coloredDiff.Take(50)) // Limit output for TUI
		{
			var markup = line.Color switch
			{
				DiffColor.Addition => $"[green]{line.Content.EscapeMarkup()}[/]",
				DiffColor.Deletion => $"[red]{line.Content.EscapeMarkup()}[/]",
				DiffColor.ChunkHeader => $"[cyan]{line.Content.EscapeMarkup()}[/]",
				DiffColor.FileHeader => $"[bold blue]{line.Content.EscapeMarkup()}[/]",
				DiffColor.Default => line.Content.EscapeMarkup(),
				_ => line.Content.EscapeMarkup()
			};
			AnsiConsole.MarkupLine(markup);
		}

		if (coloredDiff.Count > 50)
		{
			AnsiConsole.MarkupLine($"[dim]... and {coloredDiff.Count - 50} more lines[/]");
		}
	}

	/// <summary>
	/// Renders colored diff for display in panels
	/// </summary>
	/// <param name="coloredDiff">The colored diff lines</param>
	/// <returns>Rendered markup string</returns>
	private static string RenderColoredDiff(IEnumerable<ColoredDiffLine> coloredDiff)
	{
		var lines = coloredDiff.Take(50).Select(line => line.Color switch
		{
			DiffColor.Addition => $"[green]{line.Content.EscapeMarkup()}[/]",
			DiffColor.Deletion => $"[red]{line.Content.EscapeMarkup()}[/]",
			DiffColor.ChunkHeader => $"[cyan]{line.Content.EscapeMarkup()}[/]",
			DiffColor.FileHeader => $"[bold blue]{line.Content.EscapeMarkup()}[/]",
			DiffColor.Default => line.Content.EscapeMarkup(),
			_ => line.Content.EscapeMarkup()
		});

		return string.Join("\n", lines);
	}

	/// <summary>
	/// Offers sync options to the user
	/// </summary>
	/// <param name="fileGroups">The file groups to potentially sync</param>
	private static void OfferSyncOptions(List<FileGroup> fileGroups)
	{
		if (!AnsiConsole.Confirm("[cyan]Would you like to sync files to make them identical?[/]", false))
		{
			return;
		}

		var sourceVersionIndex = AnsiConsole.Prompt(
			new SelectionPrompt<int>()
				.Title("[yellow]Select the source version to sync FROM:[/]")
				.AddChoices(Enumerable.Range(1, fileGroups.Count))
				.UseConverter(i => $"Version {i} ({fileGroups[i - 1].FilePaths.Count} files)"));

		var sourceGroup = fileGroups[sourceVersionIndex - 1];
		var sourceFile = sourceGroup.FilePaths.First();

		AnsiConsole.MarkupLine($"[green]Syncing FROM:[/] {sourceFile}");
		AnsiConsole.WriteLine();

		// For each other group, ask which files to sync
		var otherGroups = fileGroups.Where((g, i) => i != sourceVersionIndex - 1).ToList();

		foreach (var group in otherGroups)
		{
			var groupFiles = group.FilePaths.ToList();

			var table = new Table()
				.AddColumn("[bold]#[/]")
				.AddColumn("[bold]File Path[/]")
				.Border(TableBorder.Simple);

			for (var i = 0; i < groupFiles.Count; i++)
			{
				table.AddRow($"[cyan]{i + 1}[/]", groupFiles[i]);
			}

			AnsiConsole.Write(table);

			var syncChoice = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title($"[cyan]Files to sync in group with hash {group.Hash[..8]}...:[/]")
					.AddChoices([
						"🎯 Select specific files",
						"📋 Sync all files in this group",
						"⏭️  Skip this group"
					]));

			List<string> filesToSync = [];

			if (syncChoice.Contains("🎯"))
			{
				var selectedIndices = AnsiConsole.Prompt(
					new MultiSelectionPrompt<int>()
						.Title("[yellow]Select files to sync:[/]")
						.AddChoices(Enumerable.Range(1, groupFiles.Count))
						.UseConverter(i => $"{i}. {Path.GetFileName(groupFiles[i - 1])}"));

				filesToSync = [.. selectedIndices.Select(i => groupFiles[i - 1])];
			}
			else if (syncChoice.Contains("📋"))
			{
				filesToSync = groupFiles;
			}
			else if (syncChoice.Contains("⏭️"))
			{
				continue;
			}

			// Perform sync with progress bar
			AnsiConsole.Progress()
				.Start(ctx =>
				{
					var task = ctx.AddTask("[green]Syncing files[/]", maxValue: filesToSync.Count);

					foreach (var file in filesToSync)
					{
						try
						{
							FileDiffer.SyncFile(sourceFile, file);
							AnsiConsole.MarkupLine($"[green]✓[/] Synced: {file}");
						}
						catch (IOException ex)
						{
							AnsiConsole.MarkupLine($"[red]✗[/] Failed to sync {file}: {ex.Message}");
						}
						catch (UnauthorizedAccessException ex)
						{
							AnsiConsole.MarkupLine($"[red]✗[/] Failed to sync {file}: {ex.Message}");
						}

						task.Increment(1);
					}
				});

			AnsiConsole.WriteLine();
		}

		AnsiConsole.MarkupLine("[green]Sync operation completed![/]");
	}

	/// <summary>
	/// Compares two directories
	/// </summary>
	/// <param name="dir1">First directory</param>
	/// <param name="dir2">Second directory</param>
	/// <param name="pattern">File pattern</param>
	/// <param name="recursive">Whether to search recursively</param>
	private static void CompareDirectories(string dir1, string dir2, string pattern, bool recursive)
	{
		DirectoryComparisonResult result = default!;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start("[yellow]Comparing directories...[/]", ctx =>
			{
				result = FileDiffer.FindDifferences(dir1, dir2, pattern, recursive);
			});

		ShowDirectoryComparisonResult(result, dir1, dir2);
	}

	/// <summary>
	/// Shows the result of directory comparison
	/// </summary>
	/// <param name="result">The comparison result</param>
	/// <param name="dir1">First directory path</param>
	/// <param name="dir2">Second directory path</param>
	private static void ShowDirectoryComparisonResult(DirectoryComparisonResult result, string dir1, string dir2)
	{
		var table = new Table()
			.AddColumn("[bold]Category[/]")
			.AddColumn("[bold]Count[/]")
			.AddColumn("[bold]Files[/]")
			.Border(TableBorder.Rounded);

		table.AddRow(
			"[green]Identical[/]",
			$"[green]{result.SameFiles.Count}[/]",
			string.Join(", ", result.SameFiles.Take(5)) + (result.SameFiles.Count > 5 ? "..." : "")
		);

		table.AddRow(
			"[yellow]Modified[/]",
			$"[yellow]{result.ModifiedFiles.Count}[/]",
			string.Join(", ", result.ModifiedFiles.Take(5)) + (result.ModifiedFiles.Count > 5 ? "..." : "")
		);

		table.AddRow(
			"[blue]Only in Dir1[/]",
			$"[blue]{result.OnlyInDir1.Count}[/]",
			string.Join(", ", result.OnlyInDir1.Take(5)) + (result.OnlyInDir1.Count > 5 ? "..." : "")
		);

		table.AddRow(
			"[magenta]Only in Dir2[/]",
			$"[magenta]{result.OnlyInDir2.Count}[/]",
			string.Join(", ", result.OnlyInDir2.Take(5)) + (result.OnlyInDir2.Count > 5 ? "..." : "")
		);

		AnsiConsole.Write(table);

		// Offer to show detailed differences for modified files
		if (result.ModifiedFiles.Count > 0 &&
			AnsiConsole.Confirm("[cyan]Would you like to see detailed differences for modified files?[/]", false))
		{
			ShowModifiedFilesDetails(result.ModifiedFiles, dir1, dir2);
		}
	}

	/// <summary>
	/// Shows detailed differences for modified files
	/// </summary>
	/// <param name="modifiedFiles">Collection of modified file paths</param>
	/// <param name="dir1">First directory path</param>
	/// <param name="dir2">Second directory path</param>
	private static void ShowModifiedFilesDetails(IReadOnlyCollection<string> modifiedFiles, string dir1, string dir2)
	{
		var selectedFile = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Select a file to see differences:[/]")
				.AddChoices(modifiedFiles));

		var file1Path = Path.Combine(dir1, selectedFile);
		var file2Path = Path.Combine(dir2, selectedFile);

		CompareTwoFiles(file1Path, file2Path);
	}

	/// <summary>
	/// Compares two specific files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	private static void CompareTwoFiles(string file1, string file2)
	{
		var rule = new Rule($"[bold]Comparing Files[/]")
		{
			Style = Style.Parse("blue")
		};
		AnsiConsole.Write(rule);

		AnsiConsole.MarkupLine($"[dim]File 1:[/] {file1}");
		AnsiConsole.MarkupLine($"[dim]File 2:[/] {file2}");
		AnsiConsole.WriteLine();

		// Check if files are identical first
		var hash1 = FileHasher.ComputeFileHash(file1);
		var hash2 = FileHasher.ComputeFileHash(file2);

		if (hash1 == hash2)
		{
			AnsiConsole.MarkupLine("[green]✓ Files are identical![/]");
			return;
		}

		var diffFormat = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Choose diff format:[/]")
				.AddChoices([
					"📊 Change Summary",
					"🔧 Git-style Diff",
					"🎨 Colored Diff"
				]));

		if (diffFormat.Contains("📊"))
		{
			ShowChangeSummary(file1, file2);
		}
		else if (diffFormat.Contains("🔧"))
		{
			ShowGitStyleDiff(file1, file2);
		}
		else if (diffFormat.Contains("🎨"))
		{
			ShowColoredDiff(file1, file2);
		}

		// Offer to sync files
		if (AnsiConsole.Confirm("[cyan]Would you like to sync these files?[/]", false))
		{
			var syncDirection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("[yellow]Which direction to sync?[/]")
					.AddChoices([
						$"📄 {Path.GetFileName(file1)} → {Path.GetFileName(file2)}",
						$"📄 {Path.GetFileName(file2)} → {Path.GetFileName(file1)}"
					]));

			try
			{
				if (syncDirection.Contains('→'))
				{
					var isFirstToSecond = syncDirection.Contains($"📄 {Path.GetFileName(file1)}");
					if (isFirstToSecond)
					{
						FileDiffer.SyncFile(file1, file2);
						AnsiConsole.MarkupLine($"[green]✓[/] Synced {Path.GetFileName(file1)} to {Path.GetFileName(file2)}");
					}
					else
					{
						FileDiffer.SyncFile(file2, file1);
						AnsiConsole.MarkupLine($"[green]✓[/] Synced {Path.GetFileName(file2)} to {Path.GetFileName(file1)}");
					}
				}
			}
			catch (IOException ex)
			{
				AnsiConsole.MarkupLine($"[red]✗[/] Sync failed: {ex.Message}");
			}
			catch (UnauthorizedAccessException ex)
			{
				AnsiConsole.MarkupLine($"[red]✗[/] Sync failed: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Runs the iterative merge process for multiple file versions
	/// </summary>
	private static void RunIterativeMerge()
	{
		var directory = AnsiConsole.Ask<string>("[cyan]Enter the directory path containing multiple versions:[/]");

		if (!Directory.Exists(directory))
		{
			AnsiConsole.MarkupLine("[red]Error: Directory does not exist![/]");
			return;
		}

		var fileName = AnsiConsole.Ask<string>("[cyan]Enter the filename to search for (multiple versions):[/]");

		// First, find files and prepare data (within Status operation)
		List<FileGroup>? uniqueGroups = null;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start("[yellow]Finding file versions...[/]", ctx =>
			{
				var files = FileFinder.FindFiles(directory, fileName);
				var filesList = files.ToList();

				if (filesList.Count < 2)
				{
					AnsiConsole.MarkupLine($"[red]Need at least 2 versions to merge. Found {filesList.Count} files.[/]");
					return;
				}

				// Group files by hash to find unique versions
				var fileGroups = FileDiffer.GroupFilesByHash(files);
				uniqueGroups = [.. fileGroups.Where(g => g.FilePaths.Count >= 1)];

				if (uniqueGroups.Count < 2)
				{
					AnsiConsole.MarkupLine("[green]All files are identical. No merge needed.[/]");
					return;
				}

				AnsiConsole.MarkupLine($"[green]Found {uniqueGroups.Count} unique versions to merge.[/]");
			});

		// Then, run the interactive merge process (outside Status operation)
		if (uniqueGroups != null && uniqueGroups.Count >= 2)
		{
			StartIterativeMergeProcess(uniqueGroups, fileName);
		}
	}

	/// <summary>
	/// Starts the iterative merge process
	/// </summary>
	/// <param name="fileGroups">The unique file groups to merge</param>
	/// <param name="originalFileName">The original filename being merged</param>
	private static void StartIterativeMergeProcess(List<FileGroup> fileGroups, string originalFileName)
	{
		// Create a list of representative files (one from each group)
		var filesToMerge = fileGroups.Select(g => g.FilePaths.First()).ToList();
		var session = new IterativeMergeSession(filesToMerge);

		var mergeCount = 1;
		string? lastMergedContent = null;

		while (session.RemainingFiles.Count > 1)
		{
			// Show current status
			ShowMergeSessionStatus(session, mergeCount);

			FileSimilarity? similarity;

			if (lastMergedContent != null)
			{
				// Find the most similar file to our current merged result
				similarity = FindMostSimilarToMergedContent(session.RemainingFiles, lastMergedContent);
			}
			else
			{
				// Find the two most similar files from remaining files
				var remainingGroups = fileGroups.Where(g => session.RemainingFiles.Contains(g.FilePaths.First())).ToList();
				similarity = FileDiffer.FindMostSimilarFiles(remainingGroups);
			}

			if (similarity == null)
			{
				AnsiConsole.MarkupLine("[red]Error: Could not find files to merge.[/]");
				break;
			}

			// Show similarity information
			AnsiConsole.MarkupLine($"[yellow]Merging most similar pair (similarity: {similarity.SimilarityScore:P1}):[/]");
			AnsiConsole.MarkupLine($"[dim]  📄 {Path.GetFileName(similarity.FilePath1)}[/]");
			AnsiConsole.MarkupLine($"[dim]  📄 {Path.GetFileName(similarity.FilePath2)}[/]");

			// Perform the merge
			var mergeResult = PerformMergeWithConflictResolution(similarity.FilePath1, similarity.FilePath2, lastMergedContent);

			if (mergeResult == null)
			{
				AnsiConsole.MarkupLine("[red]Merge cancelled by user.[/]");
				break;
			}

			// Update session
			lastMergedContent = string.Join(Environment.NewLine, mergeResult.MergedLines);
			session.AddMergedContent(lastMergedContent);

			// Remove the merged files from remaining files
			session.RemoveFile(similarity.FilePath1);
			if (lastMergedContent == null) // Only remove second file if this wasn't a merge with existing content
			{
				session.RemoveFile(similarity.FilePath2);
			}
			else
			{
				session.RemoveFile(similarity.FilePath2);
			}

			mergeCount++;

			if (session.RemainingFiles.Count > 1)
			{
				if (!AnsiConsole.Confirm("[cyan]Continue with next merge iteration?[/]", true))
				{
					break;
				}
			}
		}

		if (session.IsComplete || session.RemainingFiles.Count <= 1)
		{
			ShowMergeCompletion(lastMergedContent, originalFileName);
		}
	}

	/// <summary>
	/// Shows the current status of the merge session
	/// </summary>
	/// <param name="session">The merge session</param>
	/// <param name="mergeCount">Current merge iteration number</param>
	private static void ShowMergeSessionStatus(IterativeMergeSession session, int mergeCount)
	{
		var rule = new Rule($"[bold]Merge Iteration {mergeCount}[/]")
		{
			Style = Style.Parse("blue")
		};
		AnsiConsole.Write(rule);

		AnsiConsole.MarkupLine($"[green]Remaining files to merge: {session.RemainingFiles.Count}[/]");
		AnsiConsole.MarkupLine($"[green]Completed merges: {session.MergedContents.Count}[/]");
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Finds the file most similar to the current merged content
	/// </summary>
	/// <param name="remainingFiles">List of remaining files to consider</param>
	/// <param name="mergedContent">The current merged content</param>
	/// <returns>A FileSimilarity object with the most similar file</returns>
	private static FileSimilarity? FindMostSimilarToMergedContent(IReadOnlyList<string> remainingFiles, string mergedContent)
	{
		var mergedLines = mergedContent.Split(Environment.NewLine);
		FileSimilarity? mostSimilar = null;
		var highestSimilarity = -1.0;

		foreach (var file in remainingFiles)
		{
			var fileLines = File.ReadAllLines(file);
			var similarity = FileDiffer.CalculateLineSimilarity(mergedLines, fileLines);

			if (similarity > highestSimilarity)
			{
				highestSimilarity = similarity;
				mostSimilar = new FileSimilarity
				{
					FilePath1 = "<merged_content>",
					FilePath2 = file,
					SimilarityScore = similarity
				};
			}
		}

		return mostSimilar;
	}

	/// <summary>
	/// Performs a merge with interactive conflict resolution
	/// </summary>
	/// <param name="file1">First file to merge</param>
	/// <param name="file2">Second file to merge</param>
	/// <param name="existingMergedContent">Existing merged content (if any)</param>
	/// <returns>The resolved merge result, or null if cancelled</returns>
	private static MergeResult? PerformMergeWithConflictResolution(string file1, string file2, string? existingMergedContent)
	{
		MergeResult mergeResult;
		string[]? originalLines1 = null;
		string[]? originalLines2 = null;

		if (existingMergedContent != null)
		{
			// Merge with existing content
			var mergedLines = existingMergedContent.Split(Environment.NewLine);
			var file2Lines = File.ReadAllLines(file2);
			originalLines1 = mergedLines;
			originalLines2 = file2Lines;
			mergeResult = FileDiffer.MergeLines(mergedLines, file2Lines);
		}
		else
		{
			// Merge two files
			originalLines1 = File.ReadAllLines(file1);
			originalLines2 = File.ReadAllLines(file2);
			mergeResult = FileDiffer.MergeFiles(file1, file2);
		}

		if (mergeResult.Conflicts.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]✓ Automatic merge successful (no conflicts)![/]");
			return mergeResult;
		}

		AnsiConsole.MarkupLine($"[yellow]⚠ Found {mergeResult.Conflicts.Count} conflict(s) that need resolution.[/]");

		return !AnsiConsole.Confirm("[cyan]Would you like to resolve conflicts interactively?[/]", true)
			? null
			: ResolveConflictsInteractively(mergeResult, originalLines1, originalLines2);
	}

	/// <summary>
	/// Resolves merge conflicts interactively through TUI
	/// </summary>
	/// <param name="mergeResult">The merge result with conflicts</param>
	/// <param name="originalLines1">Original lines of the first file</param>
	/// <param name="originalLines2">Original lines of the second file</param>
	/// <returns>The resolved merge result</returns>
	private static MergeResult ResolveConflictsInteractively(MergeResult mergeResult, string[]? originalLines1, string[]? originalLines2)
	{
		var rule = new Rule("[bold]Interactive Conflict Resolution[/]")
		{
			Style = Style.Parse("red")
		};
		AnsiConsole.Write(rule);

		var resolvedLines = mergeResult.MergedLines.ToList();
		var conflictIndex = 1;

		foreach (var conflict in mergeResult.Conflicts)
		{
			if (conflict.IsResolved)
			{
				continue;
			}

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[yellow]Conflict {conflictIndex}/{mergeResult.Conflicts.Count} at line {conflict.LineNumber}:[/]");

			// Show the conflict with context in side-by-side view
			if (originalLines1 != null && originalLines2 != null)
			{
				ShowConflictWithContext(conflict, originalLines1, originalLines2);
			}
			else
			{
				// Fallback to simple display if original lines are not available
				var panel = new Panel(
					$"[red]Version 1:[/]\n{conflict.Content1 ?? "(deleted)"}\n\n" +
					$"[green]Version 2:[/]\n{conflict.Content2 ?? "(added)"}")
				{
					Header = new PanelHeader("Conflict Details"),
					Border = BoxBorder.Rounded
				};
				AnsiConsole.Write(panel);
			}

			// Let user choose resolution
			var choices = new List<string>
			{
				"📄 Use Version 1",
				"📄 Use Version 2",
				"✏️  Edit Manually",
				"❌ Skip This Conflict"
			};

			if (conflict.Content1 != null && conflict.Content2 != null)
			{
				choices.Insert(2, "🔀 Combine Both");
			}

			var choice = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("[cyan]How would you like to resolve this conflict?[/]")
					.AddChoices(choices));

			if (choice.Contains("Use Version 1"))
			{
				conflict.ResolvedContent = conflict.Content1 ?? "";
				conflict.IsResolved = true;
			}
			else if (choice.Contains("Use Version 2"))
			{
				conflict.ResolvedContent = conflict.Content2 ?? "";
				conflict.IsResolved = true;
			}
			else if (choice.Contains("Combine Both"))
			{
				conflict.ResolvedContent = $"{conflict.Content1}\n{conflict.Content2}";
				conflict.IsResolved = true;
			}
			else if (choice.Contains("Edit Manually"))
			{
				var manualContent = AnsiConsole.Ask<string>("[cyan]Enter the resolved content:[/]");
				conflict.ResolvedContent = manualContent;
				conflict.IsResolved = true;
			}
			else
			{
				AnsiConsole.MarkupLine("[yellow]Skipping conflict - will keep conflict markers.[/]");
			}

			conflictIndex++;
		}

		// Apply resolved conflicts to the merged content
		ApplyConflictResolutions(resolvedLines, mergeResult.Conflicts);

		return new MergeResult
		{
			MergedLines = resolvedLines.AsReadOnly(),
			Conflicts = mergeResult.Conflicts
		};
	}

	/// <summary>
	/// Shows a conflict with context lines in a side-by-side view
	/// </summary>
	/// <param name="conflict">The conflict to display</param>
	/// <param name="originalLines1">Original lines from first file/content</param>
	/// <param name="originalLines2">Original lines from second file/content</param>
	private static void ShowConflictWithContext(MergeConflict conflict, string[] originalLines1, string[] originalLines2)
	{
		const int contextLines = 3; // Number of context lines before and after conflict

		// Find the actual lines in the original files that contain the conflicting content
		var line1Index = FindLineInFile(originalLines1, conflict.Content1);
		var line2Index = FindLineInFile(originalLines2, conflict.Content2);

		// Build version 1 content (left side)
		var version1Content = new List<string>();
		var version2Content = new List<string>();

		// Handle Version 1 display
		if (line1Index >= 0)
		{
			// Found the conflict line, show context around it
			var startIndex = Math.Max(0, line1Index - contextLines);
			var endIndex = Math.Min(originalLines1.Length - 1, line1Index + contextLines);

			for (var i = startIndex; i <= endIndex; i++)
			{
				var isConflictLine = i == line1Index;
				var lineContent = isConflictLine
					? $"[red bold]{i + 1,4}: {originalLines1[i].EscapeMarkup()}[/]"
					: $"[dim]{i + 1,4}: {originalLines1[i].EscapeMarkup()}[/]";
				version1Content.Add(lineContent);
			}
		}
		else if (conflict.Content1 == null)
		{
			// Handle deletion case - show that content was deleted
			version1Content.Add($"[red bold]    : (deleted)[/]");
		}
		else
		{
			// Could not find the exact line, but we have content - try fuzzy matching or show best effort
			var bestMatch = FindBestMatchingLine(originalLines1, conflict.Content1);
			if (bestMatch >= 0)
			{
				var startIndex = Math.Max(0, bestMatch - contextLines);
				var endIndex = Math.Min(originalLines1.Length - 1, bestMatch + contextLines);

				for (var i = startIndex; i <= endIndex; i++)
				{
					var isMatchLine = i == bestMatch;
					var lineContent = isMatchLine
						? $"[red bold]{i + 1,4}: {originalLines1[i].EscapeMarkup()}[/]"
						: $"[dim]{i + 1,4}: {originalLines1[i].EscapeMarkup()}[/]";
					version1Content.Add(lineContent);
				}
			}
			else
			{
				// Show the conflict content as it appears in the conflict
				version1Content.Add($"[red bold]????: {conflict.Content1.EscapeMarkup()}[/]");
				version1Content.Add($"[dim]     (content not found in original file)[/]");
			}
		}

		// Handle Version 2 display
		if (line2Index >= 0)
		{
			// Found the conflict line, show context around it
			var startIndex = Math.Max(0, line2Index - contextLines);
			var endIndex = Math.Min(originalLines2.Length - 1, line2Index + contextLines);

			for (var i = startIndex; i <= endIndex; i++)
			{
				var isConflictLine = i == line2Index;
				var lineContent = isConflictLine
					? $"[green bold]{i + 1,4}: {originalLines2[i].EscapeMarkup()}[/]"
					: $"[dim]{i + 1,4}: {originalLines2[i].EscapeMarkup()}[/]";
				version2Content.Add(lineContent);
			}
		}
		else if (conflict.Content2 == null)
		{
			// Handle addition case - show that content was added
			version2Content.Add($"[green bold]    : (added)[/]");
		}
		else
		{
			// Could not find the exact line, but we have content - try fuzzy matching or show best effort
			var bestMatch = FindBestMatchingLine(originalLines2, conflict.Content2);
			if (bestMatch >= 0)
			{
				var startIndex = Math.Max(0, bestMatch - contextLines);
				var endIndex = Math.Min(originalLines2.Length - 1, bestMatch + contextLines);

				for (var i = startIndex; i <= endIndex; i++)
				{
					var isMatchLine = i == bestMatch;
					var lineContent = isMatchLine
						? $"[green bold]{i + 1,4}: {originalLines2[i].EscapeMarkup()}[/]"
						: $"[dim]{i + 1,4}: {originalLines2[i].EscapeMarkup()}[/]";
					version2Content.Add(lineContent);
				}
			}
			else
			{
				// Show the conflict content as it appears in the conflict
				version2Content.Add($"[green bold]????: {conflict.Content2.EscapeMarkup()}[/]");
				version2Content.Add($"[dim]     (content not found in original file)[/]");
			}
		}

		// Ensure both sides have the same number of lines for better alignment
		var maxLines = Math.Max(version1Content.Count, version2Content.Count);
		while (version1Content.Count < maxLines)
		{
			version1Content.Add($"[dim]    : [/]");
		}

		while (version2Content.Count < maxLines)
		{
			version2Content.Add($"[dim]    : [/]");
		}

		// Create side-by-side layout
		var leftPanel = new Panel(string.Join("\n", version1Content))
		{
			Header = new PanelHeader("[red bold]Version 1[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("red")
		};

		var rightPanel = new Panel(string.Join("\n", version2Content))
		{
			Header = new PanelHeader("[green bold]Version 2[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("green")
		};

		// Create the layout with two columns
		var layout = new Layout()
			.SplitColumns(
				new Layout("left").Update(leftPanel),
				new Layout("right").Update(rightPanel)
			);

		AnsiConsole.Write(layout);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Finds the best matching line for the given content using fuzzy matching
	/// </summary>
	/// <param name="lines">The file lines to search</param>
	/// <param name="content">The content to find</param>
	/// <returns>The line index if a reasonable match is found, -1 if not found</returns>
	private static int FindBestMatchingLine(string[] lines, string? content)
	{
		if (content == null || lines.Length == 0)
		{
			return -1;
		}

		var contentTrimmed = content.Trim();
		if (string.IsNullOrEmpty(contentTrimmed))
		{
			return -1;
		}

		// First try exact match on trimmed content
		for (var i = 0; i < lines.Length; i++)
		{
			if (lines[i].Trim() == contentTrimmed)
			{
				return i;
			}
		}

		// Try partial match - content contains line or line contains content
		for (var i = 0; i < lines.Length; i++)
		{
			var lineTrimmed = lines[i].Trim();
			if (!string.IsNullOrEmpty(lineTrimmed) &&
				(contentTrimmed.Contains(lineTrimmed) || lineTrimmed.Contains(contentTrimmed)))
			{
				return i;
			}
		}

		// Try word-based similarity for more flexible matching
		var contentWords = contentTrimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
		var bestMatch = -1;
		var bestScore = 0.0;

		for (var i = 0; i < lines.Length; i++)
		{
			var lineWords = lines[i].Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
			var commonWords = contentWords.Intersect(lineWords, StringComparer.OrdinalIgnoreCase).Count();
			var totalWords = Math.Max(contentWords.Length, lineWords.Length);

			if (totalWords > 0)
			{
				var score = (double)commonWords / totalWords;
				if (score > bestScore && score >= 0.5) // At least 50% word similarity
				{
					bestScore = score;
					bestMatch = i;
				}
			}
		}

		return bestMatch;
	}

	/// <summary>
	/// Finds the line index of the given content in the file lines
	/// </summary>
	/// <param name="lines">The file lines to search</param>
	/// <param name="content">The content to find</param>
	/// <returns>The line index if found, -1 if not found</returns>
	private static int FindLineInFile(string[] lines, string? content)
	{
		if (content == null || lines.Length == 0)
		{
			return -1;
		}

		// First try exact match
		for (var i = 0; i < lines.Length; i++)
		{
			if (lines[i] == content)
			{
				return i;
			}
		}

		// Try exact match on trimmed content
		var contentTrimmed = content.Trim();
		for (var i = 0; i < lines.Length; i++)
		{
			if (lines[i].Trim() == contentTrimmed)
			{
				return i;
			}
		}

		// If exact match not found, try to find a line that contains the content
		for (var i = 0; i < lines.Length; i++)
		{
			if (lines[i].Contains(content) || content.Contains(lines[i]))
			{
				return i;
			}
		}

		// Try trimmed partial matching
		for (var i = 0; i < lines.Length; i++)
		{
			var lineTrimmed = lines[i].Trim();
			if (!string.IsNullOrEmpty(lineTrimmed) &&
				(contentTrimmed.Contains(lineTrimmed) || lineTrimmed.Contains(contentTrimmed)))
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// Applies conflict resolutions to the merged lines
	/// </summary>
	/// <param name="mergedLines">The merged lines to update</param>
	/// <param name="conflicts">The conflicts with resolutions</param>
	private static void ApplyConflictResolutions(List<string> mergedLines, IReadOnlyCollection<MergeConflict> conflicts)
	{
		// Work backwards through conflicts to avoid index shifting
		foreach (var conflict in conflicts.Where(c => c.IsResolved).OrderByDescending(c => c.LineNumber))
		{
			// Find and replace conflict markers with resolved content
			for (var i = mergedLines.Count - 1; i >= 0; i--)
			{
				if (mergedLines[i].Contains("<<<<<<< Version"))
				{
					// Find the end of this conflict block
					var endIndex = i;
					while (endIndex < mergedLines.Count && !mergedLines[endIndex].Contains(">>>>>>> Version"))
					{
						endIndex++;
					}

					if (endIndex < mergedLines.Count)
					{
						// Remove the conflict block and replace with resolved content
						var linesToRemove = endIndex - i + 1;
						mergedLines.RemoveRange(i, linesToRemove);

						if (!string.IsNullOrEmpty(conflict.ResolvedContent))
						{
							var resolvedLines = conflict.ResolvedContent.Split('\n');
							mergedLines.InsertRange(i, resolvedLines);
						}

						break;
					}
				}
			}
		}
	}

	/// <summary>
	/// Shows the completion of the merge process
	/// </summary>
	/// <param name="finalMergedContent">The final merged content</param>
	/// <param name="originalFileName">The original filename</param>
	private static void ShowMergeCompletion(string? finalMergedContent, string originalFileName)
	{
		var rule = new Rule("[bold green]Merge Completed Successfully![/]")
		{
			Style = Style.Parse("green")
		};
		AnsiConsole.Write(rule);

		if (finalMergedContent == null)
		{
			AnsiConsole.MarkupLine("[red]No merged content available.[/]");
			return;
		}

		AnsiConsole.MarkupLine("[green]All versions have been successfully merged![/]");
		AnsiConsole.WriteLine();

		// Show merge statistics
		var lines = finalMergedContent.Split(Environment.NewLine);
		AnsiConsole.MarkupLine($"[dim]Final merged content: {lines.Length} lines[/]");

		if (AnsiConsole.Confirm("[cyan]Would you like to preview the merged content?[/]", true))
		{
			var panel = new Panel(finalMergedContent.Length > 2000
				? finalMergedContent[..2000] + "\n[dim]... (truncated)[/]"
				: finalMergedContent)
			{
				Header = new PanelHeader("Merged Content Preview"),
				Border = BoxBorder.Rounded
			};
			AnsiConsole.Write(panel);
		}

		if (AnsiConsole.Confirm("[cyan]Would you like to save the merged content to a file?[/]", true))
		{
			var outputPath = AnsiConsole.Ask("[cyan]Enter output file path:[/]", $"merged_{originalFileName}");

			try
			{
				var outputDir = Path.GetDirectoryName(outputPath);
				if (!string.IsNullOrEmpty(outputDir))
				{
					Directory.CreateDirectory(outputDir);
				}

				File.WriteAllText(outputPath, finalMergedContent);
				AnsiConsole.MarkupLine($"[green]✓ Merged content saved to: {outputPath}[/]");
			}
			catch (IOException ex)
			{
				AnsiConsole.MarkupLine($"[red]✗ Failed to save merged content: {ex.Message}[/]");
			}
			catch (UnauthorizedAccessException ex)
			{
				AnsiConsole.MarkupLine($"[red]✗ Failed to save merged content: {ex.Message}[/]");
			}
		}
	}
}
