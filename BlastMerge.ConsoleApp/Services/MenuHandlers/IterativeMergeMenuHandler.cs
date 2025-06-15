// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.ConsoleApp.Models;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Services;

/// <summary>
/// Menu handler for iterative merge operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class IterativeMergeMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
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

		directory = AppDataHistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		fileName = AppDataHistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		ApplicationService.RunIterativeMerge(directory, fileName);
		WaitForKeyPress();
		GoBack();
	}
}
