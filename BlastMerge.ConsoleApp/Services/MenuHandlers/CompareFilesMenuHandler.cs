// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Models;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Contracts;
using Spectre.Console;

/// <summary>
/// Menu handler for compare files operations.
/// </summary>
public class CompareFilesMenuHandler(
	IApplicationService applicationService,
	IInputHistoryService inputHistoryService,
	ComparisonOperationsService comparisonOperationsService)
	: BaseMenuHandler
{
	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => MenuNames.CompareFiles;

	/// <summary>
	/// Handles the compare files operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle(MenuNames.CompareFiles);

		Dictionary<string, CompareChoice> compareChoices = new()
		{
			[CompareFilesDisplay.CompareFilesInDirectory] = CompareChoice.CompareFilesInDirectory,
			[CompareFilesDisplay.CompareTwoDirectories] = CompareChoice.CompareTwoDirectories,
			[CompareFilesDisplay.CompareTwoSpecificFiles] = CompareChoice.CompareTwoSpecificFiles,
			[GetBackMenuText()] = CompareChoice.BackToMainMenu
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Select comparison type:[/]")
				.AddChoices(compareChoices.Keys));

		if (compareChoices.TryGetValue(selection, out CompareChoice choice))
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
					GoBack();
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

		string directory;
		string fileName;

		directory = inputHistoryService.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			UIHelper.ShowWarning(UIHelper.OperationCancelledMessage);
			return;
		}

		fileName = inputHistoryService.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			UIHelper.ShowWarning(UIHelper.OperationCancelledMessage);
			return;
		}

		applicationService.ProcessFiles(directory, fileName);
	}

	/// <summary>
	/// Handles comparing two directories.
	/// </summary>
	private void HandleCompareTwoDirectories() => comparisonOperationsService.HandleCompareTwoDirectories();

	/// <summary>
	/// Handles comparing two specific files.
	/// </summary>
	private void HandleCompareTwoSpecificFiles() => comparisonOperationsService.HandleCompareTwoSpecificFiles();
}
