// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using System.Text.Json;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for batch operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class BatchOperationsMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	private const string OperationCancelledMessage = "Operation cancelled.";

	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => "Batch Operations";

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
		ShowMenuTitle("Batch Operations");

		Dictionary<string, BatchOperationChoice> batchOperationChoices = new()
		{
			["⚙️ Manage Batch Configurations"] = BatchOperationChoice.ManageBatchConfigurations,
			["▶️ Run Batch Configuration"] = BatchOperationChoice.RunBatchConfiguration,
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
			ShowMenuTitle("Batch Management");

			Dictionary<string, string> managementChoices = new()
			{
				["📋 List Batches"] = "list",
				["➕ Create New Batch"] = "create",
				["👁️ View Batch Details"] = "view",
				["✏️ Edit Batch"] = "edit",
				["📄 Duplicate Batch"] = "duplicate",
				["🗑️ Delete Batch"] = "delete",
				["📤 Export Batches"] = "export",
				["📥 Import Batches"] = "import",
				[GetBackMenuText()] = "back"
			};

			string selection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("[cyan]Select batch management operation:[/]")
					.PageSize(10)
					.AddChoices(managementChoices.Keys));

			if (!managementChoices.TryGetValue(selection, out string? action))
			{
				return;
			}

			switch (action)
			{
				case "list":
					HandleListBatches();
					break;
				case "create":
					HandleCreateNewBatch();
					break;
				case "view":
					HandleViewBatch();
					break;
				case "edit":
					HandleEditBatch();
					break;
				case "duplicate":
					HandleDuplicateBatch();
					break;
				case "delete":
					HandleDeleteBatch();
					break;
				case "export":
					HandleExportBatches();
					break;
				case "import":
					HandleImportBatches();
					break;
				case "back":
					return;
				default:
					return;
			}
		}
	}

	/// <summary>
	/// Handles listing available batch configurations.
	/// </summary>
	private static void HandleListBatches()
	{
		ShowMenuTitle("Available Batch Configurations");

		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();

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

		// Get batch name
		string batchName = HistoryInput.AskWithHistory("[cyan]Enter batch name[/]");
		if (string.IsNullOrWhiteSpace(batchName))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		// Check if batch already exists
		BatchConfiguration? existingBatch = BatchManager.LoadBatch(batchName);
		if (existingBatch != null)
		{
			bool overwrite = AnsiConsole.Confirm($"[yellow]A batch named '{batchName}' already exists. Overwrite it?[/]");
			if (!overwrite)
			{
				ShowWarning(OperationCancelledMessage);
				return;
			}
		}

		// Get description
		string description = HistoryInput.AskWithHistory("[cyan]Enter description (optional)[/]");

		// Get file patterns
		AnsiConsole.MarkupLine("[cyan]Enter file patterns (one per line, empty line to finish):[/]");
		AnsiConsole.MarkupLine("[dim]Examples: *.txt, .gitignore, README.md, **/*.cs[/]");

		List<string> patterns = [];
		while (true)
		{
			string pattern = HistoryInput.AskWithHistory($"[cyan]Pattern {patterns.Count + 1}[/]");
			if (string.IsNullOrWhiteSpace(pattern))
			{
				break;
			}
			patterns.Add(pattern.Trim());
		}

		if (patterns.Count == 0)
		{
			ShowWarning("At least one file pattern is required.");
			return;
		}

		// Get options
		bool skipEmptyPatterns = AnsiConsole.Confirm("[cyan]Skip patterns that don't find any files?[/]", true);
		bool promptBeforeEachPattern = AnsiConsole.Confirm("[cyan]Prompt before processing each pattern?[/]", false);

		// Create batch configuration
		BatchConfiguration newBatch = new()
		{
			Name = batchName,
			Description = description,
			FilePatterns = [.. patterns],
			SkipEmptyPatterns = skipEmptyPatterns,
			PromptBeforeEachPattern = promptBeforeEachPattern,
			CreatedDate = DateTime.UtcNow,
			LastModified = DateTime.UtcNow
		};

		// Save the batch
		bool saved = BatchManager.SaveBatch(newBatch);
		if (saved)
		{
			ShowSuccess($"Batch configuration '{batchName}' created successfully!");

			// Show summary
			AnsiConsole.MarkupLine($"\n[bold]Batch Summary:[/]");
			AnsiConsole.MarkupLine($"[cyan]Name:[/] {newBatch.Name}");
			AnsiConsole.MarkupLine($"[cyan]Description:[/] {newBatch.Description}");
			AnsiConsole.MarkupLine($"[cyan]Patterns:[/] {newBatch.FilePatterns.Count}");
			AnsiConsole.MarkupLine($"[cyan]Skip Empty:[/] {newBatch.SkipEmptyPatterns}");
			AnsiConsole.MarkupLine($"[cyan]Prompt Before Each:[/] {newBatch.PromptBeforeEachPattern}");
		}
		else
		{
			ShowError($"Failed to save batch configuration '{batchName}'.");
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Handles running a batch configuration.
	/// </summary>
	private void HandleRunBatch()
	{
		ShowMenuTitle("Run Batch Configuration");

		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();

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

		string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		AnsiConsole.Status()
			.Start($"Running batch '{batchName}'...", ctx =>
			{
				ApplicationService.ProcessBatch(directory, batchName);
				ctx.Refresh();
				ShowSuccess("Batch operation completed.");
			});

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

		// For simplicity, we'll recreate the batch with new values
		string description = HistoryInput.AskWithHistory($"[cyan]Enter description[/] (current: {batch.Description ?? ""})", batch.Description ?? "");

		// Show current patterns
		AnsiConsole.MarkupLine($"\n[cyan]Current patterns ({batch.FilePatterns.Count}):[/]");
		for (int i = 0; i < batch.FilePatterns.Count; i++)
		{
			AnsiConsole.MarkupLine($"  {i + 1}. [yellow]{batch.FilePatterns[i]}[/]");
		}

		bool modifyPatterns = AnsiConsole.Confirm("[cyan]Modify file patterns?[/]");
		List<string> patterns = [.. batch.FilePatterns];

		if (modifyPatterns)
		{
			patterns.Clear();
			AnsiConsole.MarkupLine("[cyan]Enter new file patterns (one per line, empty line to finish):[/]");

			while (true)
			{
				string pattern = HistoryInput.AskWithHistory($"[cyan]Pattern {patterns.Count + 1}[/]");
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

		bool skipEmpty = AnsiConsole.Confirm($"[cyan]Skip patterns that don't find any files?[/] (current: {batch.SkipEmptyPatterns})", batch.SkipEmptyPatterns);
		bool promptBefore = AnsiConsole.Confirm($"[cyan]Prompt before processing each pattern?[/] (current: {batch.PromptBeforeEachPattern})", batch.PromptBeforeEachPattern);

		// Create updated batch
		BatchConfiguration updatedBatch = new()
		{
			Name = batch.Name,
			Description = description,
			FilePatterns = [.. patterns],
			SkipEmptyPatterns = skipEmpty,
			PromptBeforeEachPattern = promptBefore,
			CreatedDate = batch.CreatedDate,
			LastModified = DateTime.UtcNow
		};

		bool saved = BatchManager.SaveBatch(updatedBatch);
		if (saved)
		{
			ShowSuccess($"Batch configuration '{batch.Name}' updated successfully!");
		}
		else
		{
			ShowError($"Failed to save batch configuration '{batch.Name}'.");
		}

		WaitForKeyPress();
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

		string newName = HistoryInput.AskWithHistory($"[cyan]Enter new name for duplicate[/] (original: {sourceBatch.Name})");
		if (string.IsNullOrWhiteSpace(newName))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		// Check if batch already exists
		BatchConfiguration? existingBatch = BatchManager.LoadBatch(newName);
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
			SkipEmptyPatterns = sourceBatch.SkipEmptyPatterns,
			PromptBeforeEachPattern = sourceBatch.PromptBeforeEachPattern,
			CreatedDate = DateTime.UtcNow,
			LastModified = DateTime.UtcNow
		};

		bool saved = BatchManager.SaveBatch(duplicateBatch);
		if (saved)
		{
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

		bool deleted = BatchManager.DeleteBatch(batch.Name);
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

		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();
		if (allBatches.Count == 0)
		{
			ShowWarning("No batch configurations to export.");
			WaitForKeyPress();
			return;
		}

		string defaultFileName = $"BlastMerge_Batches_{DateTime.Now:yyyyMMdd_HHmmss}.json";
		string exportPath = HistoryInput.AskWithHistory($"[cyan]Enter export file path[/] (default: {defaultFileName})");
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

		string importPath = HistoryInput.AskWithHistory("[cyan]Enter import file path[/]");
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
		BatchConfiguration? existing = BatchManager.LoadBatch(batch.Name);
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
		return BatchManager.SaveBatch(batch);
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

		IReadOnlyCollection<BatchConfiguration> allBatches = BatchManager.GetAllBatches();
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
