// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Cli.Models;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="AppDataHistoryInput"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class AppDataHistoryInputTests : ConsoleTestBase
{
	private const string DirectoryPrompt = "[cyan]Enter directory path[/]";

	private MockFileSystem fileSystem = null!;

	/// <summary>
	/// Starts from empty history and default settings, saving to a file system the test can read.
	/// The application data is one instance shared by every test, so history left by an earlier
	/// test would otherwise still be there.
	/// </summary>
	[TestInitialize]
	public void IsolateAppData()
	{
		fileSystem = SharedAppDataState.UseInspectableFileSystem();
		SharedAppDataState.Reset();
	}

	/// <summary>
	/// Leaves the shared application data empty, with default settings, for whichever test runs next.
	/// </summary>
	[TestCleanup]
	public void RestoreAppData() => SharedAppDataState.Reset();

	/// <summary>
	/// The typed answer is returned and remembered under the prompt's key.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_ReturnsInputAndRecordsIt()
	{
		Input.PushTextWithEnter("first");

		string result = AppDataHistoryInput.AskWithHistory(DirectoryPrompt);

		Assert.AreEqual("first", result);
		StringAssert.Contains(Output, "Enter directory path");
		Assert.AreSequenceEqual(["first"], AppDataHistoryInput.GetAllHistory()["directory"]);
	}

	/// <summary>
	/// The key is the word after "Enter", with markup stripped, so related prompts share history.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_KeysHistoryByWordAfterEnter()
	{
		Input.PushTextWithEnter("a");
		Input.PushTextWithEnter("b");
		Input.PushTextWithEnter("c");

		AppDataHistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");
		AppDataHistoryInput.AskWithHistory("Enter filename to look for");
		AppDataHistoryInput.AskWithHistory("Enter path");

		Assert.AreEqual(2, AppDataHistoryInput.GetHistoryCount("filename"));
		Assert.AreEqual(1, AppDataHistoryInput.GetHistoryCount("path"));
	}

	/// <summary>
	/// A prompt without "Enter" is keyed by its whole text, trimmed.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_PromptWithoutEnter_KeyedByWholePrompt()
	{
		Input.PushTextWithEnter("blue");

		AppDataHistoryInput.AskWithHistory("  Favourite colour?  ");

		Assert.AreEqual(1, AppDataHistoryInput.GetHistoryCount("Favourite colour?"));
	}

	/// <summary>
	/// Repeating an earlier answer moves it to the most recent position rather than duplicating it.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_RepeatedAnswer_MovesToEnd()
	{
		Input.PushTextWithEnter("one");
		Input.PushTextWithEnter("two");
		Input.PushTextWithEnter("one");

		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);
		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);
		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);

		Assert.AreSequenceEqual(["two", "one"], AppDataHistoryInput.GetAllHistory()["directory"]);
	}

	/// <summary>
	/// History is trimmed to the configured maximum, dropping the oldest entries.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_TrimsToMaximumEntries()
	{
		BlastMergeAppData.Get().Settings.MaxHistoryEntriesPerPrompt = 2;
		Input.PushTextWithEnter("one");
		Input.PushTextWithEnter("two");
		Input.PushTextWithEnter("three");

		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);
		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);
		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);

		Assert.AreSequenceEqual(["two", "three"], AppDataHistoryInput.GetAllHistory()["directory"]);
	}

	/// <summary>
	/// An empty answer is returned as empty and not remembered.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_EmptyAnswer_NotRecorded()
	{
		Input.PushKey(ConsoleKey.Enter);

		string result = AppDataHistoryInput.AskWithHistory(DirectoryPrompt);

		Assert.AreEqual(string.Empty, result);
		Assert.AreEqual(0, AppDataHistoryInput.GetHistoryCount("directory"));
	}

	/// <summary>
	/// An empty answer to a prompt with a default returns the default and remembers it.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_EmptyAnswerWithDefault_ReturnsAndRecordsDefault()
	{
		Input.PushKey(ConsoleKey.Enter);

		string result = AppDataHistoryInput.AskWithHistory("[cyan]Enter file pattern:[/]", "*.*");

		Assert.AreEqual("*.*", result);
		Assert.AreSequenceEqual(["*.*"], AppDataHistoryInput.GetAllHistory()["file"]);
	}

	/// <summary>
	/// A typed answer takes precedence over the default.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_TypedAnswerOverridesDefault()
	{
		Input.PushTextWithEnter("*.cs");

		Assert.AreEqual("*.cs", AppDataHistoryInput.AskWithHistory("[cyan]Enter file pattern:[/]", "*.*"));
	}

	/// <summary>
	/// With auto-save on, a recorded answer is written to disk straight away.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_AutoSave_PersistsHistory()
	{
		Input.PushTextWithEnter("persisted");

		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);

		string? saved = SharedAppDataState.ReadSaved(fileSystem);
		Assert.IsNotNull(saved);
		StringAssert.Contains(saved, "persisted");
	}

	/// <summary>
	/// With auto-save off, a recorded answer is held in memory but not yet written.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_AutoSaveOff_DefersSave()
	{
		BlastMergeAppData.Get().Settings.AutoSaveEnabled = false;
		Input.PushTextWithEnter("deferred");

		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);

		Assert.AreEqual(1, AppDataHistoryInput.GetHistoryCount("directory"));
		Assert.IsNull(SharedAppDataState.ReadSaved(fileSystem));
	}

	/// <summary>
	/// A null prompt or default is rejected.
	/// </summary>
	[TestMethod]
	public void AskWithHistory_NullArguments_Throw()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => AppDataHistoryInput.AskWithHistory(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => AppDataHistoryInput.AskWithHistory("prompt", null!));
	}

	/// <summary>
	/// Clearing removes every prompt's history and persists the clear.
	/// </summary>
	[TestMethod]
	public void ClearAllHistory_RemovesEverything()
	{
		Input.PushTextWithEnter("remembered-directory");
		Input.PushTextWithEnter("remembered-pattern");
		AppDataHistoryInput.AskWithHistory(DirectoryPrompt);
		AppDataHistoryInput.AskWithHistory("[cyan]Enter filename pattern[/]");

		AppDataHistoryInput.ClearAllHistory();

		Assert.IsEmpty(AppDataHistoryInput.GetAllHistory());
		string? saved = SharedAppDataState.ReadSaved(fileSystem);
		Assert.IsNotNull(saved);
		Assert.DoesNotContain("remembered-directory", saved);
		Assert.DoesNotContain("remembered-pattern", saved);
	}

	/// <summary>
	/// Clearing with auto-save off still empties the in-memory history.
	/// </summary>
	[TestMethod]
	public void ClearAllHistory_AutoSaveOff_ClearsInMemory()
	{
		BlastMergeAppData appData = BlastMergeAppData.Get();
		appData.Settings.AutoSaveEnabled = false;
		appData.InputHistory["directory"] = ["somewhere"];

		AppDataHistoryInput.ClearAllHistory();

		Assert.IsEmpty(AppDataHistoryInput.GetAllHistory());
	}

	/// <summary>
	/// The count for a prompt nobody has answered is zero.
	/// </summary>
	[TestMethod]
	public void GetHistoryCount_UnknownKey_IsZero() =>
		Assert.AreEqual(0, AppDataHistoryInput.GetHistoryCount("never asked"));

	/// <summary>
	/// The history returned is a snapshot that callers cannot use to change the stored history.
	/// </summary>
	[TestMethod]
	public void GetAllHistory_ReturnsReadOnlyView()
	{
		BlastMergeAppData.Get().InputHistory["directory"] = ["somewhere"];

		IReadOnlyDictionary<string, IReadOnlyList<string>> history = AppDataHistoryInput.GetAllHistory();
		IList<string> entries = (IList<string>)history["directory"];

		Assert.IsTrue(entries.IsReadOnly);
		Assert.ThrowsExactly<NotSupportedException>(() => entries.Add("elsewhere"));
		Assert.AreEqual(1, AppDataHistoryInput.GetHistoryCount("directory"));
	}
}
