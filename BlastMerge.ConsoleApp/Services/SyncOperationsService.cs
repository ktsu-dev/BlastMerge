// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services;

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
			AnsiConsole.MarkupLine("[yellow]No groups with multiple files to sync.[/]");
			return;
		}

		AnsiConsole.MarkupLine($"[cyan]Found {groupsWithMultipleFiles.Count} groups with multiple versions.[/]");
		AnsiConsole.WriteLine();

		Dictionary<string, SyncChoice> syncChoices = new()
		{
			["🔄 Sync all files to newest version"] = SyncChoice.SyncToNewest,
			["📂 Choose a reference file for each group"] = SyncChoice.ChooseReference,
			["🔙 Back to previous menu"] = SyncChoice.Back
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
			AnsiConsole.MarkupLine("[yellow]Sync cancelled.[/]");
			return;
		}

		int syncedGroups = 0;
		int syncedFiles = 0;

		AnsiConsole.Status()
			.Start("Syncing files...", ctx =>
			{
				foreach (FileGroup group in groups)
				{
					string newestFile = GetNewestFile(group.FilePaths);

					foreach (string file in group.FilePaths)
					{
						if (file != newestFile)
						{
							try
							{
								ctx.Status($"Syncing [yellow]{Path.GetFileName(file)}[/]...");
								FileDiffer.SyncFile(newestFile, file);
								syncedFiles++;
							}
							catch (IOException ex)
							{
								AnsiConsole.MarkupLine($"[red]Failed to sync {file}: {ex.Message}[/]");
							}
							catch (UnauthorizedAccessException ex)
							{
								AnsiConsole.MarkupLine($"[red]Access denied when syncing {file}: {ex.Message}[/]");
							}
						}
					}
					syncedGroups++;
				}
			});

		AnsiConsole.MarkupLine($"[green]Sync completed! {syncedFiles} files synchronized across {syncedGroups} groups.[/]");

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
		Console.ReadKey();
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
			FileGroup group = groups[i];

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[cyan]Group {i + 1} of {groups.Count}[/]");
			AnsiConsole.WriteLine();

			Table table = new Table()
				.Border(TableBorder.Rounded)
				.AddColumn("[bold]File[/]")
				.AddColumn("[bold]Size[/]")
				.AddColumn("[bold]Modified[/]");

			foreach (string file in group.FilePaths)
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

			AnsiConsole.Write(table);

			string referenceFile = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.Title("[cyan]Choose the reference file to sync others to:[/]")
					.AddChoices(group.FilePaths));

			bool confirmGroup = AnsiConsole.Confirm($"[yellow]Sync all other files in this group to match {Path.GetFileName(referenceFile)}?[/]");

			if (confirmGroup)
			{
				foreach (string file in group.FilePaths)
				{
					if (file != referenceFile)
					{
						try
						{
							FileDiffer.SyncFile(referenceFile, file);
							syncedFiles++;
							AnsiConsole.MarkupLine($"[green]✓ Synced {Path.GetFileName(file)}[/]");
						}
						catch (IOException ex)
						{
							AnsiConsole.MarkupLine($"[red]✗ Failed to sync {Path.GetFileName(file)}: {ex.Message}[/]");
						}
						catch (UnauthorizedAccessException ex)
						{
							AnsiConsole.MarkupLine($"[red]✗ Access denied when syncing {Path.GetFileName(file)}: {ex.Message}[/]");
						}
					}
				}
			}
			else
			{
				AnsiConsole.MarkupLine("[yellow]Skipped this group.[/]");
			}
		}

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[green]Sync completed! {syncedFiles} files synchronized.[/]");

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
		Console.ReadKey();
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
				continue;
			}
		}

		return newestFile;
	}

	/// <summary>
	/// Sync operation choices.
	/// </summary>
	private enum SyncChoice
	{
		SyncToNewest,
		ChooseReference,
		Back
	}
}
