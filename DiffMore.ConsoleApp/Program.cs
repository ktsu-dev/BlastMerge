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
using DiffPlex.DiffBuilder.Model;

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
	/// Shows a colored diff between two files in side-by-side format
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	private static void ShowColoredDiff(string file1, string file2)
	{
		try
		{
			var sideBySideDiff = DiffPlexDiffer.GenerateSideBySideDiff(file1, file2);

			var table = new Table()
				.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
				.AddColumn(new TableColumn($"[bold]{Path.GetFileName(file1)}[/]").Width(50))
				.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
				.AddColumn(new TableColumn($"[bold]{Path.GetFileName(file2)}[/]").Width(50))
				.Border(TableBorder.Rounded)
				.Expand();

			var maxLines = Math.Max(sideBySideDiff.OldText.Lines.Count, sideBySideDiff.NewText.Lines.Count);
			var displayedLines = 0;
			const int maxDisplayLines = 100; // Limit for TUI performance

			for (var i = 0; i < maxLines && displayedLines < maxDisplayLines; i++)
			{
				var oldLine = i < sideBySideDiff.OldText.Lines.Count ? sideBySideDiff.OldText.Lines[i] : null;
				var newLine = i < sideBySideDiff.NewText.Lines.Count ? sideBySideDiff.NewText.Lines[i] : null;

				var lineNum1 = "";
				var content1 = "";
				var lineNum2 = "";
				var content2 = "";

				if (oldLine != null)
				{
					lineNum1 = (oldLine.Position + 1).ToString();
					content1 = FormatDiffLine(oldLine);
				}

				if (newLine != null)
				{
					lineNum2 = (newLine.Position + 1).ToString();
					content2 = FormatDiffLine(newLine);
				}

				table.AddRow(lineNum1 ?? "", content1 ?? "", lineNum2 ?? "", content2 ?? "");
				displayedLines++;
			}

			if (maxLines > maxDisplayLines)
			{
				table.AddRow("[dim]...[/]", "[dim]...[/]", "[dim]...[/]", $"[dim]... and {maxLines - maxDisplayLines} more lines[/]");
			}

			AnsiConsole.Write(table);
		}
		catch (FileNotFoundException ex)
		{
			AnsiConsole.MarkupLine($"[red]File not found: {ex.Message}[/]");
		}
		catch (IOException ex)
		{
			AnsiConsole.MarkupLine($"[red]IO error generating side-by-side diff: {ex.Message}[/]");
		}
		catch (UnauthorizedAccessException ex)
		{
			AnsiConsole.MarkupLine($"[red]Access denied: {ex.Message}[/]");
		}
	}

	/// <summary>
	/// Formats a diff line with appropriate color and prefix
	/// </summary>
	/// <param name="line">The diff line to format</param>
	/// <returns>Formatted line with color markup</returns>
	private static string FormatDiffLine(DiffPiece line)
	{
		var text = line.Text?.EscapeMarkup() ?? "";

		return line.Type switch
		{
			ChangeType.Deleted => $"[red on darkred]- {text}[/]",
			ChangeType.Inserted => $"[green on darkgreen]+ {text}[/]",
			ChangeType.Modified => $"[yellow on darkorange]~ {text}[/]",
			ChangeType.Imaginary => "[dim]   [/]",
			ChangeType.Unchanged => $"  {text}",
			_ => $"  {text}"
		};
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
	/// Runs iterative merge process
	/// </summary>
	private static void RunIterativeMerge()
	{
		var directory = HistoryInput.AskWithHistory("[cyan]Enter the directory path:[/]");

		if (!Directory.Exists(directory))
		{
			AnsiConsole.MarkupLine("[red]Error: Directory does not exist![/]");
			return;
		}

		var fileName = HistoryInput.AskWithHistory("[cyan]Enter the filename to search for:[/]");

		// Prepare file groups for merging
		var fileGroups = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(directory, fileName);

		if (fileGroups == null || fileGroups.Count < 2)
		{
			AnsiConsole.MarkupLine("[red]Error: Need at least 2 different versions to merge![/]");
			return;
		}

		AnsiConsole.MarkupLine($"[green]Found {fileGroups.Count} unique versions to merge.[/]");

		// Start the iterative merge process
		var result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			PerformSingleMerge,
			ReportMergeStatus,
			ConfirmContinuation
		);

		// Handle the result
		if (result.IsSuccessful)
		{
			AnsiConsole.MarkupLine("[green]✅ Merge completed successfully![/]");
			AnsiConsole.MarkupLine($"[cyan]Final result has {result.FinalLineCount} lines.[/]");

			if (AnsiConsole.Confirm("[cyan]Would you like to save the merged result to a file?[/]", true))
			{
				var outputPath = HistoryInput.AskWithHistory("[cyan]Enter output file path:[/]",
					Path.Combine(directory, $"merged_{fileName}"));

				try
				{
					if (result.FinalMergedContent != null)
					{
						File.WriteAllText(outputPath, result.FinalMergedContent);
						AnsiConsole.MarkupLine($"[green]✅ Merged result saved to: {outputPath}[/]");
					}
				}
				catch (IOException ex)
				{
					AnsiConsole.MarkupLine($"[red]❌ Error saving file: {ex.Message}[/]");
				}
			}
		}
		else
		{
			AnsiConsole.MarkupLine("[yellow]⚠️ Merge was cancelled or incomplete.[/]");
		}
	}

	/// <summary>
	/// Compares two directories
	/// </summary>
	/// <param name="dir1">First directory path</param>
	/// <param name="dir2">Second directory path</param>
	/// <param name="pattern">File search pattern</param>
	/// <param name="recursive">Whether to search recursively</param>
	private static void CompareDirectories(string dir1, string dir2, string pattern, bool recursive)
	{
		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start($"[yellow]Comparing directories...[/]", ctx =>
			{
				var result = FileDiffer.FindDifferences(dir1, dir2, pattern, recursive);

				ctx.Status("[yellow]Creating comparison report...[/]");

				// Display results
				var table = new Table()
					.AddColumn("[bold]Category[/]")
					.AddColumn("[bold]Count[/]")
					.AddColumn("[bold]Examples[/]")
					.Border(TableBorder.Rounded);

				table.AddRow(
					"[green]Identical Files[/]",
					$"[cyan]{result.SameFiles.Count}[/]",
					string.Join(", ", result.SameFiles.Take(3).Select(Path.GetFileName))
				);

				table.AddRow(
					"[yellow]Modified Files[/]",
					$"[cyan]{result.ModifiedFiles.Count}[/]",
					string.Join(", ", result.ModifiedFiles.Take(3).Select(Path.GetFileName))
				);

				table.AddRow(
					"[red]Only in First Directory[/]",
					$"[cyan]{result.OnlyInDir1.Count}[/]",
					string.Join(", ", result.OnlyInDir1.Take(3).Select(Path.GetFileName))
				);

				table.AddRow(
					"[red]Only in Second Directory[/]",
					$"[cyan]{result.OnlyInDir2.Count}[/]",
					string.Join(", ", result.OnlyInDir2.Take(3).Select(Path.GetFileName))
				);

				AnsiConsole.Write(table);

				// Show detailed differences for modified files if requested
				if (result.ModifiedFiles.Count > 0 &&
					AnsiConsole.Confirm("[cyan]Show detailed differences for modified files?[/]", false))
				{
					foreach (var file in result.ModifiedFiles.Take(5)) // Limit to first 5
					{
						var file1 = Path.Combine(dir1, file);
						var file2 = Path.Combine(dir2, file);

						AnsiConsole.WriteLine();
						var rule = new Rule($"[bold]Differences: {file}[/]")
						{
							Style = Style.Parse("blue")
						};
						AnsiConsole.Write(rule);

						ShowChangeSummary(file1, file2);
					}
				}
			});
	}

	/// <summary>
	/// Compares two specific files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	private static void CompareTwoFiles(string file1, string file2)
	{
		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start("[yellow]Comparing files...[/]", ctx =>
			{
				var areSame = DiffPlexDiffer.AreFilesIdentical(file1, file2);

				if (areSame)
				{
					AnsiConsole.MarkupLine("[green]✅ Files are identical![/]");
					return;
				}

				AnsiConsole.MarkupLine("[yellow]📄 Files are different.[/]");

				var diffFormat = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("[cyan]Choose diff format:[/]")
						.AddChoices([
							"📊 Change Summary (Added/Removed lines only)",
							"🔧 Git-style Diff (Full context)",
							"🎨 Colored Diff (Rich formatting)"
						]));

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
			});
	}

	/// <summary>
	/// Offers sync options for file groups
	/// </summary>
	/// <param name="fileGroups">The file groups to offer sync options for</param>
	private static void OfferSyncOptions(List<FileGroup> fileGroups)
	{
		if (fileGroups.Count <= 1)
		{
			return; // All files are already identical
		}

		if (!AnsiConsole.Confirm("[cyan]Would you like to sync files to make them identical?[/]", false))
		{
			return;
		}

		// Let user choose which version to use as the source
		var choices = new List<string>();
		for (var i = 0; i < fileGroups.Count; i++)
		{
			var group = fileGroups[i];
			choices.Add($"Version {i + 1} ({group.FilePaths.Count} files) - {group.FilePaths.First()}");
		}

		var selectedVersion = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Which version should be used as the source for syncing?[/]")
				.AddChoices(choices));

		var sourceIndex = choices.IndexOf(selectedVersion);
		var sourceGroup = fileGroups[sourceIndex];
		var sourceFile = sourceGroup.FilePaths.First();

		// Collect all target files (files that are different from the source)
		var targetFiles = new List<string>();
		for (var i = 0; i < fileGroups.Count; i++)
		{
			if (i != sourceIndex)
			{
				targetFiles.AddRange(fileGroups[i].FilePaths);
			}
		}

		if (targetFiles.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]No files need to be synced.[/]");
			return;
		}

		AnsiConsole.MarkupLine($"[yellow]This will copy content from:[/]");
		AnsiConsole.MarkupLine($"[cyan]  📁 {sourceFile}[/]");
		AnsiConsole.MarkupLine($"[yellow]To {targetFiles.Count} different files.[/]");

		if (!AnsiConsole.Confirm("[red]⚠️ This will overwrite the target files. Continue?[/]", false))
		{
			return;
		}

		// Perform the sync
		var successCount = 0;
		var failureCount = 0;

		AnsiConsole.Progress()
			.Start(ctx =>
			{
				var task = ctx.AddTask("[green]Syncing files...[/]");
				task.MaxValue = targetFiles.Count;

				foreach (var targetFile in targetFiles)
				{
					try
					{
						File.Copy(sourceFile, targetFile, overwrite: true);
						successCount++;
					}
					catch (IOException)
					{
						failureCount++;
					}
					catch (UnauthorizedAccessException)
					{
						failureCount++;
					}

					task.Increment(1);
				}
			});

		AnsiConsole.MarkupLine($"[green]✅ Successfully synced {successCount} files.[/]");
		if (failureCount > 0)
		{
			AnsiConsole.MarkupLine($"[red]❌ Failed to sync {failureCount} files.[/]");
		}
	}

	/// <summary>
	/// Performs a single merge operation
	/// </summary>
	/// <param name="file1">First file to merge</param>
	/// <param name="file2">Second file to merge</param>
	/// <param name="existingMergedContent">Existing merged content (if any)</param>
	/// <returns>The merge result, or null if cancelled</returns>
	private static MergeResult? PerformSingleMerge(string file1, string file2, string? existingMergedContent)
	{
		AnsiConsole.MarkupLine($"[yellow]🔀 Merging:[/]");
		if (existingMergedContent != null)
		{
			AnsiConsole.MarkupLine($"[dim]  📋 <existing merged content>[/]");
		}
		else
		{
			AnsiConsole.MarkupLine($"[dim]  📁 {file1}[/]");
		}
		AnsiConsole.MarkupLine($"[dim]  📁 {file2}[/]");
		AnsiConsole.WriteLine();

		return IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			file1, file2, existingMergedContent, GetBlockChoice);
	}

	/// <summary>
	/// Reports merge status to the user
	/// </summary>
	/// <param name="status">The merge status to report</param>
	private static void ReportMergeStatus(MergeSessionStatus status)
	{
		var rule = new Rule($"[bold yellow]Merge Step {status.CurrentIteration}[/]")
		{
			Style = Style.Parse("yellow")
		};
		AnsiConsole.Write(rule);

		AnsiConsole.MarkupLine($"[cyan]📊 Files remaining: {status.RemainingFilesCount}[/]");
		AnsiConsole.MarkupLine($"[cyan]📋 Merges completed: {status.CompletedMergesCount}[/]");

		if (status.MostSimilarPair != null)
		{
			AnsiConsole.MarkupLine($"[cyan]🎯 Similarity score: {status.MostSimilarPair.SimilarityScore:P1}[/]");
		}

		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Confirms whether to continue with the merge process
	/// </summary>
	/// <returns>True to continue, false to stop</returns>
	private static bool ConfirmContinuation() => AnsiConsole.Confirm("[cyan]Continue with the next merge step?[/]", true);

	/// <summary>
	/// Gets the user's choice for a merge block
	/// </summary>
	/// <param name="block">The diff block to choose for</param>
	/// <param name="context">The context around the block</param>
	/// <param name="blockNumber">The block number being processed</param>
	/// <returns>The user's choice for the block</returns>
	private static BlockChoice GetBlockChoice(DiffBlock block, BlockContext context, int blockNumber)
	{
		AnsiConsole.MarkupLine($"[yellow]🔍 Block {blockNumber} ({block.Type})[/]");

		// Show the block content with context
		ShowBlockWithContext(block, context);

		// Get user's choice based on block type
		return block.Type switch
		{
			BlockType.Insert => GetInsertChoice(),
			BlockType.Delete => GetDeleteChoice(),
			BlockType.Replace => GetReplaceChoice(),
			_ => throw new InvalidOperationException($"Unknown block type: {block.Type}")
		};
	}

	/// <summary>
	/// Shows a block with its context in a side-by-side diff format
	/// </summary>
	/// <param name="block">The diff block to show</param>
	/// <param name="context">The context around the block</param>
	private static void ShowBlockWithContext(DiffBlock block, BlockContext context)
	{
		AnsiConsole.MarkupLine($"[yellow]Block Type: {block.Type}[/]");
		AnsiConsole.WriteLine();

		// Create a side-by-side diff visualization
		var table = new Table()
			.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
			.AddColumn(new TableColumn("[bold]Version 1[/]").Width(50))
			.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
			.AddColumn(new TableColumn("[bold]Version 2[/]").Width(50))
			.Border(TableBorder.Rounded)
			.Expand();

		// Calculate correct starting line numbers for context before the block
		var firstLine1 = block.LineNumbers1.Count > 0 ? block.LineNumbers1.Min() : 1;
		var firstLine2 = block.LineNumbers2.Count > 0 ? block.LineNumbers2.Min() : 1;

		var contextStartLine1 = Math.Max(1, firstLine1 - context.ContextBefore1.Count);
		var contextStartLine2 = Math.Max(1, firstLine2 - context.ContextBefore2.Count);

		// Show context before the block
		ShowContextLines(table, [.. context.ContextBefore1], [.. context.ContextBefore2], contextStartLine1, contextStartLine2);

		// Show the actual diff block
		ShowDiffBlock(table, block);

		// Show context after the block
		ShowContextLines(table, [.. context.ContextAfter1], [.. context.ContextAfter2],
			block.LineNumbers1.LastOrDefault() + 1, block.LineNumbers2.LastOrDefault() + 1);

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows context lines in the side-by-side table
	/// </summary>
	/// <param name="table">The table to add rows to</param>
	/// <param name="lines1">Context lines from version 1</param>
	/// <param name="lines2">Context lines from version 2</param>
	/// <param name="startLine1">Starting line number for version 1</param>
	/// <param name="startLine2">Starting line number for version 2</param>
	private static void ShowContextLines(Table table, string[] lines1, string[] lines2, int startLine1, int startLine2)
	{
		var maxLines = Math.Max(lines1.Length, lines2.Length);

		for (var i = 0; i < maxLines; i++)
		{
			var line1 = i < lines1.Length ? lines1[i] : "";
			var line2 = i < lines2.Length ? lines2[i] : "";

			var lineNum1 = i < lines1.Length ? (startLine1 + i).ToString() : "";
			var lineNum2 = i < lines2.Length ? (startLine2 + i).ToString() : "";

			// Context lines are shown with dim styling
			var content1 = string.IsNullOrEmpty(line1) ? "" : $"[dim]{line1.EscapeMarkup()}[/]";
			var content2 = string.IsNullOrEmpty(line2) ? "" : $"[dim]{line2.EscapeMarkup()}[/]";

			table.AddRow(lineNum1 ?? "", content1, lineNum2 ?? "", content2);
		}
	}

	/// <summary>
	/// Shows the diff block with proper highlighting
	/// </summary>
	/// <param name="table">The table to add rows to</param>
	/// <param name="block">The diff block to display</param>
	private static void ShowDiffBlock(Table table, DiffBlock block)
	{
		switch (block.Type)
		{
			case BlockType.Insert:
				ShowInsertBlock(table, block);
				break;
			case BlockType.Delete:
				ShowDeleteBlock(table, block);
				break;
			case BlockType.Replace:
				ShowReplaceBlock(table, block);
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Shows an insert block (content only in version 2)
	/// </summary>
	/// <param name="table">The table to add rows to</param>
	/// <param name="block">The insert block</param>
	private static void ShowInsertBlock(Table table, DiffBlock block)
	{
		for (var i = 0; i < block.Lines2.Count; i++)
		{
			var lineNum2 = block.LineNumbers2.Count > i ? block.LineNumbers2[i].ToString() : "";
			var content2 = $"[green on darkgreen]+ {block.Lines2[i].EscapeMarkup()}[/]";

			table.AddRow("", "", lineNum2, content2);
		}
	}

	/// <summary>
	/// Shows a delete block (content only in version 1)
	/// </summary>
	/// <param name="table">The table to add rows to</param>
	/// <param name="block">The delete block</param>
	private static void ShowDeleteBlock(Table table, DiffBlock block)
	{
		for (var i = 0; i < block.Lines1.Count; i++)
		{
			var lineNum1 = block.LineNumbers1.Count > i ? block.LineNumbers1[i].ToString() : "";
			var content1 = $"[red on darkred]- {block.Lines1[i].EscapeMarkup()}[/]";

			table.AddRow(lineNum1, content1, "", "");
		}
	}

	/// <summary>
	/// Shows a replace block (content differs between versions)
	/// </summary>
	/// <param name="table">The table to add rows to</param>
	/// <param name="block">The replace block</param>
	private static void ShowReplaceBlock(Table table, DiffBlock block)
	{
		var maxLines = Math.Max(block.Lines1.Count, block.Lines2.Count);

		for (var i = 0; i < maxLines; i++)
		{
			var lineNum1 = i < block.LineNumbers1.Count ? block.LineNumbers1[i].ToString() : "";
			var lineNum2 = i < block.LineNumbers2.Count ? block.LineNumbers2[i].ToString() : "";

			var content1 = i < block.Lines1.Count
				? $"[red on darkred]- {block.Lines1[i].EscapeMarkup()}[/]"
				: "";
			var content2 = i < block.Lines2.Count
				? $"[green on darkgreen]+ {block.Lines2[i].EscapeMarkup()}[/]"
				: "";

			table.AddRow(lineNum1, content1, lineNum2, content2);
		}
	}

	/// <summary>
	/// Gets user choice for insert blocks
	/// </summary>
	/// <returns>User's choice</returns>
	private static BlockChoice GetInsertChoice()
	{
		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]This content exists only in Version 2. What would you like to do?[/]")
				.AddChoices([
					"✅ Include the addition",
					"❌ Skip the addition"
				]));

		return choice.Contains("Include") ? BlockChoice.Include : BlockChoice.Skip;
	}

	/// <summary>
	/// Gets user choice for delete blocks
	/// </summary>
	/// <returns>User's choice</returns>
	private static BlockChoice GetDeleteChoice()
	{
		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]This content exists only in Version 1. What would you like to do?[/]")
				.AddChoices([
					"✅ Keep the content",
					"❌ Remove the content"
				]));

		return choice.Contains("Keep") ? BlockChoice.Keep : BlockChoice.Remove;
	}

	/// <summary>
	/// Gets user choice for replace blocks
	/// </summary>
	/// <returns>User's choice</returns>
	private static BlockChoice GetReplaceChoice()
	{
		var choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]This content differs between versions. What would you like to do?[/]")
				.AddChoices([
					"1️⃣ Use Version 1",
					"2️⃣ Use Version 2",
					"🔄 Use Both Versions",
					"❌ Skip Both"
				]));

		return choice switch
		{
			var s when s.Contains("Version 1") => BlockChoice.UseVersion1,
			var s when s.Contains("Version 2") => BlockChoice.UseVersion2,
			var s when s.Contains("Both") => BlockChoice.UseBoth,
			_ => BlockChoice.Skip
		};
	}
}
