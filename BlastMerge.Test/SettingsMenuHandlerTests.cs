// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Cli.Models;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Cli.Services.MenuHandlers;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AppDataStorage = ktsu.AppDataStorage.AppData;

/// <summary>
/// Tests for <see cref="SettingsMenuHandler"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SettingsMenuHandlerTests : ConsoleTestBase
{
	private const int ViewConfigurationPaths = 0;
	private const int ClearInputHistory = 1;
	private const int ViewStatistics = 2;
	private const int Back = 3;

	private SettingsMenuHandler handler = null!;

	/// <summary>
	/// Creates the handler under test over empty application data.
	/// </summary>
	/// <remarks>
	/// The application data is one instance shared by every test, so history left by an earlier
	/// test would otherwise still be there.
	/// </remarks>
	[TestInitialize]
	public void CreateHandler()
	{
		SharedAppDataState.Reset();
		handler = new SettingsMenuHandler(new RecordingMenuApplicationService());
	}

	/// <summary>
	/// Leaves the shared application data empty for whichever test runs next.
	/// </summary>
	[TestCleanup]
	public void RestoreAppData() => SharedAppDataState.Reset();

	/// <summary>
	/// The menu offers every settings option and a way back.
	/// </summary>
	[TestMethod]
	public void Handle_ListsEveryOption()
	{
		SelectIndex(Back);

		handler.Handle();

		StringAssert.Contains(Output, "Configuration & Settings");
		StringAssert.Contains(Output, "View Configuration Paths");
		StringAssert.Contains(Output, "Clear Input History");
		StringAssert.Contains(Output, "View Statistics");
	}

	/// <summary>
	/// The configuration paths report empty input history when nothing has been entered.
	/// </summary>
	[TestMethod]
	public void ConfigurationPaths_WithNoHistory_ReportsEmpty()
	{
		SelectIndex(ViewConfigurationPaths);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "Config Directory");
		StringAssert.Contains(Output, "Working Directory");
		StringAssert.Contains(Output, "Empty");
		Assert.DoesNotContain("Has Data", Output);
	}

	/// <summary>
	/// The configuration paths report stored input history once there is some.
	/// </summary>
	[TestMethod]
	public void ConfigurationPaths_WithHistory_ReportsHasData()
	{
		BlastMergeAppData.Get().InputHistory["directory"] = ["somewhere"];
		SelectIndex(ViewConfigurationPaths);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "Has Data");
	}

	/// <summary>
	/// Confirming the clear removes every history entry.
	/// </summary>
	[TestMethod]
	public void ClearInputHistory_Confirmed_ClearsHistory()
	{
		BlastMergeAppData.Get().InputHistory["directory"] = ["first", "second"];
		BlastMergeAppData.Get().InputHistory["filename"] = ["*.txt"];
		SelectIndex(ClearInputHistory);
		Input.PushTextWithEnter("y");
		PressAnyKey();

		handler.Handle();

		Assert.IsEmpty(AppDataHistoryInput.GetAllHistory());
		StringAssert.Contains(Output, "Input history cleared successfully.");
	}

	/// <summary>
	/// Declining the clear leaves the history as it was.
	/// </summary>
	[TestMethod]
	public void ClearInputHistory_Declined_KeepsHistory()
	{
		BlastMergeAppData.Get().InputHistory["directory"] = ["first", "second"];
		SelectIndex(ClearInputHistory);
		Input.PushTextWithEnter("n");
		PressAnyKey();

		handler.Handle();

		IReadOnlyDictionary<string, IReadOnlyList<string>> history = AppDataHistoryInput.GetAllHistory();
		Assert.HasCount(1, history);
		Assert.AreSequenceEqual(["first", "second"], history["directory"]);
		StringAssert.Contains(Output, "Operation cancelled.");
	}

	/// <summary>
	/// A clear whose save fails is reported rather than crashing the menu.
	/// </summary>
	[TestMethod]
	public void ClearInputHistory_SaveFails_ReportsError()
	{
		MockFileSystem fileSystem = SharedAppDataState.UseInspectableFileSystem();
		fileSystem.AddFile(AppDataStorage.Path.ToString(), new MockFileData("a file where the directory should be"));
		BlastMergeAppData.Get().InputHistory["directory"] = ["first"];
		SelectIndex(ClearInputHistory);
		Input.PushTextWithEnter("y");
		PressAnyKey();
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "Failed to clear input history");
		Assert.DoesNotContain("Input history cleared successfully.", Output);
	}

	/// <summary>
	/// The statistics panel reports runtime, memory and process information for this process.
	/// </summary>
	[TestMethod]
	public void Statistics_ReportsProcessInformation()
	{
		SelectIndex(ViewStatistics);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "System Information");
		StringAssert.Contains(Output, "Runtime Information");
		StringAssert.Contains(Output, "Managed Memory:");
		StringAssert.Contains(Output, "Process ID: " + Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
	}

	/// <summary>
	/// Choosing back leaves the settings menu.
	/// </summary>
	[TestMethod]
	public void Back_ReturnsToPreviousMenu()
	{
		NavigationHistory.Push("Main Menu");
		SelectIndex(Back);

		handler.Enter();

		Assert.AreEqual(1, NavigationHistory.Count);
		Assert.AreEqual("Main Menu", NavigationHistory.Peek());
	}

	/// <summary>
	/// Viewing an option keeps the user in the settings menu.
	/// </summary>
	[TestMethod]
	public void Option_StaysInSettingsMenu()
	{
		NavigationHistory.Push("Main Menu");
		SelectIndex(ViewStatistics);
		PressAnyKey();

		handler.Enter();

		Assert.AreEqual("Settings", NavigationHistory.Peek());
	}
}
