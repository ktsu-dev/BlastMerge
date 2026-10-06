// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Cli.Services;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="ConsoleApplicationService"/>, driven through its public operations with queued
/// console input and real files in a temporary directory.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ConsoleApplicationServiceTests : ConsoleTestBase
{
	private const int MainMenuBatchOperations = 3;
	private const int MainMenuRunRecentBatch = 4;
	private const int MainMenuHelp = 6;
	private const int MainMenuExit = 7;

	private const int FileActionViewDetailedList = 0;
	private const int FileActionShowDifferences = 1;
	private const int FileActionSyncFiles = 3;
	private const int FileActionReturnToMainMenu = 4;

	private readonly ConsoleApplicationService service = new();

	/// <summary>
	/// Empties the saved batches, the recent batch and the input history, so each test starts from a known state.
	/// </summary>
	[TestInitialize]
	public void ResetSharedAppData() => ResetAppData();

	/// <summary>
	/// Removes every saved batch and the recent batch from the shared application data.
	/// </summary>
	internal static void ResetAppData()
	{
		foreach (BatchConfiguration batch in AppDataBatchManager.GetAllBatches())
		{
			AppDataBatchManager.DeleteBatch(batch.Name);
		}

		BlastMergeAppData appData = BlastMergeAppData.Get();
		appData.RecentBatch = null;
		appData.InputHistory.Clear();
	}

	/// <summary>
	/// Runs an action with standard output captured, for the parts of the service that write with
	/// <see cref="System.Console"/> rather than through Spectre.Console.
	/// </summary>
	/// <param name="action">The action to run.</param>
	/// <returns>Everything written to standard output.</returns>
	private static string CaptureStandardOutput(Action action)
	{
		TextWriter original = System.Console.Out;
		using StringWriter captured = new();
		System.Console.SetOut(captured);
		try
		{
			action();
		}
		finally
		{
			System.Console.SetOut(original);
		}

		return captured.ToString();
	}

	/// <summary>
	/// Saves a batch configuration to the isolated application data.
	/// </summary>
	/// <param name="name">The batch name.</param>
	/// <param name="description">The batch description.</param>
	/// <param name="searchPaths">The configured search paths, if any.</param>
	/// <param name="patterns">The file patterns.</param>
	/// <returns>The saved batch.</returns>
	private static BatchConfiguration SaveBatch(string name, string description, IEnumerable<string>? searchPaths, params string[] patterns)
	{
		BatchConfiguration batch = new()
		{
			Name = name,
			Description = description,
			FilePatterns = [.. patterns],
			SearchPaths = [.. searchPaths ?? []],
		};
		Assert.IsTrue(AppDataBatchManager.SaveBatch(batch));
		return batch;
	}

	/// <summary>
	/// Writes two copies of a file whose middle line differs, in two sibling folders.
	/// </summary>
	/// <param name="fileName">The file name to use in both folders.</param>
	/// <returns>The two file paths.</returns>
	private (string First, string Second) WriteDivergentPair(string fileName)
	{
		string first = WriteFile(Path.Join("one", fileName), "alpha\nfirst version\nomega\n");
		string second = WriteFile(Path.Join("two", fileName), "alpha\nsecond version\nomega\n");
		return (first, second);
	}

	/// <summary>
	/// Writes two identical copies of one version of a file and one copy of a version whose middle line
	/// differs. Iterative merge only proceeds when some content group holds more than one file.
	/// </summary>
	/// <param name="fileName">The file name to use in each folder.</param>
	/// <param name="common">The content of the two identical copies.</param>
	/// <param name="other">The content of the third copy.</param>
	/// <returns>The three file paths.</returns>
	private string[] WriteMergeableTriple(string fileName, string common, string other) =>
	[
		WriteFile(Path.Join("one", fileName), common),
		WriteFile(Path.Join("two", fileName), common),
		WriteFile(Path.Join("three", fileName), other),
	];

	/// <summary>
	/// Asserts that every file has the same content, and returns it.
	/// </summary>
	/// <param name="paths">The files to compare.</param>
	/// <returns>The shared content.</returns>
	private static string AssertAllEqual(string[] paths)
	{
		string expected = File.ReadAllText(paths[0]);
		foreach (string path in paths)
		{
			Assert.AreEqual(expected, File.ReadAllText(path), path);
		}

		return expected;
	}

	/// <summary>
	/// Listing with nothing saved says so and explains that defaults can be created.
	/// </summary>
	[TestMethod]
	public void ListBatches_WithNoBatches_ReportsNone()
	{
		string output = CaptureStandardOutput(service.ListBatches);

		StringAssert.Contains(output, "Available batch configurations:");
		StringAssert.Contains(output, "No batch configurations found.");
	}

	/// <summary>
	/// Listing shows each batch's name, description and pattern count, and omits an empty description.
	/// </summary>
	[TestMethod]
	public void ListBatches_WithBatches_ListsEachOne()
	{
		SaveBatch("Config Files", "Shared configuration", null, "*.json", ".editorconfig");
		SaveBatch("Docs", string.Empty, null, "README.md");

		string output = CaptureStandardOutput(service.ListBatches);

		StringAssert.Contains(output, "  - Config Files");
		StringAssert.Contains(output, "    Shared configuration");
		StringAssert.Contains(output, "    Patterns: 2");
		StringAssert.Contains(output, "  - Docs");
		StringAssert.Contains(output, "    Patterns: 1");
		Assert.IsFalse(output.Contains("No batch configurations found.", StringComparison.Ordinal), output);
	}

	/// <summary>
	/// Processing files in a directory that does not exist is refused before anything is searched.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_MissingDirectory_Throws()
	{
		string missing = Path.Join(TempDirectory, "missing");

		Assert.ThrowsExactly<DirectoryNotFoundException>(() => service.ProcessFiles(missing, "*.txt"));
	}

	/// <summary>
	/// A pattern that matches nothing warns and asks no question.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_NoMatches_Warns()
	{
		WriteFile("other.md", "content");

		service.ProcessFiles(TempDirectory, "*.txt");

		StringAssert.Contains(Output, "No files found matching the pattern.");
	}

	/// <summary>
	/// Matching files are summarised in a table grouped by content before the user picks an action.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_WithMatches_ShowsGroupSummary()
	{
		WriteFile(Path.Join("a", "settings.txt"), "same");
		WriteFile(Path.Join("b", "settings.txt"), "same");
		WriteFile(Path.Join("c", "settings.txt"), "different");
		SelectIndex(FileActionReturnToMainMenu);

		service.ProcessFiles(TempDirectory, "settings.txt");

		StringAssert.Contains(Output, "Found 3 files in 2 groups:");
		StringAssert.Contains(Output, "Multiple identical copies");
		StringAssert.Contains(Output, "Unique");
		StringAssert.Contains(Output, "settings.txt");
	}

	/// <summary>
	/// A group whose file names run past fifty characters is truncated in the summary table.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_LongFileNames_AreTruncatedInTheSummary()
	{
		string longName = new string('n', 60) + ".txt";
		WriteFile(longName, "content");
		SelectIndex(FileActionReturnToMainMenu);

		service.ProcessFiles(TempDirectory, "*.txt");

		StringAssert.Contains(Output, new string('n', 47) + "...");
	}

	/// <summary>
	/// Choosing the detailed file list lists every matched file.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_ViewDetailedFileList_ListsTheFiles()
	{
		WriteFile(Path.Join("a", "notes.txt"), "one");
		WriteFile(Path.Join("b", "notes.txt"), "one");
		SelectIndex(FileActionViewDetailedList);
		PressAnyKey();

		service.ProcessFiles(TempDirectory, "notes.txt");

		StringAssert.Contains(Output, "Found 2 files in 1 groups:");
		StringAssert.Contains(Output, "(2 files)");
		StringAssert.Contains(Output, "Hash:");
	}

	/// <summary>
	/// Choosing to show differences, when no content group holds more than one file, has nothing to compare.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_ShowDifferencesWithoutDuplicates_Warns()
	{
		WriteDivergentPair("notes.txt");
		SelectIndex(FileActionShowDifferences);

		service.ProcessFiles(TempDirectory, "notes.txt");

		StringAssert.Contains(Output, "No groups with multiple files to compare.");
	}

	/// <summary>
	/// Choosing to sync with no duplicated group has nothing to do and says so.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_SyncFilesWithoutDuplicates_Warns()
	{
		WriteDivergentPair("notes.txt");
		SelectIndex(FileActionSyncFiles);

		service.ProcessFiles(TempDirectory, "notes.txt");

		StringAssert.Contains(Output, "No groups with multiple files to sync.");
	}

	/// <summary>
	/// Processing an unknown batch reports it and does not record it as the recent batch.
	/// </summary>
	[TestMethod]
	public void ProcessBatch_UnknownBatch_ReportsNotFound()
	{
		service.ProcessBatch(TempDirectory, "Nope");

		StringAssert.Contains(Output, "Batch configuration 'Nope' not found.");
		StringAssert.Contains(Output, "List Batches");
		Assert.IsNull(BlastMergeAppData.Get().RecentBatch);
	}

	/// <summary>
	/// Processing a batch in a directory that does not exist is refused.
	/// </summary>
	[TestMethod]
	public void ProcessBatch_MissingDirectory_Throws()
	{
		SaveBatch("Docs", string.Empty, null, "README.md");

		Assert.ThrowsExactly<DirectoryNotFoundException>(() => service.ProcessBatch(Path.Join(TempDirectory, "missing"), "Docs"));
	}

	/// <summary>
	/// A batch whose patterns need no merge completes with a per-pattern summary, and is recorded as
	/// the most recently used batch.
	/// </summary>
	[TestMethod]
	public void ProcessBatch_WithoutConflicts_SummarisesEachPattern()
	{
		SaveBatch("Repo Files", "Files every repository has", null, "README.md", "LICENSE", "*.cfg", ".gitignore");
		WriteFile(Path.Join("one", "README.md"), "same");
		WriteFile(Path.Join("two", "README.md"), "same");
		WriteFile(Path.Join("one", "LICENSE"), "only one");
		WriteFile(Path.Join("one", "app.cfg"), "config");

		service.ProcessBatch(TempDirectory, "repo files");

		StringAssert.Contains(Output, "Found batch configuration: Repo Files");
		StringAssert.Contains(Output, "Files every repository has");
		StringAssert.Contains(Output, "Batch processing completed!");
		StringAssert.Contains(Output, "Identical");
		StringAssert.Contains(Output, "Single file");
		StringAssert.Contains(Output, "app.cfg (*.cfg)");
		Assert.AreEqual("repo files", BlastMergeAppData.Get().RecentBatch?.BatchName);
	}

	/// <summary>
	/// A batch pattern with two differing versions is merged block by block through the console, and
	/// both copies end up with the chosen content.
	/// </summary>
	[TestMethod]
	public void ProcessBatch_WithConflict_MergesUsingTheChosenVersion()
	{
		SaveBatch("Notes", string.Empty, null, "notes.txt");
		(string first, string second) = WriteDivergentPair("notes.txt");
		SelectIndex(0);

		service.ProcessBatch(TempDirectory, "Notes");

		Assert.AreEqual(File.ReadAllText(first), File.ReadAllText(second));
		StringAssert.Contains(Output, "Replace");
		StringAssert.Contains(Output, "Batch processing completed!");
	}

	/// <summary>
	/// Iterative merge with only one version of each file shows the summary and merges nothing.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_WithNothingToMerge_ShowsOnlyTheSummary()
	{
		WriteFile("single.txt", "content");

		service.RunIterativeMerge(TempDirectory, "single.txt");

		StringAssert.Contains(Output, "Found 1 files in 1 groups:");
		Assert.AreEqual("content", File.ReadAllText(Path.Join(TempDirectory, "single.txt")));
	}

	/// <summary>
	/// Iterative merge in a directory that does not exist is refused.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_MissingDirectory_Throws() =>
		Assert.ThrowsExactly<DirectoryNotFoundException>(() => service.RunIterativeMerge(Path.Join(TempDirectory, "missing"), "*.txt"));

	/// <summary>
	/// Two differing versions with no duplicated copy are summarised but not merged: iterative merge
	/// only starts when some content group holds more than one file.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_TwoUniqueVersions_LeavesThemAlone()
	{
		(string first, string second) = WriteDivergentPair("notes.txt");

		service.RunIterativeMerge(TempDirectory, "notes.txt");

		StringAssert.Contains(Output, "Found 2 files in 2 groups:");
		StringAssert.Contains(File.ReadAllText(first), "first version");
		StringAssert.Contains(File.ReadAllText(second), "second version");
	}

	/// <summary>
	/// Choosing one side of a replaced block writes that side, and only that side, to every copy.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_ChoosingOneVersion_WritesItToEveryCopy()
	{
		string[] paths = WriteMergeableTriple("notes.txt", "alpha\nfirst version\nomega\n", "alpha\nsecond version\nomega\n");
		SelectIndex(0);

		service.RunIterativeMerge(TempDirectory, "notes.txt");

		string merged = AssertAllEqual(paths);
		Assert.IsTrue(merged.Contains("first version", StringComparison.Ordinal) ^ merged.Contains("second version", StringComparison.Ordinal), merged);
		StringAssert.Contains(Output, "Content differs. What to do?");
	}

	/// <summary>
	/// Choosing both versions of a replaced block keeps both lines in every copy.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_ChoosingBoth_KeepsBothLines()
	{
		string[] paths = WriteMergeableTriple("notes.txt", "alpha\nfirst version\nomega\n", "alpha\nsecond version\nomega\n");
		SelectIndex(2);

		service.RunIterativeMerge(TempDirectory, "notes.txt");

		string merged = AssertAllEqual(paths);
		StringAssert.Contains(merged, "first version");
		StringAssert.Contains(merged, "second version");
	}

	/// <summary>
	/// Content present in only one version is offered on its own, and taking the first option keeps it.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_OneSidedBlock_CanBeKept()
	{
		string[] paths = WriteMergeableTriple("list.txt", "alpha\nomega\n", "alpha\nmiddle\nomega\n");
		SelectIndex(0);

		service.RunIterativeMerge(TempDirectory, "list.txt");

		StringAssert.Contains(AssertAllEqual(paths), "middle");
	}

	/// <summary>
	/// Content present in only one version is dropped when the second option is taken.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_OneSidedBlock_CanBeDropped()
	{
		string[] paths = WriteMergeableTriple("list.txt", "alpha\nomega\n", "alpha\nmiddle\nomega\n");
		SelectIndex(1);

		service.RunIterativeMerge(TempDirectory, "list.txt");

		Assert.IsFalse(AssertAllEqual(paths).Contains("middle", StringComparison.Ordinal));
	}

	/// <summary>
	/// Choosing Exit from the main menu ends interactive mode with the goodbye screen.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_Exit_ShowsGoodbye()
	{
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "Main Menu");
		Assert.AreEqual(0, NavigationHistory.Count);
	}

	/// <summary>
	/// Running the recent batch when none has been run explains the shortcut and returns to the menu.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_RunRecentBatchWithNoneRecorded_ExplainsTheShortcut()
	{
		SelectIndex(MainMenuRunRecentBatch);
		PressAnyKey();
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "No recent batch configurations found.");
	}

	/// <summary>
	/// A recorded recent batch that has since been deleted is reported as missing.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_RunRecentBatchThatNoLongerExists_ReportsIt()
	{
		BlastMergeAppData.Get().RecentBatch = new RecentBatchInfo { BatchName = "Gone" };
		SelectIndex(MainMenuRunRecentBatch);
		PressAnyKey();
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "Run Recent Batch (Gone)");
		StringAssert.Contains(Output, "Batch configuration 'Gone' not found.");
	}

	/// <summary>
	/// A recent batch with search paths runs against them without asking for a directory.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_RunRecentBatchWithSearchPaths_RunsItAgainstThem()
	{
		string repos = Path.Join(TempDirectory, "repos");
		WriteFile(Path.Join("repos", "a", "README.md"), "same");
		WriteFile(Path.Join("repos", "b", "README.md"), "same");
		SaveBatch("Docs", string.Empty, [repos], "README.md");
		BlastMergeAppData.Get().RecentBatch = new RecentBatchInfo { BatchName = "Docs" };
		SelectIndex(MainMenuRunRecentBatch);
		PressAnyKey();
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "Using configured search paths (1 paths)");
		StringAssert.Contains(Output, "Running recent batch 'Docs' using configured search paths");
		StringAssert.Contains(Output, "Batch operation completed.");
	}

	/// <summary>
	/// A recent batch without search paths asks for a directory and runs in it.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_RunRecentBatchWithoutSearchPaths_AsksForADirectory()
	{
		WriteFile(Path.Join("x", "README.md"), "same");
		SaveBatch("Docs", string.Empty, null, "README.md");
		BlastMergeAppData.Get().RecentBatch = new RecentBatchInfo { BatchName = "Docs" };
		SelectIndex(MainMenuRunRecentBatch);
		Input.PushTextWithEnter(TempDirectory);
		PressAnyKey();
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "doesn't have search paths configured");
		StringAssert.Contains(Output, "Running recent batch 'Docs' in");
		StringAssert.Contains(Output, "Batch operation completed.");
	}

	/// <summary>
	/// Declining to give a directory for the recent batch cancels it.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_RunRecentBatchWithEmptyDirectory_Cancels()
	{
		SaveBatch("Docs", string.Empty, null, "README.md");
		BlastMergeAppData.Get().RecentBatch = new RecentBatchInfo { BatchName = "Docs" };
		SelectIndex(MainMenuRunRecentBatch);
		Input.PushTextWithEnter(string.Empty);
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.IsFalse(Output.Contains("Batch operation completed.", StringComparison.Ordinal), Output);
	}

	/// <summary>
	/// An error raised by a menu action is reported, and the loop returns to the main menu.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_ErrorInAnAction_IsReportedAndRecovered()
	{
		SaveBatch("Docs", string.Empty, null, "README.md");
		BlastMergeAppData.Get().RecentBatch = new RecentBatchInfo { BatchName = "Docs" };
		SelectIndex(MainMenuRunRecentBatch);
		Input.PushTextWithEnter(Path.Join(TempDirectory, "missing"));
		PressAnyKey();
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "Directory not found:");
		Assert.AreEqual(0, NavigationHistory.Count);
	}

	/// <summary>
	/// A submenu that returns without going back is shown again from the navigation history.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_SubmenuLeftOnTheStack_IsShownAgain()
	{
		SelectIndex(MainMenuBatchOperations);
		SelectIndex(1); // Manage batch configurations
		SelectIndex(8); // Back to batch operations
		SelectIndex(2); // Back to main menu
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "Batch Management");
		StringAssert.Contains(Output, "Select batch operation:");
	}

	/// <summary>
	/// The help menu is reachable from the main menu.
	/// </summary>
	[TestMethod]
	public void StartInteractiveMode_Help_OpensTheHelpMenu()
	{
		SelectIndex(MainMenuHelp);
		SelectIndex(10);
		SelectIndex(MainMenuExit);

		service.StartInteractiveMode();

		StringAssert.Contains(Output, "Help");
	}
}
