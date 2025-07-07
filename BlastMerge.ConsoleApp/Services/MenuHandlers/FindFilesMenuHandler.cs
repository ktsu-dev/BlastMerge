// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using System;
using System.IO;
using ktsu.BlastMerge.ConsoleApp.Models;
using ktsu.BlastMerge.ConsoleApp.Text;
using ktsu.BlastMerge.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for find files operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
/// <param name="fileFinder">The file finder service.</param>
/// <param name="historyInput">The history input service.</param>
public class FindFilesMenuHandler(ApplicationService applicationService, FileFinder fileFinder, AppDataHistoryInput historyInput) : BaseMenuHandler(applicationService)
{
	private readonly FileFinder fileFinder = fileFinder ?? throw new ArgumentNullException(nameof(fileFinder));
	private readonly AppDataHistoryInput historyInput = historyInput ?? throw new ArgumentNullException(nameof(historyInput));

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

		AnsiConsole.Status()
			.Start("Finding files...", ctx =>
			{
				IReadOnlyCollection<string> filePaths = FileFinder.FindFiles([], directory, fileName, [], null, path =>
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
