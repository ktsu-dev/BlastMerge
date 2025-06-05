// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for compare files operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class CompareFilesMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	// Menu display text to command mappings for compare operations
	private static readonly Dictionary<string, CompareChoice> CompareChoices = new()
	{
		["🔍 Compare Files in Directory"] = CompareChoice.CompareFilesInDirectory,
		["📁 Compare Two Directories"] = CompareChoice.CompareTwoDirectories,
		["📄 Compare Two Specific Files"] = CompareChoice.CompareTwoSpecificFiles,
		["🔙 Back to Main Menu"] = CompareChoice.BackToMainMenu
	};

	/// <summary>
	/// Handles the compare files operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Compare Files");

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
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Handles comparing files in a directory.
	/// </summary>
	private void HandleCompareFilesInDirectory()
	{
		ShowMenuTitle("Compare Files in Directory");

		string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			UIHelper.ShowWarning("Operation cancelled.");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			UIHelper.ShowWarning("Operation cancelled.");
			return;
		}

		ApplicationService.ProcessFiles(directory, fileName);
	}

	/// <summary>
	/// Handles comparing two directories.
	/// </summary>
	private static void HandleCompareTwoDirectories() => ComparisonOperationsService.HandleCompareTwoDirectories();

	/// <summary>
	/// Handles comparing two specific files.
	/// </summary>
	private static void HandleCompareTwoSpecificFiles() => ComparisonOperationsService.HandleCompareTwoSpecificFiles();
}
