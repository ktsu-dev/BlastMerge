// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Contracts;

/// <summary>
/// Menu handler for iterative merge operations.
/// </summary>
public class IterativeMergeMenuHandler(
	IApplicationService applicationService,
	IInputHistoryService inputHistoryService)
	: BaseMenuHandler
{
	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => MenuNames.IterativeMerge;

	/// <summary>
	/// Handles the iterative merge operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Run Iterative Merge");

		string directory;
		string fileName;

		directory = inputHistoryService.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		fileName = inputHistoryService.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		applicationService.RunIterativeMerge(directory, fileName);
		WaitForKeyPress();
		GoBack();
	}
}
