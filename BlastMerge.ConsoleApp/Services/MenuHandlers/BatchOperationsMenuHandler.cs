// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using System.Text.Json;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
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
/// <param name="applicationService">The application service.</param>
public class BatchOperationsMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
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
			[MenuNames.BatchOperationsDisplay.RunBatchConfiguration] = BatchOperationChoice.RunBatchConfiguration,
			[MenuNames.BatchOperationsDisplay.ManageBatchConfigurations] = BatchOperationChoice.ManageBatchConfigurations,
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
	private static void HandleManageBatchConfigurations()
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
	private static bool ExecuteBatchManagementChoice(BatchManagementChoice choice)
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
	private static void HandleListBatches()
	{
		ShowMenuTitle("Available Batch Configurations");

		IReadOnlyCollection<BatchConfiguration> allBatches = AppDataBatchManager.GetAllBatches();

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
					$"[dim]{batch.Description ?? "No description"}[/]",
					$"[yellow]{batch.FilePatterns.Count}[/]");
			}

			AnsiConsole.Write(table);
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Handles creating a new batch configuration.
	/// </summary>
	private static void HandleCreateNewBatch()
	{
		ShowMenuTitle("Create New Batch Configuration");

		string? batchName = GetValidBatchName();
		if (batchName == null)
		{
			return;
		}

		BatchConfigurationData data = GatherBatchConfigurationData();
		if (data.Patterns.Count == 0)
		{
			ShowWarning("At least one file pattern is required.");
			return;
		}

		BatchConfiguration newBatch = CreateBatchConfiguration(batchName, data);
		SaveAndDisplayBatchResult(newBatch);
		WaitForKeyPress();
	}

	/// <summary>
	/// Gets a valid batch name from the user.
	/// </summary>
	/// <returns>The batch name, or null if cancelled.</returns>
	private static string? GetValidBatchName()
	{
		string batchName = AppDataHistoryInput.AskWithHistory("[cyan]Enter batch name[/]");
		if (string.IsNullOrWhiteSpace(batchName))
		{
			ShowWarning(OperationCancelledMessage);
			return null;
		}

		BatchConfiguration? existingBatch = AppDataBatchManager.LoadBatch(batchName);
		if (existingBatch != null)
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
	private static BatchConfigurationData GatherBatchConfigurationData()
	{
		string description = AppDataHistoryInput.AskWithHistory("[cyan]Enter description (optional)[/]");
		List<string> patterns = GatherFilePatterns();
		List<string> searchPaths = GatherSearchPaths();
		List<string> exclusionPatterns = GatherExclusionPatterns();

		bool skipEmptyPatterns = AnsiConsole.Confirm("[cyan]Skip patterns that don't find any files?[/]", true);
		bool promptBeforeEachPattern = AnsiConsole.Confirm("[cyan]Prompt before processing each pattern?[/]", false);

		return new BatchConfigurationData
		{
			Description = description,
			Patterns = patterns,
			SearchPaths = searchPaths,
			ExclusionPatterns = exclusionPatterns,
			SkipEmptyPatterns = skipEmptyPatterns,
			PromptBeforeEachPattern = promptBeforeEachPattern
		};
	}

	/// <summary>
	/// Gathers file patterns from the user.
	/// </summary>
	/// <returns>List of file patterns.</returns>
	private static List<string> GatherFilePatterns()
	{
		List<string> patterns = [];
		AnsiConsole.MarkupLine("[cyan]Enter file patterns (one per line, empty line to finish):[/]");
		AnsiConsole.MarkupLine("[dim]Examples: *.txt, .gitignore, README.md, **/*.cs[/]");

		while (true)
		{
			string pattern = AppDataHistoryInput.AskWithHistory($"[cyan]Pattern {patterns.Count + 1}[/]");
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
	private static List<string> GatherSearchPaths()
	{
		List<string> searchPaths = [];
		bool addSearchPaths = AnsiConsole.Confirm("[cyan]Add custom search paths? (If no, uses the directory provided at runtime)[/]", false);

		if (addSearchPaths)
		{
			AnsiConsole.MarkupLine("[cyan]Enter search paths (one per line, empty line to finish):[/]");
			AnsiConsole.MarkupLine("[dim]Examples: C:\\Projects, /home/user/repos, ../other-projects[/]");

			while (true)
			{
				string searchPath = AppDataHistoryInput.AskWithHistory($"[cyan]Search Path {searchPaths.Count + 1}[/]");
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
	private static List<string> GatherExclusionPatterns()
	{
		List<string> exclusionPatterns = [];
		bool addExclusions = AnsiConsole.Confirm("[cyan]Add path exclusion patterns?[/]", false);

		if (addExclusions)
		{
			AnsiConsole.MarkupLine("[cyan]Enter path exclusion patterns (one per line, empty line to finish):[/]");
			AnsiConsole.MarkupLine("[dim]Examples: */node_modules/*, */bin/*, */obj/*, .git/*, temp*[/]");

			while (true)
			{
				string exclusionPattern = AppDataHistoryInput.AskWithHistory($"[cyan]Exclusion Pattern {exclusionPatterns.Count + 1}[/]");
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
	/// Creates a batch configuration from the provided data.
	/// </summary>
	/// <param name="batchName">The batch name.</param>
	/// <param name="data">The batch configuration data.</param>
	/// <returns>The created batch configuration.</returns>
	private static BatchConfiguration CreateBatchConfiguration(string batchName, BatchConfigurationData data)
	{
		return new BatchConfiguration
		{
			Name = batchName,
			Description = data.Description,
			FilePatterns = [.. data.Patterns],
			SearchPaths = [.. data.SearchPaths],
			PathExclusionPatterns = [.. data.ExclusionPatterns],
			SkipEmptyPatterns = data.SkipEmptyPatterns,
			PromptBeforeEachPattern = data.PromptBeforeEachPattern,
			CreatedDate = DateTime.UtcNow,
			LastModified = DateTime.UtcNow
		};
	}

	/// <summary>
	/// Saves the batch configuration and displays the result.
	/// </summary>
	/// <param name="batch">The batch configuration to save.</param>
	private static void SaveAndDisplayBatchResult(BatchConfiguration batch)
	{
		bool saved = AppDataBatchManager.SaveBatch(batch);
		if (saved)
		{
			// Force save any queued changes to ensure the batch is immediately available
			AppDataBatchManager.SaveIfRequired();

			ShowSuccess($"Batch configuration '{batch.Name}' created successfully!");
			DisplayBatchSummary(batch);
		}
		else
		{
			ShowError($"Failed to save batch configuration '{batch.Name}'.");
		}
	}

	/// <summary>
	/// Displays a summary of the batch configuration.
	/// </summary>
	/// <param name="batch">The batch configuration to display.</param>
	private static void DisplayBatchSummary(BatchConfiguration batch)
	{
		AnsiConsole.MarkupLine($"\n[bold]Batch Summary:[/]");
		AnsiConsole.MarkupLine($"[cyan]Name:[/] {batch.Name}");
		AnsiConsole.MarkupLine($"[cyan]Description:[/] {batch.Description}");
		AnsiConsole.MarkupLine($"[cyan]Patterns:[/] {batch.FilePatterns.Count}");
		AnsiConsole.MarkupLine($"[cyan]Skip Empty:[/] {batch.SkipEmptyPatterns}");
		AnsiConsole.MarkupLine($"[cyan]Prompt Before Each:[/] {batch.PromptBeforeEachPattern}");
	}

	/// <summary>
	/// Represents the data needed to create a batch configuration.
	/// </summary>
	private sealed class BatchConfigurationData
	{
		public string Description { get; set; } = string.Empty;
		public List<string> Patterns { get; set; } = [];
		public List<string> SearchPaths { get; set; } = [];
		public List<string> ExclusionPatterns { get; set; } = [];
		public bool SkipEmptyPatterns { get; set; }
		public bool PromptBeforeEachPattern { get; set; }
	}

	/// <summary>
	/// Handles running a batch configuration.
	/// </summary>
	private void HandleRunBatch()
	{
		ShowMenuTitle("Run Batch Configuration");

		IReadOnlyCollection<BatchConfiguration> allBatches = AppDataBatchManager.GetAllBatches();

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
		BatchConfiguration? selectedBatch = AppDataBatchManager.LoadBatch(batchName);
		if (selectedBatch == null)
		{
			ShowError($"Batch configuration '{batchName}' not found.");
			WaitForKeyPress();
			return;
		}

		string directory;
		if (selectedBatch.SearchPaths.Count > 0)
		{
			// Batch has search paths configured, no need to ask for directory
			AnsiConsole.MarkupLine($"[green]Using configured search paths ({selectedBatch.SearchPaths.Count} paths)[/]");
			foreach (string searchPath in selectedBatch.SearchPaths)
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
			directory = AppDataHistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
			if (string.IsNullOrWhiteSpace(directory))
			{
				ShowWarning(OperationCancelledMessage);
				return;
			}
		}

		if (selectedBatch.SearchPaths.Count > 0)
		{
			AnsiConsole.MarkupLine($"[cyan]Processing batch configuration '[yellow]{batchName}[/]' using configured search paths[/]");
		}
		else
		{
			AnsiConsole.MarkupLine($"[cyan]Processing batch configuration '[yellow]{batchName}[/]' in '[yellow]{directory}[/]'[/]");
		}
		AnsiConsole.WriteLine();

		ApplicationService.ProcessBatch(directory, batchName);

		ShowSuccess("Batch operation completed.");
		WaitForKeyPress();
	}

	/// <summary>
	/// Views detailed information about a batch configuration.
	/// </summary>
	private static void HandleViewBatch()
	{
		BatchConfiguration? batch = SelectBatch("View Batch Details");
		if (batch == null)
		{
			return;
		}

		ShowMenuTitle($"Batch Details: {batch.Name}");

		Panel panel = new($"""
			[cyan]Name:[/] {batch.Name}
			[cyan]Description:[/] {batch.Description ?? "No description"}
			[cyan]Patterns:[/] {batch.FilePatterns.Count}
			[cyan]Search Paths:[/] {batch.SearchPaths.Count}
			[cyan]Exclusion Patterns:[/] {batch.PathExclusionPatterns.Count}
			[cyan]Skip Empty:[/] {batch.SkipEmptyPatterns}
			[cyan]Prompt Before Each:[/] {batch.PromptBeforeEachPattern}
			[cyan]Created:[/] {batch.CreatedDate:yyyy-MM-dd HH:mm:ss}
			[cyan]Modified:[/] {batch.LastModified:yyyy-MM-dd HH:mm:ss}
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

			for (int i = 0; i < batch.FilePatterns.Count; i++)
			{
				patternTable.AddRow($"{i + 1}", $"[yellow]{batch.FilePatterns[i]}[/]");
			}

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

			for (int i = 0; i < batch.SearchPaths.Count; i++)
			{
				searchPathTable.AddRow($"{i + 1}", $"[green]{batch.SearchPaths[i]}[/]");
			}

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

			for (int i = 0; i < batch.PathExclusionPatterns.Count; i++)
			{
				exclusionTable.AddRow($"{i + 1}", $"[red]{batch.PathExclusionPatterns[i]}[/]");
			}

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
	private static void HandleEditBatch()
	{
		BatchConfiguration? batch = SelectBatch("Edit Batch Configuration");
		if (batch == null)
		{
			return;
		}

		ShowMenuTitle($"Edit Batch: {batch.Name}");

		EditBatchData editData = GatherEditBatchData(batch);
		BatchConfiguration updatedBatch = CreateUpdatedBatch(batch, editData);
		SaveUpdatedBatch(updatedBatch);
		WaitForKeyPress();
	}

	/// <summary>
	/// Gathers all edit data from the user for the batch configuration.
	/// </summary>
	/// <param name="batch">The batch configuration being edited.</param>
	/// <returns>The gathered edit data.</returns>
	private static EditBatchData GatherEditBatchData(BatchConfiguration batch)
	{
		string description = AppDataHistoryInput.AskWithHistory($"[cyan]Enter description[/] (current: {batch.Description ?? ""})", batch.Description ?? "");
		List<string> patterns = HandlePatternEditing(batch);
		List<string> searchPaths = HandleSearchPathEditing(batch);
		List<string> exclusionPatterns = HandleExclusionPatternEditing(batch);
		bool skipEmpty = AnsiConsole.Confirm($"[cyan]Skip patterns that don't find any files?[/] (current: {batch.SkipEmptyPatterns})", batch.SkipEmptyPatterns);
		bool promptBefore = AnsiConsole.Confirm($"[cyan]Prompt before processing each pattern?[/] (current: {batch.PromptBeforeEachPattern})", batch.PromptBeforeEachPattern);

		return new EditBatchData
		{
			Description = description,
			Patterns = patterns,
			SearchPaths = searchPaths,
			ExclusionPatterns = exclusionPatterns,
			SkipEmptyPatterns = skipEmpty,
			PromptBeforeEachPattern = promptBefore
		};
	}

	/// <summary>
	/// Handles editing of file patterns.
	/// </summary>
	/// <param name="batch">The batch configuration being edited.</param>
	/// <returns>Updated list of patterns.</returns>
	private static List<string> HandlePatternEditing(BatchConfiguration batch)
	{
		List<string> patterns = [.. batch.FilePatterns];

		// Show current patterns
		AnsiConsole.MarkupLine($"\n[cyan]Current patterns ({batch.FilePatterns.Count}):[/]");
		for (int i = 0; i < batch.FilePatterns.Count; i++)
		{
			AnsiConsole.MarkupLine($"  {i + 1}. [yellow]{batch.FilePatterns[i]}[/]");
		}

		bool modifyPatterns = AnsiConsole.Confirm("[cyan]Modify file patterns?[/]");

		if (modifyPatterns)
		{
			patterns.Clear();
			AnsiConsole.MarkupLine("[cyan]Enter new file patterns (one per line, empty line to finish):[/]");

			while (true)
			{
				string pattern = AppDataHistoryInput.AskWithHistory($"[cyan]Pattern {patterns.Count + 1}[/]");
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
	private static List<string> HandleSearchPathEditing(BatchConfiguration batch)
	{
		List<string> searchPaths = [.. batch.SearchPaths];

		// Handle search paths
		AnsiConsole.MarkupLine($"\n[cyan]Current search paths ({batch.SearchPaths.Count}):[/]");
		if (batch.SearchPaths.Count == 0)
		{
			AnsiConsole.MarkupLine("  [dim]None (uses runtime directory)[/]");
		}
		else
		{
			for (int i = 0; i < batch.SearchPaths.Count; i++)
			{
				AnsiConsole.MarkupLine($"  {i + 1}. [green]{batch.SearchPaths[i]}[/]");
			}
		}

		bool modifySearchPaths = AnsiConsole.Confirm("[cyan]Modify search paths?[/]");

		if (modifySearchPaths)
		{
			searchPaths.Clear();
			AnsiConsole.MarkupLine("[cyan]Enter new search paths (one per line, empty line to finish):[/]");
			AnsiConsole.MarkupLine("[dim]Examples: C:\\Projects, /home/user/repos, ../other-projects[/]");

			while (true)
			{
				string searchPath = AppDataHistoryInput.AskWithHistory($"[cyan]Search Path {searchPaths.Count + 1}[/]");
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
	private static List<string> HandleExclusionPatternEditing(BatchConfiguration batch)
	{
		List<string> exclusionPatterns = [.. batch.PathExclusionPatterns];

		// Handle exclusion patterns
		AnsiConsole.MarkupLine($"\n[cyan]Current exclusion patterns ({batch.PathExclusionPatterns.Count}):[/]");
		if (batch.PathExclusionPatterns.Count == 0)
		{
			AnsiConsole.MarkupLine("  [dim]None[/]");
		}
		else
		{
			for (int i = 0; i < batch.PathExclusionPatterns.Count; i++)
			{
				AnsiConsole.MarkupLine($"  {i + 1}. [red]{batch.PathExclusionPatterns[i]}[/]");
			}
		}

		bool modifyExclusions = AnsiConsole.Confirm("[cyan]Modify exclusion patterns?[/]");

		if (modifyExclusions)
		{
			exclusionPatterns.Clear();
			AnsiConsole.MarkupLine("[cyan]Enter new exclusion patterns (one per line, empty line to finish):[/]");
			AnsiConsole.MarkupLine("[dim]Examples: */node_modules/*, */bin/*, */obj/*, .git/*, temp*[/]");

			while (true)
			{
				string exclusionPattern = AppDataHistoryInput.AskWithHistory($"[cyan]Exclusion Pattern {exclusionPatterns.Count + 1}[/]");
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
	/// Creates an updated batch configuration from the original and edit data.
	/// </summary>
	/// <param name="original">The original batch configuration.</param>
	/// <param name="editData">The gathered edit data.</param>
	/// <returns>Updated batch configuration.</returns>
	private static BatchConfiguration CreateUpdatedBatch(BatchConfiguration original, EditBatchData editData)
	{
		return new BatchConfiguration
		{
			Name = original.Name,
			Description = editData.Description,
			FilePatterns = [.. editData.Patterns],
			SearchPaths = [.. editData.SearchPaths],
			PathExclusionPatterns = [.. editData.ExclusionPatterns],
			SkipEmptyPatterns = editData.SkipEmptyPatterns,
			PromptBeforeEachPattern = editData.PromptBeforeEachPattern,
			CreatedDate = original.CreatedDate,
			LastModified = DateTime.UtcNow
		};
	}

	/// <summary>
	/// Saves the updated batch configuration and displays the result.
	/// </summary>
	/// <param name="batch">The batch configuration to save.</param>
	private static void SaveUpdatedBatch(BatchConfiguration batch)
	{
		bool saved = AppDataBatchManager.SaveBatch(batch);
		if (saved)
		{
			// Force save any queued changes to ensure the batch is immediately available
			AppDataBatchManager.SaveIfRequired();

			ShowSuccess($"Batch configuration '{batch.Name}' updated successfully!");
		}
		else
		{
			ShowError($"Failed to save batch configuration '{batch.Name}'.");
		}
	}

	/// <summary>
	/// Data transfer object for batch edit operations.
	/// </summary>
	private sealed class EditBatchData
	{
		public string Description { get; set; } = string.Empty;
		public List<string> Patterns { get; set; } = [];
		public List<string> SearchPaths { get; set; } = [];
		public List<string> ExclusionPatterns { get; set; } = [];
		public bool SkipEmptyPatterns { get; set; }
		public bool PromptBeforeEachPattern { get; set; }
	}

	/// <summary>
	/// Duplicates an existing batch configuration.
	/// </summary>
	private static void HandleDuplicateBatch()
	{
		BatchConfiguration? sourceBatch = SelectBatch("Duplicate Batch Configuration");
		if (sourceBatch == null)
		{
			return;
		}

		string newName = AppDataHistoryInput.AskWithHistory($"[cyan]Enter new name for duplicate[/] (original: {sourceBatch.Name})");
		if (string.IsNullOrWhiteSpace(newName))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		// Check if batch already exists
		BatchConfiguration? existingBatch = AppDataBatchManager.LoadBatch(newName);
		if (existingBatch != null)
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
			Description = $"Copy of {sourceBatch.Name}" + (string.IsNullOrEmpty(sourceBatch.Description) ? "" : $" - {sourceBatch.Description}"),
			FilePatterns = new(sourceBatch.FilePatterns),
			SearchPaths = new(sourceBatch.SearchPaths),
			PathExclusionPatterns = new(sourceBatch.PathExclusionPatterns),
			SkipEmptyPatterns = sourceBatch.SkipEmptyPatterns,
			PromptBeforeEachPattern = sourceBatch.PromptBeforeEachPattern,
			CreatedDate = DateTime.UtcNow,
			LastModified = DateTime.UtcNow
		};

		bool saved = AppDataBatchManager.SaveBatch(duplicateBatch);
		if (saved)
		{
			// Force save any queued changes to ensure the batch is immediately available
			AppDataBatchManager.SaveIfRequired();

			ShowSuccess($"Batch configuration '{newName}' created as a copy of '{sourceBatch.Name}'!");
		}
		else
		{
			ShowError($"Failed to create duplicate batch configuration.");
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Deletes a batch configuration.
	/// </summary>
	private static void HandleDeleteBatch()
	{
		BatchConfiguration? batch = SelectBatch("Delete Batch Configuration");
		if (batch == null)
		{
			return;
		}

		ShowMenuTitle($"Delete Batch: {batch.Name}");

		// Show batch details
		AnsiConsole.MarkupLine($"[cyan]Name:[/] {batch.Name}");
		AnsiConsole.MarkupLine($"[cyan]Description:[/] {batch.Description ?? "No description"}");
		AnsiConsole.MarkupLine($"[cyan]Patterns:[/] {batch.FilePatterns.Count}");

		bool confirm = AnsiConsole.Confirm($"[red]Are you sure you want to delete '{batch.Name}'?[/]");
		if (!confirm)
		{
			ShowWarning(OperationCancelledMessage);
			WaitForKeyPress();
			return;
		}

		bool deleted = AppDataBatchManager.DeleteBatch(batch.Name);
		if (deleted)
		{
			ShowSuccess($"Batch configuration '{batch.Name}' deleted successfully!");
		}
		else
		{
			ShowError($"Failed to delete batch configuration '{batch.Name}'.");
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Exports batch configurations to a file.
	/// </summary>
	private static void HandleExportBatches()
	{
		ShowMenuTitle("Export Batch Configurations");

		IReadOnlyCollection<BatchConfiguration> allBatches = AppDataBatchManager.GetAllBatches();
		if (allBatches.Count == 0)
		{
			ShowWarning("No batch configurations to export.");
			WaitForKeyPress();
			return;
		}

		string defaultFileName = $"BlastMerge_Batches_{DateTime.Now:yyyyMMdd_HHmmss}.json";
		string exportPath = AppDataHistoryInput.AskWithHistory($"[cyan]Enter export file path[/] (default: {defaultFileName})");
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
	private static void HandleImportBatches()
	{
		ShowMenuTitle("Import Batch Configurations");

		string importPath = AppDataHistoryInput.AskWithHistory("[cyan]Enter import file path[/]");
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
	private static ImportBatchResults ProcessBatchImport(List<BatchConfiguration> batches)
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

			if (HandleExistingBatch(batch))
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
	private static bool HandleExistingBatch(BatchConfiguration batch)
	{
		BatchConfiguration? existing = AppDataBatchManager.LoadBatch(batch.Name);
		if (existing == null)
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
	private static bool ImportSingleBatch(BatchConfiguration batch)
	{
		batch.CreatedDate = DateTime.UtcNow;
		batch.LastModified = DateTime.UtcNow;
		return AppDataBatchManager.SaveBatch(batch);
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
	private static BatchConfiguration? SelectBatch(string title)
	{
		ShowMenuTitle(title);

		IReadOnlyCollection<BatchConfiguration> allBatches = AppDataBatchManager.GetAllBatches();
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
