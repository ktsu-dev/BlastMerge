// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using ktsu.BlastMerge.Cli.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the console output of <see cref="FileDisplayService"/>, complementing the path tests in
/// <see cref="FileDisplayServiceTests"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class FileDisplayServiceCoverageTests : ConsoleTestBase
{
	/// <summary>
	/// The detailed list numbers each group, marks duplicates and unique files, abbreviates the
	/// hash, and shows each file's size, then waits for a key.
	/// </summary>
	[TestMethod]
	public void ShowDetailedFileList_ShowsGroupsHashesAndSizes()
	{
		string first = WriteFile("first.txt", "12345");
		string second = WriteFile("second.txt", "12345");
		string unique = WriteFile("unique.txt", "1234567890");
		Dictionary<string, IReadOnlyCollection<string>> groups = new()
		{
			["abcdef0123456789"] = [first, second],
			["xyz"] = [unique],
		};
		PressAnyKey();

		FileDisplayService.ShowDetailedFileList(groups);

		StringAssert.Contains(Output, "Detailed File List");
		StringAssert.Contains(Output, "Group 1 - Multiple identical copies (2 files)");
		StringAssert.Contains(Output, "Group 2 - Unique (1 files)");
		StringAssert.Contains(Output, "Hash: abcdef01...");
		Assert.DoesNotContain("abcdef012", Output);
		StringAssert.Contains(Output, "Hash: xyz...");
		StringAssert.Contains(Output, "first.txt");
		StringAssert.Contains(Output, "(5 bytes, ");
		StringAssert.Contains(Output, "(10 bytes, ");
		StringAssert.Contains(Output, "Press any key to continue...");
	}

	/// <summary>
	/// A file that cannot be read is listed with the reason instead of its size.
	/// </summary>
	[TestMethod]
	public void ShowDetailedFileList_MissingFile_ShowsExceptionName()
	{
		string missing = Path.Join(TempDirectory, "gone.txt");
		Dictionary<string, IReadOnlyCollection<string>> groups = new()
		{
			["hash"] = [missing],
		};
		PressAnyKey();

		FileDisplayService.ShowDetailedFileList(groups);

		StringAssert.Contains(Output, "gone.txt");
		StringAssert.Contains(Output, "(FileNotFoundException)");
	}

	/// <summary>
	/// With no group holding more than one file there is nothing to compare.
	/// </summary>
	[TestMethod]
	public void ShowDifferences_NoMultiFileGroups_ShowsWarning()
	{
		Dictionary<string, IReadOnlyCollection<string>> groups = new()
		{
			["a"] = [WriteFile("a.txt", "a")],
			["b"] = [WriteFile("b.txt", "b")],
		};

		FileDisplayService.ShowDifferences(groups);

		StringAssert.Contains(Output, "No groups with multiple files to compare.");
	}

	/// <summary>
	/// Each group with several files lists them, offers a comparison of the first two, and waits
	/// before moving to the next group.
	/// </summary>
	[TestMethod]
	public void ShowDifferences_MultiFileGroup_ListsFilesAndOffersComparison()
	{
		string first = WriteFile(Path.Join("one", "same.txt"), "content");
		string second = WriteFile(Path.Join("two", "same.txt"), "content");
		string third = WriteFile(Path.Join("three", "same.txt"), "content");
		Dictionary<string, IReadOnlyCollection<string>> groups = new()
		{
			["hash"] = [first, second, third],
			["single"] = [WriteFile("lonely.txt", "x")],
		};
		SelectIndex(3);
		PressAnyKey();

		FileDisplayService.ShowDifferences(groups);

		StringAssert.Contains(Output, "Group with 3 files");
		StringAssert.Contains(Output, "Comparing first two files:");
		StringAssert.Contains(Output, "Choose diff format");
		StringAssert.Contains(Output, "Press any key to continue to next group...");
		Assert.DoesNotContain("lonely.txt", Output);
	}

	/// <summary>
	/// A path with a single directory keeps that directory and the file name.
	/// </summary>
	[TestMethod]
	public void GetRelativeDirectoryName_SingleRelativeDirectory_ReturnsDirectoryAndFile()
	{
		string path = Path.Join("folder", "file.txt");

		Assert.AreEqual(path, FileDisplayService.GetRelativeDirectoryName(path));
	}

	/// <summary>
	/// When one path is the directory containing the other, the shorter label is empty and the
	/// longer keeps only what differs.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_OnePathContainsTheOther_LabelsOnlyTheDifference()
	{
		string directory = TestPaths.Rooted("work", "project");
		string file = TestPaths.Rooted("work", "project", "file.txt");

		(string label1, string label2) = FileDisplayService.MakeDistinguishedPaths(directory, file);

		Assert.AreEqual(string.Empty, label1);
		Assert.AreEqual("file.txt", label2);
	}

	/// <summary>
	/// Where the file system ignores case, paths that differ only in case have no distinguishing
	/// components, so each keeps its own file name. Where it doesn't, the folders are different and
	/// stay in the labels (#66).
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_DifferOnlyInCase_ReturnsEachFileName()
	{
		string upper = TestPaths.Rooted("Work", "File.TXT");
		string lower = TestPaths.Rooted("work", "file.txt");

		(string label1, string label2) = FileDisplayService.MakeDistinguishedPaths(upper, lower);

		bool ignoresCase = FileDisplayService.PathComponentComparison == StringComparison.OrdinalIgnoreCase;
		Assert.AreEqual(ignoresCase ? "File.TXT" : "Work/File.TXT", label1);
		Assert.AreEqual(ignoresCase ? "file.txt" : "work/file.txt", label2);
	}

	/// <summary>
	/// Where the file system is case-sensitive, files in folders that differ only in case get
	/// different labels, so the user can tell which is which (#66).
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_FoldersDifferOnlyInCase_LabelsDifferOnCaseSensitivePlatforms()
	{
		if (FileDisplayService.PathComponentComparison != StringComparison.Ordinal)
		{
			Assert.Inconclusive("Docs/ and docs/ are the same folder on this platform");
		}

		(string label1, string label2) = FileDisplayService.MakeDistinguishedPaths(
			TestPaths.Rooted("repo", "Docs", "guide.md"),
			TestPaths.Rooted("repo", "docs", "guide.md"));

		Assert.AreEqual("Docs/guide.md", label1);
		Assert.AreEqual("docs/guide.md", label2);
	}

	/// <summary>
	/// Two identical root paths have no file name to show.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_IdenticalRoots_ReturnsRootLabel()
	{
		(string label1, string label2) = FileDisplayService.MakeDistinguishedPaths(TestPaths.Root, TestPaths.Root);

		// A Unix root has no components; a Windows drive root normalizes to its lower-case letter.
		string expected = OperatingSystem.IsWindows() ? "c" : string.Empty;
		Assert.AreEqual(expected, label1);
		Assert.AreEqual(expected, label2);
	}
}
