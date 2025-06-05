// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiffPlex.DiffBuilder.Model;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Console-specific implementation of the application service that adds UI functionality.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
public class ConsoleApplicationService : ApplicationService
{
	private const string PressAnyKeyMessage = "Press any key to continue...";

	// Table column names
	private const string GroupColumnName = "Group";
	private const string FilesColumnName = "Files";
	private const string StatusColumnName = "Status";
	private const string FilePathColumnName = "File Path";
	private const string SizeColumnName = "Size";
	private const string NameColumnName = "Name";
	private const string DescriptionColumnName = "Description";
	private const string PatternsColumnName = "Patterns";
	private const string KeyColumnName = "Key";
	private const string ActionColumnName = "Action";

	// Menu display text to command mappings
	private static readonly Dictionary<string, MenuChoice> MainMenuChoices = new()
	{
		["🔍 Find Files"] = MenuChoice.FindFiles,
		["📊 Compare Files"] = MenuChoice.CompareFiles,
		["🔄 Iterative Merge"] = MenuChoice.IterativeMerge,
		["📦 Batch Operations"] = MenuChoice.BatchOperations,
		["⚙️ Configuration & Settings"] = MenuChoice.Settings,
		["❓ Help & Information"] = MenuChoice.Help,
		["🚪 Exit"] = MenuChoice.Exit
	};

	// Menu display text to command mappings for compare operations
	private static readonly Dictionary<string, CompareChoice> CompareChoices = new()
	{
		["🔍 Compare Files in Directory"] = CompareChoice.CompareFilesInDirectory,
		["📁 Compare Two Directories"] = CompareChoice.CompareTwoDirectories,
		["📄 Compare Two Specific Files"] = CompareChoice.CompareTwoSpecificFiles,
		["🔙 Back to Main Menu"] = CompareChoice.BackToMainMenu
	};

	/// <summary>
	/// Processes files in a directory with a specified filename pattern.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public override void ProcessFiles(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		AnsiConsole.MarkupLine($"[cyan]Processing files matching pattern '[yellow]{fileName}[/]' in '[yellow]{directory}[/]'[/]");
		AnsiConsole.WriteLine();

		IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName);

		if (filePaths.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No files found matching the pattern.[/]");
			return;
		}

		// Group files by hash to identify duplicates and candidates for merging
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = CompareFiles(directory, fileName);

		if (fileGroups.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No files to compare.[/]");
			return;
		}

		AnsiConsole.MarkupLine($"[green]Found {filePaths.Count} files in {fileGroups.Count} groups:[/]");
		AnsiConsole.WriteLine();

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Blue)
			.AddColumn(GroupColumnName)
			.AddColumn(FilesColumnName)
			.AddColumn(StatusColumnName);

		foreach ((KeyValuePair<string, IReadOnlyCollection<string>> group, int groupIndex) in fileGroups.Select((group, index) => (group, index + 1)))
		{
			string status = group.Value.Count > 1 ? "[yellow]Multiple versions[/]" : "[green]Unique[/]";
			table.AddRow(
				$"[cyan]{groupIndex}[/]",
				$"[dim]{group.Value.Count}[/]",
				status);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();

		// Ask user what to do with the files
		Dictionary<string, ProcessFileActionChoice> processFileActionChoices = new()
		{
			["View detailed file list"] = ProcessFileActionChoice.ViewDetailedFileList,
			["Show differences between versions"] = ProcessFileActionChoice.ShowDifferences,
			["Run iterative merge on duplicates"] = ProcessFileActionChoice.RunIterativeMergeOnDuplicates,
			["Sync files to make them identical"] = ProcessFileActionChoice.SyncFiles,
			["Return to main menu"] = ProcessFileActionChoice.ReturnToMainMenu
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("What would you like to do with these files?")
				.AddChoices(processFileActionChoices.Keys));

		if (processFileActionChoices.TryGetValue(selection, out ProcessFileActionChoice choice))
		{
			switch (choice)
			{
				case ProcessFileActionChoice.ViewDetailedFileList:
					ShowDetailedFileList(fileGroups);
					break;
				case ProcessFileActionChoice.ShowDifferences:
					ShowDifferences(fileGroups);
					break;
				case ProcessFileActionChoice.RunIterativeMergeOnDuplicates:
					RunIterativeMerge(directory, fileName);
					break;
				case ProcessFileActionChoice.SyncFiles:
					OfferSyncOptions(fileGroups);
					break;
				case ProcessFileActionChoice.ReturnToMainMenu:
					// Default action - do nothing
					break;
				default:
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Shows differences between file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to show differences for.</param>
	private void ShowDifferences(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		// Convert to FileGroup objects for easier handling
		List<FileGroup> groups = [.. fileGroups.Select(g => new FileGroup(g.Value))];

		// Filter to groups with multiple files
		List<FileGroup> groupsWithMultipleFiles = [.. groups.Where(g => g.FilePaths.Count > 1)];

		if (groupsWithMultipleFiles.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No groups with multiple files to compare.[/]");
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
				string file1 = group.FilePaths.First();
				string file2 = group.FilePaths.Skip(1).First();

				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine($"[yellow]Comparing first two files:[/]");

				string diffFormat = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title("[cyan]Choose diff format:[/]")
						.AddChoices([
							"📊 Change Summary (Added/Removed lines only)",
							"🔧 Git-style Diff (Full context)",
							"🎨 Side-by-Side Diff (Rich formatting)",
							"⏭️ Skip this group"
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
					ShowSideBySideDiff(file1, file2);
				}
			}
		}
	}

	/// <summary>
	/// Offers sync options for file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to offer sync options for.</param>
	private void OfferSyncOptions(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		// Convert to FileGroup objects for easier handling
		List<FileGroup> groups = [.. fileGroups.Select(g => new FileGroup(g.Value))];

		// Filter to groups with multiple files
		List<FileGroup> groupsWithMultipleFiles = [.. groups.Where(g => g.FilePaths.Count > 1)];

		if (groupsWithMultipleFiles.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]All files are already unique (no duplicates to sync).[/]");
			return;
		}

		if (!AnsiConsole.Confirm("[cyan]Would you like to sync files to make them identical?[/]", false))
		{
			return;
		}

		// Let user choose which version to use as the source
		List<string> choices = [];
		for (int i = 0; i < groupsWithMultipleFiles.Count; i++)
		{
			FileGroup group = groupsWithMultipleFiles[i];
			choices.Add($"Version {i + 1} ({group.FilePaths.Count} files) - {group.FilePaths.First()}");
		}

		string selectedVersion = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Which version should be used as the source for syncing?[/]")
				.AddChoices(choices));

		int sourceIndex = choices.IndexOf(selectedVersion);
		FileGroup sourceGroup = groupsWithMultipleFiles[sourceIndex];
		string sourceFile = sourceGroup.FilePaths.First();

		// Collect all target files (files that are different from the source)
		List<string> targetFiles = [];
		for (int i = 0; i < groupsWithMultipleFiles.Count; i++)
		{
			if (i != sourceIndex)
			{
				targetFiles.AddRange(groupsWithMultipleFiles[i].FilePaths);
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
	/// Shows a change summary between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	private static void ShowChangeSummary(string file1, string file2)
	{
		IReadOnlyCollection<LineDifference> differences = FileDiffer.FindDifferences(file1, file2);

		if (differences.Count == 0)
		{
			AnsiConsole.MarkupLine("[green]Files are identical![/]");
			return;
		}

		Panel panel = new(
			$"[yellow]Found {differences.Count} differences[/]\n" +
			$"[green]+ Additions: {differences.Count(d => d.Type == LineDifferenceType.Added)}[/]\n" +
			$"[red]- Deletions: {differences.Count(d => d.Type == LineDifferenceType.Deleted)}[/]\n" +
			$"[yellow]~ Modifications: {differences.Count(d => d.Type == LineDifferenceType.Modified)}[/]")
		{
			Header = new PanelHeader("[bold]Change Summary[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(panel);
	}

	/// <summary>
	/// Shows a git-style diff between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
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
	/// Shows a side-by-side diff between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	private static void ShowSideBySideDiff(string file1, string file2)
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
	/// Formats a diff line with appropriate color and prefix.
	/// </summary>
	/// <param name="line">The diff line to format.</param>
	/// <returns>Formatted line with color markup.</returns>
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
	/// Renders colored diff for display in panels.
	/// </summary>
	/// <param name="coloredDiff">The colored diff lines.</param>
	/// <returns>Rendered markup string.</returns>
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
	/// Gets the relative directory name from a file path.
	/// </summary>
	/// <param name="filePath">The file path.</param>
	/// <returns>The relative directory name.</returns>
	private static string GetRelativeDirectoryName(string filePath)
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
	/// Processes a batch configuration in a specified directory.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batchName">The name of the batch configuration.</param>
	public override void ProcessBatch(string directory, string batchName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(batchName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		AnsiConsole.MarkupLine($"[cyan]Processing batch configuration '[yellow]{batchName}[/]' in '[yellow]{directory}[/]'[/]");
		AnsiConsole.WriteLine();

		// Get the batch configuration
		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();
		BatchConfiguration? batch = allBatches.FirstOrDefault(b => b.Name.Equals(batchName, StringComparison.OrdinalIgnoreCase));

		if (batch == null)
		{
			AnsiConsole.MarkupLine($"[red]Batch configuration '[yellow]{batchName}[/]' not found.[/]");
			AnsiConsole.MarkupLine("[dim]Use the 'List Batches' option to see available configurations.[/]");
			return;
		}

		AnsiConsole.MarkupLine($"[green]Found batch configuration: {batch.Name}[/]");
		if (!string.IsNullOrEmpty(batch.Description))
		{
			AnsiConsole.MarkupLine($"[dim]{batch.Description}[/]");
		}
		AnsiConsole.WriteLine();

		// Process each file pattern in the batch
		int totalPatternsProcessed = 0;
		int totalFilesFound = 0;

		foreach (string pattern in batch.FilePatterns)
		{
			AnsiConsole.MarkupLine($"[cyan]Processing pattern: [yellow]{pattern}[/][/]");

			IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, pattern);

			if (filePaths.Count == 0)
			{
				if (!batch.SkipEmptyPatterns)
				{
					AnsiConsole.MarkupLine($"[yellow]No files found for pattern '{pattern}'[/]");
				}
				continue;
			}

			totalPatternsProcessed++;
			totalFilesFound += filePaths.Count;

			AnsiConsole.MarkupLine($"[green]Found {filePaths.Count} files matching '{pattern}'[/]");

			if (batch.PromptBeforeEachPattern)
			{
				bool shouldProcess = AnsiConsole.Confirm($"Process files for pattern '{pattern}'?");
				if (!shouldProcess)
				{
					AnsiConsole.MarkupLine("[yellow]Skipping pattern.[/]");
					continue;
				}
			}

			// Group files to identify merge candidates
			IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = CompareFiles(directory, pattern);
			int groupsWithMultipleFiles = fileGroups.Count(g => g.Value.Count > 1);

			if (groupsWithMultipleFiles > 0)
			{
				AnsiConsole.MarkupLine($"[yellow]Found {groupsWithMultipleFiles} groups with multiple versions that could be merged.[/]");

				Dictionary<string, BatchActionChoice> batchActionChoices = new()
				{
					["Run iterative merge"] = BatchActionChoice.RunIterativeMerge,
					["Skip this pattern"] = BatchActionChoice.SkipPattern,
					["Stop batch processing"] = BatchActionChoice.StopBatchProcessing
				};

				string selection = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title($"Multiple versions found for pattern '{pattern}'. What would you like to do?")
						.AddChoices(batchActionChoices.Keys));

				if (batchActionChoices.TryGetValue(selection, out BatchActionChoice choice))
				{
					switch (choice)
					{
						case BatchActionChoice.RunIterativeMerge:
							RunIterativeMerge(directory, pattern);
							break;
						case BatchActionChoice.SkipPattern:
							AnsiConsole.MarkupLine("[yellow]Skipping pattern.[/]");
							continue; // Skip to next pattern
						case BatchActionChoice.StopBatchProcessing:
							AnsiConsole.MarkupLine("[yellow]Stopping batch processing.[/]");
							return; // Stop batch processing
						default:
							AnsiConsole.MarkupLine("[yellow]Unknown choice, skipping pattern.[/]");
							continue;
					}
				}
			}
			else
			{
				AnsiConsole.MarkupLine($"[green]All files for pattern '{pattern}' are unique (no duplicates found).[/]");
			}

			AnsiConsole.WriteLine();
		}

		AnsiConsole.MarkupLine($"[green]Batch processing completed![/]");
		AnsiConsole.MarkupLine($"[dim]Processed {totalPatternsProcessed} patterns, found {totalFilesFound} total files.[/]");
	}

	/// <summary>
	/// Compares files in a directory and returns file groups.
	/// </summary>
	/// <param name="directory">The directory containing files to compare.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <returns>Dictionary of file groups organized by hash.</returns>
	public override IReadOnlyDictionary<string, IReadOnlyCollection<string>> CompareFiles(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName);
		IReadOnlyCollection<FileGroup> fileGroups = FileDiffer.GroupFilesByHash(filePaths);

		// Convert FileGroup collection to Dictionary<string, IReadOnlyCollection<string>>
		Dictionary<string, IReadOnlyCollection<string>> result = [];
		foreach (FileGroup group in fileGroups)
		{
			result[group.Hash] = group.FilePaths;
		}

		return result.AsReadOnly();
	}

	/// <summary>
	/// Runs the iterative merge process on files in a directory.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public override void RunIterativeMerge(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		// Prepare file groups for merging
		IReadOnlyCollection<FileGroup>? fileGroups = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(directory, fileName);

		if (fileGroups == null)
		{
			Console.WriteLine("No files found or insufficient unique versions to merge.");
			return;
		}

		// Start iterative merge process
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			PerformMergeCallback,
			ReportMergeStatus,
			ContinueMergeCallback);

		// Handle result
		if (result.IsSuccessful)
		{
			Console.WriteLine($"Merge completed successfully. Final file: {result.OriginalFileName}");
		}
		else
		{
			Console.WriteLine($"Merge failed or was cancelled: {result.OriginalFileName}");
		}
	}

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	public override void ListBatches()
	{
		IReadOnlyCollection<string> batchNames = BatchManager.ListBatches();
		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();

		Console.WriteLine("Available batch configurations:");

		if (batchNames.Count == 0)
		{
			Console.WriteLine("  No batch configurations found.");
			Console.WriteLine("  Default configurations can be created automatically.");
			return;
		}

		foreach (BatchConfiguration batch in allBatches)
		{
			Console.WriteLine($"  - {batch.Name}");
			if (!string.IsNullOrEmpty(batch.Description))
			{
				Console.WriteLine($"    {batch.Description}");
			}
			Console.WriteLine($"    Patterns: {batch.FilePatterns.Count}");
		}
	}

	/// <summary>
	/// Starts the interactive mode with a comprehensive TUI menu system.
	/// </summary>
	public override void StartInteractiveMode()
	{
		ShowWelcomeScreen();

		while (true)
		{
			try
			{
				MenuChoice choice = ShowMainMenu();

				if (choice == MenuChoice.Exit)
				{
					break;
				}

				ExecuteMenuChoice(choice);
			}
			catch (DirectoryNotFoundException ex)
			{
				AnsiConsole.MarkupLine($"[red]Directory not found: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
			}
			catch (UnauthorizedAccessException ex)
			{
				AnsiConsole.MarkupLine($"[red]Access denied: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
			}
			catch (IOException ex)
			{
				AnsiConsole.MarkupLine($"[red]File I/O error: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
			}
			catch (ArgumentException ex)
			{
				AnsiConsole.MarkupLine($"[red]Invalid input: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
			}
			catch (InvalidOperationException ex)
			{
				AnsiConsole.MarkupLine($"[red]Operation error: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
			}
		}

		ShowGoodbyeScreen();
	}

	/// <summary>
	/// Shows the welcome screen with application information.
	/// </summary>
	private void ShowWelcomeScreen()
	{
		AnsiConsole.Clear();

		FigletText figlet = new FigletText("BlastMerge")
			.LeftJustified()
			.Color(Color.Cyan1);

		AnsiConsole.Write(figlet);

		Panel panel = new Panel(
			new Markup("[bold]Cross-Repository File Synchronization Tool[/]\n\n" +
					  "[dim]Efficiently merge and synchronize files across multiple repositories[/]\n" +
					  "[dim]Navigate using arrow keys, press Enter to select[/]"))
			.Header("Welcome")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Blue);

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows the main menu and returns the user's choice.
	/// </summary>
	/// <returns>The selected menu choice.</returns>
	private MenuChoice ShowMainMenu()
	{
		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[bold cyan]Main Menu[/] - Select an option:")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
				.AddChoices(MainMenuChoices.Keys));

		return MainMenuChoices.TryGetValue(selection, out MenuChoice command) ? command : MenuChoice.Help;
	}

	/// <summary>
	/// Executes the selected menu choice.
	/// </summary>
	/// <param name="choice">The menu choice to execute.</param>
	private void ExecuteMenuChoice(MenuChoice choice)
	{
		Action menuAction = choice switch
		{
			MenuChoice.FindFiles => HandleFindFiles,
			MenuChoice.IterativeMerge => HandleIterativeMerge,
			MenuChoice.CompareFiles => HandleCompareFiles,
			MenuChoice.BatchOperations => HandleBatchOperations,
			MenuChoice.Settings => HandleSettings,
			MenuChoice.Help => HandleHelp,
			MenuChoice.Exit => HandleHelp, // This should never be called since Exit is handled in the main loop
			_ => HandleHelp
		};
		menuAction();
	}

	/// <summary>
	/// Handles the find files operation.
	/// </summary>
	private void HandleFindFiles()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Find & Process Files[/]");
		AnsiConsole.WriteLine();

		string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		AnsiConsole.Status()
			.Start("Finding files...", ctx =>
			{
				IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName);

				ctx.Refresh();

				if (filePaths.Count == 0)
				{
					AnsiConsole.MarkupLine("[yellow]No files found matching the pattern.[/]");
					return;
				}

				Table table = new Table()
					.Border(TableBorder.Rounded)
					.BorderColor(Color.Blue)
					.AddColumn(FilePathColumnName)
					.AddColumn(SizeColumnName);

				foreach (string filePath in filePaths)
				{
					FileInfo fileInfo = new(filePath);
					table.AddRow(
						$"[green]{filePath}[/]",
						$"[dim]{fileInfo.Length:N0} bytes[/]");
				}

				AnsiConsole.Write(table);
				AnsiConsole.MarkupLine($"\n[green]Found {filePaths.Count} files.[/]");
			});

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Handles the iterative merge operation.
	/// </summary>
	private void HandleIterativeMerge()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Run Iterative Merge[/]");
		AnsiConsole.WriteLine();

		string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		RunIterativeMergeWithConsoleOutput(directory, fileName);

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Handles the compare files operation.
	/// </summary>
	private void HandleCompareFiles()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Compare Files[/]");
		AnsiConsole.WriteLine();

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Select comparison type:[/]")
				.AddChoices(CompareChoices.Keys));

		if (CompareChoices.TryGetValue(selection, out CompareChoice choice))
		{
			switch (choice)
			{
				case CompareChoice.CompareFilesInDirectory:
					HandleCompareFilesInDirectory();
					break;
				case CompareChoice.CompareTwoDirectories:
					HandleCompareTwoDirectories();
					break;
				case CompareChoice.CompareTwoSpecificFiles:
					HandleCompareTwoSpecificFiles();
					break;
				case CompareChoice.BackToMainMenu:
					// Return to main menu
					break;
				default:
					// Unknown choice - return to main menu
					break;
			}
		}
	}

	/// <summary>
	/// Handles comparing files in a directory.
	/// </summary>
	private void HandleCompareFilesInDirectory()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Compare Files in Directory[/]");
		AnsiConsole.WriteLine();

		string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		ProcessFiles(directory, fileName);
	}

	/// <summary>
	/// Handles comparing two directories.
	/// </summary>
	private void HandleCompareTwoDirectories()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Compare Two Directories[/]");
		AnsiConsole.WriteLine();

		string dir1 = HistoryInput.AskWithHistory("[cyan]Enter the first directory path:[/]");
		if (string.IsNullOrWhiteSpace(dir1))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		if (!Directory.Exists(dir1))
		{
			AnsiConsole.MarkupLine("[red]Error: First directory does not exist![/]");
			return;
		}

		string dir2 = HistoryInput.AskWithHistory("[cyan]Enter the second directory path:[/]");
		if (string.IsNullOrWhiteSpace(dir2))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		if (!Directory.Exists(dir2))
		{
			AnsiConsole.MarkupLine("[red]Error: Second directory does not exist![/]");
			return;
		}

		string pattern = HistoryInput.AskWithHistory("[cyan]Enter file pattern (e.g., *.txt, *.cs):[/]", "*.*");
		bool recursive = AnsiConsole.Confirm("[cyan]Search subdirectories recursively?[/]", false);

		CompareDirectories(dir1, dir2, pattern, recursive);

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Handles comparing two specific files.
	/// </summary>
	private void HandleCompareTwoSpecificFiles()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Compare Two Specific Files[/]");
		AnsiConsole.WriteLine();

		string file1 = HistoryInput.AskWithHistory("[cyan]Enter the first file path:[/]");
		if (string.IsNullOrWhiteSpace(file1))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		if (!File.Exists(file1))
		{
			AnsiConsole.MarkupLine("[red]Error: First file does not exist![/]");
			return;
		}

		string file2 = HistoryInput.AskWithHistory("[cyan]Enter the second file path:[/]");
		if (string.IsNullOrWhiteSpace(file2))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		if (!File.Exists(file2))
		{
			AnsiConsole.MarkupLine("[red]Error: Second file does not exist![/]");
			return;
		}

		CompareTwoFiles(file1, file2);

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Compares two directories.
	/// </summary>
	/// <param name="dir1">First directory path.</param>
	/// <param name="dir2">Second directory path.</param>
	/// <param name="pattern">File search pattern.</param>
	/// <param name="recursive">Whether to search recursively.</param>
	private void CompareDirectories(string dir1, string dir2, string pattern, bool recursive)
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
	/// Compares two specific files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	private void CompareTwoFiles(string file1, string file2)
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
					"🎨 Side-by-Side Diff (Rich formatting)"
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
			ShowSideBySideDiff(file1, file2);
		}
	}

	/// <summary>
	/// Handles batch operations.
	/// </summary>
	private void HandleBatchOperations()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Batch Operations[/]");
		AnsiConsole.WriteLine();

		Dictionary<string, BatchOperationChoice> batchOperationChoices = new()
		{
			["📋 List Available Batches"] = BatchOperationChoice.ListAvailableBatches,
			["▶️ Run Batch Configuration"] = BatchOperationChoice.RunBatchConfiguration,
			["⬅️ Back to Main Menu"] = BatchOperationChoice.BackToMainMenu
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select batch operation:")
				.AddChoices(batchOperationChoices.Keys));

		if (batchOperationChoices.TryGetValue(selection, out BatchOperationChoice choice))
		{
			switch (choice)
			{
				case BatchOperationChoice.ListAvailableBatches:
					HandleListBatches();
					break;
				case BatchOperationChoice.RunBatchConfiguration:
					HandleRunBatch();
					break;
				case BatchOperationChoice.BackToMainMenu:
					// Default action - do nothing
					break;
				default:
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Handles listing available batch configurations.
	/// </summary>
	private void HandleListBatches()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Available Batch Configurations[/]");
		AnsiConsole.WriteLine();

		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();

		if (allBatches.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No batch configurations found.[/]");
			AnsiConsole.MarkupLine("[dim]Default configurations can be created automatically.[/]");
		}
		else
		{
			Table table = new Table()
				.Border(TableBorder.Rounded)
				.BorderColor(Color.Blue)
				.AddColumn(NameColumnName)
				.AddColumn(DescriptionColumnName)
				.AddColumn(PatternsColumnName);

			foreach (BatchConfiguration batch in allBatches)
			{
				table.AddRow(
					$"[green]{batch.Name}[/]",
					$"[dim]{batch.Description ?? "No description"}[/]",
					$"[yellow]{batch.FilePatterns.Count}[/]");
			}

			AnsiConsole.Write(table);
		}

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Handles running a batch configuration.
	/// </summary>
	private void HandleRunBatch()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Run Batch Configuration[/]");
		AnsiConsole.WriteLine();

		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();

		if (allBatches.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No batch configurations available.[/]");
			AnsiConsole.WriteLine(PressAnyKeyMessage);
			Console.ReadKey();
			return;
		}

		string batchName = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select batch configuration:")
				.AddChoices(allBatches.Select(b => b.Name)));

		string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		AnsiConsole.Status()
			.Start($"Running batch '{batchName}'...", ctx =>
			{
				ProcessBatch(directory, batchName);
				ctx.Refresh();
				AnsiConsole.MarkupLine("[green]Batch operation completed.[/]");
			});

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Handles configuration and settings.
	/// </summary>
	private void HandleSettings()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Configuration & Settings[/]");
		AnsiConsole.WriteLine();

		Panel panel = new Panel(
			new Markup("[yellow]Settings functionality will be implemented in a future version.[/]\n\n" +
					  "Planned features:\n" +
					  "• Configure default directories\n" +
					  "• Set merge preferences\n" +
					  "• Manage batch configurations\n" +
					  "• Export/import settings"))
			.Header("Coming Soon")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Yellow);

		AnsiConsole.Write(panel);

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Handles help and information display.
	/// </summary>
	private void HandleHelp()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Help & Information[/]");
		AnsiConsole.WriteLine();

		Dictionary<string, HelpMenuChoice> helpMenuChoices = new()
		{
			["📖 Application Overview"] = HelpMenuChoice.ApplicationOverview,
			["🎯 Feature Guide"] = HelpMenuChoice.FeatureGuide,
			["⌨️ Keyboard Shortcuts"] = HelpMenuChoice.KeyboardShortcuts,
			["⬅️ Back to Main Menu"] = HelpMenuChoice.BackToMainMenu
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select help topic:")
				.AddChoices(helpMenuChoices.Keys));

		if (helpMenuChoices.TryGetValue(selection, out HelpMenuChoice choice))
		{
			switch (choice)
			{
				case HelpMenuChoice.ApplicationOverview:
					ShowApplicationOverview();
					break;
				case HelpMenuChoice.FeatureGuide:
					ShowFeatureGuide();
					break;
				case HelpMenuChoice.KeyboardShortcuts:
					ShowKeyboardShortcuts();
					break;
				case HelpMenuChoice.BackToMainMenu:
					// Default action - do nothing
					break;
				default:
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Shows application overview information.
	/// </summary>
	private void ShowApplicationOverview()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Application Overview[/]");
		AnsiConsole.WriteLine();

		Panel panel = new Panel(
			new Markup("[bold]BlastMerge[/] is a cross-repository file synchronization tool designed to:\n\n" +
					  "• [green]Find and process files[/] across directory structures\n" +
					  "• [green]Compare files[/] and identify duplicates or differences\n" +
					  "• [green]Perform iterative merging[/] of similar files\n" +
					  "• [green]Execute batch operations[/] for automation\n\n" +
					  "[dim]Perfect for maintaining consistency across multiple code repositories, " +
					  "documentation projects, and configuration management.[/]"))
			.Header("What is BlastMerge?")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Green);

		AnsiConsole.Write(panel);

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Shows feature guide information.
	/// </summary>
	private void ShowFeatureGuide()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Feature Guide[/]");
		AnsiConsole.WriteLine();

		Tree tree = new("[bold]Available Features[/]");

		TreeNode findNode = tree.AddNode("[cyan]🔍 Find & Process Files[/]");
		findNode.AddNode("Search for files matching specific patterns");
		findNode.AddNode("View file information and statistics");

		TreeNode mergeNode = tree.AddNode("[cyan]🔄 Run Iterative Merge[/]");
		mergeNode.AddNode("Intelligently merge similar files");
		mergeNode.AddNode("Interactive conflict resolution");
		mergeNode.AddNode("Preserve the best parts of each file");

		TreeNode compareNode = tree.AddNode("[cyan]📊 Compare Files[/]");
		compareNode.AddNode("Group files by content similarity");
		compareNode.AddNode("Identify exact duplicates");
		compareNode.AddNode("Hash-based comparison");

		TreeNode batchNode = tree.AddNode("[cyan]📦 Batch Operations[/]");
		batchNode.AddNode("Run predefined batch configurations");
		batchNode.AddNode("Process multiple file patterns");
		batchNode.AddNode("Automate repetitive tasks");

		AnsiConsole.Write(tree);

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Shows keyboard shortcuts information.
	/// </summary>
	private void ShowKeyboardShortcuts()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Keyboard Shortcuts[/]");
		AnsiConsole.WriteLine();

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Blue)
			.AddColumn(KeyColumnName)
			.AddColumn(ActionColumnName);

		table.AddRow("[yellow]↑↓ Arrow Keys[/]", "Navigate menu options");
		table.AddRow("[yellow]Enter[/]", "Select current option");
		table.AddRow("[yellow]Ctrl+C[/]", "Exit application");
		table.AddRow("[yellow]↑↓ (Input)[/]", "Browse input history");
		table.AddRow("[yellow]Esc[/]", "Cancel current operation");

		AnsiConsole.Write(table);

		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Shows the goodbye screen when exiting.
	/// </summary>
	private void ShowGoodbyeScreen()
	{
		AnsiConsole.Clear();

		Panel panel = new Panel(
			new Markup("[bold green]Thank you for using BlastMerge![/]\n\n" +
					  "[dim]Your files have been processed safely.[/]"))
			.Header("Goodbye")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Green);

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows a detailed list of files grouped by hash.
	/// </summary>
	/// <param name="fileGroups">The file groups to display.</param>
	private void ShowDetailedFileList(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Detailed File List[/]");
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
				catch (UnauthorizedAccessException)
				{
					groupNode.AddNode($"[green]{filePath}[/] [dim](access denied)[/]");
				}
				catch (DirectoryNotFoundException)
				{
					groupNode.AddNode($"[green]{filePath}[/] [dim](directory not found)[/]");
				}
				catch (FileNotFoundException)
				{
					groupNode.AddNode($"[green]{filePath}[/] [dim](file not found)[/]");
				}
				catch (PathTooLongException)
				{
					groupNode.AddNode($"[green]{filePath}[/] [dim](path too long)[/]");
				}
				catch (ArgumentException)
				{
					groupNode.AddNode($"[green]{filePath}[/] [dim](invalid path)[/]");
				}
				catch (IOException)
				{
					groupNode.AddNode($"[green]{filePath}[/] [dim](I/O error)[/]");
				}
			}

			groupIndex++;
		}

		AnsiConsole.Write(tree);
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
	}

	/// <summary>
	/// Runs iterative merge with console output and user interaction.
	/// </summary>
	/// <param name="directory">The directory containing files to merge.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	private void RunIterativeMergeWithConsoleOutput(string directory, string fileName)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(fileName);

		if (!Directory.Exists(directory))
		{
			throw new DirectoryNotFoundException($"Directory '{directory}' does not exist.");
		}

		// Prepare file groups for merging
		IReadOnlyCollection<FileGroup>? fileGroups = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(directory, fileName);

		if (fileGroups == null)
		{
			AnsiConsole.MarkupLine("[yellow]No files found or insufficient unique versions to merge.[/]");
			return;
		}

		// Start iterative merge process with console callbacks
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups,
			ConsoleMergeCallback,
			ConsoleStatusCallback,
			ConsoleContinueCallback);

		// Handle result
		if (result.IsSuccessful)
		{
			AnsiConsole.MarkupLine($"[green]Merge completed successfully. Final file: {result.OriginalFileName}[/]");
		}
		else
		{
			AnsiConsole.MarkupLine($"[red]Merge failed or was cancelled: {result.OriginalFileName}[/]");
		}
	}

	/// <summary>
	/// Console-specific callback to perform merge operation between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	/// <param name="existingContent">Existing merged content.</param>
	/// <returns>Merge result or null if cancelled.</returns>
	private MergeResult? ConsoleMergeCallback(string file1, string file2, string? existingContent)
	{
		// Determine which file should be on the left (fewer changes)
		string leftFile, rightFile;
		if (existingContent == null)
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
		if (existingContent != null)
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
			leftFile, rightFile, existingContent, (block, context, blockNumber) => GetBlockChoice(block, context, blockNumber, leftFile, rightFile));

		if (result != null)
		{
			AnsiConsole.MarkupLine($"[green]✅ Merged successfully! Versions reduced by 1.[/]");
			AnsiConsole.WriteLine();
		}

		return result;
	}

	/// <summary>
	/// Console-specific callback to report merge status.
	/// </summary>
	/// <param name="status">Current merge session status.</param>
	private void ConsoleStatusCallback(MergeSessionStatus status)
	{
		AnsiConsole.MarkupLine($"[yellow]Merge {status.CurrentIteration}: {status.MostSimilarPair?.FilePath1} <-> {status.MostSimilarPair?.FilePath2}[/]");
		AnsiConsole.MarkupLine($"[dim]Similarity: {status.MostSimilarPair?.SimilarityScore:F1} | Remaining files: {status.RemainingFilesCount}[/]");
	}

	/// <summary>
	/// Console-specific callback to ask if the user wants to continue merging.
	/// </summary>
	/// <returns>True to continue, false to stop.</returns>
	private bool ConsoleContinueCallback() => AnsiConsole.Confirm("[cyan]Continue with next merge?[/]");

	/// <summary>
	/// Callback to perform merge operation between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	/// <param name="existingContent">Existing merged content.</param>
	/// <returns>Merge result or null if cancelled.</returns>
	private MergeResult? PerformMergeCallback(string file1, string file2, string? existingContent)
	{
		// For console app, perform automatic merge or prompt user
		// This is a simplified implementation - in a real app you'd want user interaction
		return IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			file1,
			file2,
			existingContent,
			(block, context, index) => BlockChoice.UseVersion1); // Default to version 1 choice
	}

	/// <summary>
	/// Callback to report merge status.
	/// </summary>
	/// <param name="status">Current merge session status.</param>
	private void ReportMergeStatus(MergeSessionStatus status)
	{
		Console.WriteLine($"Merge {status.CurrentIteration}: {status.MostSimilarPair?.FilePath1} <-> {status.MostSimilarPair?.FilePath2}");
		Console.WriteLine($"Similarity: {status.MostSimilarPair?.SimilarityScore:F1} | Remaining files: {status.RemainingFilesCount}");
	}

	/// <summary>
	/// Callback to ask if the user wants to continue merging.
	/// </summary>
	/// <returns>True to continue, false to stop.</returns>
	private bool ContinueMergeCallback()
	{
		Console.Write("Continue with next merge? (y/n): ");
		string? response = Console.ReadLine();
		return response?.ToLowerInvariant() is "y" or "yes";
	}

	/// <summary>
	/// Gets the user's choice for a merge block with visual conflict resolution
	/// </summary>
	/// <param name="block">The diff block to choose for</param>
	/// <param name="context">The context around the block</param>
	/// <param name="blockNumber">The block number being processed</param>
	/// <param name="leftFile">The left file path</param>
	/// <param name="rightFile">The right file path</param>
	/// <returns>The user's choice for the block</returns>
	private BlockChoice GetBlockChoice(DiffBlock block, BlockContext context, int blockNumber, string leftFile, string rightFile)
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
}
