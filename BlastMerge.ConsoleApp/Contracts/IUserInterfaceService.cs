// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Contracts;

using System.Collections.Generic;
using ktsu.BlastMerge.Models;

/// <summary>
/// Interface for user interface and display operations.
/// </summary>
public interface IUserInterfaceService
{
	/// <summary>
	/// Shows a file group summary table.
	/// </summary>
	/// <param name="fileGroups">The file groups to display.</param>
	/// <param name="directory">The base directory for relative path calculation.</param>
	/// <param name="totalFiles">Total number of files found.</param>
	public void ShowFileGroupSummaryTable(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups, string directory, int totalFiles);

	/// <summary>
	/// Shows the welcome screen.
	/// </summary>
	public void ShowWelcomeScreen();

	/// <summary>
	/// Shows the goodbye screen.
	/// </summary>
	public void ShowGoodbyeScreen();

	/// <summary>
	/// Shows batch processing completion message.
	/// </summary>
	/// <param name="totalPatternsProcessed">Total patterns processed.</param>
	/// <param name="totalFilesFound">Total files found.</param>
	/// <param name="batchResult">The batch result containing merge details.</param>
	public void ShowBatchCompletion(int totalPatternsProcessed, int totalFilesFound, BatchResult? batchResult = null);

	/// <summary>
	/// Shows batch processing header information.
	/// </summary>
	/// <param name="batch">The batch configuration.</param>
	/// <param name="directory">The directory being processed.</param>
	public void ShowBatchHeader(BatchConfiguration batch, string directory);

	/// <summary>
	/// Shows error message.
	/// </summary>
	/// <param name="message">The error message to display.</param>
	public void ShowError(string message);

	/// <summary>
	/// Shows warning message.
	/// </summary>
	/// <param name="message">The warning message to display.</param>
	public void ShowWarning(string message);

	/// <summary>
	/// Shows informational message.
	/// </summary>
	/// <param name="message">The informational message to display.</param>
	public void ShowInfo(string message);

	/// <summary>
	/// Shows success message.
	/// </summary>
	/// <param name="message">The success message to display.</param>
	public void ShowSuccess(string message);
}
