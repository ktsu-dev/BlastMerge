// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.Cli.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

/// <summary>
/// Tests for <see cref="InteractiveMergeService"/>, driven against real files in a temporary directory.
/// </summary>
/// <remarks>
/// The service groups its input by content hash and only merges groups holding more than one file, so
/// every group it merges starts out identical. The conflict paths are reached by changing a file of a
/// later group while the service waits for a key at the end of an earlier one.
/// </remarks>
[TestClass]
[DoNotParallelize]
public class InteractiveMergeServiceTests : ConsoleTestBase
{
	private static readonly string NewLine = Environment.NewLine;

	/// <summary>
	/// A null set of groups is rejected.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_NullGroups_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => InteractiveMergeService.PerformIterativeMerge(null!));

	/// <summary>
	/// Unique files are summarised and nothing is merged or asked.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_OnlyUniqueFiles_ShowsSummaryWithoutMerging()
	{
		string first = WriteFile("first.txt", "one");
		string second = WriteFile("second.txt", "two");
		Dictionary<string, IReadOnlyCollection<string>> groups = new()
		{
			["0123456789abcdef"] = [first],
			["abc"] = [second],
		};

		InteractiveMergeService.PerformIterativeMerge(groups);

		StringAssert.Contains(Output, "Found 2 files in 2 groups:");
		StringAssert.Contains(Output, "0 groups have multiple identical copies that can be merged.");
		StringAssert.Contains(Output, "Unique");
		StringAssert.Contains(Output, "first.txt");
		StringAssert.Contains(Output, "second.txt");
		StringAssert.Contains(Output, "01234567...");
		Assert.IsFalse(Output.Contains("0123456789", StringComparison.Ordinal), "A long hash is shortened.");
		StringAssert.Contains(Output, "abc");
		Assert.IsFalse(Output.Contains("Starting iterative merge", StringComparison.Ordinal));
		Assert.AreEqual("one", File.ReadAllText(first));
	}

	/// <summary>
	/// Filenames longer than the column are truncated, and names that look like markup are shown literally.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_SummaryTruncatesLongNamesAndEscapesMarkup()
	{
		string longName = new string('n', 60) + ".txt";
		string longFile = WriteFile(longName, "long");
		string markupFile = WriteFile("[red]x.txt", "markup");
		Dictionary<string, IReadOnlyCollection<string>> groups = new()
		{
			["hash1"] = [longFile],
			["hash2"] = [markupFile],
		};

		InteractiveMergeService.PerformIterativeMerge(groups);

		StringAssert.Contains(Output, new string('n', 47) + "...");
		Assert.IsFalse(Output.Contains(longName, StringComparison.Ordinal), "The long name is truncated.");
		StringAssert.Contains(Output, "[red]x.txt");
	}

	/// <summary>
	/// Two identical copies merge cleanly into the first, leaving its content as it was.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_TwoIdenticalCopies_MergesCleanly()
	{
		string content = Lines("alpha", "beta");
		string[] files = WriteCopies("a.txt", content, "d1", "d2");
		PressAnyKey();

		InteractiveMergeService.PerformIterativeMerge(Group(files));

		StringAssert.Contains(Output, "1 groups have multiple identical copies that can be merged.");
		StringAssert.Contains(Output, "Multiple identical copies");
		StringAssert.Contains(Output, "Starting iterative merge for 2 files...");
		StringAssert.Contains(Output, "Merging most similar files");
		StringAssert.Contains(Output, "Clean merge - no conflicts detected!");
		StringAssert.Contains(Output, "Merged successfully! Versions reduced by 1. (1 remaining)");
		StringAssert.Contains(Output, "Final merged result is saved in the remaining file.");
		Assert.IsFalse(Output.Contains("Continuing to next merge step", StringComparison.Ordinal));
		Assert.AreEqual(content, File.ReadAllText(files[0]));
		Assert.IsFalse(Input.IsKeyAvailable());
	}

	/// <summary>
	/// Three copies take two merge steps.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_ThreeCopies_TakesTwoSteps()
	{
		string[] files = WriteCopies("a.txt", Lines("alpha", "beta"), "d1", "d2", "d3");
		PressAnyKey();

		InteractiveMergeService.PerformIterativeMerge(Group(files));

		StringAssert.Contains(Output, "(2 remaining)");
		StringAssert.Contains(Output, "Continuing to next merge step...");
		StringAssert.Contains(Output, "(1 remaining)");
		StringAssert.Contains(Output, "Final merged result is saved in the remaining file.");
		Assert.IsFalse(Input.IsKeyAvailable());
	}

	/// <summary>
	/// Binary copies are never merged as text, so the group stops without a result and the bytes are untouched.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_BinaryCopies_AreNotMerged()
	{
		byte[] bytes = [0x00, 0x01, 0xFF, 0xFE, 0x00, 0x80];
		string[] files = new string[2];
		for (int i = 0; i < files.Length; i++)
		{
			files[i] = Path.Combine(TempDirectory, $"d{i}", "blob.bin");
			Directory.CreateDirectory(Path.GetDirectoryName(files[i])!);
			File.WriteAllBytes(files[i], bytes);
		}

		PressAnyKey();

		InteractiveMergeService.PerformIterativeMerge(Group(files));

		StringAssert.Contains(Output, "Could not find similar files to merge.");
		Assert.IsFalse(Output.Contains("Final merged result", StringComparison.Ordinal));
		foreach (string file in files)
		{
			Assert.AreSequenceEqual(bytes, File.ReadAllBytes(file));
		}

		Assert.IsFalse(Input.IsKeyAvailable());
	}

	/// <summary>
	/// A copy that can no longer be read is skipped, leaving nothing to merge.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_UnreadableCopy_StopsWithoutMerging()
	{
		string[] aFiles = WriteCopies("a.txt", Lines("alpha"), "d1", "d2");
		string[] bFiles = WriteCopies("b.txt", Lines("beta"), "d1", "d2");
		ScriptedConsole script = Script()
			.Then(() => File.Delete(bFiles[1]))
			.Key(ConsoleKey.Enter)
			.Key(ConsoleKey.Enter);

		InteractiveMergeService.PerformIterativeMerge(Group([.. aFiles, .. bFiles]));

		StringAssert.Contains(Output, "Could not find similar files to merge.");
		Assert.AreEqual(Lines("beta"), File.ReadAllText(bFiles[0]));
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// Accepting a conflicted merge writes the conflict markers to the first file and to every copy
	/// that matched the second.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_ConflictAccepted_WritesConflictMarkers()
	{
		string[] aFiles = WriteCopies("a.txt", Lines("alpha"), "d1", "d2");
		string[] bFiles = WriteCopies("b.txt", Lines("one", "two", "three"), "d1", "d2");
		ScriptedConsole script = Script()
			.Then(() => File.WriteAllText(bFiles[1], Lines("one", "changed", "three")))
			.Key(ConsoleKey.Enter)
			.Line("y")
			.Key(ConsoleKey.Enter);

		InteractiveMergeService.PerformIterativeMerge(Group([.. aFiles, .. bFiles]));

		StringAssert.Contains(Output, "Merge has conflicts. Displaying merge with conflict markers...");
		StringAssert.Contains(Output, "Merge Preview (first 50 lines):");
		StringAssert.Contains(Output, "<<<<<<< Version 1");
		StringAssert.Contains(Output, ">>>>>>> Version 2");
		Assert.IsFalse(Output.Contains("content truncated", StringComparison.Ordinal));

		string merged = File.ReadAllText(bFiles[0]);
		StringAssert.Contains(merged, "<<<<<<< Version 1");
		StringAssert.Contains(merged, "two");
		StringAssert.Contains(merged, "changed");
		Assert.AreEqual(merged, File.ReadAllText(bFiles[1]));
		Assert.AreEqual(2, CountOf(Output, "Final merged result is saved in the remaining file."));
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// Declining a conflicted merge leaves both files as they were.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_ConflictDeclined_LeavesFilesUntouched()
	{
		string[] aFiles = WriteCopies("a.txt", Lines("alpha"), "d1", "d2");
		string original = Lines("one", "two", "three");
		string changed = Lines("one", "changed", "three");
		string[] bFiles = WriteCopies("b.txt", original, "d1", "d2");
		ScriptedConsole script = Script()
			.Then(() => File.WriteAllText(bFiles[1], changed))
			.Key(ConsoleKey.Enter)
			.Line("n")
			.Key(ConsoleKey.Enter);

		InteractiveMergeService.PerformIterativeMerge(Group([.. aFiles, .. bFiles]));

		StringAssert.Contains(Output, "Merge cancelled. Skipping this group.");
		Assert.AreEqual(original, File.ReadAllText(bFiles[0]));
		Assert.AreEqual(changed, File.ReadAllText(bFiles[1]));
		Assert.AreEqual(1, CountOf(Output, "Final merged result is saved in the remaining file."));
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	/// <summary>
	/// A long conflicted merge is previewed only up to its first fifty lines, and content that looks like
	/// markup is shown literally.
	/// </summary>
	[TestMethod]
	public void PerformIterativeMerge_LongConflict_TruncatesPreviewAndEscapesContent()
	{
		string[] aFiles = WriteCopies("a.txt", Lines("alpha"), "d1", "d2");
		string[] body = [.. Enumerable.Range(0, 60).Select(i => $"line {i}")];
		body[1] = "[bold]not markup[/]";
		string[] changedBody = [.. body];
		changedBody[0] = "first line changed";
		string[] bFiles = WriteCopies("b.txt", Lines(body), "d1", "d2");
		ScriptedConsole script = Script()
			.Then(() => File.WriteAllText(bFiles[1], Lines(changedBody)))
			.Key(ConsoleKey.Enter)
			.Line("n")
			.Key(ConsoleKey.Enter);

		InteractiveMergeService.PerformIterativeMerge(Group([.. aFiles, .. bFiles]));

		StringAssert.Contains(Output, "... (content truncated)");
		StringAssert.Contains(Output, "[bold]not markup[/]");
		StringAssert.Contains(Output, "line 45");
		Assert.IsFalse(Output.Contains("line 59", StringComparison.Ordinal), "Lines past the fiftieth are not previewed.");
		Assert.IsFalse(script.Input.IsKeyAvailable());
	}

	private static string Lines(params string[] lines) => string.Join(NewLine, lines) + NewLine;

	private static int CountOf(string text, string value)
	{
		int count = 0;
		int index = text.IndexOf(value, StringComparison.Ordinal);
		while (index >= 0)
		{
			count++;
			index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
		}

		return count;
	}

	private static Dictionary<string, IReadOnlyCollection<string>> Group(params string[] files) =>
		new() { ["all"] = files };

	private ScriptedConsole Script()
	{
		ScriptedConsole script = new(Console);
		AnsiConsole.Console = script;
		return script;
	}

	private string[] WriteCopies(string fileName, string content, params string[] directories)
	{
		string[] paths = new string[directories.Length];
		for (int i = 0; i < directories.Length; i++)
		{
			paths[i] = WriteFile(Path.Combine(directories[i], fileName), content);
		}

		return paths;
	}
}
