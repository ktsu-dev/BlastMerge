// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ktsu.BlastMerge.ConsoleApp.Models;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using ktsu.FileSystemProvider;
using Spectre.Console;

/// <summary>
/// Console-specific implementation of the application service that adds UI functionality.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConsoleApplicationService class.
/// </remarks>
/// <param name="fileSystemProvider">The file system provider for dependency injection.</param>
/// <param name="fileFinder">The file finder service for dependency injection.</param>
/// <param name="fileDiffer">The file differ service for dependency injection.</param>
/// <param name="batchManager">The batch manager service.</param>
/// <param name="historyInput">The history input service.</param>
/// <param name="persistenceService">The persistence service.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
public class ConsoleApplicationService(
	IFileSystemProvider fileSystemProvider,
	FileFinder fileFinder,
	FileDiffer fileDiffer,
	AppDataBatchManager batchManager,
	AppDataHistoryInput historyInput,
	BlastMergePersistenceService persistenceService,
	ComparisonOperationsService comparisonOperations,
	SyncOperationsService syncOperations,
	FileComparisonDisplayService fileComparisonDisplayService,
	InteractiveMergeService interactiveMergeService) : ApplicationService(fileSystemProvider, fileFinder, fileDiffer)
{
	private readonly AppDataBatchManager batchManager = batchManager ?? throw new ArgumentNullException(nameof(batchManager));
	private readonly AppDataHistoryInput historyInput = historyInput ?? throw new ArgumentNullException(nameof(historyInput));
	private readonly ComparisonOperationsService comparisonOperations = comparisonOperations ?? throw new ArgumentNullException(nameof(comparisonOperations));
	private readonly SyncOperationsService syncOperations = syncOperations ?? throw new ArgumentNullException(nameof(syncOperations));
	private readonly FileComparisonDisplayService fileComparisonDisplayService = fileComparisonDisplayService ?? throw new ArgumentNullException(nameof(fileComparisonDisplayService));
	private readonly InteractiveMergeService interactiveMergeService = interactiveMergeService ?? throw new ArgumentNullException(nameof(interactiveMergeService));

	/// <summary>
	/// Gets the file differ service for access to file comparison operations.
	/// </summary>
	public new FileDiffer FileDiffer => base.FileDiffer;

	// Table column names
	private const string GroupColumnName = "Group";
	private const string FilesColumnName = "Files";
	private const string StatusColumnName = "Status";
	private const string FilenameColumnName = "Filename";
	private const string HashColumnName = "Hash";

	// Menu display text to command mappings
	private static readonly Dictionary<string, MenuChoice> MainMenuChoices = new()
	{
		[MainMenuDisplay.FindFiles] = MenuChoice.FindFiles,
		[MainMenuDisplay.CompareFiles] = MenuChoice.CompareFiles,
		[MainMenuDisplay.IterativeMerge] = MenuChoice.IterativeMerge,
		[MainMenuDisplay.BatchOperations] = MenuChoice.BatchOperations,
		[MainMenuDisplay.RunRecentBatch] = MenuChoice.RunRecentBatch,
		[MainMenuDisplay.Settings] = MenuChoice.Settings,
		[MainMenuDisplay.Help] = MenuChoice.Help,
		[MainMenuDisplay.Exit] = MenuChoice.Exit
	};

	/// <summary>
	/// Processes files in a directory with a specified filename pattern.
	/// </summary>
	/// <param name="directory">The directory to process.</param>
	/// <param name="fileName">The filename pattern to search for.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public override async Task ProcessFilesAsync(string directory, string fileName, CancellationToken cancellationToken = default)
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

		IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups = await CompareFilesAsync(directory, fileName, cancellationToken).ConfigureAwait(false);
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
	private List<string> DiscoverFiles(string directory, string fileName)
	{
		List<string> filePaths = [];
		AnsiConsole.Status()
			.Start("Discovering files...", ctx =>
			{
				IFileSystem fileSystem = ktsu.FileSystemProvider.FileSystemProvider.Current;
				IReadOnlyCollection<string> foundFiles = FileFinder.FindFiles(directory, fileName, [], path =>
				{
					ctx.Status($"Discovering files... Found: {fileSystem.Path.GetFileName(path)}");
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
			[ActionDisplay.ViewDetailedFileList] = ProcessFileActionChoice.ViewDetailedFileList,
			[ActionDisplay.ShowDifferences] = ProcessFileActionChoice.ShowDifferences,
			[ActionDisplay.RunIterativeMergeOnDuplicates] = ProcessFileActionChoice.RunIterativeMergeOnDuplicates,
			[ActionDisplay.SyncFiles] = ProcessFileActionChoice.SyncFiles,
			[ActionDisplay.ReturnToMainMenu] = ProcessFileActionChoice.ReturnToMainMenu
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
				interactiveMergeService.PerformIterativeMerge(fileGroups);
				break;
			case ProcessFileActionChoice.SyncFiles:
				syncOperations.OfferSyncOptions(fileGroups);
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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public override async Task ProcessBatchAsync(string directory, string batchName, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(directory);
		ArgumentNullException.ThrowIfNull(batchName);
		ValidateDirectoryExists(directory);

		BatchConfiguration? batch = await GetBatchConfigurationAsync(batchName, cancellationToken).ConfigureAwait(false);
		if (batch == null)
		{
			return;
		}

		// Record this batch as recently used
		await batchManager.RecordBatchUsageAsync(batchName, cancellationToken).ConfigureAwait(false);

		ShowBatchHeader(batch, directory);
		(int totalPatternsProcessed, int totalFilesFound, BatchResult? batchResult) = ProcessAllPatterns(directory, batch);
		ShowBatchCompletion(totalPatternsProcessed, totalFilesFound, batchResult);
	}

	/// <summary>
	/// Gets the batch configuration by name.
	/// </summary>
	/// <param name="batchName">The name of the batch configuration.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The batch configuration or null if not found.</returns>
	private async Task<BatchConfiguration?> GetBatchConfigurationAsync(string batchName, CancellationToken cancellationToken = default)
	{
		IReadOnlyCollection<BatchConfiguration> allBatches = await batchManager.GetAllBatchesAsync(cancellationToken).ConfigureAwait(false);
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
			() => AlwaysContinue,
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
		AnsiConsole.MarkupLine($"[bold cyan]{OutputDisplay.BatchProcessingSummary}[/]");
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
			AnsiConsole.MarkupLine($"[bold cyan]{OutputDisplay.DetailedMergeOperations}[/]");
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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public override async Task RunIterativeMergeAsync(string directory, string fileName, CancellationToken cancellationToken = default) => await Task.Run(() => RunIterativeMergeWithCallbacks(directory, fileName, ConsoleMergeCallback, ConsoleStatusCallback, () => AlwaysContinue), cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Lists all available batch configurations.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public override async Task ListBatchesAsync(CancellationToken cancellationToken = default)
	{
		IReadOnlyCollection<string> batchNames = await batchManager.ListBatchesAsync(cancellationToken).ConfigureAwait(false);
		IReadOnlyCollection<BatchConfiguration> allBatches = await batchManager.GetAllBatchesAsync(cancellationToken).ConfigureAwait(false);

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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public override async Task StartInteractiveModeAsync(CancellationToken cancellationToken = default)
	{
		while (true)
		{
			try
			{
				if (await ProcessInteractiveMenuLoopAsync(this, cancellationToken).ConfigureAwait(false))
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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the user chose to exit, false to continue.</returns>
	private static async Task<bool> ProcessInteractiveMenuLoopAsync(ConsoleApplicationService instance, CancellationToken cancellationToken = default)
	{
		// Check navigation stack to determine what menu to show
		if (NavigationHistory.ShouldGoToMainMenu())
		{
			return await instance.ProcessMainMenuInteractionAsync(cancellationToken).ConfigureAwait(false);
		}

		// Navigate back based on navigation stack
		await NavigateBasedOnStackAsync(instance, cancellationToken).ConfigureAwait(false);
		return false;
	}

	/// <summary>
	/// Processes main menu interaction and returns whether user chose to exit.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the user chose to exit, false to continue.</returns>
	private async Task<bool> ProcessMainMenuInteractionAsync(CancellationToken cancellationToken = default)
	{
		ShowWelcomeScreen();
		MenuChoice choice = await ShowMainMenuAsync(cancellationToken).ConfigureAwait(false);

		if (choice == MenuChoice.Exit)
		{
			return true;
		}

		await ExecuteMenuChoiceAsync(choice, cancellationToken).ConfigureAwait(false);
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
		AnsiConsole.WriteLine(CommonMessages.PressAnyKeyToContinue);
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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The selected menu choice.</returns>
	private async Task<MenuChoice> ShowMainMenuAsync(CancellationToken cancellationToken = default)
	{
		// Create a dynamic menu with recent batch info
		Dictionary<string, MenuChoice> dynamicMenuChoices = new(MainMenuChoices);

		// Update the recent batch menu text if there's a recent batch
		string? recentBatch = await GetMostRecentBatchAsync(cancellationToken).ConfigureAwait(false);
		if (!string.IsNullOrEmpty(recentBatch))
		{
			// Remove the old entry and add the updated one with batch name
			dynamicMenuChoices.Remove(MainMenuDisplay.RunRecentBatch);
			dynamicMenuChoices[$"{MainMenuDisplay.RunRecentBatch} ({recentBatch})"] = MenuChoice.RunRecentBatch;
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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task ExecuteMenuChoiceAsync(MenuChoice choice, CancellationToken cancellationToken = default)
	{
		InitializeMenuNavigation();
		await ExecuteSpecificMenuChoiceAsync(choice, cancellationToken).ConfigureAwait(false);
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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task ExecuteSpecificMenuChoiceAsync(MenuChoice choice, CancellationToken cancellationToken = default)
	{
		switch (choice)
		{
			case MenuChoice.FindFiles:
				await Task.Run(() => new FindFilesMenuHandler(this, FileFinder, historyInput).Enter(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuChoice.IterativeMerge:
				await Task.Run(() => new IterativeMergeMenuHandler(this, historyInput).Enter(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuChoice.CompareFiles:
				await Task.Run(() => new CompareFilesMenuHandler(this, historyInput, comparisonOperations).Enter(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuChoice.BatchOperations:
				await Task.Run(() => new BatchOperationsMenuHandler(this, batchManager, historyInput).Enter(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuChoice.RunRecentBatch:
				await HandleRunRecentBatchAsync(this, cancellationToken).ConfigureAwait(false);
				break;
			case MenuChoice.Settings:
				await Task.Run(() => new SettingsMenuHandler(this, historyInput).Enter(), cancellationToken).ConfigureAwait(false);
				break;
			default:
				new HelpMenuHandler(this).Enter();
				break;
		}
	}

	/// <summary>
	/// Handles running the most recently used batch configuration.
	/// </summary>
	/// <param name="instance">The console application service instance.</param>
	/// <param name="cancellationToken"></param>
	private static async Task HandleRunRecentBatchAsync(ConsoleApplicationService instance, CancellationToken cancellationToken = default)
	{
		string? recentBatch = await instance.GetMostRecentBatchAsync(cancellationToken).ConfigureAwait(false);

		if (string.IsNullOrEmpty(recentBatch))
		{
			ShowNoRecentBatchMessage();
			return;
		}

		BatchConfiguration? selectedBatch = await instance.GetBatchConfigurationAsync(recentBatch, cancellationToken).ConfigureAwait(false);
		if (selectedBatch == null)
		{
			ShowBatchNotFoundMessage(recentBatch);
			return;
		}

		string directory = await GetDirectoryForBatchAsync(selectedBatch, instance, cancellationToken).ConfigureAwait(false);
		if (string.IsNullOrWhiteSpace(directory))
		{
			AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
			return;
		}

		ShowRunningBatchMessage(selectedBatch, recentBatch, directory);
		await instance.ProcessBatchAsync(directory, recentBatch, cancellationToken).ConfigureAwait(false);
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
		Console.WriteLine(CommonMessages.PressAnyKeyToContinue);
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
		Console.WriteLine(CommonMessages.PressAnyKeyToContinue);
		Console.ReadKey(true);
	}

	/// <summary>
	/// Gets the directory for the batch, either from configured search paths or user input.
	/// </summary>
	/// <param name="selectedBatch">The batch configuration.</param>
	/// <param name="instance">The console application service instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The directory to use for the batch.</returns>
	private static async Task<string> GetDirectoryForBatchAsync(BatchConfiguration selectedBatch, ConsoleApplicationService instance, CancellationToken cancellationToken = default)
	{
		if (selectedBatch.SearchPaths.Count > 0)
		{
			ShowConfiguredSearchPaths(selectedBatch);
			return "."; // Use placeholder directory since API requires it, but it won't be used
		}

		return await GetDirectoryFromUserAsync(instance, cancellationToken).ConfigureAwait(false);
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
	/// <param name="instance">The console application service instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The directory path entered by the user.</returns>
	private static async Task<string> GetDirectoryFromUserAsync(ConsoleApplicationService instance, CancellationToken cancellationToken = default)
	{
		AnsiConsole.MarkupLine("[yellow]This batch configuration doesn't have search paths configured.[/]");
		return await instance.historyInput.AskWithHistoryAsync("[cyan]Enter directory path[/]", cancellationToken).ConfigureAwait(false);
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
		Console.WriteLine(CommonMessages.PressAnyKeyToContinue);
		Console.ReadKey(true);
	}

	/// <summary>
	/// Gets the most recently used batch configuration name.
	/// </summary>
	/// <returns>The name of the most recent batch, or null if none found.</returns>
	private async Task<string?> GetMostRecentBatchAsync(CancellationToken cancellationToken = default) => await batchManager.GetMostRecentBatchAsync(cancellationToken).ConfigureAwait(false);

	/// <summary>
	/// Shows the goodbye screen when exiting.
	/// </summary>
	private static void ShowGoodbyeScreen() => MenuDisplayService.ShowGoodbyeScreen();

	/// <summary>
	/// Navigates based on the current navigation stack.
	/// </summary>
	/// <param name="instance">The console application service instance.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private static async Task NavigateBasedOnStackAsync(ConsoleApplicationService instance, CancellationToken cancellationToken = default)
	{
		string? currentMenu = NavigationHistory.Peek();

		if (currentMenu == null)
		{
			NavigationHistory.Clear();
			return;
		}

		await instance.NavigateToMenuHandlerAsync(currentMenu, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Routes to the appropriate menu handler based on menu name.
	/// </summary>
	/// <param name="menuName">The name of the menu to navigate to.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	private async Task NavigateToMenuHandlerAsync(string menuName, CancellationToken cancellationToken = default)
	{
		switch (menuName)
		{
			case MenuNames.FindFiles:
				await Task.Run(() => new FindFilesMenuHandler(this, FileFinder, historyInput).Handle(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuNames.IterativeMerge:
				await Task.Run(() => new IterativeMergeMenuHandler(this, historyInput).Handle(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuNames.CompareFiles:
				await Task.Run(() => new CompareFilesMenuHandler(this, historyInput, comparisonOperations).Handle(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuNames.BatchOperations:
				await Task.Run(() => new BatchOperationsMenuHandler(this, batchManager, historyInput).Handle(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuNames.Settings:
				await Task.Run(() => new SettingsMenuHandler(this, historyInput).Handle(), cancellationToken).ConfigureAwait(false);
				break;
			case MenuNames.Help:
				await Task.Run(() => new HelpMenuHandler(this).Handle(), cancellationToken).ConfigureAwait(false);
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

		// First, show the complete file comparison and ask what the user wants to do
		(bool userCancelled, MergeResult? quickChoiceResult) = ShowFullFileComparisonAndAskForChoice(leftFile, rightFile, existingContent);
		if (userCancelled)
		{
			return null; // User cancelled
		}

		if (quickChoiceResult != null)
		{
			// User chose to take one file entirely
			ProgressReportingService.ReportMergeStepSuccess();
			return quickChoiceResult;
		}

		// User chose piecewise merge, proceed with block-by-block resolution
		MergeResult? result = IterativeMergeOrchestrator.PerformMergeWithConflictResolution(
			leftFile, rightFile, existingContent,
			(diffBlock, context, blockNumber) => GetBlockChoice(diffBlock, context, blockNumber, leftFile, rightFile));

		if (result != null)
		{
			ProgressReportingService.ReportMergeStepSuccess();
		}

		return result;
	}

	/// <summary>
	/// Shows the complete difference between two files and asks the user for their merge strategy choice.
	/// </summary>
	/// <param name="leftFile">The left file path.</param>
	/// <param name="rightFile">The right file path.</param>
	/// <param name="existingContent">Existing merged content.</param>
	/// <returns>Tuple indicating if user cancelled and MergeResult if user chose to take one file entirely.</returns>
	private static (bool userCancelled, MergeResult? result) ShowFullFileComparisonAndAskForChoice(string leftFile, string rightFile, string? existingContent)
	{
		// Show header
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[yellow]═══ Starting Iterative Merge ═══[/]");
		(string leftLabel, string rightLabel) = FileDisplayService.MakeDistinguishedPaths(leftFile, rightFile);
		AnsiConsole.MarkupLine($"[cyan]Comparing:[/] [green]{leftLabel}[/] [cyan]vs[/] [green]{rightLabel}[/]");
		AnsiConsole.WriteLine();

		// Show the complete file comparison
		if (existingContent != null)
		{
			AnsiConsole.MarkupLine("[yellow]Note: This is a continuation merge with existing content.[/]");
			AnsiConsole.WriteLine();
		}
		else
		{
			// Show side-by-side diff of the entire files
			FileComparisonDisplayService.ShowSideBySideDiff(leftFile, rightFile);
			AnsiConsole.WriteLine();
		}

		// Present the user with options
		string[] choices = existingContent != null
			? [
				$"📄 Take {rightLabel} entirely (replace existing content)",
				"🔧 Continue with piecewise merge",
				"❌ Cancel merge"
			]
			: [
				$"📄 Take {leftLabel} entirely",
				$"📄 Take {rightLabel} entirely",
				"🔧 Proceed with piecewise merge",
				"❌ Cancel merge"
			];

		string choice = UserInteractionService.ShowSelectionPrompt(
			"[cyan]How would you like to merge these files?[/]",
			choices);

		// Handle the user's choice
		if (choice.Contains("Cancel"))
		{
			return (true, null); // User cancelled
		}

		if (choice.Contains("piecewise") || choice.Contains("Continue"))
		{
			AnsiConsole.MarkupLine("[yellow]Proceeding with block-by-block merge...[/]");
			AnsiConsole.WriteLine();
			return (false, null); // Continue with piecewise merge
		}

		// User chose to take one file entirely
		if (existingContent != null)
		{
			// For existing content merges, only option is to take the new file
			string[] rightLines = File.ReadAllLines(rightFile);
			return (false, new MergeResult(rightLines, []));
		}
		else
		{
			// User chose to take one of the original files entirely
			if (choice.Contains(leftLabel))
			{
				string[] leftLines = File.ReadAllLines(leftFile);
				AnsiConsole.MarkupLine($"[green]✅ Taking {leftLabel} entirely.[/]");
				return (false, new MergeResult(leftLines, []));
			}
			else if (choice.Contains(rightLabel))
			{
				string[] rightLines = File.ReadAllLines(rightFile);
				AnsiConsole.MarkupLine($"[green]✅ Taking {rightLabel} entirely.[/]");
				return (false, new MergeResult(rightLines, []));
			}
		}

		// Fallback to piecewise merge
		return (false, null);
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
	/// Constant indicating to always continue processing without interruption for seamless batch processing.
	/// </summary>
	private const bool AlwaysContinue = true;

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
		AnsiConsole.MarkupLine($"[yellow]{OutputDisplay.BlockPrefix} {blockNumber} ({blockType})[/]");
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
				ActionDisplay.UseBoth,
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
