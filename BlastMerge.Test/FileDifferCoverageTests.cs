// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the parts of <see cref="FileDiffer"/> the other FileDiffer suites leave untouched: the
/// colour output, the change summaries, directory comparison, hash-only grouping and file syncing.
/// </summary>
[TestClass]
public class FileDifferCoverageTests : MockFileSystemTestBase
{
	private const string Reset = "\u001b[0m";

	/// <summary>
	/// Hash-only grouping puts identical content together even when the file names differ.
	/// </summary>
	[TestMethod]
	public void GroupFilesByHashOnly_GroupsIdenticalContentRegardlessOfName()
	{
		string first = CreateFile("first.txt", "same content");
		string second = CreateFile(Path.Combine("sub", "second.txt"), "same content");
		string third = CreateFile("third.txt", "other content");

		IReadOnlyCollection<FileGroup> groups = FileDiffer.GroupFilesByHashOnly([first, second, third], MockFileSystem);

		Assert.HasCount(2, groups);
		FileGroup shared = groups.Single(g => g.FilePaths.Count == 2);
		Assert.AreSequenceEqual([first, second], shared.FilePaths);
		Assert.AreEqual(FileHasher.ComputeFileHash(first, MockFileSystem), shared.Hash);
		FileGroup single = groups.Single(g => g.FilePaths.Count == 1);
		Assert.AreEqual(third, single.FilePaths.Single());
	}

	/// <summary>
	/// Directory comparison sorts files into identical, modified and one-sided sets.
	/// </summary>
	[TestMethod]
	public void FindDifferences_Directories_ClassifiesEveryFile()
	{
		string dir1 = CreateDirectory("left");
		string dir2 = CreateDirectory("right");
		CreateFile(Path.Combine("left", "same.txt"), "identical");
		CreateFile(Path.Combine("right", "same.txt"), "identical");
		CreateFile(Path.Combine("left", "changed.txt"), "one");
		CreateFile(Path.Combine("right", "changed.txt"), "two");
		CreateFile(Path.Combine("left", "leftonly.txt"), "l");
		CreateFile(Path.Combine("right", "rightonly.txt"), "r");

		DirectoryComparisonResult result = FileDiffer.FindDifferences(dir1, dir2, "*.txt", recursive: false, MockFileSystem);

		Assert.AreSequenceEqual(["same.txt"], result.SameFiles);
		Assert.AreSequenceEqual(["changed.txt"], result.ModifiedFiles);
		Assert.AreSequenceEqual(["leftonly.txt"], result.OnlyInDir1);
		Assert.AreSequenceEqual(["rightonly.txt"], result.OnlyInDir2);
	}

	/// <summary>
	/// A recursive comparison reports files in subdirectories by their relative path.
	/// </summary>
	[TestMethod]
	public void FindDifferences_Directories_RecursiveReportsRelativePaths()
	{
		string dir1 = CreateDirectory("left");
		string dir2 = CreateDirectory("right");
		string relative = Path.Combine("nested", "deep.txt");
		CreateFile(Path.Combine("left", relative), "same");
		CreateFile(Path.Combine("right", relative), "same");

		DirectoryComparisonResult flat = FileDiffer.FindDifferences(dir1, dir2, "*.txt", recursive: false, MockFileSystem);
		DirectoryComparisonResult deep = FileDiffer.FindDifferences(dir1, dir2, "*.txt", recursive: true, MockFileSystem);

		Assert.IsEmpty(flat.SameFiles);
		Assert.AreSequenceEqual([relative], deep.SameFiles);
	}

	/// <summary>
	/// A missing directory contributes no files rather than failing the comparison.
	/// </summary>
	[TestMethod]
	public void FindDifferences_Directories_MissingDirectoryIsTreatedAsEmpty()
	{
		string dir1 = CreateDirectory("present");
		CreateFile(Path.Combine("present", "a.txt"), "a");
		string missing = Path.Combine(TestDirectory, "missing");

		DirectoryComparisonResult result = FileDiffer.FindDifferences(dir1, missing, "*.txt", recursive: false, MockFileSystem);

		Assert.AreSequenceEqual(["a.txt"], result.OnlyInDir1);
		Assert.IsEmpty(result.OnlyInDir2);
		Assert.IsEmpty(result.SameFiles);
		Assert.IsEmpty(result.ModifiedFiles);
	}

	/// <summary>
	/// A file that cannot be read is reported as modified instead of failing the comparison.
	/// </summary>
	[TestMethod]
	public void FindDifferences_Directories_UnreadableFileIsReportedAsModified()
	{
		string dir1 = CreateDirectory("left");
		string dir2 = CreateDirectory("right");
		CreateFile(Path.Combine("left", "locked.txt"), "same");
		MockFileSystem.AddFile(Path.Combine(dir2, "locked.txt"), new MockFileData("same") { AllowedFileShare = FileShare.None });

		DirectoryComparisonResult result = FileDiffer.FindDifferences(dir1, dir2, "*.txt", recursive: false, MockFileSystem);

		Assert.AreSequenceEqual(["locked.txt"], result.ModifiedFiles);
		Assert.IsEmpty(result.SameFiles);
	}

	/// <summary>
	/// The coloured git-style diff wraps headers, deletions and additions in their ANSI colours.
	/// </summary>
	[TestMethod]
	public void GenerateGitStyleDiff_WithColor_AddsAnsiCodesPerLineKind()
	{
		string file1 = CreateFile("v1.txt", "keep\nold\n");
		string file2 = CreateFile("v2.txt", "keep\nnew\n");

		string diff = FileDiffer.GenerateGitStyleDiff(file1, file2, useColor: true);

		Assert.Contains($"\u001b[1;34m--- {file1}{Reset}", diff);
		Assert.Contains($"\u001b[1;34m+++ {file2}{Reset}", diff);
		Assert.Contains($"\u001b[31m-old{Reset}", diff);
		Assert.Contains($"\u001b[32m+new{Reset}", diff);
		string[] lines = diff.Split(["\r\n", "\n"], StringSplitOptions.None);
		Assert.Contains(" keep", lines);
	}

	/// <summary>
	/// Identical files give an empty diff even when colour is requested.
	/// </summary>
	[TestMethod]
	public void GenerateGitStyleDiff_WithColor_IdenticalFilesGiveEmptyDiff()
	{
		string file1 = CreateFile("a.txt", "same\n");
		string file2 = CreateFile("b.txt", "same\n");

		Assert.AreEqual(string.Empty, FileDiffer.GenerateGitStyleDiff(file1, file2, useColor: true));
	}

	/// <summary>
	/// The uncoloured overload matches the unified diff and carries no escape codes.
	/// </summary>
	[TestMethod]
	public void GenerateGitStyleDiff_WithoutColor_HasNoEscapeCodes()
	{
		string file1 = CreateFile("a.txt", "one\n");
		string file2 = CreateFile("b.txt", "two\n");

		string diff = FileDiffer.GenerateGitStyleDiff(file1, file2);

		Assert.AreEqual(DiffPlexDiffer.GenerateUnifiedDiff(file1, file2), diff);
		Assert.DoesNotContain("\u001b[", diff);
	}

	/// <summary>
	/// The array overload of GenerateColoredDiff returns the same lines as DiffPlex.
	/// </summary>
	[TestMethod]
	public void GenerateColoredDiff_ReturnsDiffPlexLines()
	{
		string file1 = CreateFile("a.txt", "x\ny\n");
		string file2 = CreateFile("b.txt", "x\nz\n");

		Collection<ColoredDiffLine> lines = FileDiffer.GenerateColoredDiff(file1, file2, [], []);

		Assert.AreSequenceEqual(DiffPlexDiffer.GenerateColoredDiff(file1, file2), lines);
		Assert.Contains(new ColoredDiffLine("-y", DiffColor.Deletion), lines);
		Assert.Contains(new ColoredDiffLine("+z", DiffColor.Addition), lines);
	}

	/// <summary>
	/// Identical files produce the header followed by "No differences found."
	/// </summary>
	[TestMethod]
	public void GenerateChangeSummaryDiff_IdenticalFiles_SaysNoDifferences()
	{
		string file1 = CreateFile("one.txt", "a\nb\n");
		string file2 = CreateFile("two.txt", "a\nb\n");

		string summary = FileDiffer.GenerateChangeSummaryDiff(file1, file2);

		Assert.Contains("Change Summary: one.txt vs two.txt", summary);
		Assert.Contains("No differences found.", summary);
		Assert.DoesNotContain("line(s)", summary);
	}

	/// <summary>
	/// A changed line is counted and described as a modification.
	/// </summary>
	[TestMethod]
	public void GenerateChangeSummaryDiff_ModifiedLine_IsCountedAndDescribed()
	{
		string file1 = CreateFile("one.txt", "a\nb\nc\n");
		string file2 = CreateFile("two.txt", "a\nB\nc\n");

		string summary = FileDiffer.GenerateChangeSummaryDiff(file1, file2);

		Assert.Contains("1 line(s) modified", summary);
		Assert.Contains("Modified line 2: b → B", summary);
		Assert.DoesNotContain("No differences found.", summary);
		Assert.DoesNotContain("\u001b[", summary);
	}

	/// <summary>
	/// Added and deleted lines are counted and described separately.
	/// </summary>
	[TestMethod]
	public void GenerateChangeSummaryDiff_AddedAndDeletedLines_AreCountedAndDescribed()
	{
		string original = CreateFile("one.txt", "a\nb\nc\n");
		string added = CreateFile("added.txt", "a\nb\nc\nd\n");
		string deleted = CreateFile("deleted.txt", "a\nc\n");

		string addSummary = FileDiffer.GenerateChangeSummaryDiff(original, added);
		string deleteSummary = FileDiffer.GenerateChangeSummaryDiff(original, deleted);

		Assert.Contains("1 line(s) added", addSummary);
		Assert.Contains("Added line 4: d", addSummary);
		Assert.Contains("1 line(s) deleted", deleteSummary);
		Assert.Contains("Deleted line 2: b", deleteSummary);
	}

	/// <summary>
	/// With colour on, each kind of change is wrapped in its own ANSI colour and the reset code.
	/// </summary>
	[TestMethod]
	public void GenerateChangeSummaryDiff_WithColor_WrapsEachKindInItsColour()
	{
		string original = CreateFile("one.txt", "a\nb\nc\n");
		string modified = CreateFile("mod.txt", "a\nB\nc\n");
		string added = CreateFile("added.txt", "a\nb\nc\nd\n");
		string deleted = CreateFile("deleted.txt", "a\nc\n");

		Assert.Contains($"\u001b[33mModified line 2: b → B{Reset}", FileDiffer.GenerateChangeSummaryDiff(original, modified, useColor: true));
		Assert.Contains($"\u001b[32mAdded line 4: d{Reset}", FileDiffer.GenerateChangeSummaryDiff(original, added, useColor: true));
		Assert.Contains($"\u001b[31mDeleted line 2: b{Reset}", FileDiffer.GenerateChangeSummaryDiff(original, deleted, useColor: true));
	}

	/// <summary>
	/// The coloured change summary lists removed lines and added lines under their own headers.
	/// </summary>
	[TestMethod]
	public void GenerateColoredChangeSummary_ListsRemovedThenAddedLines()
	{
		string file1 = CreateFile("v1.txt", "a\nb\nc\n");
		string file2 = CreateFile("v2.txt", "a\nc\nd\ne\n");

		Collection<ColoredDiffLine> lines = FileDiffer.GenerateColoredChangeSummary(file1, file2);

		ColoredDiffLine[] expected =
		[
			new("Change Summary: v1.txt vs v2.txt", DiffColor.FileHeader),
			new("----------------------------------------", DiffColor.Default),
			new("REMOVED LINES (in version 1 but not in version X):", DiffColor.ChunkHeader),
			new("- Line 2: b", DiffColor.Deletion),
			new("", DiffColor.Default),
			new("ADDED LINES (in version X but not in version 1):", DiffColor.ChunkHeader),
			new("+ Line 3: d", DiffColor.Addition),
			new("+ Line 4: e", DiffColor.Addition),
		];
		Assert.AreSequenceEqual(expected, lines);
	}

	/// <summary>
	/// Modifications are not listed by the coloured change summary, only the header is.
	/// </summary>
	[TestMethod]
	public void GenerateColoredChangeSummary_ModificationOnly_HasOnlyTheHeader()
	{
		string file1 = CreateFile("v1.txt", "a\nb\n");
		string file2 = CreateFile("v2.txt", "a\nB\n");

		Collection<ColoredDiffLine> lines = FileDiffer.GenerateColoredChangeSummary(file1, file2);

		Assert.HasCount(2, lines);
		Assert.AreEqual(DiffColor.FileHeader, lines[0].Color);
	}

	/// <summary>
	/// Syncing creates the target directory and overwrites any existing target.
	/// </summary>
	[TestMethod]
	public void SyncFile_CreatesTargetDirectoryAndOverwrites()
	{
		string source = CreateFile("source.txt", "fresh");
		string target = Path.Combine(TestDirectory, "new", "dir", "target.txt");

		FileDiffer.SyncFile(source, target, MockFileSystem);
		Assert.AreEqual("fresh", MockFileSystem.File.ReadAllText(target));

		MockFileSystem.File.WriteAllText(source, "fresher");
		FileDiffer.SyncFile(source, target, MockFileSystem);
		Assert.AreEqual("fresher", MockFileSystem.File.ReadAllText(target));
	}

	/// <summary>
	/// Two empty line sets are fully similar, and an empty set against a non-empty one is not similar at all.
	/// </summary>
	[TestMethod]
	public void CalculateLineSimilarity_EmptyInputs()
	{
		Assert.AreEqual(1.0, FileDiffer.CalculateLineSimilarity([], []));
		Assert.AreEqual(0.0, FileDiffer.CalculateLineSimilarity([], ["a"]));
		Assert.AreEqual(0.0, FileDiffer.CalculateLineSimilarity(["a"], []));
	}

	/// <summary>
	/// With fewer than two groups there is no pair to compare.
	/// </summary>
	[TestMethod]
	public void FindMostSimilarFiles_FewerThanTwoGroups_ReturnsNull()
	{
		string file = CreateFile("only.txt", "x");

		Assert.IsNull(FileDiffer.FindMostSimilarFiles([], MockFileSystem));
		Assert.IsNull(FileDiffer.FindMostSimilarFiles([new FileGroup([file]) { Hash = "h" }], MockFileSystem));
	}

	/// <summary>
	/// The content hash depends only on the content.
	/// </summary>
	[TestMethod]
	public void CalculateFileHash_DependsOnlyOnContent()
	{
		Assert.AreEqual(FileDiffer.CalculateFileHash("abc"), FileDiffer.CalculateFileHash("abc"));
		Assert.AreNotEqual(FileDiffer.CalculateFileHash("abc"), FileDiffer.CalculateFileHash("abd"));
		Assert.AreEqual(FileHasher.ComputeContentHash("abc"), FileDiffer.CalculateFileHash("abc"));
	}
}
