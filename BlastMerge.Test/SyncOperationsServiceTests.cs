// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Cli.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

/// <summary>
/// Tests for <see cref="SyncOperationsService"/>, driven against real files in a temporary directory.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SyncOperationsServiceTests : ConsoleTestBase
{
	private const string Original = "original content";
	private const string Fresh = "fresh content";

	private static readonly DateTime Oldest = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime Middle = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime Newest = new(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// A null set of groups is rejected.
	/// </summary>
	[TestMethod]
	public void OfferSyncOptions_NullGroups_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => SyncOperationsService.OfferSyncOptions(null!));

	/// <summary>
	/// When no file has an identical copy there is nothing to sync, and nothing is asked.
	/// </summary>
	[TestMethod]
	public void OfferSyncOptions_NoDuplicates_WarnsWithoutPrompting()
	{
		string first = WriteFile("first.txt", "one");
		string second = WriteFile("second.txt", "two");

		SyncOperationsService.OfferSyncOptions(Groups(first, second));

		StringAssert.Contains(Output, "No groups with multiple files to sync.");
		Assert.IsFalse(Input.IsKeyAvailable());
	}

	/// <summary>
	/// Choosing to go back leaves every file untouched.
	/// </summary>
	[TestMethod]
	public void OfferSyncOptions_Back_ChangesNothing()
	{
		string[] files = WriteCopies("a.txt", "d1", "d2");
		ScriptedConsole script = Script()
			.Then(() => File.WriteAllText(files[0], Fresh))
			.Select(2);

		SyncOperationsService.OfferSyncOptions(Groups(files));

		StringAssert.Contains(Output, "Found 1 groups with multiple identical copies.");
		Assert.AreEqual(Fresh, File.ReadAllText(files[0]));
		Assert.AreEqual(Original, File.ReadAllText(files[1]));
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// Declining the sync-to-newest confirmation cancels it.
	/// </summary>
	[TestMethod]
	public void SyncToNewest_Declined_CancelsWithoutCopying()
	{
		string[] files = WriteCopies("a.txt", "d1", "d2");
		ScriptedConsole script = Script()
			.Select(0)
			.Then(() => WriteDated(files[1], Fresh, Newest))
			.Line("n");

		SyncOperationsService.OfferSyncOptions(Groups(files));

		StringAssert.Contains(Output, "Sync cancelled.");
		Assert.AreEqual(Original, File.ReadAllText(files[0]));
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// Syncing to the newest version copies the most recently written file over every other copy.
	/// </summary>
	[TestMethod]
	public void SyncToNewest_Confirmed_CopiesTheNewestFileOverTheOthers()
	{
		string[] files = WriteCopies("a.txt", "d1", "d2", "d3");
		File.SetLastWriteTimeUtc(files[0], Oldest);
		File.SetLastWriteTimeUtc(files[2], Middle);
		ScriptedConsole script = Script()
			.Select(0)
			.Then(() => WriteDated(files[1], Fresh, Newest))
			.Line("y")
			.Key(ConsoleKey.Enter);

		SyncOperationsService.OfferSyncOptions(Groups(files));

		foreach (string file in files)
		{
			Assert.AreEqual(Fresh, File.ReadAllText(file), file);
		}

		StringAssert.Contains(Output, "Sync completed! 2 files synchronized across 1 groups.");
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// A copy that cannot be written is reported, and the others are still synced and counted.
	/// </summary>
	[TestMethod]
	public void SyncToNewest_TargetCannotBeWritten_ReportsItAndSyncsTheRest()
	{
		string[] files = WriteCopies("a.txt", "d1", "d2", "d3");
		File.SetLastWriteTimeUtc(files[0], Newest);
		File.SetLastWriteTimeUtc(files[1], Middle);
		File.SetLastWriteTimeUtc(files[2], Oldest);
		ScriptedConsole script = Script()
			.Select(0)
			.Then(() =>
			{
				WriteDated(files[0], Fresh, Newest);
				BlockDirectory("d3");
			})
			.Line("y")
			.Key(ConsoleKey.Enter);

		SyncOperationsService.OfferSyncOptions(Groups(files));

		StringAssert.Contains(Output, "Failed to sync");
		StringAssert.Contains(Output, "Sync completed! 1 files synchronized across 1 groups.");
		Assert.AreEqual(Fresh, File.ReadAllText(files[1]));
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// Choosing a reference file copies that file, not the first or the newest, over the others.
	/// </summary>
	[TestMethod]
	public void ChooseReference_Confirmed_CopiesTheChosenFileOverTheOthers()
	{
		string[] files = WriteCopies("a.txt", "d1", "d2", "d3");
		File.SetLastWriteTimeUtc(files[2], Newest);
		ScriptedConsole script = Script()
			.Select(1)
			.Then(() => WriteDated(files[1], Fresh, Oldest))
			.Select(1)
			.Line("y")
			.Key(ConsoleKey.Enter);

		SyncOperationsService.OfferSyncOptions(Groups(files));

		foreach (string file in files)
		{
			Assert.AreEqual(Fresh, File.ReadAllText(file), file);
		}

		StringAssert.Contains(Output, "Group 1 of 1");
		StringAssert.Contains(Output, "✓ Synced a.txt");
		StringAssert.Contains(Output, "Sync completed! 2 files synchronized.");
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// Declining a group leaves it untouched and moves on to the next group.
	/// </summary>
	[TestMethod]
	public void ChooseReference_GroupDeclined_SkipsOnlyThatGroup()
	{
		string[] aFiles = WriteCopies("a.txt", "d1", "d2");
		string[] bFiles = WriteCopies("b.txt", "d1", "d2");
		ScriptedConsole script = Script()
			.Select(1)
			.Then(() =>
			{
				File.WriteAllText(aFiles[0], Fresh);
				File.WriteAllText(bFiles[0], Fresh);
			})
			.Select(0)
			.Line("n")
			.Select(0)
			.Line("y")
			.Key(ConsoleKey.Enter);

		SyncOperationsService.OfferSyncOptions(Groups([.. aFiles, .. bFiles]));

		Assert.AreEqual(Original, File.ReadAllText(aFiles[1]));
		Assert.AreEqual(Fresh, File.ReadAllText(bFiles[1]));
		StringAssert.Contains(Output, "Group 2 of 2");
		StringAssert.Contains(Output, "Sync completed! 1 files synchronized.");
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// When every copy in a group fails to sync, the failure is reported and the group counts as skipped.
	/// </summary>
	[TestMethod]
	public void ChooseReference_AllTargetsFail_ReportsFailureAndSkipsGroup()
	{
		string[] files = WriteCopies("a.txt", "d1", "d2");
		ScriptedConsole script = Script()
			.Select(1)
			.Select(0)
			.Then(() => BlockDirectory("d2"))
			.Line("y")
			.Key(ConsoleKey.Enter);

		SyncOperationsService.OfferSyncOptions(Groups(files));

		StringAssert.Contains(Output, "✗ Failed to sync a.txt");
		StringAssert.Contains(Output, "Skipped this group.");
		StringAssert.Contains(Output, "Sync completed! 0 files synchronized.");
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// A file that disappears before its group is shown is listed as an error rather than failing the
	/// whole operation, and choosing a surviving reference restores it.
	/// </summary>
	[TestMethod]
	public void ChooseReference_FileMissingWhenListed_ShowsErrorRow()
	{
		string[] aFiles = WriteCopies("a.txt", "d1", "d2");
		string[] bFiles = WriteCopies("b.txt", "d1", "d2");
		ScriptedConsole script = Script()
			.Select(1)
			.Select(0)
			.Then(() => File.Delete(bFiles[1]))
			.Line("n")
			.Select(0)
			.Line("y")
			.Key(ConsoleKey.Enter);

		SyncOperationsService.OfferSyncOptions(Groups([.. aFiles, .. bFiles]));

		StringAssert.Contains(Output, "Error");
		Assert.AreEqual(Original, File.ReadAllText(bFiles[1]));
		StringAssert.Contains(Output, "Sync completed! 1 files synchronized.");
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	private static Dictionary<string, IReadOnlyCollection<string>> Groups(params string[] files)
	{
		Dictionary<string, IReadOnlyCollection<string>> groups = [];
		for (int i = 0; i < files.Length; i++)
		{
			groups[$"group{i}"] = [files[i]];
		}

		return groups;
	}

	private static void WriteDated(string path, string content, DateTime lastWriteUtc)
	{
		File.WriteAllText(path, content);
		File.SetLastWriteTimeUtc(path, lastWriteUtc);
	}

	private ScriptedConsole Script()
	{
		ScriptedConsole script = new(Console);
		AnsiConsole.Console = script;
		return script;
	}

	private string[] WriteCopies(string fileName, params string[] directories)
	{
		string[] paths = new string[directories.Length];
		for (int i = 0; i < directories.Length; i++)
		{
			paths[i] = WriteFile(Path.Join(directories[i], fileName), Original);
		}

		return paths;
	}

	/// <summary>
	/// Replaces a directory with a file of the same name, so nothing can be written beneath it.
	/// </summary>
	private void BlockDirectory(string directory)
	{
		string path = Path.Join(TempDirectory, directory);
		Directory.Delete(path, recursive: true);
		File.WriteAllText(path, "blocker");
	}
}
