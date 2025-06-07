// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.ConsoleApp.Models;
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
	private const string FilenameColumnName = "Filename";
	private const string HashColumnName = "Hash";

	// Menu display text to command mappings
	private static readonly Dictionary<string, MenuChoice> MainMenuChoices = new()
	{
		[MenuNames.Display.FindFiles] = MenuChoice.FindFiles,
		[MenuNames.Display.CompareFiles] = MenuChoice.CompareFiles,
		[MenuNames.Display.IterativeMerge] = MenuChoice.IterativeMerge,
		[MenuNames.Display.BatchOperations] = MenuChoice.BatchOperations,
		[MenuNames.Display.RunRecentBatch] = MenuChoice.RunRecentBatch,
		[MenuNames.Display.Settings] = MenuChoice.Settings,
		[MenuNames.Display.Help] = MenuChoice.Help,
		[MenuNames.Display.Exit] = MenuChoice.Exit
	};

	/// <summary>
	/// Processes files in a directory with a specified filename pattern.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	public override void ProcessFiles(string directory, string fileName)
	{
		ValidateDirectoryAndFileName(directory, fileName);

		AnsiConsole.MarkupLine($"[cyan]Processing files matching pattern '[yellow]{fileName}[/]' in '[yellow]{directory}[/]'[/]");
		AnsiConsole.WriteLine();

		List<string> filePaths = DiscoverFiles(directory, fileName);
		if (filePaths.Count == 0)
		{
			UIHelper.ShowWarning("No files found matching the pattern.");
			return;
		}

		IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = CompareFiles(directory, fileName);
		if (fileGroups.Count == 0)
		{
			UIHelper.ShowWarning("No files to compare.");
			return;
		}

		ShowFileGroupSummaryTable(fileGroups, directory, filePaths.Count);
		ExecuteUserSelectedAction(fileGroups);
	}

	/// <summary>
	/// Discovers files matching the pattern in the directory.
	/// </summary>
	/// <param name="directory">The directory to search.</param>
	/// <param name="fileName">The filename pattern.</param>
	/// <returns>List of discovered file paths.</returns>
	private static List<string> DiscoverFiles(string directory, string fileName)
	{
		List<string> filePaths = [];
		AnsiConsole.Status()
			.Start("Discovering files...", ctx =>
			{
				IReadOnlyCollection<string> foundFiles = FileFinder.FindFiles(directory, fileName, path =>
				{
					ctx.Status($"Discovering files... Found: {Path.GetFileName(path)}");
					ctx.Refresh();
				});
				filePaths.AddRange(foundFiles);
				ctx.Status($"Discovery complete! Found {foundFiles.Count} files.");
			});
		return filePaths;
	}

	/// <summary>
	/// Executes the user-selected action on the file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to process.</param>
	private static void ExecuteUserSelectedAction(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		Dictionary<string, ProcessFileActionChoice> processFileActionChoices = new()
		{
			[MenuNames.Actions.ViewDetailedFileList] = ProcessFileActionChoice.ViewDetailedFileList,
			[MenuNames.Actions.ShowDifferences] = ProcessFileActionChoice.ShowDifferences,
			[MenuNames.Actions.RunIterativeMergeOnDuplicates] = ProcessFileActionChoice.RunIterativeMergeOnDuplicates,
			[MenuNames.Actions.SyncFiles] = ProcessFileActionChoice.SyncFiles,
			[MenuNames.Actions.ReturnToMainMenu] = ProcessFileActionChoice.ReturnToMainMenu
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("What would you like to do with these files?")
				.AddChoices(processFileActionChoices.Keys));

		if (processFileActionChoices.TryGetValue(selection, out ProcessFileActionChoice choice))
		{
			ExecuteFileAction(choice, fileGroups);
		}
	}

	/// <summary>
	/// Executes the specific file action.
	/// </summary>
	/// <param name="choice">The action choice.</param>
	/// <param name="fileGroups">The file groups to process.</param>
	private static void ExecuteFileAction(ProcessFileActionChoice choice, IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
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
			default:
				// Default action - do nothing
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
		ValidateDirectoryExists(directory);

		BatchConfiguration? batch = GetBatchConfiguration(batchName);
		if (batch == null)
		{
			return;
		}

		// Record this batch as recently used
		BlastMergeAppData appData = BlastMergeAppData.Get();
		appData.RecentBatch = new RecentBatchInfo
		{
			BatchName = batchName,
			LastUsed = DateTime.UtcNow
		};
		appData.Save();

		ShowBatchHeader(batch, directory);
		(int totalPatternsProcessed, int totalFilesFound, BatchResult? batchResult) = ProcessAllPatterns(directory, batch);
		ShowBatchCompletion(totalPatternsProcessed, totalFilesFound, batchResult);
	}

	/// <summary>
	/// Gets the batch configuration by name.
	/// </summary>
	/// <param name="batchName">The name of the batch configuration.</param>
	/// <returns>The batch configuration or null if not found.</returns>
	private static BatchConfiguration? GetBatchConfiguration(string batchName)
	{
		IReadOnlyCollection<BatchConfiguration> allBatches = AppDataBatchManager.GetAllBatches();
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
	/// Processes all patterns in the batch configuration using parallel optimization.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="batch">The batch configuration.</param>
	/// <returns>A tuple containing the total patterns processed, total files found, and batch result.</returns>
	private static (int totalPatternsProcessed, int totalFilesFound, BatchResult? batchResult) ProcessAllPatterns(string directory, BatchConfiguration batch)
	{
		BatchResult? result = null;

		// Use discrete phases batch processing with better separation of concerns
		// User interaction only occurs in the resolving phase, avoiding conflicts with progress display
		result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			directory,
			ConsoleMergeCallback,
			ConsoleStatusCallback,
			ConsoleContinueCallback,
			(progressMessage) => AnsiConsole.MarkupLine($"[yellow]{progressMessage}[/]")); // Direct output with phase separation

		int totalFilesFound = result?.PatternResults.Sum(pr => pr.FilesFound) ?? 0;
		return (result?.TotalPatternsProcessed ?? 0, totalFilesFound, result);
	}

	/// <summary>
	/// Runs iterative merge with the specified callbacks.
	/// </summary>
	/// <param name="directory">The directory to search.</param>
	/// <param name="fileName">The file name pattern to match.</param>
	/// <param name="mergeCallback">Callback for handling merge operations.</param>
	/// <param name="statusCallback">Callback for reporting merge status.</param>
	/// <param name="continueCallback">Callback for asking whether to continue.</param>
	private void RunIterativeMergeWithCallbacks(
		string directory,
		string fileName,
		Func<string, string, string?, MergeResult?> mergeCallback,
		Action<MergeSessionStatus> statusCallback,
		Func<bool> continueCallback)
	{
		ValidateDirectoryExists(directory);

		// First get all files for the table display
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = CompareFiles(directory, fileName);
		int totalFiles = fileGroups.Sum(g => g.Value.Count);

		// Show the improved table summary first
		ShowFileGroupSummaryTable(fileGroups, directory, totalFiles);

		// Check if there are any groups with multiple files that can be merged
		int groupsWithMultipleFiles = fileGroups.Count(g => g.Value.Count > 1);
		if (groupsWithMultipleFiles == 0)
		{
			// Table already showed this information, so just return
			return;
		}

		// Prepare file groups for merging using the Core library
		IReadOnlyCollection<FileGroup>? coreFileGroups = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(directory, fileName);

		if (coreFileGroups == null)
		{
			AnsiConsole.MarkupLine("[yellow]No files found or insufficient unique versions to merge.[/]");
			return;
		}

		// Start iterative merge process with provided callbacks
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			coreFileGroups,
			mergeCallback,
			statusCallback,
			continueCallback);

		// Handle result
		ProgressReportingService.ReportCompletionResult(result);

		// Show detailed summary of all operations
		ProgressReportingService.ShowDetailedMergeSummary(result);
	}

	/// <summary>
	/// Shows the batch processing completion message.
	/// </summary>
	/// <param name="totalPatternsProcessed">Total patterns processed.</param>
	/// <param name="totalFilesFound">Total files found.</param>
	/// <param name="batchResult">The batch result containing merge details.</param>
	private static void ShowBatchCompletion(int totalPatternsProcessed, int totalFilesFound, BatchResult? batchResult = null)
	{
		AnsiConsole.MarkupLine($"[green]Batch processing completed![/]");
		AnsiConsole.MarkupLine($"[dim]Processed {totalPatternsProcessed} patterns, found {totalFilesFound} total files.[/]");

		// Show detailed summary for all patterns processed
		if (batchResult != null)
		{
			ShowBatchDetailedSummary(batchResult);
		}
	}

	/// <summary>
	/// Shows detailed summary for all patterns in the batch, including those without merge operations.
	/// </summary>
	/// <param name="batchResult">The batch result containing pattern results.</param>
	private static void ShowBatchDetailedSummary(BatchResult batchResult)
	{
		if (batchResult.PatternResults.Count == 0)
		{
			return; // No patterns to show
		}

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[bold cyan]{MenuNames.Output.BatchProcessingSummary}[/]");
		AnsiConsole.WriteLine();

		// Create summary table
		Table summaryTable = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Blue)
			.AddColumn("[bold]Pattern[/]")
			.AddColumn("[bold]Files Found[/]")
			.AddColumn("[bold]Unique Versions[/]")
			.AddColumn("[bold]Status[/]")
			.AddColumn("[bold]Result[/]");

		foreach (PatternResult patternResult in batchResult.PatternResults)
		{
			string status = patternResult.Success ? "[green]✓[/]" : "[red]✗[/]";
			string result = GetPatternResultDescription(patternResult);
			string displayName = GetDisplayName(patternResult);

			summaryTable.AddRow(
				$"[yellow]{displayName}[/]",
				patternResult.FilesFound.ToString(),
				patternResult.UniqueVersions.ToString(),
				status,
				result
			);
		}

		AnsiConsole.Write(summaryTable);

		// Show detailed merge summaries for patterns that had actual merge operations
		List<PatternResult> mergeResults = [.. batchResult.PatternResults
			.Where(pr => pr.MergeResult != null)];

		if (mergeResults.Count > 0)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[bold cyan]{MenuNames.Output.DetailedMergeOperations}[/]");
			AnsiConsole.WriteLine();

			foreach (PatternResult patternResult in mergeResults)
			{
				if (patternResult.MergeResult != null)
				{
					AnsiConsole.MarkupLine($"[bold yellow]Pattern: {patternResult.Pattern}[/]");
					ProgressReportingService.ShowDetailedMergeSummary(patternResult.MergeResult);
				}
			}
		}
	}

	/// <summary>
	/// Gets a descriptive result for a pattern result.
	/// </summary>
	/// <param name="patternResult">The pattern result.</param>
	/// <returns>A formatted description of the result.</returns>
	private static string GetPatternResultDescription(PatternResult patternResult)
	{
		if (!patternResult.Success)
		{
			return $"[red]{patternResult.Message}[/]";
		}

		if (patternResult.FilesFound == 0)
		{
			return "[dim]No files[/]";
		}

		if (patternResult.FilesFound == 1)
		{
			return "[green]Single file[/]";
		}

		if (patternResult.UniqueVersions == 1)
		{
			return "[green]Identical[/]";
		}

		if (patternResult.MergeResult == null)
		{
			return "[yellow]Multiple versions[/]";
		}

		return patternResult.MergeResult.IsSuccessful
			? "[green]Merged[/]"
			: "[red]Failed[/]";
	}

	/// <summary>
	/// Gets the display name for a pattern result, showing filename with pattern in parentheses if it's a glob.
	/// </summary>
	/// <param name="patternResult">The pattern result.</param>
	/// <returns>A formatted display name.</returns>
	private static string GetDisplayName(PatternResult patternResult)
	{
		// If no filename is available, fall back to pattern
		if (string.IsNullOrEmpty(patternResult.FileName))
		{
			return patternResult.Pattern;
		}

		// If pattern equals filename, it's not a glob pattern
		if (patternResult.Pattern.Equals(patternResult.FileName, StringComparison.OrdinalIgnoreCase))
		{
			return patternResult.FileName;
		}

		// Check if pattern contains glob characters
		bool isGlobPattern = patternResult.Pattern.Contains('*') ||
			patternResult.Pattern.Contains('?') ||
			patternResult.Pattern.Contains('[') ||
			patternResult.Pattern.Contains('{');

		return isGlobPattern ? $"{patternResult.FileName} ({patternResult.Pattern})" : patternResult.FileName;
	}

	// CompareFiles method removed - using base class implementation since it's identical

	/// <summary>
	/// Runs iterative merge on files in a directory.
	/// </summary>
	/// <param name="directory">The directory to search.</param>
	/// <param name="fileName">The file name pattern to match.</param>
	public override void RunIterativeMerge(string directory, string fileName) =>
		RunIterativeMergeWithCallbacks(directory, fileName, ConsoleMergeCallback, ConsoleStatusCallback, ConsoleContinueCallback);

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	public override void ListBatches()
	{
		IReadOnlyCollection<string> batchNames = AppDataBatchManager.ListBatches();
		IReadOnlyCollection<BatchConfiguration> allBatches = AppDataBatchManager.GetAllBatches();

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
				if (ProcessInteractiveMenuLoop(this))
				{
					break;
				}
			}
			catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException or IOException or ArgumentException or InvalidOperationException)
			{
				HandleInteractiveModeError(GetErrorMessage(ex));
			}
		}

		ShowGoodbyeScreen();
	}

	/// <summary>
	/// Processes a single iteration of the interactive menu loop.
	/// </summary>
	/// <param name="instance">The console application service instance.</param>
	/// <returns>True if the user chose to exit, false to continue.</returns>
	private static bool ProcessInteractiveMenuLoop(ConsoleApplicationService instance)
	{
		// Check navigation stack to determine what menu to show
		if (NavigationHistory.ShouldGoToMainMenu())
		{
			return instance.ProcessMainMenuInteraction();
		}

		// Navigate back based on navigation stack
		NavigateBasedOnStack(instance);
		return false;
	}

	/// <summary>
	/// Processes main menu interaction and returns whether user chose to exit.
	/// </summary>
	/// <returns>True if the user chose to exit, false to continue.</returns>
	private bool ProcessMainMenuInteraction()
	{
		ShowWelcomeScreen();
		MenuChoice choice = ShowMainMenu();

		if (choice == MenuChoice.Exit)
		{
			return true;
		}

		ExecuteMenuChoice(choice);
		return false;
	}

	/// <summary>
	/// Gets an appropriate error message for the given exception.
	/// </summary>
	/// <param name="ex">The exception to get a message for.</param>
	/// <returns>A formatted error message.</returns>
	private static string GetErrorMessage(Exception ex) => ex switch
	{
		DirectoryNotFoundException => $"Directory not found: {ex.Message}",
		UnauthorizedAccessException => $"Access denied: {ex.Message}",
		IOException => $"File I/O error: {ex.Message}",
		ArgumentException => $"Invalid input: {ex.Message}",
		InvalidOperationException => $"Operation error: {ex.Message}",
		_ => $"Unexpected error: {ex.Message}"
	};

	/// <summary>
	/// Handles errors that occur during interactive mode with consistent formatting and cleanup.
	/// </summary>
	/// <param name="errorMessage">The error message to display.</param>
	private static void HandleInteractiveModeError(string errorMessage)
	{
		AnsiConsole.MarkupLine($"[red]{errorMessage}[/]");
		AnsiConsole.WriteLine(PressAnyKeyMessage);
		Console.ReadKey();
		// Clear navigation on error to return to main menu
		NavigationHistory.Clear();
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
		// Create a dynamic menu with recent batch info
		Dictionary<string, MenuChoice> dynamicMenuChoices = new(MainMenuChoices);

		// Update the recent batch menu text if there's a recent batch
		string? recentBatch = GetMostRecentBatch();
		if (!string.IsNullOrEmpty(recentBatch))
		{
			// Remove the old entry and add the updated one with batch name
			dynamicMenuChoices.Remove(MenuNames.Display.RunRecentBatch);
			dynamicMenuChoices[$"{MenuNames.Display.RunRecentBatch} ({recentBatch})"] = MenuChoice.RunRecentBatch;
		}

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[bold cyan]Main Menu[/] - Select an option:")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
				.AddChoices(dynamicMenuChoices.Keys));

		return dynamicMenuChoices.TryGetValue(selection, out MenuChoice command) ? command : MenuChoice.Help;
	}

	/// <summary>
	/// Executes the selected menu choice.
	/// </summary>
	/// <param name="choice">The menu choice to execute.</param>
	private void ExecuteMenuChoice(MenuChoice choice)
	{
		InitializeMenuNavigation();
		ExecuteSpecificMenuChoice(choice);
	}

	/// <summary>
	/// Initializes navigation for menu execution.
	/// </summary>
	private static void InitializeMenuNavigation()
	{
		NavigationHistory.Clear();
		NavigationHistory.Push(MenuNames.MainMenu);
	}

	/// <summary>
	/// Executes the specific menu choice action.
	/// </summary>
	/// <param name="choice">The menu choice to execute.</param>
	private void ExecuteSpecificMenuChoice(MenuChoice choice)
	{
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
			case MenuChoice.RunRecentBatch:
				HandleRunRecentBatch(this);
				break;
			case MenuChoice.Settings:
				new SettingsMenuHandler(this).Enter();
				break;
			case MenuChoice.Help:
			case MenuChoice.Exit:
			default:
				new HelpMenuHandler(this).Enter();
				break;
		}
	}

	/// <summary>
	/// Handles running the most recently used batch configuration.
	/// </summary>
	/// <param name="instance">The console application service instance.</param>
	private static void HandleRunRecentBatch(ConsoleApplicationService instance)
	{
		string? recentBatch = GetMostRecentBatch();

		if (string.IsNullOrEmpty(recentBatch))
		{
			ShowNoRecentBatchMessage();
			return;
		}

		BatchConfiguration? selectedBatch = GetBatchConfiguration(recentBatch);
		if (selectedBatch == null)
		{
			ShowBatchNotFoundMessage(recentBatch);
			return;
		}

		string directory = GetDirectoryForBatch(selectedBatch);
		if (string.IsNullOrWhiteSpace(directory))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		ShowRunningBatchMessage(selectedBatch, recentBatch, directory);
		instance.ProcessBatch(directory, recentBatch);
		ShowCompletionMessage();
	}

	/// <summary>
	/// Shows the message when no recent batch is found.
	/// </summary>
	private static void ShowNoRecentBatchMessage()
	{
		AnsiConsole.MarkupLine("[yellow]No recent batch configurations found.[/]");
		AnsiConsole.MarkupLine("[dim]Run a batch configuration first to use this shortcut.[/]");
		Console.WriteLine();
		Console.WriteLine(PressAnyKeyMessage);
		Console.ReadKey(true);
	}

	/// <summary>
	/// Shows the message when a batch configuration is not found.
	/// </summary>
	/// <param name="batchName">The batch name that was not found.</param>
	private static void ShowBatchNotFoundMessage(string batchName)
	{
		AnsiConsole.MarkupLine($"[red]Batch configuration '{batchName}' not found.[/]");
		Console.WriteLine();
		Console.WriteLine(PressAnyKeyMessage);
		Console.ReadKey(true);
	}

	/// <summary>
	/// Gets the directory for the batch, either from configured search paths or user input.
	/// </summary>
	/// <param name="selectedBatch">The batch configuration.</param>
	/// <returns>The directory to use for the batch.</returns>
	private static string GetDirectoryForBatch(BatchConfiguration selectedBatch)
	{
		if (selectedBatch.SearchPaths.Count > 0)
		{
			ShowConfiguredSearchPaths(selectedBatch);
			return "."; // Use placeholder directory since API requires it, but it won't be used
		}

		return GetDirectoryFromUser();
	}

	/// <summary>
	/// Shows the configured search paths for the batch.
	/// </summary>
	/// <param name="selectedBatch">The batch configuration.</param>
	private static void ShowConfiguredSearchPaths(BatchConfiguration selectedBatch)
	{
		AnsiConsole.MarkupLine($"[green]Using configured search paths ({selectedBatch.SearchPaths.Count} paths)[/]");
		foreach (string searchPath in selectedBatch.SearchPaths)
		{
			AnsiConsole.MarkupLine($"  [dim]• {searchPath}[/]");
		}
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Gets the directory from user input.
	/// </summary>
	/// <returns>The directory path entered by the user.</returns>
	private static string GetDirectoryFromUser()
	{
		AnsiConsole.MarkupLine("[yellow]This batch configuration doesn't have search paths configured.[/]");
		return AppDataHistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
	}

	/// <summary>
	/// Shows the message indicating which batch is running.
	/// </summary>
	/// <param name="selectedBatch">The batch configuration.</param>
	/// <param name="batchName">The batch name.</param>
	/// <param name="directory">The directory being processed.</param>
	private static void ShowRunningBatchMessage(BatchConfiguration selectedBatch, string batchName, string directory)
	{
		string message = selectedBatch.SearchPaths.Count > 0
			? $"[cyan]Running recent batch '{batchName}' using configured search paths[/]"
			: $"[cyan]Running recent batch '{batchName}' in '{directory}'[/]";

		AnsiConsole.MarkupLine(message);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Shows the completion message for the batch operation.
	/// </summary>
	private static void ShowCompletionMessage()
	{
		AnsiConsole.MarkupLine("[green]Batch operation completed.[/]");
		Console.WriteLine();
		Console.WriteLine(PressAnyKeyMessage);
		Console.ReadKey(true);
	}

	/// <summary>
	/// Gets the most recently used batch configuration name.
	/// </summary>
	/// <returns>The name of the most recent batch, or null if none found.</returns>
	private static string? GetMostRecentBatch() => BlastMergeAppData.Get().RecentBatch?.BatchName;

	/// <summary>
	/// Shows the goodbye screen when exiting.
	/// </summary>
	private static void ShowGoodbyeScreen() => MenuDisplayService.ShowGoodbyeScreen();

	/// <summary>
	/// Navigates based on the current navigation stack.
	/// </summary>
	/// <param name="instance">The console application service instance.</param>
	private static void NavigateBasedOnStack(ConsoleApplicationService instance)
	{
		string? currentMenu = NavigationHistory.Peek();

		if (currentMenu == null)
		{
			NavigationHistory.Clear();
			return;
		}

		instance.NavigateToMenuHandler(currentMenu);
	}

	/// <summary>
	/// Routes to the appropriate menu handler based on menu name.
	/// </summary>
	/// <param name="menuName">The name of the menu to navigate to.</param>
	private void NavigateToMenuHandler(string menuName)
	{
		switch (menuName)
		{
			case MenuNames.FindFiles:
				new FindFilesMenuHandler(this).Handle();
				break;
			case MenuNames.IterativeMerge:
				new IterativeMergeMenuHandler(this).Handle();
				break;
			case MenuNames.CompareFiles:
				new CompareFilesMenuHandler(this).Handle();
				break;
			case MenuNames.BatchOperations:
				new BatchOperationsMenuHandler(this).Handle();
				break;
			case MenuNames.Settings:
				new SettingsMenuHandler(this).Handle();
				break;
			case MenuNames.Help:
				new HelpMenuHandler(this).Handle();
				break;
			default:
				NavigationHistory.Clear();
				break;
		}
	}

	/// <summary>
	/// Creates and displays a summary table of file groups with improved formatting.
	/// </summary>
	/// <param name="fileGroups">The file groups to display.</param>
	/// <param name="directory">The base directory for relative path calculation.</param>
	/// <param name="totalFiles">Total number of files found.</param>
	private static void ShowFileGroupSummaryTable(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups, string directory, int totalFiles)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);
		ArgumentNullException.ThrowIfNull(directory);

		AnsiConsole.MarkupLine($"[green]Found {totalFiles} files in {fileGroups.Count} groups:[/]");
		AnsiConsole.WriteLine();

		// Sort fileGroups by the first filename in each group for better organization
		List<KeyValuePair<string, IReadOnlyCollection<string>>> sortedFileGroups = [.. fileGroups
			.OrderBy(g => Path.GetFileName(g.Value.FirstOrDefault() ?? string.Empty))];

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Blue)
			.AddColumn(GroupColumnName)
			.AddColumn(FilesColumnName)
			.AddColumn(StatusColumnName)
			.AddColumn(FilenameColumnName)
			.AddColumn(HashColumnName);

		foreach ((KeyValuePair<string, IReadOnlyCollection<string>> group, int groupIndex) in sortedFileGroups.Select((group, index) => (group, index + 1)))
		{
			string status = group.Value.Count > 1 ? "[yellow]Multiple identical copies[/]" : "[green]Unique[/]";

			// Get unique filenames in this group
			IEnumerable<string> uniqueFilenames = group.Value
				.Select(Path.GetFileName)
				.OfType<string>()
				.Where(f => !string.IsNullOrEmpty(f))
				.Distinct()
				.OrderBy(f => f);

			string filenamesDisplay = string.Join("; ", uniqueFilenames);
			if (filenamesDisplay.Length > 50)
			{
				filenamesDisplay = filenamesDisplay[..47] + "...";
			}

			// Show first 8 characters of hash
			string shortHash = group.Key.Length > 8 ? group.Key[..8] + "..." : group.Key;

			table.AddRow(
				$"[cyan]{groupIndex}[/]",
				$"[dim]{group.Value.Count}[/]",
				status,
				$"[dim]{filenamesDisplay}[/]",
				$"[dim]{shortHash}[/]");
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	/// <summary>
	/// Console-specific callback to perform merge operation between two files.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	/// <param name="existingContent">Existing merged content.</param>
	/// <returns>Merge result or null if cancelled.</returns>
	private static MergeResult? ConsoleMergeCallback(string file1, string file2, string? existingContent)
	{
		(string leftFile, string rightFile) = DetermineFileOrder(file1, file2, existingContent);

		MergeResult? result = IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			leftFile, rightFile, existingContent, (diffBlock, context, blockNumber) => GetBlockChoice(diffBlock, context, blockNumber, leftFile, rightFile));

		if (result != null)
		{
			ProgressReportingService.ReportMergeStepSuccess();
		}

		return result;
	}

	/// <summary>
	/// Determines the order of files for merging based on existing content and change counts.
	/// </summary>
	/// <param name="file1">First file path.</param>
	/// <param name="file2">Second file path.</param>
	/// <param name="existingContent">Existing merged content.</param>
	/// <returns>A tuple with the left and right file paths in the determined order.</returns>
	private static (string leftFile, string rightFile) DetermineFileOrder(string file1, string file2, string? existingContent)
	{
		if (existingContent != null)
		{
			// For merges with existing content, keep original order
			return (file1, file2);
		}

		// For new merges, count changes to determine order (fewer changes on left)
		IReadOnlyCollection<LineDifference> differences = FileDiffer.FindDifferences(file1, file2);
		int changesInFile1 = differences.Count(d => d.LineNumber1.HasValue);
		int changesInFile2 = differences.Count(d => d.LineNumber2.HasValue);

		return changesInFile1 <= changesInFile2 ? (file1, file2) : (file2, file1);
	}

	/// <summary>
	/// Console-specific callback to report merge status.
	/// </summary>
	/// <param name="status">Current merge session status.</param>
	private static void ConsoleStatusCallback(MergeSessionStatus status) => ProgressReportingService.ReportMergeStatus(status);

	/// <summary>
	/// Console-specific callback to ask if the user wants to continue merging.
	/// Always returns true for seamless batch processing.
	/// </summary>
	/// <returns>Always true to continue processing without interruption.</returns>
	private static bool ConsoleContinueCallback() => true;

	/// <summary>
	/// Gets the user's choice for a merge block with visual conflict resolution
	/// </summary>
	/// <param name="diffBlock">The DiffPlex diff block to choose for</param>
	/// <param name="context">The context around the block</param>
	/// <param name="blockNumber">The block number being processed</param>
	/// <param name="leftFile">The left file path</param>
	/// <param name="rightFile">The right file path</param>
	/// <returns>The user's choice for the block</returns>
	private static BlockChoice GetBlockChoice(DiffPlex.Model.DiffBlock diffBlock, BlockContext context, int blockNumber, string leftFile, string rightFile)
	{
		DisplayBlockHeader(diffBlock, blockNumber);
		ShowDiffBlockDisplay(diffBlock, context, leftFile, rightFile);
		ShowMergeStatistics(diffBlock, leftFile, rightFile);
		return GetChoiceBasedOnBlockType(diffBlock, leftFile, rightFile);
	}

	/// <summary>
	/// Displays the block header with type information.
	/// </summary>
	/// <param name="diffBlock">The diff block.</param>
	/// <param name="blockNumber">The block number.</param>
	private static void DisplayBlockHeader(DiffPlex.Model.DiffBlock diffBlock, int blockNumber)
	{
		string blockType = DetermineBlockType(diffBlock);
		AnsiConsole.MarkupLine($"[yellow]{MenuNames.Output.BlockPrefix} {blockNumber} ({blockType})[/]");
	}

	/// <summary>
	/// Shows the diff block display with file comparison.
	/// </summary>
	/// <param name="diffBlock">The diff block.</param>
	/// <param name="context">The block context.</param>
	/// <param name="leftFile">The left file path.</param>
	/// <param name="rightFile">The right file path.</param>
	private static void ShowDiffBlockDisplay(DiffPlex.Model.DiffBlock diffBlock, BlockContext context, string leftFile, string rightFile)
	{
		(string leftLabel, string rightLabel) = FileDisplayService.MakeDistinguishedPaths(leftFile, rightFile);
		string[] lines1 = File.ReadAllLines(leftFile);
		string[] lines2 = File.ReadAllLines(rightFile);
		FileComparisonDisplayService.ShowDiffBlock(lines1, lines2, diffBlock, context, leftLabel, rightLabel);
	}

	/// <summary>
	/// Shows merge statistics and initiation information.
	/// </summary>
	/// <param name="diffBlock">The diff block.</param>
	/// <param name="leftFile">The left file path.</param>
	/// <param name="rightFile">The right file path.</param>
	private static void ShowMergeStatistics(DiffPlex.Model.DiffBlock diffBlock, string leftFile, string rightFile)
	{
		(int deletions, int insertions) = FileComparisonDisplayService.GetDiffBlockStatistics(diffBlock);
		ProgressReportingService.ReportMergeInitiation(leftFile, rightFile, false, deletions, insertions);
	}

	/// <summary>
	/// Gets the appropriate choice based on the block type.
	/// </summary>
	/// <param name="diffBlock">The diff block.</param>
	/// <param name="leftFile">The left file path.</param>
	/// <param name="rightFile">The right file path.</param>
	/// <returns>The user's choice for the block.</returns>
	private static BlockChoice GetChoiceBasedOnBlockType(DiffPlex.Model.DiffBlock diffBlock, string leftFile, string rightFile)
	{
		if (diffBlock.DeleteCountA > 0 && diffBlock.InsertCountB > 0)
		{
			return GetReplaceChoice(leftFile, rightFile);
		}

		if (diffBlock.InsertCountB > 0)
		{
			return GetInsertChoice(rightFile, leftFile);
		}

		if (diffBlock.DeleteCountA > 0)
		{
			return GetDeleteChoice(leftFile, rightFile);
		}

		return BlockChoice.UseVersion2;
	}

	/// <summary>
	/// Determines the block type for display purposes
	/// </summary>
	/// <param name="diffBlock">The DiffPlex diff block</param>
	/// <returns>A string describing the block type</returns>
	private static string DetermineBlockType(DiffPlex.Model.DiffBlock diffBlock)
	{
		if (diffBlock.DeleteCountA > 0 && diffBlock.InsertCountB > 0)
		{
			return "Replace";
		}

		if (diffBlock.InsertCountB > 0)
		{
			return "Insert";
		}

		if (diffBlock.DeleteCountA > 0)
		{
			return "Delete";
		}

		return "Unchanged";
	}

	/// <summary>
	/// Gets user choice for insert blocks
	/// </summary>
	/// <param name="rightFile">The right file path</param>
	/// <param name="leftFile">The left file path (for context in distinguishing labels)</param>
	/// <returns>User's choice</returns>
	private static BlockChoice GetInsertChoice(string rightFile, string leftFile)
	{
		(_, string rightLabel) = FileDisplayService.MakeDistinguishedPaths(leftFile, rightFile);
		string choice = UserInteractionService.ShowSelectionPrompt(
			$"[cyan]Content only in {rightLabel}. What to do?[/]",
			[
				"✅ Include",
				"❌ Skip"
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
		(string leftLabel, _) = FileDisplayService.MakeDistinguishedPaths(leftFile, rightFile);
		string choice = UserInteractionService.ShowSelectionPrompt(
			$"[cyan]Content only in {leftLabel}. What to do?[/]",
			[
				"✅ Keep",
				"❌ Remove"
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
		(string leftLabel, string rightLabel) = FileDisplayService.MakeDistinguishedPaths(leftFile, rightFile);
		string choice = UserInteractionService.ShowSelectionPrompt(
			"[cyan]Content differs. What to do?[/]",
			[
				$"  Use {leftLabel}",
				$"  Use {rightLabel}",
				MenuNames.Actions.UseBoth,
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
