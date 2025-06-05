// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Services.MenuHandlers;

using ktsu.BlastMerge.Core.Services;
using Spectre.Console;

/// <summary>
/// Menu handler for configuration and settings operations.
/// </summary>
/// <param name="applicationService">The application service.</param>
public class SettingsMenuHandler(ApplicationService applicationService) : BaseMenuHandler(applicationService)
{
	/// <summary>
	/// Handles the settings menu operation.
	/// </summary>
	public override void Handle()
	{
		ShowMenuTitle("Configuration & Settings");

		Dictionary<string, SettingsChoice> settingsChoices = new()
		{
			["📁 View Configuration Paths"] = SettingsChoice.ViewConfigurationPaths,
			["🧹 Clear Input History"] = SettingsChoice.ClearInputHistory,
			["📊 View Statistics"] = SettingsChoice.ViewStatistics,
			["🔙 Back to Main Menu"] = SettingsChoice.BackToMainMenu
		};

		string selection = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[cyan]Select settings option:[/]")
				.AddChoices(settingsChoices.Keys));

		if (settingsChoices.TryGetValue(selection, out SettingsChoice choice))
		{
			switch (choice)
			{
				case SettingsChoice.ViewConfigurationPaths:
					ShowConfigurationPaths();
					break;
				case SettingsChoice.ClearInputHistory:
					ClearInputHistory();
					break;
				case SettingsChoice.ViewStatistics:
					ShowStatistics();
					break;
				case SettingsChoice.BackToMainMenu:
					// Return to main menu
					break;
				default:
					// Unknown choice - do nothing
					break;
			}
		}
	}

	/// <summary>
	/// Shows configuration paths.
	/// </summary>
	private static void ShowConfigurationPaths()
	{
		ShowMenuTitle("Configuration Paths");

		Table table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("[bold]Item[/]")
			.AddColumn("[bold]Path[/]")
			.AddColumn("[bold]Status[/]");

		// Get application data folder
		string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string configDir = Path.Combine(appDataPath, "BlastMerge");

		table.AddRow(
			"[cyan]Config Directory[/]",
			$"[dim]{configDir}[/]",
			Directory.Exists(configDir) ? "[green]Exists[/]" : "[yellow]Not Created[/]");

		string historyFile = Path.Combine(configDir, "input_history.json");
		table.AddRow(
			"[cyan]Input History[/]",
			$"[dim]{historyFile}[/]",
			File.Exists(historyFile) ? "[green]Exists[/]" : "[yellow]Not Created[/]");

		table.AddRow(
			"[cyan]Working Directory[/]",
			$"[dim]{Environment.CurrentDirectory}[/]",
			"[green]Current[/]");

		table.AddRow(
			"[cyan]Temp Directory[/]",
			$"[dim]{Path.GetTempPath()}[/]",
			"[green]Available[/]");

		AnsiConsole.Write(table);
		WaitForKeyPress();
	}

	/// <summary>
	/// Clears input history.
	/// </summary>
	private static void ClearInputHistory()
	{
		ShowMenuTitle("Clear Input History");

		bool confirm = AnsiConsole.Confirm("[yellow]Are you sure you want to clear all input history?[/]");

		if (confirm)
		{
			try
			{
				string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string historyFile = Path.Combine(appDataPath, "BlastMerge", "input_history.json");

				if (File.Exists(historyFile))
				{
					File.Delete(historyFile);
					ShowSuccess("Input history cleared successfully.");
				}
				else
				{
					ShowWarning("No input history file found.");
				}
			}
			catch (UnauthorizedAccessException ex)
			{
				ShowError($"Failed to clear input history (access denied): {ex.Message}");
			}
			catch (IOException ex)
			{
				ShowError($"Failed to clear input history (I/O error): {ex.Message}");
			}
		}
		else
		{
			ShowWarning("Operation cancelled.");
		}

		WaitForKeyPress();
	}

	/// <summary>
	/// Shows application statistics.
	/// </summary>
	private static void ShowStatistics()
	{
		ShowMenuTitle("Application Statistics");

		Panel statistics = new("""
			[bold cyan]Runtime Information[/]

			• [green]Operating System:[/] {0}
			• [green]Runtime Version:[/] {1}
			• [green]Working Directory:[/] {2}
			• [green]Available Memory:[/] {3:N0} MB

			[bold cyan]Application Information[/]

			• [green]Process ID:[/] {4}
			• [green]Thread Count:[/] {5}
			• [green]Start Time:[/] {6}
			""".Replace("{0}", Environment.OSVersion.ToString())
			   .Replace("{1}", Environment.Version.ToString())
			   .Replace("{2}", Environment.CurrentDirectory)
			   .Replace("{3}", (GC.GetTotalMemory(false) / 1024 / 1024).ToString())
			   .Replace("{4}", Environment.ProcessId.ToString())
			   .Replace("{5}", Environment.ProcessorCount.ToString())
			   .Replace("{6}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
		{
			Header = new PanelHeader("[bold]System Information[/]"),
			Border = BoxBorder.Rounded
		};

		AnsiConsole.Write(statistics);
		WaitForKeyPress();
	}

	/// <summary>
	/// Settings menu choices.
	/// </summary>
	private enum SettingsChoice
	{
		ViewConfigurationPaths,
		ClearInputHistory,
		ViewStatistics,
		BackToMainMenu
	}
}
