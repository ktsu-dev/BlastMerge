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
	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => "Compare Files";

	/// <summary>
	/// Handles the compare files operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Compare Files");

		Dictionary<string, CompareChoice> compareChoices = new()
		{
			["🔍 Compare Files in Directory"] = CompareChoice.CompareFilesInDirectory,
			["📁 Compare Two Directories"] = CompareChoice.CompareTwoDirectories,
			["📄 Compare Two Specific Files"] = CompareChoice.CompareTwoSpecificFiles,
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

		try
		{
			directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
			if (string.IsNullOrWhiteSpace(directory))
			{
				UIHelper.ShowWarning("Operation cancelled.");
				return;
			}

			fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
			if (string.IsNullOrWhiteSpace(fileName))
			{
				UIHelper.ShowWarning("Operation cancelled.");
				return;
			}
		}
		catch (InputCancelledException)
		{
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
