// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Cli.Services.MenuHandlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="CompareFilesMenuHandler"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class CompareFilesMenuHandlerTests : ConsoleTestBase
{
	private const int CompareFilesInDirectory = 0;
	private const int CompareTwoDirectories = 1;
	private const int CompareTwoSpecificFiles = 2;
	private const int Back = 3;

	private RecordingMenuApplicationService service = null!;
	private CompareFilesMenuHandler handler = null!;

	/// <summary>
	/// Creates the handler under test.
	/// </summary>
	[TestInitialize]
	public void CreateHandler()
	{
		service = new RecordingMenuApplicationService();
		handler = new CompareFilesMenuHandler(service);
	}

	/// <summary>
	/// The menu offers every comparison type and a way back.
	/// </summary>
	[TestMethod]
	public void Handle_ListsEveryComparisonType()
	{
		SelectIndex(Back);

		handler.Handle();

		StringAssert.Contains(Output, "Compare Files in Directory");
		StringAssert.Contains(Output, "Compare Two Directories");
		StringAssert.Contains(Output, "Compare Two Specific Files");
	}

	/// <summary>
	/// Comparing files in a directory hands the directory and pattern to the application service.
	/// </summary>
	[TestMethod]
	public void CompareFilesInDirectory_ProcessesTheEnteredDirectoryAndPattern()
	{
		SelectIndex(CompareFilesInDirectory);
		Input.PushTextWithEnter(TempDirectory);
		Input.PushTextWithEnter("*.txt");

		handler.Handle();

		Assert.AreSequenceEqual([$"ProcessFiles({TempDirectory}, *.txt)"], service.Calls);
	}

	/// <summary>
	/// An empty directory cancels before the pattern is asked for.
	/// </summary>
	[TestMethod]
	public void CompareFilesInDirectory_EmptyDirectory_Cancels()
	{
		SelectIndex(CompareFilesInDirectory);
		Input.PushKey(ConsoleKey.Enter);

		handler.Handle();

		Assert.IsEmpty(service.Calls);
		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.DoesNotContain("filename pattern", Output);
	}

	/// <summary>
	/// An empty pattern cancels without processing anything.
	/// </summary>
	[TestMethod]
	public void CompareFilesInDirectory_EmptyPattern_Cancels()
	{
		SelectIndex(CompareFilesInDirectory);
		Input.PushTextWithEnter(TempDirectory);
		Input.PushKey(ConsoleKey.Enter);

		handler.Handle();

		Assert.IsEmpty(service.Calls);
		StringAssert.Contains(Output, "Operation cancelled.");
	}

	/// <summary>
	/// Comparing two directories reports what the directories have in common.
	/// </summary>
	[TestMethod]
	public void CompareTwoDirectories_ReportsTheComparison()
	{
		WriteFile(Path.Join("left", "same.txt"), "same");
		WriteFile(Path.Join("right", "same.txt"), "same");
		SelectIndex(CompareTwoDirectories);
		Input.PushTextWithEnter(Path.Join(TempDirectory, "left"));
		Input.PushTextWithEnter(Path.Join(TempDirectory, "right"));
		Input.PushKey(ConsoleKey.Enter); // default pattern
		Input.PushKey(ConsoleKey.Enter); // not recursive
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "Identical Files");
		StringAssert.Contains(Output, "same.txt");
		Assert.IsEmpty(service.Calls);
	}

	/// <summary>
	/// Comparing two specific files reports whether they match.
	/// </summary>
	[TestMethod]
	public void CompareTwoSpecificFiles_ReportsTheComparison()
	{
		string first = WriteFile("a.txt", "content");
		string second = WriteFile("b.txt", "content");
		SelectIndex(CompareTwoSpecificFiles);
		Input.PushTextWithEnter(first);
		Input.PushTextWithEnter(second);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "Files are identical!");
		Assert.IsEmpty(service.Calls);
	}

	/// <summary>
	/// Choosing back leaves the compare menu.
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
}
