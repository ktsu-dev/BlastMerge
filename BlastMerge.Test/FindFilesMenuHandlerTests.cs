// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Cli.Services.MenuHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="FindFilesMenuHandler"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class FindFilesMenuHandlerTests : ConsoleTestBase
{
	private RecordingMenuApplicationService service = null!;
	private FindFilesMenuHandler handler = null!;

	/// <summary>
	/// Creates the handler under test, entered from the main menu.
	/// </summary>
	[TestInitialize]
	public void CreateHandler()
	{
		service = new RecordingMenuApplicationService();
		handler = new FindFilesMenuHandler(service);
		NavigationHistory.Push("Main Menu");
	}

	/// <summary>
	/// Every matching file in the directory tree is listed with its size, and the menu then returns.
	/// </summary>
	[TestMethod]
	public void Handle_ListsMatchingFilesThenGoesBack()
	{
		WriteFile(Path.Join("one", "settings.json"), "12345");
		WriteFile(Path.Join("two", "deeper", "settings.json"), "1234567");
		WriteFile(Path.Join("two", "other.json"), "{}");
		Input.PushTextWithEnter(TempDirectory);
		Input.PushTextWithEnter("settings.json");
		PressAnyKey();

		handler.Enter();

		StringAssert.Contains(Output, "Find & Process Files");
		StringAssert.Contains(Output, "Found 2 files.");
		StringAssert.Contains(Output, "5 bytes");
		StringAssert.Contains(Output, "7 bytes");
		Assert.DoesNotContain("other.json", Output);
		Assert.AreEqual(1, NavigationHistory.Count);
		Assert.AreEqual("Main Menu", NavigationHistory.Peek());
		Assert.IsEmpty(service.Calls);
	}

	/// <summary>
	/// A pattern that matches nothing says so, and still waits and returns.
	/// </summary>
	[TestMethod]
	public void Handle_NoMatches_ReportsNothingFound()
	{
		WriteFile("present.txt", "x");
		Input.PushTextWithEnter(TempDirectory);
		Input.PushTextWithEnter("absent.txt");
		PressAnyKey();

		handler.Enter();

		StringAssert.Contains(Output, "No files found matching the pattern.");
		Assert.DoesNotContain("Found 0 files", Output);
		Assert.AreEqual("Main Menu", NavigationHistory.Peek());
	}

	/// <summary>
	/// An empty directory cancels without searching or waiting.
	/// </summary>
	[TestMethod]
	public void Handle_EmptyDirectory_Cancels()
	{
		Input.PushKey(ConsoleKey.Enter);

		handler.Handle();

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.DoesNotContain("filename pattern", Output);
		Assert.DoesNotContain("Press any key", Output);
	}

	/// <summary>
	/// An empty pattern cancels without searching or waiting.
	/// </summary>
	[TestMethod]
	public void Handle_EmptyPattern_Cancels()
	{
		Input.PushTextWithEnter(TempDirectory);
		Input.PushKey(ConsoleKey.Enter);

		handler.Handle();

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.DoesNotContain("Press any key", Output);
	}
}
