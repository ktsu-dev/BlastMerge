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
			["Run iterative merge on duplicates"] = ProcessFileActionChoice.RunIterativeMergeOnDuplicates,
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
				case ProcessFileActionChoice.RunIterativeMergeOnDuplicates:
					RunIterativeMerge(directory, fileName);
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

		AnsiConsole.WriteLine(PressAnyKeyMessage);
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
	private bool ContinueMergeCallback()
	{
		Console.Write("Continue with next merge? (y/n): ");
		string? response = Console.ReadLine();
		return response?.ToLowerInvariant() is "y" or "yes";
	}
}
