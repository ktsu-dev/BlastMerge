// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;
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
			UIHelper.ShowWarning("No files found matching the pattern.");
			return;
		}

		// Group files by hash to identify duplicates and candidates for merging
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = CompareFiles(directory, fileName);

		if (fileGroups.Count == 0)
		{
			UIHelper.ShowWarning("No files to compare.");
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
					FileDisplayService.ShowDetailedFileList(fileGroups);
					break;
				case ProcessFileActionChoice.ShowDifferences:
					FileDisplayService.ShowDifferences(fileGroups);
					break;
				case ProcessFileActionChoice.RunIterativeMergeOnDuplicates:
					InteractiveMergeService.PerformIterativeMerge(fileGroups);
					break;
				case ProcessFileActionChoice.SyncFiles:
					SyncOperationsService.OfferSyncOptions(fileGroups);
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

		BatchConfiguration? batch = GetBatchConfiguration(batchName);
		if (batch == null)
		{
			return;
		}

		ShowBatchHeader(batch, directory);
		(int totalPatternsProcessed, int totalFilesFound) = ProcessAllPatterns(directory, batch);
		ShowBatchCompletion(totalPatternsProcessed, totalFilesFound);
	}

	/// <summary>
	/// Gets the batch configuration by name.
	/// </summary>
	/// <param name="batchName">The name of the batch configuration.</param>
	/// <returns>The batch configuration or null if not found.</returns>
	private static BatchConfiguration? GetBatchConfiguration(string batchName)
	{
		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();
		BatchConfiguration? batch = allBatches.FirstOrDefault(b => b.Name.Equals(batchName, StringComparison.OrdinalIgnoreCase));

		if (batch == null)
		{
			UIHelper.ShowError($"Batch configuration '{batchName}' not found.");
			UIHelper.ShowInfo("Use the 'List Batches' option to see available configurations.");
		}

		return batch;
	}

	/// <summary>
	/// Shows the batch processing header information.
	/// </summary>
	/// <param name="batch">The batch configuration.</param>
	/// <param name="directory">The directory being processed.</param>
	private static void ShowBatchHeader(BatchConfiguration batch, string directory)
	{
		AnsiConsole.MarkupLine($"[cyan]Processing batch configuration '[yellow]{batch.Name}[/]' in '[yellow]{directory}[/]'[/]");
		AnsiConsole.WriteLine();

		AnsiConsole.MarkupLine($"[green]Found batch configuration: {batch.Name}[/]");
		if (!string.IsNullOrEmpty(batch.Description))
		{
			AnsiConsole.MarkupLine($"[dim]{batch.Description}[/]");
		}
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Processes all patterns in the batch configuration.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batch">The batch configuration.</param>
	/// <returns>A tuple containing the total patterns processed and total files found.</returns>
	private (int totalPatternsProcessed, int totalFilesFound) ProcessAllPatterns(string directory, BatchConfiguration batch)
	{
		int totalPatternsProcessed = 0;
		int totalFilesFound = 0;

		foreach (string pattern in batch.FilePatterns)
		{
			BatchPatternResult result = ProcessSinglePattern(directory, pattern, batch);

			if (result.ShouldStop)
			{
				break;
			}

			if (result.WasProcessed)
			{
				totalPatternsProcessed++;
				totalFilesFound += result.FilesFound;
			}
		}

		return (totalPatternsProcessed, totalFilesFound);
	}

	/// <summary>
	/// Processes a single file pattern in the batch.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="pattern">The file pattern to process.</param>
	/// <param name="batch">The batch configuration.</param>
	/// <returns>The result of processing the pattern.</returns>
	private BatchPatternResult ProcessSinglePattern(string directory, string pattern, BatchConfiguration batch)
	{
		AnsiConsole.MarkupLine($"[cyan]Processing pattern: [yellow]{pattern}[/][/]");

		IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, pattern);

		if (filePaths.Count == 0)
		{
			if (!batch.SkipEmptyPatterns)
			{
				AnsiConsole.MarkupLine($"[yellow]No files found for pattern '{pattern}'[/]");
			}
			return new BatchPatternResult { WasProcessed = false };
		}

		AnsiConsole.MarkupLine($"[green]Found {filePaths.Count} files matching '{pattern}'[/]");

		if (batch.PromptBeforeEachPattern && !AnsiConsole.Confirm($"Process files for pattern '{pattern}'?"))
		{
			AnsiConsole.MarkupLine("[yellow]Skipping pattern.[/]");
			return new BatchPatternResult { WasProcessed = false };
		}

		BatchActionResult actionResult = HandlePatternWithMultipleVersions(directory, pattern);
		AnsiConsole.WriteLine();

		return new BatchPatternResult
		{
			WasProcessed = true,
			FilesFound = filePaths.Count,
			ShouldStop = actionResult.ShouldStop
		};
	}

	/// <summary>
	/// Handles patterns that have multiple versions requiring user action.
	/// </summary>
	/// <param name="directory">The directory being processed.</param>
	/// <param name="pattern">The file pattern.</param>
	/// <returns>The result of the user action.</returns>
	private BatchActionResult HandlePatternWithMultipleVersions(string directory, string pattern)
	{
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = CompareFiles(directory, pattern);
		int groupsWithMultipleFiles = fileGroups.Count(g => g.Value.Count > 1);

		if (groupsWithMultipleFiles == 0)
		{
			AnsiConsole.MarkupLine($"[green]All files for pattern '{pattern}' are unique (no duplicates found).[/]");
			return new BatchActionResult { ShouldStop = false };
		}

		AnsiConsole.MarkupLine($"[yellow]Found {groupsWithMultipleFiles} groups with multiple versions that could be merged.[/]");
		return GetUserActionForPattern(directory, pattern);
	}

	/// <summary>
	/// Gets the user's action choice for a pattern with multiple versions.
	/// </summary>
	/// <param name="directory">The directory being processed.</param>
	/// <param name="pattern">The file pattern.</param>
	/// <returns>The result of the user action.</returns>
	private BatchActionResult GetUserActionForPattern(string directory, string pattern)
	{
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

		if (!batchActionChoices.TryGetValue(selection, out BatchActionChoice choice))
		{
			AnsiConsole.MarkupLine("[yellow]Unknown choice, skipping pattern.[/]");
			return new BatchActionResult { ShouldStop = false };
		}

		return choice switch
		{
			BatchActionChoice.RunIterativeMerge => ExecuteIterativeMerge(directory, pattern),
			BatchActionChoice.SkipPattern => HandleSkipPattern(),
			BatchActionChoice.StopBatchProcessing => HandleStopProcessing(),
			_ => new BatchActionResult { ShouldStop = false }
		};
	}

	/// <summary>
	/// Executes iterative merge for a pattern.
	/// </summary>
	/// <param name="directory">The directory being processed.</param>
	/// <param name="pattern">The file pattern.</param>
	/// <returns>The result of the merge operation.</returns>
	private BatchActionResult ExecuteIterativeMerge(string directory, string pattern)
	{
		RunIterativeMerge(directory, pattern);
		return new BatchActionResult { ShouldStop = false };
	}

	/// <summary>
	/// Handles skipping the current pattern.
	/// </summary>
	/// <returns>The result indicating to continue processing.</returns>
	private BatchActionResult HandleSkipPattern()
	{
		AnsiConsole.MarkupLine("[yellow]Skipping pattern.[/]");
		return new BatchActionResult { ShouldStop = false };
	}

	/// <summary>
	/// Handles stopping batch processing.
	/// </summary>
	/// <returns>The result indicating to stop processing.</returns>
	private BatchActionResult HandleStopProcessing()
	{
		AnsiConsole.MarkupLine("[yellow]Stopping batch processing.[/]");
		return new BatchActionResult { ShouldStop = true };
	}

	/// <summary>
	/// Shows the batch processing completion message.
	/// </summary>
	/// <param name="totalPatternsProcessed">Total patterns processed.</param>
	/// <param name="totalFilesFound">Total files found.</param>
	private static void ShowBatchCompletion(int totalPatternsProcessed, int totalFilesFound)
	{
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
		ProgressReportingService.ReportCompletionResult(result);
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
		while (true)
		{
			try
			{
				// Check navigation stack to determine what menu to show
				if (NavigationHistory.ShouldGoToMainMenu())
				{
					ShowWelcomeScreen();
					MenuChoice choice = ShowMainMenu();

					if (choice == MenuChoice.Exit)
					{
						break;
					}

					ExecuteMenuChoice(choice);
				}
				else
				{
					// Navigate back based on navigation stack
					NavigateBasedOnStack();
				}
			}
			catch (DirectoryNotFoundException ex)
			{
				AnsiConsole.MarkupLine($"[red]Directory not found: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
				// Clear navigation on error to return to main menu
				NavigationHistory.Clear();
			}
			catch (UnauthorizedAccessException ex)
			{
				AnsiConsole.MarkupLine($"[red]Access denied: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
				// Clear navigation on error to return to main menu
				NavigationHistory.Clear();
			}
			catch (IOException ex)
			{
				AnsiConsole.MarkupLine($"[red]File I/O error: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
				// Clear navigation on error to return to main menu
				NavigationHistory.Clear();
			}
			catch (ArgumentException ex)
			{
				AnsiConsole.MarkupLine($"[red]Invalid input: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
				// Clear navigation on error to return to main menu
				NavigationHistory.Clear();
			}
			catch (InvalidOperationException ex)
			{
				AnsiConsole.MarkupLine($"[red]Operation error: {ex.Message}[/]");
				AnsiConsole.WriteLine(PressAnyKeyMessage);
				Console.ReadKey();
				// Clear navigation on error to return to main menu
				NavigationHistory.Clear();
			}
		}

		ShowGoodbyeScreen();
	}

	/// <summary>
	/// Shows the welcome screen with application information.
	/// </summary>
	private static void ShowWelcomeScreen() => MenuDisplayService.ShowWelcomeScreen();

	/// <summary>
	/// Shows the main menu and returns the user's choice.
	/// </summary>
	/// <returns>The selected menu choice.</returns>
	private static MenuChoice ShowMainMenu()
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
		// Clear navigation history when starting from main menu
		NavigationHistory.Clear();
		NavigationHistory.Push("Main Menu");

		switch (choice)
		{
			case MenuChoice.FindFiles:
				new FindFilesMenuHandler(this).Enter();
				break;
			case MenuChoice.IterativeMerge:
				new IterativeMergeMenuHandler(this).Enter();
				break;
			case MenuChoice.CompareFiles:
				new CompareFilesMenuHandler(this).Enter();
				break;
			case MenuChoice.BatchOperations:
				new BatchOperationsMenuHandler(this).Enter();
				break;
			case MenuChoice.Settings:
				new SettingsMenuHandler(this).Enter();
				break;
			case MenuChoice.Help:
				new HelpMenuHandler(this).Enter();
				break;
			case MenuChoice.Exit:
				HandleHelp(); // This should never be called since Exit is handled in the main loop
				break;
			default:
				new HelpMenuHandler(this).Enter();
				break;
		}
	}

	/// <summary>
	/// Handles help and information display.
	/// </summary>
	private void HandleHelp()
	{
		AnsiConsole.Clear();
		HelpMenuHandler helpHandler = new(this);
		helpHandler.Handle();
	}

	/// <summary>
	/// Shows the goodbye screen when exiting.
	/// </summary>
	private static void ShowGoodbyeScreen() => MenuDisplayService.ShowGoodbyeScreen();

	/// <summary>
	/// Navigates based on the current navigation stack.
	/// </summary>
	private void NavigateBasedOnStack()
	{
		string? currentMenu = NavigationHistory.Peek();

		if (currentMenu == null)
		{
			NavigationHistory.Clear();
			return;
		}

		// Route to the appropriate menu handler based on navigation stack
		switch (currentMenu)
		{
			case "Find Files":
				new FindFilesMenuHandler(this).Handle();
				break;
			case "Iterative Merge":
				new IterativeMergeMenuHandler(this).Handle();
				break;
			case "Compare Files":
				new CompareFilesMenuHandler(this).Handle();
				break;
			case "Batch Operations":
				new BatchOperationsMenuHandler(this).Handle();
				break;
			case "Settings":
				new SettingsMenuHandler(this).Handle();
				break;
			case "Help":
				new HelpMenuHandler(this).Handle();
				break;
			default:
				// Unknown menu - clear navigation and return to main
				NavigationHistory.Clear();
				break;
		}
	}

	/// <summary>
	/// Shows a detailed list of files grouped by hash.
	/// </summary>

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

		ProgressReportingService.ReportMergeInitiation(leftFile, rightFile, existingContent != null);

		MergeResult? result = IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			leftFile, rightFile, existingContent, (diffBlock, context, blockNumber) => GetBlockChoice(diffBlock, context, blockNumber, leftFile, rightFile));

		if (result != null)
		{
			ProgressReportingService.ReportMergeStepSuccess();
		}

		return result;
	}

	/// <summary>
	/// Console-specific callback to report merge status.
	/// </summary>
	/// <param name="status">Current merge session status.</param>
	private static void ConsoleStatusCallback(MergeSessionStatus status) => ProgressReportingService.ReportMergeStatus(status);

	/// <summary>
	/// Console-specific callback to ask if the user wants to continue merging.
	/// </summary>
	/// <returns>True to continue, false to stop.</returns>
	private static bool ConsoleContinueCallback() => UserInteractionService.ConfirmContinueMerge();

	/// <summary>
	/// Gets the user's choice for a merge block with visual conflict resolution
	/// </summary>
	/// <param name="diffBlock">The DiffPlex diff block to choose for</param>
	/// <param name="context">The context around the block</param>
	/// <param name="blockNumber">The block number being processed</param>
	/// <param name="leftFile">The left file path</param>
	/// <param name="rightFile">The right file path</param>
	/// <returns>The user's choice for the block</returns>
	private BlockChoice GetBlockChoice(DiffPlex.Model.DiffBlock diffBlock, BlockContext context, int blockNumber, string leftFile, string rightFile)
	{
		string blockType = DetermineBlockType(diffBlock);
		AnsiConsole.MarkupLine($"[yellow]🔍 Block {blockNumber} ({blockType})[/]");

		(string leftLabel, string rightLabel) = FileDisplayService.GetDistinguishingLabels(leftFile, rightFile);

		// Show the diff block using our improved display service
		string[] lines1 = File.ReadAllLines(leftFile);
		string[] lines2 = File.ReadAllLines(rightFile);
		FileComparisonDisplayService.ShowDiffBlock(lines1, lines2, diffBlock, context, leftLabel, rightLabel);
		FileComparisonDisplayService.ShowDiffBlockStatistics(diffBlock);

		// Get user's choice based on block content
		if (diffBlock.DeleteCountA > 0 && diffBlock.InsertCountB > 0)
		{
			// Replace block - both deleted and inserted content
			return GetReplaceChoice(leftFile, rightFile);
		}
		else if (diffBlock.InsertCountB > 0)
		{
			// Insert block - only inserted content
			return GetInsertChoice(rightFile, leftFile);
		}
		else if (diffBlock.DeleteCountA > 0)
		{
			// Delete block - only deleted content
			return GetDeleteChoice(leftFile, rightFile);
		}
		else
		{
			// No changes (shouldn't happen in practice)
			return BlockChoice.UseVersion2;
		}
	}

	/// <summary>
	/// Determines the block type for display purposes
	/// </summary>
	/// <param name="diffBlock">The DiffPlex diff block</param>
	/// <returns>A string describing the block type</returns>
	private static string DetermineBlockType(DiffPlex.Model.DiffBlock diffBlock)
	{
		return diffBlock.DeleteCountA > 0 && diffBlock.InsertCountB > 0
			? "Replace"
			: diffBlock.InsertCountB > 0 ? "Insert" : diffBlock.DeleteCountA > 0 ? "Delete" : "Unchanged";
	}

	/// <summary>
	/// Gets user choice for insert blocks
	/// </summary>
	/// <param name="rightFile">The right file path</param>
	/// <param name="leftFile">The left file path (for context in distinguishing labels)</param>
	/// <returns>User's choice</returns>
	private static BlockChoice GetInsertChoice(string rightFile, string leftFile)
	{
		(_, string rightLabel) = FileDisplayService.GetDistinguishingLabels(leftFile, rightFile);
		string choice = UserInteractionService.ShowSelectionPrompt(
			$"[cyan]This content exists only in {rightLabel}. What would you like to do?[/]",
			[
				"✅ Include the addition",
				"❌ Skip the addition"
			]);

		return choice.Contains("Include") ? BlockChoice.Include : BlockChoice.Skip;
	}

	/// <summary>
	/// Gets user choice for delete blocks
	/// </summary>
	/// <param name="leftFile">The left file path</param>
	/// <param name="rightFile">The right file path (for context in distinguishing labels)</param>
	/// <returns>User's choice</returns>
	private static BlockChoice GetDeleteChoice(string leftFile, string rightFile)
	{
		(string leftLabel, _) = FileDisplayService.GetDistinguishingLabels(leftFile, rightFile);
		string choice = UserInteractionService.ShowSelectionPrompt(
			$"[cyan]This content exists only in {leftLabel}. What would you like to do?[/]",
			[
				"✅ Keep the content",
				"❌ Remove the content"
			]);

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
		(string leftLabel, string rightLabel) = FileDisplayService.GetDistinguishingLabels(leftFile, rightFile);
		string choice = UserInteractionService.ShowSelectionPrompt(
			"[cyan]This content differs between versions. What would you like to do?[/]",
			[
				$"  Use {leftLabel}",
				$"  Use {rightLabel}",
				"🔄 Use Both Versions",
				"❌ Skip Both"
			]);

		return choice switch
		{
			var s when s.Contains($"Use {leftLabel}") => BlockChoice.UseVersion1,
			var s when s.Contains($"Use {rightLabel}") => BlockChoice.UseVersion2,
			var s when s.Contains("Both") => BlockChoice.UseBoth,
			_ => BlockChoice.Skip
		};
	}
}
