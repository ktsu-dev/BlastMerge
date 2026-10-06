// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Cli.Services.MenuHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="HelpMenuHandler"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class HelpMenuHandlerTests : ConsoleTestBase
{
	private const int ApplicationOverview = 0;
	private const int FeatureGuide = 1;
	private const int KeyboardShortcuts = 2;
	private const int Back = 3;

	private HelpMenuHandler handler = null!;

	/// <summary>
	/// Creates the handler under test.
	/// </summary>
	[TestInitialize]
	public void CreateHandler() => handler = new HelpMenuHandler(new RecordingMenuApplicationService());

	/// <summary>
	/// The menu offers every help topic and a way back.
	/// </summary>
	[TestMethod]
	public void Handle_ListsEveryTopic()
	{
		SelectIndex(Back);

		handler.Handle();

		StringAssert.Contains(Output, "Help & Information");
		StringAssert.Contains(Output, "Application Overview");
		StringAssert.Contains(Output, "Feature Guide");
		StringAssert.Contains(Output, "Keyboard Shortcuts");
		StringAssert.Contains(Output, "Back to Main Menu");
	}

	/// <summary>
	/// The overview describes the application and waits for a key.
	/// </summary>
	[TestMethod]
	public void ApplicationOverview_ShowsOverviewAndWaits()
	{
		SelectIndex(ApplicationOverview);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "What is BlastMerge?");
		StringAssert.Contains(Output, "Cross-Repository File Synchronization");
		StringAssert.Contains(Output, "Press any key to continue...");
	}

	/// <summary>
	/// The overview cannot be dismissed without a key press.
	/// </summary>
	[TestMethod]
	public void ApplicationOverview_WithoutKeyPress_Throws()
	{
		SelectIndex(ApplicationOverview);

		Assert.ThrowsExactly<InvalidOperationException>(handler.Handle);
	}

	/// <summary>
	/// The feature guide lists each of the application's features.
	/// </summary>
	[TestMethod]
	public void FeatureGuide_ListsFeatures()
	{
		SelectIndex(FeatureGuide);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "Iterative Merge");
		StringAssert.Contains(Output, "Find Files");
		StringAssert.Contains(Output, "Compare Files");
		StringAssert.Contains(Output, "Batch Operations");
		StringAssert.Contains(Output, "Sync Files");
	}

	/// <summary>
	/// The shortcut table lists the keys the menus respond to.
	/// </summary>
	[TestMethod]
	public void KeyboardShortcuts_ListsKeys()
	{
		SelectIndex(KeyboardShortcuts);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "Ctrl+C");
		StringAssert.Contains(Output, "Cancel operation");
		StringAssert.Contains(Output, "Toggle selection");
	}

	/// <summary>
	/// Choosing back leaves the help menu and returns to the menu beneath it, without waiting.
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
	/// The back choice names the menu the user came from.
	/// </summary>
	[TestMethod]
	public void Back_NamesTheMenuItReturnsTo()
	{
		NavigationHistory.Push("Main Menu");
		NavigationHistory.Push("Settings");
		SelectIndex(Back);

		handler.Enter();

		StringAssert.Contains(Output, "Back to Settings");
		Assert.AreEqual("Settings", NavigationHistory.Peek());
	}

	/// <summary>
	/// Showing a topic does not leave the help menu.
	/// </summary>
	[TestMethod]
	public void Topic_StaysInHelpMenu()
	{
		NavigationHistory.Push("Main Menu");
		SelectIndex(KeyboardShortcuts);
		PressAnyKey();

		handler.Enter();

		Assert.AreEqual("Help", NavigationHistory.Peek());
	}
}
