// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for batch operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class BatchOperationsMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => "Batch Operations";

	/// <summary>
	/// Handles batch operations.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Batch Operations");

		Dictionary<string, BatchOperationChoice> batchOperationChoices = new()
		{
			["📋 List Available Batches"] = BatchOperationChoice.ListAvailableBatches,
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
			ShowWarning("Operation cancelled.");
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
}
