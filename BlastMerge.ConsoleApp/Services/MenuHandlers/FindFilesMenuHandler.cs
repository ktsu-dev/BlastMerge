// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using System.IO;
using ktsu.BlastMerge.ConsoleApp.Models;
using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for find files operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class FindFilesMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	/// <summary>
	/// Gets the name of this menu for navigation purposes.
	/// </summary>
	protected override string MenuName => MenuNames.FindFiles;

	/// <summary>
	/// Handles the find files operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Find & Process Files");

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

		AnsiConsole.Status()
			.Start("Finding files...", ctx =>
			{
				IReadOnlyCollection<string> filePaths = FileFinder.FindFiles(directory, fileName, path =>
				{
					ctx.Status($"Finding files... Found: {Path.GetFileName(path)}");
					ctx.Refresh();
				});

				ctx.Status("Finding files... Complete!");
				ctx.Refresh();

				if (filePaths.Count == 0)
				{
					ShowWarning("No files found matching the pattern.");
					return;
				}

				Table table = new Table()
					.Border(TableBorder.Rounded)
					.BorderColor(Color.Blue)
					.AddColumn("File Path")
					.AddColumn("Size");

				foreach (string filePath in filePaths)
				{
					FileInfo fileInfo = new(filePath);
					table.AddRow(
						$"[green]{filePath}[/]",
						$"[dim]{fileInfo.Length:N0} bytes[/]");
				}

				AnsiConsole.Write(table);
				AnsiConsole.MarkupLine($"\n[green]Found {filePaths.Count} files.[/]");
			});

		WaitForKeyPress();
		GoBack();
	}
}
