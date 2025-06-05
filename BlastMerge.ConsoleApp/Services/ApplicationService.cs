// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Console-specific implementation of the application service that adds UI functionality.
/// </summary>
public class ConsoleApplicationService : ApplicationService
{
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
			.AddColumn("Group")
			.AddColumn("Files")
			.AddColumn("Status");

		int groupIndex = 1;
		foreach (KeyValuePair<string, IReadOnlyCollection<string>> group in fileGroups)
		{
			string status = group.Value.Count > 1 ? "[yellow]Multiple versions[/]" : "[green]Unique[/]";
			table.AddRow(
				$"[cyan]{groupIndex}[/]",
				$"[dim]{group.Value.Count}[/]",
				status);

			groupIndex++;
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();

		// Ask user what to do with the files
		string[] choices =
		[
			"View detailed file list",
			"Run iterative merge on duplicates",
			"Return to main menu"
		];

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("What would you like to do with these files?")
				.AddChoices(choices));

		switch (selection)
		{
			case "View detailed file list":
				ShowDetailedFileList(fileGroups);
				break;
			case "Run iterative merge on duplicates":
				RunIterativeMerge(directory, fileName);
				break;
			default:
				break;
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

				string[] choices =
				[
					"Run iterative merge",
					"Skip this pattern",
					"Stop batch processing"
				];

				string selection = AnsiConsole.Prompt(
					new SelectionPrompt<string>()
						.Title($"Multiple versions found for pattern '{pattern}'. What would you like to do?")
						.AddChoices(choices));

				switch (selection)
				{
					case "Run iterative merge":
						RunIterativeMerge(directory, pattern);
						break;
					case "Skip this pattern":
						AnsiConsole.MarkupLine("[yellow]Skipping pattern.[/]");
						continue;
					case "Stop batch processing":
						AnsiConsole.MarkupLine("[yellow]Stopping batch processing.[/]");
						return;
					default:
						break;
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
		return (IReadOnlyDictionary<string, IReadOnlyCollection<string>>)FileDiffer.GroupFilesByHash(filePaths);
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
				string choice = ShowMainMenu();

				if (choice == "exit")
				{
					break;
				}

				ExecuteMenuChoice(choice);
			}
			catch (DirectoryNotFoundException ex)
			{
				AnsiConsole.MarkupLine($"[red]Directory not found: {ex.Message}[/]");
				AnsiConsole.WriteLine("Press any key to continue...");
				Console.ReadKey();
			}
			catch (UnauthorizedAccessException ex)
			{
				AnsiConsole.MarkupLine($"[red]Access denied: {ex.Message}[/]");
				AnsiConsole.WriteLine("Press any key to continue...");
				Console.ReadKey();
			}
			catch (IOException ex)
			{
				AnsiConsole.MarkupLine($"[red]File I/O error: {ex.Message}[/]");
				AnsiConsole.WriteLine("Press any key to continue...");
				Console.ReadKey();
			}
			catch (ArgumentException ex)
			{
				AnsiConsole.MarkupLine($"[red]Invalid input: {ex.Message}[/]");
				AnsiConsole.WriteLine("Press any key to continue...");
				Console.ReadKey();
			}
			catch (InvalidOperationException ex)
			{
				AnsiConsole.MarkupLine($"[red]Operation error: {ex.Message}[/]");
				AnsiConsole.WriteLine("Press any key to continue...");
				Console.ReadKey();
			}
		}

		ShowGoodbyeScreen();
	}

	/// <summary>
	/// Shows the welcome screen with application information.
	/// </summary>
	private static void ShowWelcomeScreen()
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
	private static string ShowMainMenu()
	{
		string[] choices =
		[
			"🔍 Find & Process Files",
			"🔄 Run Iterative Merge",
			"📊 Compare Files",
			"📦 Batch Operations",
			"⚙️  Configuration & Settings",
			"❓ Help & Information",
			"🚪 Exit"
		];

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[bold cyan]Main Menu[/] - Select an option:")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
				.AddChoices(choices));

		return selection switch
		{
			"🔍 Find & Process Files" => "find_files",
			"🔄 Run Iterative Merge" => "iterative_merge",
			"📊 Compare Files" => "compare_files",
			"📦 Batch Operations" => "batch_operations",
			"⚙️  Configuration & Settings" => "settings",
			"❓ Help & Information" => "help",
			"🚪 Exit" => "exit",
			_ => "help"
		};
	}

	/// <summary>
	/// Executes the selected menu choice.
	/// </summary>
	/// <param name="choice">The menu choice to execute.</param>
	private void ExecuteMenuChoice(string choice)
	{
		switch (choice)
		{
			case "find_files":
				HandleFindFiles();
				break;
			case "iterative_merge":
				HandleIterativeMerge();
				break;
			case "compare_files":
				HandleCompareFiles();
				break;
			case "batch_operations":
				HandleBatchOperations();
				break;
			case "settings":
				HandleSettings();
				break;
			case "help":
				HandleHelp();
				break;
			default:
				HandleHelp();
				break;
		}
	}

	/// <summary>
	/// Handles the find files operation.
	/// </summary>
	private static void HandleFindFiles()
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
					.AddColumn("File Path")
					.AddColumn("Size");

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

		AnsiConsole.WriteLine("Press any key to continue...");
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

		AnsiConsole.WriteLine("Press any key to continue...");
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
			.Start("Comparing files...", ctx =>
			{
				IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = CompareFiles(directory, fileName);

				ctx.Refresh();

				if (!fileGroups.Any())
				{
					AnsiConsole.MarkupLine("[yellow]No files found to compare.[/]");
					return;
				}

				Tree tree = new("[bold]File Comparison Results[/]");

				int groupIndex = 1;
				foreach (KeyValuePair<string, IReadOnlyCollection<string>> group in fileGroups)
				{
					TreeNode groupNode = tree.AddNode($"[cyan]Group {groupIndex} ({group.Value.Count} files)[/]");
					groupNode.AddNode($"[dim]Hash: {group.Key[..8]}...[/]");

					foreach (string filePath in group.Value)
					{
						groupNode.AddNode($"[green]{filePath}[/]");
					}
					groupIndex++;
				}

				AnsiConsole.Write(tree);
				AnsiConsole.MarkupLine($"\n[green]Found {fileGroups.Count} file groups.[/]");
			});

		AnsiConsole.WriteLine("Press any key to continue...");
		Console.ReadKey();
	}

	/// <summary>
	/// Handles batch operations.
	/// </summary>
	private void HandleBatchOperations()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Batch Operations[/]");
		AnsiConsole.WriteLine();

		string[] choices =
		[
			"📋 List Available Batches",
			"▶️  Run Batch Configuration",
			"⬅️  Back to Main Menu"
		];

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select batch operation:")
				.AddChoices(choices));

		switch (selection)
		{
			case "📋 List Available Batches":
				HandleListBatches();
				break;
			case "▶️  Run Batch Configuration":
				HandleRunBatch();
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Handles listing available batch configurations.
	/// </summary>
	private static void HandleListBatches()
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
				.AddColumn("Name")
				.AddColumn("Description")
				.AddColumn("Patterns");

			foreach (BatchConfiguration batch in allBatches)
			{
				table.AddRow(
					$"[green]{batch.Name}[/]",
					$"[dim]{batch.Description ?? "No description"}[/]",
					$"[yellow]{batch.FilePatterns.Count}[/]");
			}

			AnsiConsole.Write(table);
		}

		AnsiConsole.WriteLine("Press any key to continue...");
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
			AnsiConsole.WriteLine("Press any key to continue...");
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

		AnsiConsole.WriteLine("Press any key to continue...");
		Console.ReadKey();
	}

	/// <summary>
	/// Handles configuration and settings.
	/// </summary>
	private static void HandleSettings()
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

		AnsiConsole.WriteLine("Press any key to continue...");
		Console.ReadKey();
	}

	/// <summary>
	/// Handles help and information display.
	/// </summary>
	private static void HandleHelp()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Help & Information[/]");
		AnsiConsole.WriteLine();

		string[] choices =
		[
			"📖 Application Overview",
			"🎯 Feature Guide",
			"⌨️  Keyboard Shortcuts",
			"⬅️  Back to Main Menu"
		];

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select help topic:")
				.AddChoices(choices));

		switch (selection)
		{
			case "📖 Application Overview":
				ShowApplicationOverview();
				break;
			case "🎯 Feature Guide":
				ShowFeatureGuide();
				break;
			case "⌨️  Keyboard Shortcuts":
				ShowKeyboardShortcuts();
				break;
			default:
				break;
		}
	}

	/// <summary>
	/// Shows application overview information.
	/// </summary>
	private static void ShowApplicationOverview()
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

		AnsiConsole.WriteLine("Press any key to continue...");
		Console.ReadKey();
	}

	/// <summary>
	/// Shows feature guide information.
	/// </summary>
	private static void ShowFeatureGuide()
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

		AnsiConsole.WriteLine("Press any key to continue...");
		Console.ReadKey();
	}

	/// <summary>
	/// Shows keyboard shortcuts information.
	/// </summary>
	private static void ShowKeyboardShortcuts()
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine("[bold cyan]Keyboard Shortcuts[/]");
		AnsiConsole.WriteLine();

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Blue)
			.AddColumn("Key")
			.AddColumn("Action");

		table.AddRow("[yellow]↑↓ Arrow Keys[/]", "Navigate menu options");
		table.AddRow("[yellow]Enter[/]", "Select current option");
		table.AddRow("[yellow]Ctrl+C[/]", "Exit application");
		table.AddRow("[yellow]↑↓ (Input)[/]", "Browse input history");
		table.AddRow("[yellow]Esc[/]", "Cancel current operation");

		AnsiConsole.Write(table);

		AnsiConsole.WriteLine("Press any key to continue...");
		Console.ReadKey();
	}

	/// <summary>
	/// Shows the goodbye screen when exiting.
	/// </summary>
	private static void ShowGoodbyeScreen()
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
	private static void ShowDetailedFileList(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
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
		AnsiConsole.WriteLine("Press any key to continue...");
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
		// Console app performs automatic merge with user notification
		AnsiConsole.MarkupLine($"[cyan]Merging: {file1} <-> {file2}[/]");
		return IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			file1,
			file2,
			existingContent,
			(block, context, index) => BlockChoice.UseVersion1); // Default to version 1 choice
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
	private static bool ContinueMergeCallback()
	{
		Console.Write("Continue with next merge? (y/n): ");
		string? response = Console.ReadLine();
		return response?.ToLowerInvariant() is "y" or "yes";
	}
}
