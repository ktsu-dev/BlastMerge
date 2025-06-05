// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using DiffPlex.DiffBuilder.Model;
using ktsu.BlastMerge.Core;
using Spectre.Console;

/// <summary>
/// Main program class for the BlastMerge TUI
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

		// Parse command line arguments using CommandLineParser
		using Parser parser = new(with =>
		{
			with.HelpWriter = null; // We'll handle help output ourselves
			with.CaseSensitive = false;
		});

		ParserResult<CommandLineOptions> parseResult = parser.ParseArguments<CommandLineOptions>(args);

		parseResult
			.WithParsed(options =>
			{
				// Handle version command for winget validation
				if (options.ShowVersion)
				{
					ShowVersion();
					return;
				}

				// Handle help command
				if (options.ShowHelp)
				{
					ShowCommandLineHelp();
					return;
				}

				try
				{
					ShowBanner();

					// If directory and filename are provided, try to use them directly
					if (!string.IsNullOrEmpty(options.Directory) && !string.IsNullOrEmpty(options.FileName))
					{
						if (Directory.Exists(options.Directory))
						{
							ProcessFiles(options.Directory, options.FileName);
							return;
						}
						else
						{
							AnsiConsole.MarkupLine($"[red]Error: Directory '{options.Directory}' does not exist![/]");
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
			})
			.WithNotParsed(errors =>
			{
				// Check if it's just a help or version request that failed parsing
				if (errors.Any(e => e.Tag is ErrorType.HelpRequestedError or ErrorType.VersionRequestedError))
				{
					ShowCommandLineHelp();
					return;
				}

				// For other parsing errors, show help
				AnsiConsole.MarkupLine("[red]Invalid command line arguments.[/]");
				ShowCommandLineHelp();
			});
	}

	/// <summary>
	/// Shows the application banner
	/// </summary>
	private static void ShowBanner()
	{
		Rule rule = new("[bold yellow]BlastMerge[/] - Cross-Repository File Synchronization")
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
			string choice = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("[green]What would you like to do?[/]")
					.AddChoices([
						"🔀 Iterative Merge Multiple Versions [bold cyan](PRIMARY FEATURE)[/]",
						"🔍 Compare Files in Directory",
						"📁 Compare Two Directories",
						"📄 Compare Two Specific Files",
						"ℹ️  Show Help",
						"❌ Exit"
					]));

			if (choice.Contains("Iterative Merge Multiple Versions"))
			{
				RunIterativeMerge();
			}
			else if (choice.Contains("Compare Files in Directory"))
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
	/// <param name="directory">The directory to search</param>
	/// <param name="fileName">The filename to search for</param>
	private static void ProcessFiles(string directory, string fileName)
	{
		List<FileGroup>? fileGroupsList = null;
		List<string> filesList = [];

		// Show progress while searching
		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start($"[yellow]Searching for '{fileName}' in '{directory}'...[/]", ctx =>
			{
				IReadOnlyCollection<string> files = FileFinder.FindFiles(directory, fileName);
				filesList = [.. files];

				ctx.Status($"[yellow]Found {filesList.Count} files. Analyzing...[/]");

				if (filesList.Count == 0)
				{
					return;
				}

				// Group files by hash
				IReadOnlyCollection<FileGroup> fileGroups = FileDiffer.GroupFilesByHash(files);
				fileGroupsList = [.. fileGroups];

				// Sort groups by number of files (descending)
				fileGroupsList.Sort((a, b) => b.FilePaths.Count.CompareTo(a.FilePaths.Count));
			});

		// Handle results outside the status context
		if (filesList.Count == 0)
		{
			AnsiConsole.MarkupLine($"[red]No files with name '{fileName}' found.[/]");
			return;
		}

		ShowFileGroups(fileGroupsList!, filesList.Count);

		if (fileGroupsList!.Count <= 1)
		{
			AnsiConsole.MarkupLine("[green]All files are identical.[/]");
			return;
		}

		ShowDifferences(fileGroupsList);
		OfferSyncOptions(fileGroupsList);
	}

	/// <summary>
	/// Compares files with the same name in a directory
	/// </summary>
	private static void CompareFilesInDirectory()
	{
		string directory = HistoryInput.AskWithHistory("[cyan]Enter the directory path:[/]");

		if (!Directory.Exists(directory))
		{
			AnsiConsole.MarkupLine("[red]Error: Directory does not exist![/]");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter the filename to search for:[/]");

		ProcessFiles(directory, fileName);
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

		Table table = new Table()
			.AddColumn("[bold]Version[/]")
			.AddColumn("[bold]Files Count[/]")
			.AddColumn("[bold]Hash[/]")
			.AddColumn("[bold]Files[/]")
			.Border(TableBorder.Rounded);

		for (int i = 0; i < fileGroups.Count; i++)
		{
			FileGroup group = fileGroups[i];
			string filesDisplay = string.Join("\n", group.FilePaths.Take(3));
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

		string diffFormat = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Choose diff format:[/]")
				.AddChoices([
					"📊 Change Summary (Added/Removed lines only)",
					"🔧 Git-style Diff (Full context)",
					"🎨 Colored Diff (Rich formatting)"
				]));

		// Compare all pairs of file groups
		for (int i = 0; i < fileGroups.Count; i++)
		{
			for (int j = i + 1; j < fileGroups.Count; j++)
			{
				FileGroup group1 = fileGroups[i];
				FileGroup group2 = fileGroups[j];
				string file1 = group1.FilePaths.First();
				string file2 = group2.FilePaths.First();

				// Count changes to determine which file should be on the left (fewer changes)
				IReadOnlyCollection<LineDifference> differences = FileDiffer.FindDifferences(file1, file2);
				int changesInFile1 = differences.Count(d => d.LineNumber1.HasValue);  // Deletions and modifications
				int changesInFile2 = differences.Count(d => d.LineNumber2.HasValue);  // Additions and modifications

				// Get relative paths for display
				string relativeFile1 = GetRelativeDirectoryName(file1);
				string relativeFile2 = GetRelativeDirectoryName(file2);

				// Determine order: put file with fewer changes on left
				string leftFile, rightFile, leftLabel, rightLabel;
				if (changesInFile1 <= changesInFile2)
				{
					leftFile = file1;
					rightFile = file2;
					leftLabel = relativeFile1;
					rightLabel = relativeFile2;
				}
				else
				{
					leftFile = file2;
					rightFile = file1;
					leftLabel = relativeFile2;
					rightLabel = relativeFile1;
				}

				Rule rule = new($"[bold]{leftLabel} vs {rightLabel}[/]")
				{
					Style = Style.Parse("blue")
				};
				AnsiConsole.Write(rule);

				AnsiConsole.MarkupLine($"[dim]Comparing:[/]");
				AnsiConsole.MarkupLine($"[dim]  📁 {leftFile}[/]");
				AnsiConsole.MarkupLine($"[dim]  📁 {rightFile}[/]");
				AnsiConsole.WriteLine();

				if (diffFormat.Contains("📊"))
				{
					ShowChangeSummary(leftFile, rightFile);
				}
				else if (diffFormat.Contains("🔧"))
				{
					ShowGitStyleDiff(leftFile, rightFile);
				}
				else if (diffFormat.Contains("🎨"))
				{
					ShowColoredDiff(leftFile, rightFile);
				}

				AnsiConsole.WriteLine();
			}
		}
	}

	/// <summary>
	/// Gets the relative directory name from a file path for display purposes
	/// </summary>
	/// <param name="filePath">The full file path</param>
	/// <returns>The directory name relative to the parent directory</returns>
	private static string GetRelativeDirectoryName(string filePath)
	{
		string? directory = Path.GetDirectoryName(filePath);
		if (string.IsNullOrEmpty(directory))
		{
			return Path.GetFileName(filePath);
		}

		// Get the directory name (e.g., "Version1", "subfolder", etc.)
		string dirName = Path.GetFileName(directory);

		// If it's empty or just a drive letter, use the full path
		return string.IsNullOrEmpty(dirName) || dirName.Length <= 3 ? filePath : dirName;
	}

	/// <summary>
	/// Shows a change summary between two files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	private static void ShowChangeSummary(string file1, string file2)
	{
		System.Collections.ObjectModel.Collection<ColoredDiffLine> coloredSummary = FileDiffer.GenerateColoredChangeSummary(file1, file2);

		foreach (ColoredDiffLine line in coloredSummary)
		{
			string markup = line.Color switch
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
		string[] lines1 = File.ReadAllLines(file1);
		string[] lines2 = File.ReadAllLines(file2);
		System.Collections.ObjectModel.Collection<ColoredDiffLine> coloredDiff = FileDiffer.GenerateColoredDiff(file1, file2, lines1, lines2);

		Panel panel = new(RenderColoredDiff(coloredDiff))
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
			SideBySideDiffModel sideBySideDiff = DiffPlexDiffer.GenerateSideBySideDiff(file1, file2);

			// Use relative directory names for headers
			string header1 = GetRelativeDirectoryName(file1);
			string header2 = GetRelativeDirectoryName(file2);

			Table table = new Table()
				.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
				.AddColumn(new TableColumn($"[bold]{header1}[/]").Width(50))
				.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
				.AddColumn(new TableColumn($"[bold]{header2}[/]").Width(50))
				.Border(TableBorder.Rounded)
				.Expand();

			int maxLines = Math.Max(sideBySideDiff.OldText.Lines.Count, sideBySideDiff.NewText.Lines.Count);
			int displayedLines = 0;
			const int maxDisplayLines = 100; // Limit for TUI performance

			for (int i = 0; i < maxLines && displayedLines < maxDisplayLines; i++)
			{
				DiffPiece? oldLine = i < sideBySideDiff.OldText.Lines.Count ? sideBySideDiff.OldText.Lines[i] : null;
				DiffPiece? newLine = i < sideBySideDiff.NewText.Lines.Count ? sideBySideDiff.NewText.Lines[i] : null;

				string? lineNum1 = "";
				string content1 = "";
				string? lineNum2 = "";
				string content2 = "";

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
		string text = line.Text?.EscapeMarkup() ?? "";

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
		IEnumerable<string> lines = coloredDiff.Take(50).Select(line => line.Color switch
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
		string directory = HistoryInput.AskWithHistory("[cyan]Enter the directory path:[/]");

		if (!Directory.Exists(directory))
		{
			AnsiConsole.MarkupLine("[red]Error: Directory does not exist![/]");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter the filename to search for:[/]");

		// Prepare file groups for merging
		IReadOnlyCollection<FileGroup>? fileGroups = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(directory, fileName);

		if (fileGroups == null || fileGroups.Count < 2)
		{
			AnsiConsole.MarkupLine("[red]Error: Need at least 2 different versions to merge![/]");
			return;
		}

		AnsiConsole.MarkupLine($"[green]Found {fileGroups.Count} unique versions to merge.[/]");

		// Start the iterative merge process
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
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
			AnsiConsole.MarkupLine($"[cyan]💾 Final merged result is saved in the remaining file.[/]");
		}
		else
		{
			AnsiConsole.MarkupLine("[yellow]⚠️ Merge was cancelled or incomplete.[/]");
		}
	}

	/// <summary>
	/// Compares two directories interactively
	/// </summary>
	private static void CompareTwoDirectories()
	{
		string dir1 = HistoryInput.AskWithHistory("[cyan]Enter the first directory path:[/]");

		if (!Directory.Exists(dir1))
		{
			AnsiConsole.MarkupLine("[red]Error: First directory does not exist![/]");
			return;
		}

		string dir2 = HistoryInput.AskWithHistory("[cyan]Enter the second directory path:[/]");

		if (!Directory.Exists(dir2))
		{
			AnsiConsole.MarkupLine("[red]Error: Second directory does not exist![/]");
			return;
		}

		string pattern = HistoryInput.AskWithHistory("[cyan]Enter file pattern (e.g., *.txt, *.cs):[/]", "*.*");
		bool recursive = AnsiConsole.Confirm("[cyan]Search subdirectories recursively?[/]", false);

		CompareDirectories(dir1, dir2, pattern, recursive);
	}

	/// <summary>
	/// Compares two specific files
	/// </summary>
	private static void CompareTwoSpecificFiles()
	{
		string file1 = HistoryInput.AskWithHistory("[cyan]Enter the first file path:[/]");

		if (!File.Exists(file1))
		{
			AnsiConsole.MarkupLine("[red]Error: First file does not exist![/]");
			return;
		}

		string file2 = HistoryInput.AskWithHistory("[cyan]Enter the second file path:[/]");

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
		Panel panel = new("""
		[bold]BlastMerge - Cross-Repository File Synchronization Tool[/]

		[yellow]Primary Feature - Iterative File Synchronization:[/]
		• [cyan][bold]Unify multiple file versions across repositories and directories[/bold][/]
		• [cyan]Smart discovery and hash-based grouping of file versions[/]
		• [cyan]Optimal merge order based on similarity calculation[/]
		• [cyan]Interactive conflict resolution with visual TUI[/]
		• [cyan]Cross-repository updates - sync all locations with merged result[/]

		[yellow]Supporting Features:[/]
		• Compare files with the same name across directories
		• Compare two directories with file patterns and recursive search
		• Compare two specific files with multiple diff formats
		• View differences in git-style, change summary, or rich colored formats

		[yellow]Command Line Usage:[/]
		BlastMerge.ConsoleApp <directory> <filename>

		[yellow]Interactive Mode:[/]
		Run without arguments for the full interactive TUI interface.
		The primary workflow is "[cyan]🔀 Iterative Merge Multiple Versions[/]"

		[yellow]Real-World Use Cases:[/]
		• Sync configuration files across microservices
		• Merge scattered feature branch changes
		• Unify deployment scripts across environments
		• Consolidate similar files when merging codebases
		• Align documentation across related projects
		""")
		{
			Header = new PanelHeader("[bold blue]Help[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(panel);
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
		DirectoryComparisonResult? result = null;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start($"[yellow]Comparing directories...[/]", ctx =>
			{
				result = FileDiffer.FindDifferences(dir1, dir2, pattern, recursive);
				ctx.Status("[yellow]Creating comparison report...[/]");
			});

		// Display results (outside the status context)
		Table table = new Table()
			.AddColumn("[bold]Category[/]")
			.AddColumn("[bold]Count[/]")
			.AddColumn("[bold]Examples[/]")
			.Border(TableBorder.Rounded);

		table.AddRow(
			"[green]Identical Files[/]",
			$"[cyan]{result!.SameFiles.Count}[/]",
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

		// Show detailed differences for modified files if requested (outside status context)
		if (result.ModifiedFiles.Count > 0 &&
			AnsiConsole.Confirm("[cyan]Show detailed differences for modified files?[/]", false))
		{
			foreach (string? file in result.ModifiedFiles.Take(5)) // Limit to first 5
			{
				string file1 = Path.Combine(dir1, file);
				string file2 = Path.Combine(dir2, file);

				AnsiConsole.WriteLine();
				Rule rule = new($"[bold]Differences: {file}[/]")
				{
					Style = Style.Parse("blue")
				};
				AnsiConsole.Write(rule);

				ShowChangeSummary(file1, file2);
			}
		}
	}

	/// <summary>
	/// Compares two specific files
	/// </summary>
	/// <param name="file1">First file path</param>
	/// <param name="file2">Second file path</param>
	private static void CompareTwoFiles(string file1, string file2)
	{
		bool areSame = false;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.Start("[yellow]Comparing files...[/]", ctx =>
			{
				areSame = DiffPlexDiffer.AreFilesIdentical(file1, file2);
			});

		if (areSame)
		{
			AnsiConsole.MarkupLine("[green]✅ Files are identical![/]");
			return;
		}

		AnsiConsole.MarkupLine("[yellow]📄 Files are different.[/]");

		string diffFormat = AnsiConsole.Prompt(
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
		List<string> choices = [];
		for (int i = 0; i < fileGroups.Count; i++)
		{
			FileGroup group = fileGroups[i];
			choices.Add($"Version {i + 1} ({group.FilePaths.Count} files) - {group.FilePaths.First()}");
		}

		string selectedVersion = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Which version should be used as the source for syncing?[/]")
				.AddChoices(choices));

		int sourceIndex = choices.IndexOf(selectedVersion);
		FileGroup sourceGroup = fileGroups[sourceIndex];
		string sourceFile = sourceGroup.FilePaths.First();

		// Collect all target files (files that are different from the source)
		List<string> targetFiles = [];
		for (int i = 0; i < fileGroups.Count; i++)
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
		int successCount = 0;
		int failureCount = 0;

		AnsiConsole.Progress()
			.Start(ctx =>
			{
				ProgressTask task = ctx.AddTask("[green]Syncing files...[/]");
				task.MaxValue = targetFiles.Count;

				foreach (string targetFile in targetFiles)
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
		// Determine which file should be on the left (fewer changes)
		string leftFile, rightFile;
		if (existingMergedContent == null)
		{
			// For new merges, count changes to determine order
			IReadOnlyCollection<LineDifference> differences = FileDiffer.FindDifferences(file1, file2);
			int changesInFile1 = differences.Count(d => d.LineNumber1.HasValue);
			int changesInFile2 = differences.Count(d => d.LineNumber2.HasValue);

			if (changesInFile1 <= changesInFile2)
			{
				leftFile = file1;
				rightFile = file2;
			}
			else
			{
				leftFile = file2;
				rightFile = file1;
			}
		}
		else
		{
			// For merges with existing content, keep original order
			leftFile = file1;
			rightFile = file2;
		}

		string leftLabel = GetRelativeDirectoryName(leftFile);
		string rightLabel = GetRelativeDirectoryName(rightFile);

		AnsiConsole.MarkupLine($"[yellow]🔀 Merging:[/]");
		if (existingMergedContent != null)
		{
			AnsiConsole.MarkupLine($"[dim]  📋 <existing merged content> → {leftLabel}[/]");
		}
		else
		{
			AnsiConsole.MarkupLine($"[dim]  📁 {leftLabel}[/]");
		}
		AnsiConsole.MarkupLine($"[dim]  📁 {rightLabel}[/]");
		AnsiConsole.MarkupLine($"[green]  ➡️  Result will replace both files[/]");
		AnsiConsole.WriteLine();

		MergeResult? result = IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			leftFile, rightFile, existingMergedContent, (block, context, blockNumber) => GetBlockChoice(block, context, blockNumber, leftFile, rightFile));

		if (result != null)
		{
			AnsiConsole.MarkupLine($"[green]✅ Merged successfully! Versions reduced by 1.[/]");
			AnsiConsole.WriteLine();
		}

		return result;
	}

	/// <summary>
	/// Reports merge status to the user
	/// </summary>
	/// <param name="status">The merge status to report</param>
	private static void ReportMergeStatus(MergeSessionStatus status)
	{
		Rule rule = new($"[bold yellow]Merge Step {status.CurrentIteration}[/]")
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
	private static bool ConfirmContinuation()
	{
		AnsiConsole.MarkupLine("[cyan]Continuing to next merge step...[/]");
		return true;
	}

	/// <summary>
	/// Gets the user's choice for a merge block
	/// </summary>
	/// <param name="block">The diff block to choose for</param>
	/// <param name="context">The context around the block</param>
	/// <param name="blockNumber">The block number being processed</param>
	/// <param name="leftFile">The left file path</param>
	/// <param name="rightFile">The right file path</param>
	/// <returns>The user's choice for the block</returns>
	private static BlockChoice GetBlockChoice(DiffBlock block, BlockContext context, int blockNumber, string leftFile, string rightFile)
	{
		AnsiConsole.MarkupLine($"[yellow]🔍 Block {blockNumber} ({block.Type})[/]");

		// Show the block content with context
		ShowBlockWithContext(block, context, leftFile, rightFile);

		// Get user's choice based on block type
		return block.Type switch
		{
			BlockType.Insert => GetInsertChoice(rightFile),
			BlockType.Delete => GetDeleteChoice(leftFile),
			BlockType.Replace => GetReplaceChoice(leftFile, rightFile),
			_ => throw new InvalidOperationException($"Unknown block type: {block.Type}")
		};
	}

	/// <summary>
	/// Shows a block with its context in a side-by-side diff format
	/// </summary>
	/// <param name="block">The diff block to show</param>
	/// <param name="context">The context around the block</param>
	/// <param name="leftFile">The left file path</param>
	/// <param name="rightFile">The right file path</param>
	private static void ShowBlockWithContext(DiffBlock block, BlockContext context, string leftFile, string rightFile)
	{
		AnsiConsole.MarkupLine($"[yellow]Block Type: {block.Type}[/]");
		AnsiConsole.WriteLine();

		string leftLabel = GetRelativeDirectoryName(leftFile);
		string rightLabel = GetRelativeDirectoryName(rightFile);

		// Create a side-by-side diff visualization
		Table table = new Table()
			.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
			.AddColumn(new TableColumn($"[bold]{leftLabel}[/]").Width(50))
			.AddColumn(new TableColumn("[bold]Line[/]").Width(6))
			.AddColumn(new TableColumn($"[bold]{rightLabel}[/]").Width(50))
			.Border(TableBorder.Rounded)
			.Expand();

		// Calculate correct starting line numbers for context before the block
		int firstLine1 = block.LineNumbers1.Count > 0 ? block.LineNumbers1.Min() : 1;
		int firstLine2 = block.LineNumbers2.Count > 0 ? block.LineNumbers2.Min() : 1;

		int contextStartLine1 = Math.Max(1, firstLine1 - context.ContextBefore1.Count);
		int contextStartLine2 = Math.Max(1, firstLine2 - context.ContextBefore2.Count);

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
		int maxLines = Math.Max(lines1.Length, lines2.Length);

		for (int i = 0; i < maxLines; i++)
		{
			string line1 = i < lines1.Length ? lines1[i] : "";
			string line2 = i < lines2.Length ? lines2[i] : "";

			string lineNum1 = i < lines1.Length ? (startLine1 + i).ToString() : "";
			string lineNum2 = i < lines2.Length ? (startLine2 + i).ToString() : "";

			// Context lines are shown with dim styling
			string content1 = string.IsNullOrEmpty(line1) ? "" : $"[dim]{line1.EscapeMarkup()}[/]";
			string content2 = string.IsNullOrEmpty(line2) ? "" : $"[dim]{line2.EscapeMarkup()}[/]";

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
		for (int i = 0; i < block.Lines2.Count; i++)
		{
			string lineNum2 = block.LineNumbers2.Count > i ? block.LineNumbers2[i].ToString() : "";
			string content2 = $"[green on darkgreen]+ {block.Lines2[i].EscapeMarkup()}[/]";

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
		for (int i = 0; i < block.Lines1.Count; i++)
		{
			string lineNum1 = block.LineNumbers1.Count > i ? block.LineNumbers1[i].ToString() : "";
			string content1 = $"[red on darkred]- {block.Lines1[i].EscapeMarkup()}[/]";

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
		int maxLines = Math.Max(block.Lines1.Count, block.Lines2.Count);

		for (int i = 0; i < maxLines; i++)
		{
			string lineNum1 = i < block.LineNumbers1.Count ? block.LineNumbers1[i].ToString() : "";
			string lineNum2 = i < block.LineNumbers2.Count ? block.LineNumbers2[i].ToString() : "";

			string content1 = i < block.Lines1.Count
				? $"[red on darkred]- {block.Lines1[i].EscapeMarkup()}[/]"
				: "";
			string content2 = i < block.Lines2.Count
				? $"[green on darkgreen]+ {block.Lines2[i].EscapeMarkup()}[/]"
				: "";

			table.AddRow(lineNum1, content1, lineNum2, content2);
		}
	}

	/// <summary>
	/// Gets user choice for insert blocks
	/// </summary>
	/// <param name="rightFile">The right file path</param>
	/// <returns>User's choice</returns>
	private static BlockChoice GetInsertChoice(string rightFile)
	{
		string rightLabel = GetRelativeDirectoryName(rightFile);
		string choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title($"[cyan]This content exists only in {rightLabel}. What would you like to do?[/]")
				.AddChoices([
					"✅ Include the addition",
					"❌ Skip the addition"
				]));

		return choice.Contains("Include") ? BlockChoice.Include : BlockChoice.Skip;
	}

	/// <summary>
	/// Gets user choice for delete blocks
	/// </summary>
	/// <param name="leftFile">The left file path</param>
	/// <returns>User's choice</returns>
	private static BlockChoice GetDeleteChoice(string leftFile)
	{
		string leftLabel = GetRelativeDirectoryName(leftFile);
		string choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title($"[cyan]This content exists only in {leftLabel}. What would you like to do?[/]")
				.AddChoices([
					"✅ Keep the content",
					"❌ Remove the content"
				]));

		return choice.Contains("Keep") ? BlockChoice.Keep : BlockChoice.Remove;
	}

	/// <summary>
	/// Gets user choice for replace blocks
	/// </summary>
	/// <param name="leftFile">The left file path</param>
	/// <param name="rightFile">The right file path</param>
	/// <returns>User's choice</returns>
	private static BlockChoice GetReplaceChoice(string leftFile, string rightFile)
	{
		string leftLabel = GetRelativeDirectoryName(leftFile);
		string rightLabel = GetRelativeDirectoryName(rightFile);
		string choice = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]This content differs between versions. What would you like to do?[/]")
				.AddChoices([
					$"1️⃣ Use {leftLabel}",
					$"2️⃣ Use {rightLabel}",
					"🔄 Use Both Versions",
					"❌ Skip Both"
				]));

		return choice switch
		{
			var s when s.Contains($"Use {leftLabel}") => BlockChoice.UseVersion1,
			var s when s.Contains($"Use {rightLabel}") => BlockChoice.UseVersion2,
			var s when s.Contains("Both") => BlockChoice.UseBoth,
			_ => BlockChoice.Skip
		};
	}

	/// <summary>
	/// Shows the application version
	/// </summary>
	private static void ShowVersion()
	{
		try
		{
			// Try to read version from VERSION.md file
			string versionFile = Path.Combine(AppContext.BaseDirectory, "VERSION.md");
			if (File.Exists(versionFile))
			{
				string version = File.ReadAllText(versionFile).Trim();
				Console.WriteLine(version);
				return;
			}

			// Fallback to assembly version
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			string version2 = assembly.GetName().Version?.ToString() ?? "1.0.0";
			Console.WriteLine(version2);
		}
		catch (IOException)
		{
			// Fallback version if file reading fails
			Console.WriteLine("1.0.14");
		}
		catch (UnauthorizedAccessException)
		{
			// Fallback version if access denied
			Console.WriteLine("1.0.14");
		}
		catch (System.Security.SecurityException)
		{
			// Fallback version if security exception
			Console.WriteLine("1.0.14");
		}
	}

	/// <summary>
	/// Shows command line help
	/// </summary>
	private static void ShowCommandLineHelp()
	{
		Console.WriteLine("BlastMerge - Cross-Repository File Synchronization");
		Console.WriteLine();
		Console.WriteLine("USAGE:");
		Console.WriteLine("  BlastMerge.exe [options] [directory] [filename]");
		Console.WriteLine();
		Console.WriteLine("OPTIONS:");
		Console.WriteLine("  -v, --version         Display version information and exit");
		Console.WriteLine("  -h, --help            Display this help screen and exit");
		Console.WriteLine();
		Console.WriteLine("ARGUMENTS:");
		Console.WriteLine("  directory             Directory path to search for files");
		Console.WriteLine("  filename              Filename to search for in the directory");
		Console.WriteLine();
		Console.WriteLine("EXAMPLES:");
		Console.WriteLine("  BlastMerge.exe                           Start interactive mode");
		Console.WriteLine("  BlastMerge.exe C:\\Projects README.md      Process files directly");
		Console.WriteLine("  BlastMerge.exe --version                 Show version");
		Console.WriteLine("  BlastMerge.exe --help                    Show this help");
		Console.WriteLine();
		Console.WriteLine("INTERACTIVE MODE FEATURES:");
		Console.WriteLine("  🔀 Iterative Merge Multiple Versions (PRIMARY FEATURE)");
		Console.WriteLine("  🔍 Compare Files in Directory");
		Console.WriteLine("  📁 Compare Two Directories");
		Console.WriteLine("  📄 Compare Two Specific Files");
	}
}
