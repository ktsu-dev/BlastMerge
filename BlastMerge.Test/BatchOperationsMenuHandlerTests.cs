// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using ktsu.BlastMerge.Cli.Services;
using ktsu.BlastMerge.Cli.Services.Common;
using ktsu.BlastMerge.Cli.Services.MenuHandlers;
using ktsu.BlastMerge.Cli.Text;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="BatchOperationsMenuHandler"/>, driving the batch run and management flows with
/// queued console input against isolated application data.
/// </summary>
[TestClass]
[DoNotParallelize]
public class BatchOperationsMenuHandlerTests : ConsoleTestBase
{
	private const int RunBatch = 0;
	private const int ManageBatches = 1;
	private const int BackFromBatchOperations = 2;

	private const int List = 0;
	private const int Create = 1;
	private const int View = 2;
	private const int Edit = 3;
	private const int Duplicate = 4;
	private const int Delete = 5;
	private const int Export = 6;
	private const int Import = 7;
	private const int BackFromManagement = 8;

	private readonly BatchOperationsMenuHandler handler = new(new ConsoleApplicationService());

	/// <summary>
	/// Empties the shared application data, which outlives a test because
	/// <see cref="BlastMergeAppData.ResetForTesting"/> does not currently reset the singleton.
	/// </summary>
	[TestInitialize]
	public void ResetSharedAppData() => ConsoleApplicationServiceTests.ResetAppData();

	/// <summary>
	/// Saves a batch configuration to the isolated application data.
	/// </summary>
	/// <param name="name">The batch name.</param>
	/// <param name="patterns">The file patterns.</param>
	/// <returns>The saved batch.</returns>
	private static BatchConfiguration SaveBatch(string name, params string[] patterns)
	{
		BatchConfiguration batch = new()
		{
			Name = name,
			Description = $"{name} description",
			FilePatterns = [.. patterns],
		};
		Assert.IsTrue(AppDataBatchManager.SaveBatch(batch));
		return batch;
	}

	/// <summary>
	/// Queues text answers to consecutive prompts.
	/// </summary>
	/// <param name="answers">The answers, each followed by Enter.</param>
	private void Answer(params string[] answers)
	{
		foreach (string answer in answers)
		{
			Input.PushTextWithEnter(answer);
		}
	}

	/// <summary>
	/// Opens batch management, queues one management operation, and leaves management again.
	/// </summary>
	/// <param name="operation">The index of the management operation.</param>
	/// <param name="queueFlow">Queues the input the operation needs.</param>
	private void Manage(int operation, Action queueFlow)
	{
		SelectIndex(ManageBatches);
		SelectIndex(operation);
		queueFlow();
		SelectIndex(BackFromManagement);

		handler.Handle();
	}

	/// <summary>
	/// Going back from batch operations removes the menu from the navigation history.
	/// </summary>
	[TestMethod]
	public void Enter_Back_ReturnsToThePreviousMenu()
	{
		NavigationHistory.Push(MenuNames.MainMenu);
		SelectIndex(BackFromBatchOperations);

		handler.Enter();

		Assert.AreEqual(1, NavigationHistory.Count);
		Assert.AreEqual(MenuNames.MainMenu, NavigationHistory.Peek());
		StringAssert.Contains(Output, "Select batch operation:");
	}

	/// <summary>
	/// Leaving batch management returns to the caller with the batch operations menu still current.
	/// </summary>
	[TestMethod]
	public void Enter_ManageThenBack_LeavesBatchOperationsOnTheStack()
	{
		NavigationHistory.Push(MenuNames.MainMenu);
		SelectIndex(ManageBatches);
		SelectIndex(BackFromManagement);

		handler.Enter();

		Assert.AreEqual(MenuNames.BatchOperations, NavigationHistory.Peek());
		StringAssert.Contains(Output, "Select batch management operation:");
	}

	/// <summary>
	/// Listing with nothing saved says so.
	/// </summary>
	[TestMethod]
	public void List_WithNoBatches_ReportsNone()
	{
		Manage(List, PressAnyKey);

		StringAssert.Contains(Output, "No batch configurations found.");
	}

	/// <summary>
	/// Listing shows a table of every saved batch.
	/// </summary>
	[TestMethod]
	public void List_WithBatches_ShowsEachOne()
	{
		SaveBatch("Alpha", "*.txt", "*.md");
		SaveBatch("Beta", "*.cs");

		Manage(List, PressAnyKey);

		StringAssert.Contains(Output, "Alpha description");
		StringAssert.Contains(Output, "Beta description");
		Assert.IsFalse(Output.Contains("No batch configurations found.", StringComparison.Ordinal), Output);
	}

	/// <summary>
	/// Creating a batch records every answer, rejects a pattern naming a folder, and strips a leading
	/// recursive wildcard.
	/// </summary>
	[TestMethod]
	public void Create_WithEveryOption_SavesTheBatch()
	{
		string folderPattern = string.Join("/", "src", "*.cs");

		Manage(Create, () =>
		{
			Answer("My Batch", "Shared files");
			Answer("*.txt", folderPattern, "**/README.md", string.Empty);
			Answer("y", "repos", string.Empty);
			Answer("y", "*/bin/*", string.Empty);
			Answer("n", "y");
			PressAnyKey();
		});

		BatchConfiguration? batch = AppDataBatchManager.LoadBatch("My Batch");
		Assert.IsNotNull(batch);
		Assert.AreEqual("Shared files", batch.Description);
		Assert.AreSequenceEqual(["*.txt", "README.md"], batch.FilePatterns);
		Assert.AreSequenceEqual(["repos"], batch.SearchPaths);
		Assert.AreSequenceEqual(["*/bin/*"], batch.PathExclusionPatterns);
		Assert.IsFalse(batch.SkipEmptyPatterns);
		Assert.IsTrue(batch.PromptBeforeEachPattern);
		StringAssert.Contains(Output, "cannot contain a directory separator");
		StringAssert.Contains(Output, "Batch configuration 'My Batch' created successfully!");
	}

	/// <summary>
	/// An empty name cancels creation before anything else is asked.
	/// </summary>
	[TestMethod]
	public void Create_WithEmptyName_Cancels()
	{
		Manage(Create, () => Answer(string.Empty));

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.IsEmpty(AppDataBatchManager.GetAllBatches());
	}

	/// <summary>
	/// Reusing a name and declining to overwrite leaves the existing batch alone.
	/// </summary>
	[TestMethod]
	public void Create_ExistingNameNotOverwritten_KeepsTheOriginal()
	{
		SaveBatch("Docs", "README.md");

		Manage(Create, () => Answer("Docs", "n"));

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.AreSequenceEqual(["README.md"], AppDataBatchManager.LoadBatch("Docs")!.FilePatterns);
	}

	/// <summary>
	/// Reusing a name and agreeing to overwrite replaces the existing batch.
	/// </summary>
	[TestMethod]
	public void Create_ExistingNameOverwritten_ReplacesIt()
	{
		SaveBatch("Docs", "README.md");

		Manage(Create, () =>
		{
			Answer("Docs", "y", "Replaced");
			Answer("*.md", string.Empty);
			Answer("n", "n", "y", "n");
			PressAnyKey();
		});

		BatchConfiguration? batch = AppDataBatchManager.LoadBatch("Docs");
		Assert.IsNotNull(batch);
		Assert.AreEqual("Replaced", batch.Description);
		Assert.AreSequenceEqual(["*.md"], batch.FilePatterns);
		Assert.IsEmpty(batch.SearchPaths);
		Assert.IsTrue(batch.SkipEmptyPatterns);
	}

	/// <summary>
	/// A batch with no file patterns is refused and not saved.
	/// </summary>
	[TestMethod]
	public void Create_WithoutPatterns_IsRefused()
	{
		Manage(Create, () =>
		{
			Answer("Empty", string.Empty);
			Answer(string.Empty);
			Answer("n", "n", "y", "n");
		});

		StringAssert.Contains(Output, "At least one file pattern is required.");
		Assert.IsNull(AppDataBatchManager.LoadBatch("Empty"));
	}

	/// <summary>
	/// Running with nothing saved says there is nothing to run.
	/// </summary>
	[TestMethod]
	public void Run_WithNoBatches_ReportsNone()
	{
		SelectIndex(RunBatch);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "No batch configurations available.");
	}

	/// <summary>
	/// A batch with search paths runs against them and is recorded as the recent batch.
	/// </summary>
	[TestMethod]
	public void Run_WithSearchPaths_ProcessesTheBatch()
	{
		string repos = Path.Combine(TempDirectory, "repos");
		WriteFile(Path.Combine("repos", "a", "README.md"), "same");
		WriteFile(Path.Combine("repos", "b", "README.md"), "same");
		BatchConfiguration batch = new()
		{
			Name = "Docs",
			FilePatterns = ["README.md"],
			SearchPaths = [repos],
		};
		Assert.IsTrue(AppDataBatchManager.SaveBatch(batch));
		SelectIndex(RunBatch);
		SelectIndex(0);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "Using configured search paths (1 paths)");
		StringAssert.Contains(Output, "using configured search paths");
		StringAssert.Contains(Output, "Batch operation completed.");
		Assert.AreEqual("Docs", BlastMergeAppData.Get().RecentBatch?.BatchName);
	}

	/// <summary>
	/// A batch without search paths asks for the directory to run in.
	/// </summary>
	[TestMethod]
	public void Run_WithoutSearchPaths_AsksForADirectory()
	{
		WriteFile(Path.Combine("a", "README.md"), "same");
		SaveBatch("Alpha", "*.txt");
		SaveBatch("Docs", "README.md");
		SelectIndex(RunBatch);
		SelectIndex(1);
		Answer(TempDirectory);
		PressAnyKey();

		handler.Handle();

		StringAssert.Contains(Output, "doesn't have search paths configured");
		StringAssert.Contains(Output, "Processing batch configuration 'Docs' in");
		StringAssert.Contains(Output, "Batch operation completed.");
		Assert.AreEqual("Docs", BlastMergeAppData.Get().RecentBatch?.BatchName);
	}

	/// <summary>
	/// Giving no directory cancels the run.
	/// </summary>
	[TestMethod]
	public void Run_WithEmptyDirectory_Cancels()
	{
		SaveBatch("Docs", "README.md");
		SelectIndex(RunBatch);
		SelectIndex(0);
		Answer(string.Empty);

		handler.Handle();

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.IsNull(BlastMergeAppData.Get().RecentBatch);
	}

	/// <summary>
	/// Viewing a batch shows its patterns, search paths and exclusions.
	/// </summary>
	[TestMethod]
	public void View_ShowsEveryPartOfTheBatch()
	{
		BatchConfiguration batch = new()
		{
			Name = "Full",
			Description = "Everything set",
			FilePatterns = ["*.json"],
			SearchPaths = ["first-path"],
			PathExclusionPatterns = ["*/node_modules/*"],
		};
		Assert.IsTrue(AppDataBatchManager.SaveBatch(batch));

		Manage(View, () =>
		{
			SelectIndex(0);
			PressAnyKey();
		});

		StringAssert.Contains(Output, "Batch Details: Full");
		StringAssert.Contains(Output, "Everything set");
		StringAssert.Contains(Output, "*.json");
		StringAssert.Contains(Output, "first-path");
		StringAssert.Contains(Output, "*/node_modules/*");
	}

	/// <summary>
	/// Viewing a batch with no search paths or exclusions says so.
	/// </summary>
	[TestMethod]
	public void View_WithoutOptionalParts_ExplainsTheDefaults()
	{
		SaveBatch("Plain", "*.txt");

		Manage(View, () =>
		{
			SelectIndex(0);
			PressAnyKey();
		});

		StringAssert.Contains(Output, "No custom search paths configured");
		StringAssert.Contains(Output, "No path exclusion patterns configured");
	}

	/// <summary>
	/// Choosing a batch to view with none saved says there are none.
	/// </summary>
	[TestMethod]
	public void View_WithNoBatches_ReportsNone()
	{
		Manage(View, PressAnyKey);

		StringAssert.Contains(Output, "No batch configurations available.");
	}

	/// <summary>
	/// Editing replaces every part of a batch the user chooses to modify.
	/// </summary>
	[TestMethod]
	public void Edit_ModifyingEverything_SavesTheChanges()
	{
		BatchConfiguration original = new()
		{
			Name = "Docs",
			Description = "Old",
			FilePatterns = ["README.md"],
			SearchPaths = ["old-path"],
			PathExclusionPatterns = ["*/old/*"],
		};
		Assert.IsTrue(AppDataBatchManager.SaveBatch(original));

		Manage(Edit, () =>
		{
			SelectIndex(0);
			Answer("New");
			Answer("y", "*.md", " LICENSE ", string.Empty);
			Answer("y", "new-path", string.Empty);
			Answer("y", "*/obj/*", string.Empty);
			Answer("n", "y");
			PressAnyKey();
		});

		BatchConfiguration? batch = AppDataBatchManager.LoadBatch("Docs");
		Assert.IsNotNull(batch);
		Assert.AreEqual("New", batch.Description);
		Assert.AreSequenceEqual(["*.md", "LICENSE"], batch.FilePatterns);
		Assert.AreSequenceEqual(["new-path"], batch.SearchPaths);
		Assert.AreSequenceEqual(["*/obj/*"], batch.PathExclusionPatterns);
		Assert.IsFalse(batch.SkipEmptyPatterns);
		Assert.IsTrue(batch.PromptBeforeEachPattern);
		StringAssert.Contains(Output, "Batch configuration 'Docs' updated successfully!");
	}

	/// <summary>
	/// Editing that keeps the defaults, and enters no replacement patterns, leaves the batch as it was.
	/// </summary>
	[TestMethod]
	public void Edit_KeepingDefaults_LeavesTheBatchUnchanged()
	{
		BatchConfiguration original = new()
		{
			Name = "Docs",
			Description = "Kept",
			FilePatterns = ["README.md"],
		};
		Assert.IsTrue(AppDataBatchManager.SaveBatch(original));

		Manage(Edit, () =>
		{
			SelectIndex(0);
			Answer(string.Empty);
			Answer("y", string.Empty);
			Answer("n", "n");
			Answer(string.Empty, string.Empty);
			PressAnyKey();
		});

		BatchConfiguration? batch = AppDataBatchManager.LoadBatch("Docs");
		Assert.IsNotNull(batch);
		Assert.AreEqual("Kept", batch.Description);
		Assert.AreSequenceEqual(["README.md"], batch.FilePatterns);
		Assert.IsEmpty(batch.SearchPaths);
		Assert.IsTrue(batch.SkipEmptyPatterns);
		Assert.IsFalse(batch.PromptBeforeEachPattern);
		StringAssert.Contains(Output, "Keeping original patterns.");
		StringAssert.Contains(Output, "None (uses runtime directory)");
	}

	/// <summary>
	/// Duplicating copies the patterns under the new name with a description naming the source.
	/// </summary>
	[TestMethod]
	public void Duplicate_CopiesTheBatch()
	{
		SaveBatch("Docs", "README.md", "*.md");

		Manage(Duplicate, () =>
		{
			SelectIndex(0);
			Answer("Docs Copy");
			PressAnyKey();
		});

		BatchConfiguration? copy = AppDataBatchManager.LoadBatch("Docs Copy");
		Assert.IsNotNull(copy);
		Assert.AreSequenceEqual(["README.md", "*.md"], copy.FilePatterns);
		StringAssert.Contains(copy.Description, "Docs");
		Assert.IsNotNull(AppDataBatchManager.LoadBatch("Docs"));
		StringAssert.Contains(Output, "created as a copy of 'Docs'");
	}

	/// <summary>
	/// An empty name cancels duplication.
	/// </summary>
	[TestMethod]
	public void Duplicate_WithEmptyName_Cancels()
	{
		SaveBatch("Docs", "README.md");

		Manage(Duplicate, () =>
		{
			SelectIndex(0);
			Answer(string.Empty);
		});

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.HasCount(1, AppDataBatchManager.GetAllBatches());
	}

	/// <summary>
	/// Duplicating onto an existing name and declining to overwrite leaves the target alone.
	/// </summary>
	[TestMethod]
	public void Duplicate_OntoExistingNameNotOverwritten_KeepsTheTarget()
	{
		SaveBatch("Alpha", "*.txt");
		SaveBatch("Docs", "README.md");

		Manage(Duplicate, () =>
		{
			SelectIndex(1);
			Answer("Alpha", "n");
		});

		StringAssert.Contains(Output, "Operation cancelled.");
		Assert.AreSequenceEqual(["*.txt"], AppDataBatchManager.LoadBatch("Alpha")!.FilePatterns);
	}

	/// <summary>
	/// Duplicating onto an existing name and agreeing to overwrite replaces the target.
	/// </summary>
	[TestMethod]
	public void Duplicate_OntoExistingNameOverwritten_ReplacesTheTarget()
	{
		SaveBatch("Alpha", "*.txt");
		SaveBatch("Docs", "README.md");

		Manage(Duplicate, () =>
		{
			SelectIndex(1);
			Answer("Alpha", "y");
			PressAnyKey();
		});

		Assert.AreSequenceEqual(["README.md"], AppDataBatchManager.LoadBatch("Alpha")!.FilePatterns);
	}

	/// <summary>
	/// Confirming a delete removes the batch.
	/// </summary>
	[TestMethod]
	public void Delete_Confirmed_RemovesTheBatch()
	{
		SaveBatch("Docs", "README.md");

		Manage(Delete, () =>
		{
			SelectIndex(0);
			Answer("y");
			PressAnyKey();
		});

		Assert.IsNull(AppDataBatchManager.LoadBatch("Docs"));
		StringAssert.Contains(Output, "Batch configuration 'Docs' deleted successfully!");
	}

	/// <summary>
	/// Declining a delete keeps the batch.
	/// </summary>
	[TestMethod]
	public void Delete_Declined_KeepsTheBatch()
	{
		SaveBatch("Docs", "README.md");

		Manage(Delete, () =>
		{
			SelectIndex(0);
			Answer("n");
			PressAnyKey();
		});

		Assert.IsNotNull(AppDataBatchManager.LoadBatch("Docs"));
		StringAssert.Contains(Output, "Operation cancelled.");
	}

	/// <summary>
	/// Exporting with nothing saved says there is nothing to export.
	/// </summary>
	[TestMethod]
	public void Export_WithNoBatches_ReportsNone()
	{
		Manage(Export, PressAnyKey);

		StringAssert.Contains(Output, "No batch configurations to export.");
	}

	/// <summary>
	/// Exporting writes every batch as camel-cased JSON to the chosen file.
	/// </summary>
	[TestMethod]
	public void Export_ToAPath_WritesTheBatches()
	{
		SaveBatch("Alpha", "*.txt");
		SaveBatch("Docs", "README.md");
		string exportPath = Path.Combine(TempDirectory, "batches.json");

		Manage(Export, () =>
		{
			Answer(exportPath);
			PressAnyKey();
		});

		Assert.IsTrue(File.Exists(exportPath));
		string json = File.ReadAllText(exportPath);
		StringAssert.Contains(json, "\"name\": \"Alpha\"");
		StringAssert.Contains(json, "\"filePatterns\"");
		StringAssert.Contains(json, "README.md");
		StringAssert.Contains(Output, "Exported 2 batch configurations");
	}

	/// <summary>
	/// Exporting with no path writes a timestamped file to the working directory.
	/// </summary>
	[TestMethod]
	public void Export_WithoutAPath_UsesADefaultFileName()
	{
		SaveBatch("Docs", "README.md");

		Manage(Export, () =>
		{
			Answer(string.Empty);
			PressAnyKey();
		});

		string[] exported = Directory.GetFiles(TempDirectory, "BlastMerge_Batches_*.json");
		Assert.HasCount(1, exported);
		StringAssert.Contains(File.ReadAllText(exported[0]), "README.md");
	}

	/// <summary>
	/// Exporting into a folder that does not exist reports the error rather than throwing.
	/// </summary>
	[TestMethod]
	public void Export_ToAMissingFolder_ReportsTheError()
	{
		SaveBatch("Docs", "README.md");
		string exportPath = Path.Combine(TempDirectory, "missing", "batches.json");

		Manage(Export, () =>
		{
			Answer(exportPath);
			PressAnyKey();
			PressAnyKey();
		});

		StringAssert.Contains(Output, "Directory not found for path");
		Assert.IsFalse(File.Exists(exportPath));
	}

	/// <summary>
	/// An empty import path cancels the import.
	/// </summary>
	[TestMethod]
	public void Import_WithEmptyPath_Cancels()
	{
		Manage(Import, () =>
		{
			Answer(string.Empty);
			PressAnyKey();
		});

		StringAssert.Contains(Output, "Operation cancelled.");
	}

	/// <summary>
	/// Importing a file that does not exist reports it.
	/// </summary>
	[TestMethod]
	public void Import_MissingFile_ReportsIt()
	{
		Manage(Import, () =>
		{
			Answer(Path.Combine(TempDirectory, "missing.json"));
			PressAnyKey();
			PressAnyKey();
		});

		StringAssert.Contains(Output, "File not found:");
	}

	/// <summary>
	/// Importing a file that is not JSON reports the format error.
	/// </summary>
	[TestMethod]
	public void Import_InvalidJson_ReportsIt()
	{
		string path = WriteFile("bad.json", "this is not json");

		Manage(Import, () =>
		{
			Answer(path);
			PressAnyKey();
			PressAnyKey();
		});

		StringAssert.Contains(Output, "Invalid JSON format");
		Assert.IsEmpty(AppDataBatchManager.GetAllBatches());
	}

	/// <summary>
	/// Importing an empty list has nothing to import.
	/// </summary>
	[TestMethod]
	public void Import_EmptyList_ReportsNothingToImport()
	{
		string path = WriteFile("empty.json", "[]");

		Manage(Import, () =>
		{
			Answer(path);
			PressAnyKey();
		});

		StringAssert.Contains(Output, "No valid batch configurations found in the file.");
	}

	/// <summary>
	/// Declining the import preview imports nothing.
	/// </summary>
	[TestMethod]
	public void Import_Declined_ImportsNothing()
	{
		string path = WriteFile("one.json", "[{\"name\":\"Incoming\",\"filePatterns\":[\"*.md\"]}]");

		Manage(Import, () =>
		{
			Answer(path, "n");
			PressAnyKey();
		});

		StringAssert.Contains(Output, "Found 1 batch configurations to import:");
		StringAssert.Contains(Output, "Import cancelled.");
		Assert.IsNull(AppDataBatchManager.LoadBatch("Incoming"));
	}

	/// <summary>
	/// Importing saves new batches, asks before overwriting existing ones, and skips invalid ones,
	/// then summarises the counts.
	/// </summary>
	[TestMethod]
	public void Import_MixedFile_ImportsSkipsAndRejects()
	{
		SaveBatch("Kept", "README.md");
		SaveBatch("Replaced", "README.md");
		string path = WriteFile("mixed.json", """
			[
				{ "name": "Incoming", "filePatterns": ["*.md"] },
				{ "name": "Kept", "filePatterns": ["*.kept"] },
				{ "name": "Replaced", "filePatterns": ["*.new"] },
				{ "name": "Invalid", "filePatterns": [] }
			]
			""");

		Manage(Import, () =>
		{
			Answer(path, "y");
			Answer("n", "y");
			PressAnyKey();
		});

		Assert.AreSequenceEqual(["*.md"], AppDataBatchManager.LoadBatch("Incoming")!.FilePatterns);
		Assert.AreSequenceEqual(["README.md"], AppDataBatchManager.LoadBatch("Kept")!.FilePatterns);
		Assert.AreSequenceEqual(["*.new"], AppDataBatchManager.LoadBatch("Replaced")!.FilePatterns);
		Assert.IsNull(AppDataBatchManager.LoadBatch("Invalid"));
		StringAssert.Contains(Output, "Skipping invalid batch: Invalid");
		StringAssert.Contains(Output, "Skipped existing batch: Kept");
		StringAssert.Contains(Output, "Imported: 2");
		StringAssert.Contains(Output, "Skipped: 1");
		StringAssert.Contains(Output, "Errors: 1");
	}

	/// <summary>
	/// A batch exported and then imported over an empty store comes back with the same patterns.
	/// </summary>
	[TestMethod]
	public void ExportThenImport_RoundTripsTheBatches()
	{
		SaveBatch("Docs", "README.md", "*.md");
		string exportPath = Path.Combine(TempDirectory, "roundtrip.json");

		Manage(Export, () =>
		{
			Answer(exportPath);
			PressAnyKey();
		});
		Assert.IsTrue(AppDataBatchManager.DeleteBatch("Docs"));

		Manage(Import, () =>
		{
			Answer(exportPath, "y");
			PressAnyKey();
		});

		Assert.AreSequenceEqual(["README.md", "*.md"], AppDataBatchManager.LoadBatch("Docs")!.FilePatterns);
	}
}
