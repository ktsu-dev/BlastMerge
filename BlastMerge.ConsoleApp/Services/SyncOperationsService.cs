// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

using ktsu.BlastMerge.ConsoleApp.Contracts;
using ktsu.BlastMerge.ConsoleApp.Services.Common;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Service for handling file synchronization UI operations.
/// </summary>
public static class SyncOperationsService
{
	/// <summary>
	/// Offers sync options for file groups.
	/// </summary>
	/// <param name="fileGroups">The file groups to offer sync options for.</param>
	public static void OfferSyncOptions(IReadOnlyDictionary<string, IReadOnlyCollection<string>> fileGroups)
	{
		ArgumentNullException.ThrowIfNull(fileGroups);

		// Convert to FileGroup objects for easier handling
		List<string> allFiles = [.. fileGroups.SelectMany(g => g.Value)];
		IReadOnlyCollection<FileGroup> groups = FileDiffer.GroupFilesByHash(allFiles);

		// Filter to groups with multiple files
		List<FileGroup> groupsWithMultipleFiles = [.. groups.Where(g => g.FilePaths.Count > 1)];

		if (groupsWithMultipleFiles.Count == 0)
		{
			UIHelper.ShowWarning("No groups with multiple files to sync.");
			return;
		}

		AnsiConsole.MarkupLine($"[cyan]Found {groupsWithMultipleFiles.Count} groups with multiple identical copies.[/]");
		AnsiConsole.WriteLine();

		Dictionary<string, SyncChoice> syncChoices = new()
		{
			[SyncOperationsDisplay.SyncToNewest] = SyncChoice.SyncToNewest,
			[SyncOperationsDisplay.ChooseReference] = SyncChoice.ChooseReference,
			[SyncOperationsDisplay.BackToPreviousMenu] = SyncChoice.Back
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]How would you like to sync the files?[/]")
				.AddChoices(syncChoices.Keys));

		if (syncChoices.TryGetValue(selection, out SyncChoice choice))
		{
			switch (choice)
			{
				case SyncChoice.SyncToNewest:
					SyncToNewestVersion(groupsWithMultipleFiles);
					break;
				case SyncChoice.ChooseReference:
					ChooseReferenceFiles(groupsWithMultipleFiles);
					break;
				case SyncChoice.Back:
					// Return to previous menu
					break;
				default:
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Syncs all files to the newest version in each group.
	/// </summary>
	/// <param name="groups">The file groups to sync.</param>
	private static void SyncToNewestVersion(List<FileGroup> groups)
	{
		bool confirm = AnsiConsole.Confirm("[yellow]This will overwrite older versions with the newest file in each group. Continue?[/]");

		if (!confirm)
		{
			UIHelper.ShowWarning("Sync cancelled.");
			return;
		}

		(int syncedFiles, int syncedGroups) = PerformSyncOperation(groups);
		ShowNewestVersionSyncResult(syncedFiles, syncedGroups);
	}

	/// <summary>
	/// Performs the actual sync operation for all groups.
	/// </summary>
	/// <param name="groups">The file groups to sync.</param>
	/// <returns>A tuple containing the number of synced files and groups.</returns>
	private static (int syncedFiles, int syncedGroups) PerformSyncOperation(List<FileGroup> groups)
	{
		int syncedGroups = 0;
		int syncedFiles = 0;

		AnsiConsole.Status()
			.Start("Syncing files...", ctx =>
			{
				foreach (FileGroup group in groups)
				{
					syncedFiles += SyncGroupToNewest(group, ctx);
					syncedGroups++;
				}
			});

		return (syncedFiles, syncedGroups);
	}

	/// <summary>
	/// Syncs all files in a group to the newest file.
	/// </summary>
	/// <param name="group">The file group to sync.</param>
	/// <param name="ctx">The status context for updates.</param>
	/// <returns>The number of files successfully synced.</returns>
	private static int SyncGroupToNewest(FileGroup group, StatusContext ctx)
	{
		string newestFile = GetNewestFile(group.FilePaths);
		int syncedFiles = 0;

		foreach (string file in group.FilePaths)
		{
			if (file != newestFile)
			{
				syncedFiles += TrySyncFile(newestFile, file, ctx);
			}
		}

		return syncedFiles;
	}

	/// <summary>
	/// Attempts to sync a single file with error handling.
	/// </summary>
	/// <param name="sourceFile">The source file to copy from.</param>
	/// <param name="targetFile">The target file to sync.</param>
	/// <param name="ctx">The status context for updates.</param>
	/// <returns>1 if successful, 0 if failed.</returns>
	private static int TrySyncFile(string sourceFile, string targetFile, StatusContext ctx)
	{
		try
		{
			ctx.Status($"Syncing [yellow]{Path.GetFileName(targetFile)}[/]...");
			FileDiffer.SyncFile(sourceFile, targetFile, null);
			return 1;
		}
		catch (IOException ex)
		{
			UIHelper.ShowError($"Failed to sync {targetFile}: {ex.Message}");
			return 0;
		}
		catch (UnauthorizedAccessException ex)
		{
			UIHelper.ShowError($"Access denied when syncing {targetFile}: {ex.Message}");
			return 0;
		}
	}

	/// <summary>
	/// Shows the result of the newest version sync operation.
	/// </summary>
	/// <param name="syncedFiles">Number of files synced.</param>
	/// <param name="syncedGroups">Number of groups processed.</param>
	private static void ShowNewestVersionSyncResult(int syncedFiles, int syncedGroups)
	{
		UIHelper.ShowSuccess($"Sync completed! {syncedFiles} files synchronized across {syncedGroups} groups.");
		UIHelper.WaitForKeyPress();
	}

	/// <summary>
	/// Allows user to choose reference files for each group.
	/// </summary>
	/// <param name="groups">The file groups to choose reference files for.</param>
	private static void ChooseReferenceFiles(List<FileGroup> groups)
	{
		int syncedFiles = 0;

		for (int i = 0; i < groups.Count; i++)
		{
			syncedFiles += ProcessSingleGroup(groups[i], i + 1, groups.Count);
		}

		ShowSyncCompletionMessage(syncedFiles);
	}

	/// <summary>
	/// Processes a single file group for reference file selection and synchronization.
	/// </summary>
	/// <param name="group">The file group to process.</param>
	/// <param name="groupNumber">The current group number.</param>
	/// <param name="totalGroups">The total number of groups.</param>
	/// <returns>The number of files synchronized in this group.</returns>
	private static int ProcessSingleGroup(FileGroup group, int groupNumber, int totalGroups)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[cyan]Group {groupNumber} of {totalGroups}[/]");
		AnsiConsole.WriteLine();

		Table table = CreateFileInfoTable(group.FilePaths);
		AnsiConsole.Write(table);

		string referenceFile = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Choose the reference file to sync others to:[/]")
				.AddChoices(group.FilePaths));

		bool confirmGroup = AnsiConsole.Confirm($"[yellow]Sync all other files in this group to match {Path.GetFileName(referenceFile)}?[/]");

		return confirmGroup ? SyncGroupToReference(group, referenceFile) : 0;
	}

	/// <summary>
	/// Creates a table displaying file information for a group.
	/// </summary>
	/// <param name="filePaths">The file paths to display.</param>
	/// <returns>A formatted table with file information.</returns>
	private static Table CreateFileInfoTable(IReadOnlyCollection<string> filePaths)
	{
		Table table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("[bold]File[/]")
			.AddColumn("[bold]Size[/]")
			.AddColumn("[bold]Modified[/]");

		foreach (string file in filePaths)
		{
			try
			{
				FileInfo fileInfo = new(file);
				table.AddRow(
					$"[green]{file}[/]",
					$"[dim]{fileInfo.Length:N0} bytes[/]",
					$"[dim]{fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}[/]");
			}
			catch (IOException)
			{
				table.AddRow(
					$"[red]{file}[/]",
					"[red]Error[/]",
					"[red]Error[/]");
			}
		}

		return table;
	}

	/// <summary>
	/// Synchronizes all files in a group to match the reference file.
	/// </summary>
	/// <param name="group">The file group to synchronize.</param>
	/// <param name="referenceFile">The reference file to sync to.</param>
	/// <returns>The number of files successfully synchronized.</returns>
	private static int SyncGroupToReference(FileGroup group, string referenceFile)
	{
		int syncedFiles = 0;

		foreach (string file in group.FilePaths)
		{
			if (file != referenceFile)
			{
				try
				{
					FileDiffer.SyncFile(referenceFile, file, null);
					syncedFiles++;
					UIHelper.ShowSuccess($"✓ Synced {Path.GetFileName(file)}");
				}
				catch (IOException ex)
				{
					UIHelper.ShowError($"✗ Failed to sync {Path.GetFileName(file)}: {ex.Message}");
				}
				catch (UnauthorizedAccessException ex)
				{
					UIHelper.ShowError($"✗ Access denied when syncing {Path.GetFileName(file)}: {ex.Message}");
				}
			}
		}

		if (syncedFiles == 0)
		{
			UIHelper.ShowWarning("Skipped this group.");
		}

		return syncedFiles;
	}

	/// <summary>
	/// Shows the completion message after synchronization.
	/// </summary>
	/// <param name="syncedFiles">The total number of files synchronized.</param>
	private static void ShowSyncCompletionMessage(int syncedFiles)
	{
		UIHelper.ShowSuccess($"Sync completed! {syncedFiles} files synchronized.");
		UIHelper.WaitForKeyPress();
	}

	/// <summary>
	/// Gets the newest file from a collection of file paths.
	/// </summary>
	/// <param name="filePaths">The file paths to compare.</param>
	/// <returns>The path of the newest file.</returns>
	private static string GetNewestFile(IReadOnlyCollection<string> filePaths)
	{
		string newestFile = filePaths.First();
		DateTime newestTime = DateTime.MinValue;

		foreach (string file in filePaths)
		{
			try
			{
				DateTime fileTime = File.GetLastWriteTime(file);
				if (fileTime > newestTime)
				{
					newestTime = fileTime;
					newestFile = file;
				}
			}
			catch (IOException)
			{
				// If we can't get the file time, skip this file
			}
		}

		return newestFile;
	}
}
