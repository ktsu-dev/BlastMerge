// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.CLI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
		var directory = HistoryInput.AskWithHistory("[cyan]Enter the directory path:[/]");

		if (!Directory.Exists(directory))
		{
			AnsiConsole.MarkupLine("[red]Error: Directory does not exist![/]");
			return;
		}

		var fileName = HistoryInput.AskWithHistory("[cyan]Enter the filename to search for:[/]");

		ProcessFiles(directory, fileName);
	}

	/// <summary>
	/// Compares two directories
	/// </summary>
	private static void CompareTwoDirectories()
	{
		var dir1 = HistoryInput.AskWithHistory("[cyan]Enter the first directory path:[/]");

		if (!Directory.Exists(dir1))
		{
			AnsiConsole.MarkupLine("[red]Error: First directory does not exist![/]");
			return;
		}

		var dir2 = HistoryInput.AskWithHistory("[cyan]Enter the second directory path:[/]");

		if (!Directory.Exists(dir2))
		{
			AnsiConsole.MarkupLine("[red]Error: Second directory does not exist![/]");
			return;
		}

		var pattern = HistoryInput.AskWithHistory("[cyan]Enter file pattern (e.g., *.txt, *.cs):[/]", "*.*");
		var recursive = AnsiConsole.Confirm("[cyan]Search subdirectories recursively?[/]", false);

		CompareDirectories(dir1, dir2, pattern, recursive);
	}

	/// <summary>
	/// Compares two specific files
	/// </summary>
	private static void CompareTwoSpecificFiles()
	{
		var file1 = HistoryInput.AskWithHistory("[cyan]Enter the first file path:[/]");

		if (!File.Exists(file1))
		{
			AnsiConsole.MarkupLine("[red]Error: First file does not exist![/]");
			return;
		}

		var file2 = HistoryInput.AskWithHistory("[cyan]Enter the second file path:[/]");

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
		var directory = HistoryInput.AskWithHistory("[cyan]Enter the directory path containing multiple versions:[/]");

		if (!Directory.Exists(directory))
		{
			AnsiConsole.MarkupLine("[red]Error: Directory does not exist![/]");
			return;
		}

		var fileName = HistoryInput.AskWithHistory("[cyan]Enter the filename to search for (multiple versions):[/]");

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
	/// Performs a merge with manual block selection
	/// </summary>
	/// <param name="file1">First file to merge</param>
	/// <param name="file2">Second file to merge</param>
	/// <param name="existingMergedContent">Existing merged content (if any)</param>
	/// <returns>The manually merged result, or null if cancelled</returns>
	private static MergeResult? PerformMergeWithConflictResolution(string file1, string file2, string? existingMergedContent)
	{
		string[] lines1;
		string[] lines2;

		if (existingMergedContent != null)
		{
			// Merge with existing content
			lines1 = existingMergedContent.Split(Environment.NewLine);
			lines2 = File.ReadAllLines(file2);
		}
		else
		{
			// Merge two files
			lines1 = File.ReadAllLines(file1);
			lines2 = File.ReadAllLines(file2);
		}

		return PerformManualBlockSelection(lines1, lines2);
	}

	/// <summary>
	/// Performs manual block-by-block selection for merging
	/// </summary>
	/// <param name="lines1">Lines from version 1</param>
	/// <param name="lines2">Lines from version 2</param>
	/// <returns>The manually merged result</returns>
	private static MergeResult PerformManualBlockSelection(string[] lines1, string[] lines2)
	{
		// Create temporary files to use with FileDiffer
		var tempFile1 = Path.GetTempFileName();
		var tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllLines(tempFile1, lines1);
			File.WriteAllLines(tempFile2, lines2);

			var differences = FileDiffer.FindDifferences(tempFile1, tempFile2);
			var mergedLines = new List<string>();
			var conflicts = new List<MergeConflict>();

			AnsiConsole.MarkupLine("[yellow]Manual merge mode: Choose which blocks to include in the final result.[/]");
			AnsiConsole.WriteLine();

			var blockNumber = 1;
			var lastProcessedLine1 = 0;
			var lastProcessedLine2 = 0;

			// Convert differences to our custom block format
			var blocks = ConvertDifferencesToBlocks(differences);

			foreach (var block in blocks)
			{
				// Add unchanged lines between blocks (equal content)
				AddEqualLinesBetweenBlocks(lines1, lines2, ref lastProcessedLine1, ref lastProcessedLine2,
					block, mergedLines);

				switch (block.Type)
				{
					case BlockType.Insert:
						HandleInsertionBlockWithContext(lines1, lines2, block, mergedLines, blockNumber);
						break;

					case BlockType.Delete:
						HandleDeletionBlockWithContext(lines1, lines2, block, mergedLines, blockNumber);
						break;

					case BlockType.Replace:
						HandleReplacementBlockWithContext(lines1, lines2, block, mergedLines, blockNumber);
						break;

					default:
						// Handle unexpected block types
						break;
				}

				// Update last processed line numbers
				if (block.LineNumbers1.Count > 0)
				{
					lastProcessedLine1 = block.LineNumbers1.Max();
				}
				if (block.LineNumbers2.Count > 0)
				{
					lastProcessedLine2 = block.LineNumbers2.Max();
				}

				blockNumber++;
			}

			// Add any remaining equal lines after the last block
			AddRemainingEqualLines(lines1, lines2, lastProcessedLine1, lastProcessedLine2, mergedLines);

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

	/// <summary>
	/// Adds unchanged lines between blocks
	/// </summary>
	private static void AddEqualLinesBetweenBlocks(string[] lines1, string[] lines2, ref int lastProcessedLine1,
		ref int lastProcessedLine2, DiffBlock block, List<string> mergedLines)
	{
		var nextLine1 = block.LineNumbers1.Count > 0 ? block.LineNumbers1.Min() : lines1.Length + 1;
		var nextLine2 = block.LineNumbers2.Count > 0 ? block.LineNumbers2.Min() : lines2.Length + 1;

		// Add equal lines from the last processed line to the current block
		var equalLinesEnd1 = Math.Min(nextLine1 - 1, lines1.Length);
		var equalLinesEnd2 = Math.Min(nextLine2 - 1, lines2.Length);

		for (var i = lastProcessedLine1; i < equalLinesEnd1 && i < equalLinesEnd2; i++)
		{
			if (i < lines1.Length && i < lines2.Length && lines1[i] == lines2[i])
			{
				mergedLines.Add(lines1[i]);
			}
		}

		// Update the last processed line for version 2 as well
		lastProcessedLine2 = Math.Max(lastProcessedLine2, equalLinesEnd2);
	}

	/// <summary>
	/// Adds remaining equal lines after all blocks have been processed
	/// </summary>
	private static void AddRemainingEqualLines(string[] lines1, string[] lines2, int lastProcessedLine1,
		int lastProcessedLine2, List<string> mergedLines)
	{
		var remainingLines = Math.Min(lines1.Length - lastProcessedLine1, lines2.Length - lastProcessedLine2);

		for (var i = 0; i < remainingLines; i++)
		{
			var line1Index = lastProcessedLine1 + i;
			var line2Index = lastProcessedLine2 + i;

			if (line1Index < lines1.Length && line2Index < lines2.Length &&
				lines1[line1Index] == lines2[line2Index])
			{
				mergedLines.Add(lines1[line1Index]);
			}
		}
	}

	/// <summary>
	/// Gets context lines around a block
	/// </summary>
	private static (string[] contextBefore1, string[] contextAfter1, string[] contextBefore2, string[] contextAfter2)
		GetContextLines(string[] lines1, string[] lines2, DiffBlock block, int contextSize = 3)
	{
		var startLine1 = block.LineNumbers1.Count > 0 ? block.LineNumbers1.Min() - 1 : 0;
		var endLine1 = block.LineNumbers1.Count > 0 ? block.LineNumbers1.Max() - 1 : 0;
		var startLine2 = block.LineNumbers2.Count > 0 ? block.LineNumbers2.Min() - 1 : 0;
		var endLine2 = block.LineNumbers2.Count > 0 ? block.LineNumbers2.Max() - 1 : 0;

		// Context before
		var contextBefore1 = GetLinesInRange(lines1, Math.Max(0, startLine1 - contextSize), startLine1);
		var contextAfter1 = GetLinesInRange(lines1, endLine1 + 1, Math.Min(lines1.Length, endLine1 + 1 + contextSize));

		var contextBefore2 = GetLinesInRange(lines2, Math.Max(0, startLine2 - contextSize), startLine2);
		var contextAfter2 = GetLinesInRange(lines2, endLine2 + 1, Math.Min(lines2.Length, endLine2 + 1 + contextSize));

		return (contextBefore1, contextAfter1, contextBefore2, contextAfter2);
	}

	/// <summary>
	/// Gets lines in a specific range from an array
	/// </summary>
	private static string[] GetLinesInRange(string[] lines, int start, int end)
	{
		if (start >= end || start >= lines.Length || end <= 0)
		{
			return [];
		}

		var actualStart = Math.Max(0, start);
		var actualEnd = Math.Min(lines.Length, end);
		var result = new string[actualEnd - actualStart];

		Array.Copy(lines, actualStart, result, 0, actualEnd - actualStart);
		return result;
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
			var outputPath = HistoryInput.AskWithHistory("[cyan]Enter output file path:[/]", $"merged_{originalFileName}");

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

	/// <summary>
	/// Represents a block type for manual selection
	/// </summary>
	private enum BlockType
	{
		Insert,
		Delete,
		Replace
	}

	/// <summary>
	/// Represents a diff block for manual selection
	/// </summary>
	private class DiffBlock
	{
		public BlockType Type { get; set; }
		public List<string> Lines1 { get; set; } = [];
		public List<string> Lines2 { get; set; } = [];
		public List<int> LineNumbers1 { get; set; } = [];
		public List<int> LineNumbers2 { get; set; } = [];

		public int LastLineNumber1 => LineNumbers1.Count > 0 ? LineNumbers1.Last() : 0;
		public int LastLineNumber2 => LineNumbers2.Count > 0 ? LineNumbers2.Last() : 0;
	}

	/// <summary>
	/// Converts line differences to blocks for manual selection
	/// </summary>
	private static List<DiffBlock> ConvertDifferencesToBlocks(IReadOnlyCollection<LineDifference> differences)
	{
		var blocks = new List<DiffBlock>();
		var sortedDifferences = differences.OrderBy(d => Math.Max(d.LineNumber1, d.LineNumber2)).ToList();

		if (sortedDifferences.Count == 0)
		{
			return blocks;
		}

		var currentBlock = new DiffBlock();
		var isFirstDifference = true;

		foreach (var diff in sortedDifferences)
		{
			// If this is the first difference or it's contiguous with the previous one
			if (isFirstDifference || IsContiguousDifference(currentBlock, diff))
			{
				AddDifferenceToBlock(currentBlock, diff);
				isFirstDifference = false;
			}
			else
			{
				// Finalize the current block and start a new one
				if (currentBlock.Lines1.Count > 0 || currentBlock.Lines2.Count > 0)
				{
					blocks.Add(currentBlock);
				}

				currentBlock = new DiffBlock();
				AddDifferenceToBlock(currentBlock, diff);
			}
		}

		// Add the final block
		if (currentBlock.Lines1.Count > 0 || currentBlock.Lines2.Count > 0)
		{
			blocks.Add(currentBlock);
		}

		return blocks;
	}

	/// <summary>
	/// Checks if a difference is contiguous with the current block
	/// </summary>
	private static bool IsContiguousDifference(DiffBlock currentBlock, LineDifference diff)
	{
		if (currentBlock.Lines1.Count == 0 && currentBlock.Lines2.Count == 0)
		{
			return true;
		}

		// Get the last line numbers from the current block
		var lastLine1 = currentBlock.LastLineNumber1;
		var lastLine2 = currentBlock.LastLineNumber2;

		// Consider differences contiguous if they're within 1 line of each other
		var isContiguous1 = diff.LineNumber1 <= 0 || lastLine1 <= 0 || Math.Abs(diff.LineNumber1 - lastLine1) <= 1;
		var isContiguous2 = diff.LineNumber2 <= 0 || lastLine2 <= 0 || Math.Abs(diff.LineNumber2 - lastLine2) <= 1;

		return isContiguous1 && isContiguous2;
	}

	/// <summary>
	/// Adds a difference to the current block
	/// </summary>
	private static void AddDifferenceToBlock(DiffBlock currentBlock, LineDifference diff)
	{
		if (diff.LineNumber1 > 0 && !string.IsNullOrEmpty(diff.Content1))
		{
			currentBlock.Lines1.Add(diff.Content1);
			currentBlock.LineNumbers1.Add(diff.LineNumber1);
		}

		if (diff.LineNumber2 > 0 && !string.IsNullOrEmpty(diff.Content2))
		{
			currentBlock.Lines2.Add(diff.Content2);
			currentBlock.LineNumbers2.Add(diff.LineNumber2);
		}

		// Determine block type
		if (currentBlock.Lines1.Count > 0 && currentBlock.Lines2.Count > 0)
		{
			currentBlock.Type = BlockType.Replace;
		}
		else if (currentBlock.Lines1.Count > 0)
		{
			currentBlock.Type = BlockType.Delete;
		}
		else if (currentBlock.Lines2.Count > 0)
		{
			currentBlock.Type = BlockType.Insert;
		}
	}

	/// <summary>
	/// Handles an insertion block (content only in version 2)
	/// </summary>
	private static void HandleInsertionBlockWithContext(string[] lines1, string[] lines2, DiffBlock block, List<string> mergedLines, int blockNumber)
	{
		var (contextBefore1, contextAfter1, contextBefore2, contextAfter2) = GetContextLines(lines1, lines2, block);

		var rule = new Rule($"[bold green]Block {blockNumber}: Addition in Version 2[/]")
		{
			Style = Style.Parse("green")
		};
		AnsiConsole.Write(rule);

		// Show the insertion content
		var panel = new Panel(string.Join("\n", contextBefore2.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")))
		{
			Header = new PanelHeader("[green bold]Context before[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("green")
		};

		AnsiConsole.Write(panel);

		panel = new Panel(string.Join("\n", block.Lines2.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")))
		{
			Header = new PanelHeader("[green bold]Content to Add[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("green")
		};

		AnsiConsole.Write(panel);

		panel = new Panel(string.Join("\n", contextAfter2.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")))
		{
			Header = new PanelHeader("[green bold]Context after[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("green")
		};

		AnsiConsole.Write(panel);

		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]What would you like to do with this addition?[/]")
				.AddChoices([
					"✅ Include this content",
					"❌ Skip this content"
				]));

		if (choice.Contains("Include"))
		{
			for (var i = 0; i < block.Lines2.Count; i++)
			{
				mergedLines.Add(block.Lines2[i]);
			}
			AnsiConsole.MarkupLine("[green]✓ Content included[/]");
		}
		else
		{
			AnsiConsole.MarkupLine("[yellow]⚠ Content skipped[/]");
		}

		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Handles a deletion block (content only in version 1)
	/// </summary>
	private static void HandleDeletionBlockWithContext(string[] lines1, string[] lines2, DiffBlock block, List<string> mergedLines, int blockNumber)
	{
		var (contextBefore1, contextAfter1, contextBefore2, contextAfter2) = GetContextLines(lines1, lines2, block);

		var rule = new Rule($"[bold red]Block {blockNumber}: Deletion from Version 1[/]")
		{
			Style = Style.Parse("red")
		};
		AnsiConsole.Write(rule);

		// Show the deletion content
		var panel = new Panel(string.Join("\n", contextBefore1.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")))
		{
			Header = new PanelHeader("[red bold]Context before[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("red")
		};

		AnsiConsole.Write(panel);

		panel = new Panel(string.Join("\n", block.Lines1.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")))
		{
			Header = new PanelHeader("[red bold]Content to Remove[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("red")
		};

		AnsiConsole.Write(panel);

		panel = new Panel(string.Join("\n", contextAfter1.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")))
		{
			Header = new PanelHeader("[red bold]Context after[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("red")
		};

		AnsiConsole.Write(panel);

		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]What would you like to do with this deletion?[/]")
				.AddChoices([
					"✅ Keep this content (don't delete)",
					"❌ Remove this content (confirm deletion)"
				]));

		if (choice.Contains("Keep"))
		{
			for (var i = 0; i < block.Lines1.Count; i++)
			{
				mergedLines.Add(block.Lines1[i]);
			}
			AnsiConsole.MarkupLine("[green]✓ Content kept[/]");
		}
		else
		{
			AnsiConsole.MarkupLine("[yellow]⚠ Content removed[/]");
		}

		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Handles a replacement block (different content in both versions)
	/// </summary>
	private static void HandleReplacementBlockWithContext(string[] lines1, string[] lines2, DiffBlock block, List<string> mergedLines, int blockNumber)
	{
		var (contextBefore1, contextAfter1, contextBefore2, contextAfter2) = GetContextLines(lines1, lines2, block);

		var rule = new Rule($"[bold yellow]Block {blockNumber}: Different Content[/]")
		{
			Style = Style.Parse("yellow")
		};
		AnsiConsole.Write(rule);

		// Show both versions side by side
		var version1Content = string.Join("\n", contextBefore1.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")
			.Concat(block.Lines1.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}"))
			.Concat(contextAfter1.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")));
		var version2Content = string.Join("\n", contextBefore2.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")
			.Concat(block.Lines2.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}"))
			.Concat(contextAfter2.Select((line, i) => $"{i + 1,4}: {line.EscapeMarkup()}")));

		var leftPanel = new Panel(version1Content)
		{
			Header = new PanelHeader("[red bold]Version 1[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("red")
		};

		var rightPanel = new Panel(version2Content)
		{
			Header = new PanelHeader("[green bold]Version 2[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("green")
		};

		AnsiConsole.Write(new Columns(leftPanel, rightPanel)
		{
			Expand = false,
		});

		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Which version would you like to use?[/]")
				.AddChoices([
					"📄 Use Version 1 (red)",
					"📄 Use Version 2 (green)",
					"🔀 Use Both (Version 1 first, then Version 2)",
					"❌ Skip Both (exclude this content)"
				]));

		if (choice.Contains("Use Version 1"))
		{
			for (var i = 0; i < block.Lines1.Count; i++)
			{
				mergedLines.Add(block.Lines1[i]);
			}
			AnsiConsole.MarkupLine("[red]✓ Version 1 selected[/]");
		}
		else if (choice.Contains("Use Version 2"))
		{
			for (var i = 0; i < block.Lines2.Count; i++)
			{
				mergedLines.Add(block.Lines2[i]);
			}
			AnsiConsole.MarkupLine("[green]✓ Version 2 selected[/]");
		}
		else if (choice.Contains("Use Both"))
		{
			// Add Version 1 first
			for (var i = 0; i < block.Lines1.Count; i++)
			{
				mergedLines.Add(block.Lines1[i]);
			}
			// Then add Version 2
			for (var i = 0; i < block.Lines2.Count; i++)
			{
				mergedLines.Add(block.Lines2[i]);
			}
			AnsiConsole.MarkupLine("[yellow]✓ Both versions included[/]");
		}
		else
		{
			AnsiConsole.MarkupLine("[yellow]⚠ Both versions skipped[/]");
		}

		AnsiConsole.WriteLine();
	}
}

/// <summary>
/// Manages command history for the CLI application
/// </summary>
public class InputHistory
{
	private readonly List<string> _history = [];
	private readonly string _historyFilePath;
	private const int MaxHistorySize = 100;
	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	/// <summary>
	/// Initializes a new instance of the InputHistory class
	/// </summary>
	public InputHistory()
	{
		var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		var diffMorePath = Path.Combine(appDataPath, "DiffMore");
		Directory.CreateDirectory(diffMorePath);
		_historyFilePath = Path.Combine(diffMorePath, "input_history.json");
		LoadHistory();
	}

	/// <summary>
	/// Gets the current history as a read-only list
	/// </summary>
	public IReadOnlyList<string> History => _history.AsReadOnly();

	/// <summary>
	/// Adds a new entry to the history
	/// </summary>
	/// <param name="input">The input to add to history</param>
	public void AddToHistory(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return;
		}

		// Remove existing entry if it exists
		_history.Remove(input);

		// Add to the end (most recent)
		_history.Add(input);

		// Keep only the last MaxHistorySize entries
		while (_history.Count > MaxHistorySize)
		{
			_history.RemoveAt(0);
		}

		SaveHistory();
	}

	/// <summary>
	/// Loads history from the file
	/// </summary>
	private void LoadHistory()
	{
		try
		{
			if (File.Exists(_historyFilePath))
			{
				var json = File.ReadAllText(_historyFilePath);
				var history = JsonSerializer.Deserialize<List<string>>(json);
				if (history != null)
				{
					_history.Clear();
					_history.AddRange(history);
				}
			}
		}
		catch (JsonException)
		{
			// Ignore JSON parsing errors - start with empty history
		}
		catch (IOException)
		{
			// Ignore file I/O errors - start with empty history
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access errors - start with empty history
		}
	}

	/// <summary>
	/// Saves history to the file
	/// </summary>
	private void SaveHistory()
	{
		try
		{
			var json = JsonSerializer.Serialize(_history, JsonOptions);
			File.WriteAllText(_historyFilePath, json);
		}
		catch (JsonException)
		{
			// Ignore JSON serialization errors
		}
		catch (IOException)
		{
			// Ignore file I/O errors
		}
		catch (UnauthorizedAccessException)
		{
			// Ignore access errors
		}
	}
}

/// <summary>
/// Provides input methods with history support
/// </summary>
public static class HistoryInput
{
	private static readonly InputHistory _inputHistory = new();

	/// <summary>
	/// Prompts for input with history support using arrow keys
	/// </summary>
	/// <param name="prompt">The prompt message to display</param>
	/// <param name="defaultValue">Default value if provided</param>
	/// <returns>The user input</returns>
	public static string AskWithHistory(string prompt, string? defaultValue = null)
	{
		AnsiConsole.Markup(prompt + " ");

		var input = new List<char>();
		var historyIndex = _inputHistory.History.Count; // Start at end (no history selected)
		var originalInput = defaultValue ?? "";

		// Initialize with default value if provided
		if (!string.IsNullOrEmpty(defaultValue))
		{
			input.AddRange(defaultValue);
			AnsiConsole.Markup(defaultValue);
		}

		var cursorPosition = input.Count;

		while (true)
		{
			var key = Console.ReadKey(true);

			if (key.Key == ConsoleKey.Enter)
			{
				AnsiConsole.WriteLine();
				var result = new string([.. input]);
				if (!string.IsNullOrWhiteSpace(result))
				{
					_inputHistory.AddToHistory(result);
				}
				return result;
			}

			if (ProcessSpecialKey(key, input, ref historyIndex, ref cursorPosition, originalInput))
			{
				continue;
			}

			// Handle regular character input
			if (!char.IsControl(key.KeyChar))
			{
				HandleRegularInput(input, ref cursorPosition, ref historyIndex, key);
			}
		}
	}

	/// <summary>
	/// Processes special keys (arrows, home, end, etc.)
	/// </summary>
	/// <returns>True if the key was handled, false otherwise</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "<Pending>")]
	private static bool ProcessSpecialKey(ConsoleKeyInfo key, List<char> input, ref int historyIndex, ref int cursorPosition, string originalInput)
	{
		return key.Key switch
		{
			ConsoleKey.UpArrow => HandleUpArrowAndReturn(input, ref historyIndex, ref cursorPosition),
			ConsoleKey.DownArrow => HandleDownArrowAndReturn(input, ref historyIndex, ref cursorPosition, originalInput),
			ConsoleKey.LeftArrow => HandleLeftArrowAndReturn(ref cursorPosition),
			ConsoleKey.RightArrow => HandleRightArrowAndReturn(input, ref cursorPosition),
			ConsoleKey.Home => HandleHomeAndReturn(ref cursorPosition),
			ConsoleKey.End => HandleEndAndReturn(input, ref cursorPosition),
			ConsoleKey.Backspace => HandleBackspaceAndReturn(input, ref cursorPosition, ref historyIndex),
			ConsoleKey.Delete => HandleDeleteAndReturn(input, ref cursorPosition, ref historyIndex),
			ConsoleKey.Escape => HandleEscapeAndReturn(input, ref cursorPosition, ref historyIndex),
			_ => false // Key not handled
		};
	}

	/// <summary>
	/// Handles up arrow key press for history navigation and returns true
	/// </summary>
	private static bool HandleUpArrowAndReturn(List<char> input, ref int historyIndex, ref int cursorPosition)
	{
		HandleUpArrow(input, ref historyIndex, ref cursorPosition);
		return true;
	}

	/// <summary>
	/// Handles down arrow key press for history navigation and returns true
	/// </summary>
	private static bool HandleDownArrowAndReturn(List<char> input, ref int historyIndex, ref int cursorPosition, string originalInput)
	{
		HandleDownArrow(input, ref historyIndex, ref cursorPosition, originalInput);
		return true;
	}

	/// <summary>
	/// Handles left arrow key press for cursor movement and returns true
	/// </summary>
	private static bool HandleLeftArrowAndReturn(ref int cursorPosition)
	{
		HandleLeftArrow(ref cursorPosition);
		return true;
	}

	/// <summary>
	/// Handles right arrow key press for cursor movement and returns true
	/// </summary>
	private static bool HandleRightArrowAndReturn(List<char> input, ref int cursorPosition)
	{
		HandleRightArrow(input, ref cursorPosition);
		return true;
	}

	/// <summary>
	/// Handles Home key press to move cursor to beginning and returns true
	/// </summary>
	private static bool HandleHomeAndReturn(ref int cursorPosition)
	{
		HandleHome(ref cursorPosition);
		return true;
	}

	/// <summary>
	/// Handles End key press to move cursor to end and returns true
	/// </summary>
	private static bool HandleEndAndReturn(List<char> input, ref int cursorPosition)
	{
		HandleEnd(input, ref cursorPosition);
		return true;
	}

	/// <summary>
	/// Handles Backspace key press and returns true
	/// </summary>
	private static bool HandleBackspaceAndReturn(List<char> input, ref int cursorPosition, ref int historyIndex)
	{
		HandleBackspace(input, ref cursorPosition, ref historyIndex);
		return true;
	}

	/// <summary>
	/// Handles Delete key press and returns true
	/// </summary>
	private static bool HandleDeleteAndReturn(List<char> input, ref int cursorPosition, ref int historyIndex)
	{
		HandleDelete(input, ref cursorPosition, ref historyIndex);
		return true;
	}

	/// <summary>
	/// Handles Escape key press to clear input and returns true
	/// </summary>
	private static bool HandleEscapeAndReturn(List<char> input, ref int cursorPosition, ref int historyIndex)
	{
		HandleEscape(input, ref cursorPosition, ref historyIndex);
		return true;
	}

	/// <summary>
	/// Handles up arrow key press for history navigation
	/// </summary>
	private static void HandleUpArrow(List<char> input, ref int historyIndex, ref int cursorPosition)
	{
		if (_inputHistory.History.Count > 0 && historyIndex > 0)
		{
			historyIndex--;
			ReplaceCurrentInput(input, _inputHistory.History[historyIndex], ref cursorPosition);
		}
	}

	/// <summary>
	/// Handles down arrow key press for history navigation
	/// </summary>
	private static void HandleDownArrow(List<char> input, ref int historyIndex, ref int cursorPosition, string originalInput)
	{
		if (historyIndex < _inputHistory.History.Count - 1)
		{
			historyIndex++;
			ReplaceCurrentInput(input, _inputHistory.History[historyIndex], ref cursorPosition);
		}
		else if (historyIndex == _inputHistory.History.Count - 1)
		{
			historyIndex++;
			ReplaceCurrentInput(input, originalInput, ref cursorPosition);
		}
	}

	/// <summary>
	/// Handles left arrow key press for cursor movement
	/// </summary>
	private static void HandleLeftArrow(ref int cursorPosition)
	{
		if (cursorPosition > 0)
		{
			cursorPosition--;
			AnsiConsole.Cursor.Move(CursorDirection.Left, 1);
		}
	}

	/// <summary>
	/// Handles right arrow key press for cursor movement
	/// </summary>
	private static void HandleRightArrow(List<char> input, ref int cursorPosition)
	{
		if (cursorPosition < input.Count)
		{
			cursorPosition++;
			AnsiConsole.Cursor.Move(CursorDirection.Right, 1);
		}
	}

	/// <summary>
	/// Handles Home key press to move cursor to beginning
	/// </summary>
	private static void HandleHome(ref int cursorPosition)
	{
		if (cursorPosition > 0)
		{
			AnsiConsole.Cursor.Move(CursorDirection.Left, cursorPosition);
			cursorPosition = 0;
		}
	}

	/// <summary>
	/// Handles End key press to move cursor to end
	/// </summary>
	private static void HandleEnd(List<char> input, ref int cursorPosition)
	{
		if (cursorPosition < input.Count)
		{
			AnsiConsole.Cursor.Move(CursorDirection.Right, input.Count - cursorPosition);
			cursorPosition = input.Count;
		}
	}

	/// <summary>
	/// Handles Backspace key press
	/// </summary>
	private static void HandleBackspace(List<char> input, ref int cursorPosition, ref int historyIndex)
	{
		if (cursorPosition > 0)
		{
			input.RemoveAt(cursorPosition - 1);
			cursorPosition--;
			RedrawInput(input, cursorPosition);
			historyIndex = _inputHistory.History.Count; // Reset history navigation
		}
	}

	/// <summary>
	/// Handles Delete key press
	/// </summary>
	private static void HandleDelete(List<char> input, ref int cursorPosition, ref int historyIndex)
	{
		if (cursorPosition < input.Count)
		{
			input.RemoveAt(cursorPosition);
			RedrawInput(input, cursorPosition);
			historyIndex = _inputHistory.History.Count; // Reset history navigation
		}
	}

	/// <summary>
	/// Handles Escape key press to clear input
	/// </summary>
	private static void HandleEscape(List<char> input, ref int cursorPosition, ref int historyIndex)
	{
		ReplaceCurrentInput(input, "", ref cursorPosition);
		historyIndex = _inputHistory.History.Count;
	}

	/// <summary>
	/// Replaces the current input with new text
	/// </summary>
	/// <param name="input">The current input list</param>
	/// <param name="newText">The new text to replace with</param>
	/// <param name="cursorPosition">The cursor position reference</param>
	private static void ReplaceCurrentInput(List<char> input, string newText, ref int cursorPosition)
	{
		// Clear current line
		AnsiConsole.Cursor.Move(CursorDirection.Left, cursorPosition);
		AnsiConsole.Markup(new string(' ', input.Count));
		AnsiConsole.Cursor.Move(CursorDirection.Left, input.Count);

		// Update input
		input.Clear();
		input.AddRange(newText);
		cursorPosition = input.Count;

		// Display new text
		if (input.Count > 0)
		{
			AnsiConsole.Markup(new string([.. input]).EscapeMarkup());
		}
	}

	/// <summary>
	/// Redraws the input line from the current cursor position
	/// </summary>
	/// <param name="input">The current input</param>
	/// <param name="cursorPosition">The cursor position</param>
	private static void RedrawInput(List<char> input, int cursorPosition)
	{
		// Save current cursor position
		var savedPosition = cursorPosition;

		// Move to beginning of input
		AnsiConsole.Cursor.Move(CursorDirection.Left, cursorPosition);

		// Clear the line and redraw
		var inputText = new string([.. input]);
		AnsiConsole.Markup(inputText.EscapeMarkup() + " "); // Extra space to clear any remaining character

		// Move cursor back to correct position
		AnsiConsole.Cursor.Move(CursorDirection.Left, inputText.Length + 1 - savedPosition);
	}

	/// <summary>
	/// Handles regular character input
	/// </summary>
	private static void HandleRegularInput(List<char> input, ref int cursorPosition, ref int historyIndex, ConsoleKeyInfo key)
	{
		if (!char.IsControl(key.KeyChar))
		{
			input.Insert(cursorPosition, key.KeyChar);
			cursorPosition++;

			// Redraw from cursor position
			var remainingChars = input.Skip(cursorPosition - 1).ToArray();
			var remainingText = new string(remainingChars);
			AnsiConsole.Markup(remainingText.EscapeMarkup());

			// Move cursor back to correct position
			if (remainingText.Length > 1)
			{
				AnsiConsole.Cursor.Move(CursorDirection.Left, remainingText.Length - 1);
			}

			historyIndex = _inputHistory.History.Count; // Reset history navigation
		}
	}
}
