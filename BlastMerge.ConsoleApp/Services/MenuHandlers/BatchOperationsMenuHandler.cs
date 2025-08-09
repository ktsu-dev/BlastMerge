// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using System.Collections.ObjectModel;
using System.Text.Json;
using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Models;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Contracts;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using ktsu.Extensions;
using Spectre.Console;

/// <summary>
/// Represents the available batch management choices.
/// </summary>
internal enum BatchManagementChoice
{
	List,
	Create,
	View,
	Edit,
	Duplicate,
	Delete,
	Export,
	Import,
	Back
}

/// <summary>
/// Menu handler for batch operations.
/// </summary>
public class BatchOperationsMenuHandler(
	ApplicationService applicationService,
	IAppDataService appDataService,
	IInputHistoryService inputHistoryService)
	: BaseMenuHandler
{

	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => MenuNames.BatchOperations;

	private static JsonSerializerOptions JsonSerializerOptions { get; } = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	/// <summary>
	/// Handles batch operations.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle(MenuNames.BatchOperations);

		Dictionary<string, BatchOperationChoice> batchOperationChoices = new()
		{
			[BatchOperationsDisplay.RunBatchConfiguration] = BatchOperationChoice.RunBatchConfiguration,
			[BatchOperationsDisplay.ManageBatchConfigurations] = BatchOperationChoice.ManageBatchConfigurations,
			[GetBackMenuText()] = BatchOperationChoice.BackToMainMenu
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select batch operation:")
				.AddChoices(batchOperationChoices.Keys));

		if (batchOperationChoices.TryGetValue(selection, out BatchOperationChoice choice))
		{
			switch (choice)
			{
				case BatchOperationChoice.ManageBatchConfigurations:
					HandleManageBatchConfigurations();
					break;
				case BatchOperationChoice.RunBatchConfiguration:
					HandleRunBatch();
					break;
				case BatchOperationChoice.BackToMainMenu:
					GoBack();
					break;
				default:
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Handles the batch management submenu.
	/// </summary>
	private void HandleManageBatchConfigurations()
	{
		while (true)
		{
			BatchManagementChoice choice = GetBatchManagementChoice();
			if (!ExecuteBatchManagementChoice(choice))
			{
				return;
			}
		}
	}

	/// <summary>
	/// Gets the user's batch management choice.
	/// </summary>
	/// <returns>The selected batch management choice.</returns>
	private static BatchManagementChoice GetBatchManagementChoice()
	{
		ShowMenuTitle("Batch Management");

		Dictionary<string, BatchManagementChoice> managementChoices = new()
		{
			["📋 List Batches"] = BatchManagementChoice.List,
			["➕ Create New Batch"] = BatchManagementChoice.Create,
			["👁️ View Batch Details"] = BatchManagementChoice.View,
			["✏️ Edit Batch"] = BatchManagementChoice.Edit,
			["📄 Duplicate Batch"] = BatchManagementChoice.Duplicate,
			["🗑️ Delete Batch"] = BatchManagementChoice.Delete,
			["📤 Export Batches"] = BatchManagementChoice.Export,
			["📥 Import Batches"] = BatchManagementChoice.Import,
			["🔙 Back to Batch Operations"] = BatchManagementChoice.Back
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Select batch management operation:[/]")
				.PageSize(10)
				.AddChoices(managementChoices.Keys));

		return managementChoices.TryGetValue(selection, out BatchManagementChoice choice) ? choice : BatchManagementChoice.Back;
	}

	/// <summary>
	/// Executes the selected batch management choice.
	/// </summary>
	/// <param name="choice">The batch management choice to execute.</param>
	/// <returns>True to continue, false to exit the loop.</returns>
	private bool ExecuteBatchManagementChoice(BatchManagementChoice choice)
	{
		switch (choice)
		{
			case BatchManagementChoice.List:
				HandleListBatches();
				return true;
			case BatchManagementChoice.Create:
				HandleCreateNewBatch();
				return true;
			case BatchManagementChoice.View:
				HandleViewBatch();
				return true;
			case BatchManagementChoice.Edit:
				HandleEditBatch();
				return true;
			case BatchManagementChoice.Duplicate:
				HandleDuplicateBatch();
				return true;
			case BatchManagementChoice.Delete:
				HandleDeleteBatch();
				return true;
			case BatchManagementChoice.Export:
				HandleExportBatches();
				return true;
			case BatchManagementChoice.Import:
				HandleImportBatches();
				return true;
			case BatchManagementChoice.Back:
			default:
				return false;
		}
	}

	/// <summary>
	/// Handles listing available batch configurations.
	/// </summary>
	private void HandleListBatches()
	{
		ShowMenuTitle("Available Batch Configurations");

		Dictionary<string, BatchConfiguration>.ValueCollection allBatches = appDataService.AppData.BatchConfigurations.Values;

		if (allBatches.Count == 0)
		{
			ShowWarning("No batch configurations found.");
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
					$"[yellow]{batch.FilePatterns.Count}[/]");
			}

			AnsiConsole.Write(table);
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Handles creating a new batch configuration.
	/// </summary>
	private void HandleCreateNewBatch()
	{
		ShowMenuTitle("Create New Batch Configuration");

		string? batchName = GetValidBatchName();
		if (batchName == null)
		{
			return;
		}

		BatchConfiguration newBatch = GatherBatchConfigurationData();
		newBatch.Name = batchName;
		if (newBatch.FilePatterns.Count == 0)
		{
			ShowWarning("At least one file pattern is required.");
			return;
		}

		SaveAndDisplayBatchResult(newBatch);
		WaitForKeyPress();
	}

	/// <summary>
	/// Gets a valid batch name from the user.
	/// </summary>
	/// <returns>The batch name, or null if cancelled.</returns>
	private string? GetValidBatchName()
	{
		string batchName = inputHistoryService.AskWithHistory("[cyan]Enter batch name[/]");
		if (string.IsNullOrWhiteSpace(batchName))
		{
			ShowWarning(OperationCancelledMessage);
			return null;
		}

		if (appDataService.AppData.BatchConfigurations.ContainsKey(batchName))
		{
			bool overwrite = AnsiConsole.Confirm($"[yellow]A batch named '{batchName}' already exists. Overwrite it?[/]");
			if (!overwrite)
			{
				ShowWarning(OperationCancelledMessage);
				return null;
			}
		}

		return batchName;
	}

	/// <summary>
	/// Gathers all batch configuration data from the user.
	/// </summary>
	/// <returns>The gathered batch configuration data.</returns>
	private BatchConfiguration GatherBatchConfigurationData()
	{
		string description = inputHistoryService.AskWithHistory("[cyan]Enter description (optional)[/]");
		Collection<string> patterns = GatherFilePatterns();
		Collection<string> searchPaths = GatherSearchPaths();
		Collection<string> exclusionPatterns = GatherExclusionPatterns();

		bool skipEmptyPatterns = AnsiConsole.Confirm("[cyan]Skip patterns that don't find any files?[/]", true);
		bool promptBeforeEachPattern = AnsiConsole.Confirm("[cyan]Prompt before processing each pattern?[/]", false);

		return new BatchConfiguration
		{
			FilePatterns = patterns,
			SearchPaths = searchPaths,
			PathExclusionPatterns = exclusionPatterns,
		};
	}

	/// <summary>
	/// Gathers file patterns from the user.
	/// </summary>
	/// <returns>List of file patterns.</returns>
	private Collection<string> GatherFilePatterns()
	{
		Collection<string> patterns = [];
		AnsiConsole.MarkupLine("[cyan]Enter file patterns (one per line, empty line to finish):[/]");
		AnsiConsole.MarkupLine("[dim]Examples: *.txt, .gitignore, README.md, **/*.cs[/]");

		while (true)
		{
			string pattern = inputHistoryService.AskWithHistory($"[cyan]Pattern {patterns.Count + 1}[/]");
			if (string.IsNullOrWhiteSpace(pattern))
			{
				break;
			}
			patterns.Add(pattern.Trim());
		}

		return patterns;
	}

	/// <summary>
	/// Gathers search paths from the user.
	/// </summary>
	/// <returns>List of search paths.</returns>
	private Collection<string> GatherSearchPaths()
	{
		Collection<string> searchPaths = [];
		bool addSearchPaths = AnsiConsole.Confirm("[cyan]Add custom search paths? (If no, uses the directory provided at runtime)[/]", false);

		if (addSearchPaths)
		{
			AnsiConsole.MarkupLine("[cyan]Enter search paths (one per line, empty line to finish):[/]");
			AnsiConsole.MarkupLine("[dim]Examples: C:\\Projects, /home/user/repos, ../other-projects[/]");

			while (true)
			{
				string searchPath = inputHistoryService.AskWithHistory($"[cyan]Search Path {searchPaths.Count + 1}[/]");
				if (string.IsNullOrWhiteSpace(searchPath))
				{
					break;
				}
				searchPaths.Add(searchPath.Trim());
			}
		}

		return searchPaths;
	}

	/// <summary>
	/// Gathers exclusion patterns from the user.
	/// </summary>
	/// <returns>List of exclusion patterns.</returns>
	private Collection<string> GatherExclusionPatterns()
	{
		Collection<string> exclusionPatterns = [];
		bool addExclusions = AnsiConsole.Confirm("[cyan]Add path exclusion patterns?[/]", false);

		if (addExclusions)
		{
			AnsiConsole.MarkupLine("[cyan]Enter path exclusion patterns (one per line, empty line to finish):[/]");
			AnsiConsole.MarkupLine("[dim]Examples: */node_modules/*, */bin/*, */obj/*, .git/*, temp*[/]");

			while (true)
			{
				string exclusionPattern = inputHistoryService.AskWithHistory($"[cyan]Exclusion Pattern {exclusionPatterns.Count + 1}[/]");
				if (string.IsNullOrWhiteSpace(exclusionPattern))
				{
					break;
				}
				exclusionPatterns.Add(exclusionPattern.Trim());
			}
		}

		return exclusionPatterns;
	}

	/// <summary>
	/// Saves the batch configuration and displays the result.
	/// </summary>
	/// <param name="batch">The batch configuration to save.</param>
	private void SaveAndDisplayBatchResult(BatchConfiguration batch)
	{
		appDataService.AppData.BatchConfigurations[batch.Name] = batch;
		appDataService.SaveAsync().Wait();
		ShowSuccess($"Batch configuration '{batch.Name}' saved successfully!");
		DisplayBatchSummary(batch);
	}

	/// <summary>
	/// Displays a summary of the batch configuration.
	/// </summary>
	/// <param name="batch">The batch configuration to display.</param>
	private static void DisplayBatchSummary(BatchConfiguration batch)
	{
		AnsiConsole.MarkupLine($"\n[bold]Batch Summary:[/]");
		AnsiConsole.MarkupLine($"[cyan]Name:[/] {batch.Name}");
		AnsiConsole.MarkupLine($"[cyan]Patterns:[/] {batch.FilePatterns.Count}");
	}

	/// <summary>
	/// Handles running a batch configuration.
	/// </summary>
	private void HandleRunBatch()
	{
		ShowMenuTitle("Run Batch Configuration");

		Dictionary<string, BatchConfiguration>.ValueCollection allBatches = appDataService.AppData.BatchConfigurations.Values;

		if (allBatches.Count == 0)
		{
			ShowWarning("No batch configurations available.");
			WaitForKeyPress();
			return;
		}

		string batchName = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select batch configuration:")
				.AddChoices(allBatches.Select(b => b.Name)));

		// Load the selected batch to check if it has search paths configured
		if (!appDataService.AppData.BatchConfigurations.TryGetValue(batchName, out BatchConfiguration? batchConfiguration))
		{
			ShowError($"Batch configuration '{batchName}' not found.");
			WaitForKeyPress();
			return;
		}

		string directory;
		if (batchConfiguration.SearchPaths.Count > 0)
		{
			// Batch has search paths configured, no need to ask for directory
			AnsiConsole.MarkupLine($"[green]Using configured search paths ({batchConfiguration.SearchPaths.Count} paths)[/]");
			foreach (string searchPath in batchConfiguration.SearchPaths)
			{
				AnsiConsole.MarkupLine($"  [dim]• {searchPath}[/]");
			}
			AnsiConsole.WriteLine();

			// Use a placeholder directory since the API still requires it, but it won't be used
			directory = ".";
		}
		else
		{
			// No search paths configured, ask for directory
			AnsiConsole.MarkupLine("[yellow]This batch configuration doesn't have search paths configured.[/]");
			directory = inputHistoryService.AskWithHistory("[cyan]Enter directory path[/]");
			if (string.IsNullOrWhiteSpace(directory))
			{
				ShowWarning(OperationCancelledMessage);
				return;
			}
		}

		if (batchConfiguration.SearchPaths.Count > 0)
		{
			AnsiConsole.MarkupLine($"[cyan]Processing batch configuration '[yellow]{batchName}[/]' using configured search paths[/]");
		}
		else
		{
			AnsiConsole.MarkupLine($"[cyan]Processing batch configuration '[yellow]{batchName}[/]' in '[yellow]{directory}[/]'[/]");
		}
		AnsiConsole.WriteLine();

		applicationService.ProcessBatch(directory, batchName);

		ShowSuccess("Batch operation completed.");
		WaitForKeyPress();
	}

	/// <summary>
	/// Views detailed information about a batch configuration.
	/// </summary>
	private void HandleViewBatch()
	{
		BatchConfiguration? batch = SelectBatch("View Batch Details");
		if (batch == null)
		{
			return;
		}

		ShowMenuTitle($"Batch Details: {batch.Name}");

		Panel panel = new($"""
			[cyan]Name:[/] {batch.Name}
			[cyan]Patterns:[/] {batch.FilePatterns.Count}
			[cyan]Search Paths:[/] {batch.SearchPaths.Count}
			[cyan]Exclusion Patterns:[/] {batch.PathExclusionPatterns.Count}
			""")
		{
			Header = new PanelHeader("[bold]Batch Summary[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = new Style(Color.Green)
		};

		AnsiConsole.Write(panel);

		// Show file patterns in detail
		if (batch.FilePatterns.Count > 0)
		{
			AnsiConsole.MarkupLine("\n[bold cyan]File Patterns:[/]");
			Table patternTable = new Table()
				.Border(TableBorder.Rounded)
				.BorderColor(Color.Blue)
				.AddColumn("#")
				.AddColumn("Pattern");

			batch.FilePatterns.WithIndex().ForEach(item =>
				patternTable.AddRow($"{item.index + 1}", $"[yellow]{item.item}[/]"));

			AnsiConsole.Write(patternTable);
		}

		// Show search paths in detail
		if (batch.SearchPaths.Count > 0)
		{
			AnsiConsole.MarkupLine("\n[bold cyan]Search Paths:[/]");
			Table searchPathTable = new Table()
				.Border(TableBorder.Rounded)
				.BorderColor(Color.Green)
				.AddColumn("#")
				.AddColumn("Path");

			batch.SearchPaths.WithIndex().ForEach(item =>
				searchPathTable.AddRow($"{item.index + 1}", $"[green]{item.item}[/]"));

			AnsiConsole.Write(searchPathTable);
		}
		else
		{
			AnsiConsole.MarkupLine("\n[dim]No custom search paths configured (uses runtime directory)[/]");
		}

		// Show exclusion patterns in detail
		if (batch.PathExclusionPatterns.Count > 0)
		{
			AnsiConsole.MarkupLine("\n[bold cyan]Path Exclusion Patterns:[/]");
			Table exclusionTable = new Table()
				.Border(TableBorder.Rounded)
				.BorderColor(Color.Red)
				.AddColumn("#")
				.AddColumn("Pattern");

			batch.PathExclusionPatterns.WithIndex().ForEach(item =>
				exclusionTable.AddRow($"{item.index + 1}", $"[red]{item.item}[/]"));

			AnsiConsole.Write(exclusionTable);
		}
		else
		{
			AnsiConsole.MarkupLine("\n[dim]No path exclusion patterns configured[/]");
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Edits an existing batch configuration.
	/// </summary>
	private void HandleEditBatch()
	{
		BatchConfiguration? batch = SelectBatch("Edit Batch Configuration");
		if (batch == null)
		{
			return;
		}

		ShowMenuTitle($"Edit Batch: {batch.Name}");

		BatchConfiguration updatedBatch = GatherEditBatchData(batch);
		SaveAndDisplayBatchResult(updatedBatch);
		WaitForKeyPress();
	}

	/// <summary>
	/// Gathers all edit data from the user for the batch configuration.
	/// </summary>
	/// <param name="batch">The batch configuration being edited.</param>
	/// <returns>The gathered edit data.</returns>
	private BatchConfiguration GatherEditBatchData(BatchConfiguration batch)
	{
		Collection<string> patterns = HandlePatternEditing(batch);
		Collection<string> searchPaths = HandleSearchPathEditing(batch);
		Collection<string> exclusionPatterns = HandleExclusionPatternEditing(batch);

		return new BatchConfiguration
		{
			FilePatterns = patterns,
			SearchPaths = searchPaths,
			PathExclusionPatterns = exclusionPatterns,
		};
	}

	/// <summary>
	/// Handles editing of file patterns.
	/// </summary>
	/// <param name="batch">The batch configuration being edited.</param>
	/// <returns>Updated list of patterns.</returns>
	private Collection<string> HandlePatternEditing(BatchConfiguration batch)
	{
		Collection<string> patterns = [.. batch.FilePatterns];

		// Show current patterns
		AnsiConsole.MarkupLine($"\n[cyan]Current patterns ({batch.FilePatterns.Count}):[/]");
		batch.FilePatterns.WithIndex().ForEach(item =>
			AnsiConsole.MarkupLine($"  {item.index + 1}. [yellow]{item.item}[/]"));

		bool modifyPatterns = AnsiConsole.Confirm("[cyan]Modify file patterns?[/]");

		if (modifyPatterns)
		{
			patterns.Clear();
			AnsiConsole.MarkupLine("[cyan]Enter new file patterns (one per line, empty line to finish):[/]");

			while (true)
			{
				string pattern = inputHistoryService.AskWithHistory($"[cyan]Pattern {patterns.Count + 1}[/]");
				if (string.IsNullOrWhiteSpace(pattern))
				{
					break;
				}
				patterns.Add(pattern.Trim());
			}

			if (patterns.Count == 0)
			{
				ShowWarning("At least one file pattern is required. Keeping original patterns.");
				patterns = [.. batch.FilePatterns];
			}
		}

		return patterns;
	}

	/// <summary>
	/// Handles editing of search paths.
	/// </summary>
	/// <param name="batch">The batch configuration being edited.</param>
	/// <returns>Updated list of search paths.</returns>
	private Collection<string> HandleSearchPathEditing(BatchConfiguration batch)
	{
		Collection<string> searchPaths = [.. batch.SearchPaths];

		// Handle search paths
		AnsiConsole.MarkupLine($"\n[cyan]Current search paths ({batch.SearchPaths.Count}):[/]");
		if (batch.SearchPaths.Count == 0)
		{
			AnsiConsole.MarkupLine("  [dim]None (uses runtime directory)[/]");
		}
		else
		{
			batch.SearchPaths.WithIndex().ForEach(item =>
				AnsiConsole.MarkupLine($"  {item.index + 1}. [green]{item.item}[/]"));
		}

		bool modifySearchPaths = AnsiConsole.Confirm("[cyan]Modify search paths?[/]");

		if (modifySearchPaths)
		{
			searchPaths.Clear();
			AnsiConsole.MarkupLine("[cyan]Enter new search paths (one per line, empty line to finish):[/]");
			AnsiConsole.MarkupLine("[dim]Examples: C:\\Projects, /home/user/repos, ../other-projects[/]");

			while (true)
			{
				string searchPath = inputHistoryService.AskWithHistory($"[cyan]Search Path {searchPaths.Count + 1}[/]");
				if (string.IsNullOrWhiteSpace(searchPath))
				{
					break;
				}
				searchPaths.Add(searchPath.Trim());
			}
		}

		return searchPaths;
	}

	/// <summary>
	/// Handles editing of exclusion patterns.
	/// </summary>
	/// <param name="batch">The batch configuration being edited.</param>
	/// <returns>Updated list of exclusion patterns.</returns>
	private Collection<string> HandleExclusionPatternEditing(BatchConfiguration batch)
	{
		Collection<string> exclusionPatterns = [.. batch.PathExclusionPatterns];

		// Handle exclusion patterns
		AnsiConsole.MarkupLine($"\n[cyan]Current exclusion patterns ({batch.PathExclusionPatterns.Count}):[/]");
		if (batch.PathExclusionPatterns.Count == 0)
		{
			AnsiConsole.MarkupLine("  [dim]None[/]");
		}
		else
		{
			batch.PathExclusionPatterns.WithIndex().ForEach(item =>
				AnsiConsole.MarkupLine($"  {item.index + 1}. [red]{item.item}[/]"));
		}

		bool modifyExclusions = AnsiConsole.Confirm("[cyan]Modify exclusion patterns?[/]");

		if (modifyExclusions)
		{
			exclusionPatterns.Clear();
			AnsiConsole.MarkupLine("[cyan]Enter new exclusion patterns (one per line, empty line to finish):[/]");
			AnsiConsole.MarkupLine("[dim]Examples: */node_modules/*, */bin/*, */obj/*, .git/*, temp*[/]");

			while (true)
			{
				string exclusionPattern = inputHistoryService.AskWithHistory($"[cyan]Exclusion Pattern {exclusionPatterns.Count + 1}[/]");
				if (string.IsNullOrWhiteSpace(exclusionPattern))
				{
					break;
				}
				exclusionPatterns.Add(exclusionPattern.Trim());
			}
		}

		return exclusionPatterns;
	}

	/// <summary>
	/// Duplicates an existing batch configuration.
	/// </summary>
	private void HandleDuplicateBatch()
	{
		BatchConfiguration? sourceBatch = SelectBatch("Duplicate Batch Configuration");
		if (sourceBatch == null)
		{
			return;
		}

		string newName = inputHistoryService.AskWithHistory($"[cyan]Enter new name for duplicate[/] (original: {sourceBatch.Name})");
		if (string.IsNullOrWhiteSpace(newName))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		// Check if batch already exists
		if (appDataService.AppData.BatchConfigurations.ContainsKey(newName))
		{
			bool overwrite = AnsiConsole.Confirm($"[yellow]A batch named '{newName}' already exists. Overwrite it?[/]");
			if (!overwrite)
			{
				ShowWarning(OperationCancelledMessage);
				return;
			}
		}

		// Create duplicate
		BatchConfiguration duplicateBatch = new()
		{
			Name = newName,
			FilePatterns = new(sourceBatch.FilePatterns),
			SearchPaths = new(sourceBatch.SearchPaths),
			PathExclusionPatterns = new(sourceBatch.PathExclusionPatterns),
		};

		SaveAndDisplayBatchResult(duplicateBatch);
		WaitForKeyPress();
	}

	/// <summary>
	/// Deletes a batch configuration.
	/// </summary>
	private void HandleDeleteBatch()
	{
		BatchConfiguration? batch = SelectBatch("Delete Batch Configuration");
		if (batch == null)
		{
			return;
		}

		ShowMenuTitle($"Delete Batch: {batch.Name}");

		// Show batch details
		AnsiConsole.MarkupLine($"[cyan]Name:[/] {batch.Name}");
		AnsiConsole.MarkupLine($"[cyan]Patterns:[/] {batch.FilePatterns.Count}");

		bool confirm = AnsiConsole.Confirm($"[red]Are you sure you want to delete '{batch.Name}'?[/]");
		if (!confirm)
		{
			ShowWarning(OperationCancelledMessage);
			WaitForKeyPress();
			return;
		}

		// Delete the batch
		if (appDataService.AppData.BatchConfigurations.Remove(batch.Name))
		{
			appDataService.SaveAsync().Wait();
			ShowSuccess($"Batch '{batch.Name}' deleted successfully.");
		}
		else
		{
			ShowError($"Failed to delete batch '{batch.Name}'. It may not exist.");
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Exports batch configurations to a file.
	/// </summary>
	private void HandleExportBatches()
	{
		ShowMenuTitle("Export Batch Configurations");

		Dictionary<string, BatchConfiguration>.ValueCollection allBatches = appDataService.AppData.BatchConfigurations.Values;
		if (allBatches.Count == 0)
		{
			ShowWarning("No batch configurations to export.");
			WaitForKeyPress();
			return;
		}

		string defaultFileName = $"BlastMerge_Batches_{DateTime.Now:yyyyMMdd_HHmmss}.json";
		string exportPath = inputHistoryService.AskWithHistory($"[cyan]Enter export file path[/] (default: {defaultFileName})");
		if (string.IsNullOrWhiteSpace(exportPath))
		{
			exportPath = defaultFileName;
		}

		try
		{
			string json = JsonSerializer.Serialize(allBatches, JsonSerializerOptions);
			File.WriteAllText(exportPath, json);
			ShowSuccess($"Exported {allBatches.Count} batch configurations to '{exportPath}'");
		}
		catch (UnauthorizedAccessException ex)
		{
			ShowError($"Access denied to file '{exportPath}': {ex.Message}");
		}
		catch (DirectoryNotFoundException ex)
		{
			ShowError($"Directory not found for path '{exportPath}': {ex.Message}");
		}
		catch (IOException ex)
		{
			ShowError($"I/O error while writing to '{exportPath}': {ex.Message}");
		}
		catch (JsonException ex)
		{
			ShowError($"Failed to serialize batch configurations: {ex.Message}");
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Imports batch configurations from a file.
	/// </summary>
	private void HandleImportBatches()
	{
		ShowMenuTitle("Import Batch Configurations");

		string importPath = inputHistoryService.AskWithHistory("[cyan]Enter import file path[/]");
		if (string.IsNullOrWhiteSpace(importPath))
		{
			ShowWarning(OperationCancelledMessage);
			WaitForKeyPress();
			return;
		}

		if (!File.Exists(importPath))
		{
			ShowError($"File not found: {importPath}");
			WaitForKeyPress();
			return;
		}

		try
		{
			List<BatchConfiguration>? importedBatches = LoadBatchesFromFile(importPath);
			if (importedBatches == null || importedBatches.Count == 0)
			{
				ShowWarning("No valid batch configurations found in the file.");
				WaitForKeyPress();
				return;
			}

			if (!ConfirmImport(importedBatches))
			{
				ShowWarning("Import cancelled.");
				WaitForKeyPress();
				return;
			}

			ImportBatchResults results = ProcessBatchImport(importedBatches);
			DisplayImportSummary(results);
		}
		catch (UnauthorizedAccessException ex)
		{
			ShowError($"Access denied to file '{importPath}': {ex.Message}");
		}
		catch (FileNotFoundException ex)
		{
			ShowError($"File not found: {ex.Message}");
		}
		catch (IOException ex)
		{
			ShowError($"I/O error while reading '{importPath}': {ex.Message}");
		}
		catch (JsonException ex)
		{
			ShowError($"Invalid JSON format: {ex.Message}");
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Loads batch configurations from a JSON file.
	/// </summary>
	/// <param name="filePath">The path to the JSON file.</param>
	/// <returns>The loaded batch configurations, or null if loading failed.</returns>
	private static List<BatchConfiguration>? LoadBatchesFromFile(string filePath)
	{
		string json = File.ReadAllText(filePath);
		return JsonSerializer.Deserialize<List<BatchConfiguration>>(json, JsonSerializerOptions);
	}

	/// <summary>
	/// Displays import preview and asks for user confirmation.
	/// </summary>
	/// <param name="batches">The batches to import.</param>
	/// <returns>True if the user confirmed the import, false otherwise.</returns>
	private static bool ConfirmImport(List<BatchConfiguration> batches)
	{
		AnsiConsole.MarkupLine($"[cyan]Found {batches.Count} batch configurations to import:[/]");
		foreach (BatchConfiguration batch in batches)
		{
			AnsiConsole.MarkupLine($"  • [green]{batch.Name}[/] - {batch.FilePatterns.Count} patterns");
		}

		return AnsiConsole.Confirm("\n[cyan]Proceed with import?[/]");
	}

	/// <summary>
	/// Processes the import of batch configurations.
	/// </summary>
	/// <param name="batches">The batches to import.</param>
	/// <returns>The import results.</returns>
	private ImportBatchResults ProcessBatchImport(List<BatchConfiguration> batches)
	{
		ImportBatchResults results = new();

		foreach (BatchConfiguration batch in batches)
		{
			if (!batch.IsValid())
			{
				AnsiConsole.MarkupLine($"[red]Skipping invalid batch: {batch.Name}[/]");
				results.ErrorCount++;
				continue;
			}

			if (HandleBatchAlreadyExists(batch))
			{
				results.SkipCount++;
				continue;
			}

			if (ImportSingleBatch(batch))
			{
				AnsiConsole.MarkupLine($"[green]Imported: {batch.Name}[/]");
				results.SuccessCount++;
			}
			else
			{
				AnsiConsole.MarkupLine($"[red]Failed to save: {batch.Name}[/]");
				results.ErrorCount++;
			}
		}

		return results;
	}

	/// <summary>
	/// Handles the case when a batch with the same name already exists.
	/// </summary>
	/// <param name="batch">The batch to check.</param>
	/// <returns>True if the batch should be skipped, false if it should be processed.</returns>
	private bool HandleBatchAlreadyExists(BatchConfiguration batch)
	{
		if (!appDataService.AppData.BatchConfigurations.ContainsKey(batch.Name))
		{
			return false;
		}

		bool overwrite = AnsiConsole.Confirm($"[yellow]Batch '{batch.Name}' already exists. Overwrite?[/]");
		if (!overwrite)
		{
			AnsiConsole.MarkupLine($"[yellow]Skipped existing batch: {batch.Name}[/]");
			return true;
		}

		return false;
	}

	/// <summary>
	/// Imports a single batch configuration.
	/// </summary>
	/// <param name="batch">The batch to import.</param>
	/// <returns>True if the import was successful, false otherwise.</returns>
	private bool ImportSingleBatch(BatchConfiguration batch)
	{
		SaveAndDisplayBatchResult(batch);
		return true;
	}

	/// <summary>
	/// Displays the import summary.
	/// </summary>
	/// <param name="results">The import results.</param>
	private static void DisplayImportSummary(ImportBatchResults results)
	{
		AnsiConsole.MarkupLine($"\n[bold]Import Summary:[/]");
		AnsiConsole.MarkupLine($"[green]Imported: {results.SuccessCount}[/]");
		AnsiConsole.MarkupLine($"[yellow]Skipped: {results.SkipCount}[/]");
		AnsiConsole.MarkupLine($"[red]Errors: {results.ErrorCount}[/]");
	}

	/// <summary>
	/// Represents the results of a batch import operation.
	/// </summary>
	private sealed class ImportBatchResults
	{
		public int SuccessCount { get; set; }
		public int SkipCount { get; set; }
		public int ErrorCount { get; set; }
	}

	/// <summary>
	/// Allows the user to select a batch configuration.
	/// </summary>
	/// <param name="title">The title for the selection prompt.</param>
	/// <returns>The selected batch configuration, or null if cancelled.</returns>
	private BatchConfiguration? SelectBatch(string title)
	{
		ShowMenuTitle(title);

		Dictionary<string, BatchConfiguration>.ValueCollection allBatches = appDataService.AppData.BatchConfigurations.Values;
		if (allBatches.Count == 0)
		{
			ShowWarning("No batch configurations available.");
			WaitForKeyPress();
			return null;
		}

		string batchName = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Select batch configuration:[/]")
				.PageSize(10)
				.AddChoices(allBatches.Select(b => b.Name)));

		return allBatches.FirstOrDefault(b => b.Name == batchName);
	}
}
