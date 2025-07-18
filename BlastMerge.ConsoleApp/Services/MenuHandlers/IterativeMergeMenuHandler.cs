// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using System;
using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Services;

/// <summary>
/// Menu handler for iterative merge operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
/// <param name="historyInput">The history input service.</param>
public class IterativeMergeMenuHandler(ApplicationService applicationService, IAppDataHistoryInput historyInput) : BaseMenuHandler(applicationService)
{
	private readonly IAppDataHistoryInput historyInput = historyInput ?? throw new ArgumentNullException(nameof(historyInput));
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

		directory = historyInput.AskWithHistoryAsync("[cyan]Enter directory path[/]").GetAwaiter().GetResult();
		if (string.IsNullOrWhiteSpace(directory))
		{
			ShowWarning(OperationCancelledMessage);
			return;
		}

		fileName = historyInput.AskWithHistoryAsync("[cyan]Enter filename pattern[/]").GetAwaiter().GetResult();
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
