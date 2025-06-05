// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.Core.Services;

/// <summary>
/// Menu handler for iterative merge operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class IterativeMergeMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => "Iterative Merge";

	/// <summary>
	/// Handles the iterative merge operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Run Iterative Merge");

		string directory = HistoryInput.AskWithHistory("[cyan]Enter directory path[/]");
		if (string.IsNullOrWhiteSpace(directory))
		{
			ShowWarning("Operation cancelled.");
			return;
		}

		string fileName = HistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		if (string.IsNullOrWhiteSpace(fileName))
		{
			ShowWarning("Operation cancelled.");
			return;
		}

		ApplicationService.RunIterativeMerge(directory, fileName);
		WaitForKeyPress();
	}
}
