// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using DiffPlex.Model;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the paths of <see cref="DiffPlexDiffer"/> the existing suite leaves untouched: missing
/// files, raw line diffs, CR line endings and deletion-only hunks.
/// </summary>
[TestClass]
public class DiffPlexDifferCoverageTests : MockFileSystemTestBase
{
	private const string MissingFilesMessage = "One or both files do not exist";

	/// <summary>
	/// A missing file is never identical to anything.
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_MissingFile_ReturnsFalse()
	{
		string existing = CreateFile("present.txt", "x");
		string missing = Path.Join(TestDirectory, "missing.txt");

		Assert.IsFalse(DiffPlexDiffer.AreFilesIdentical(existing, missing));
		Assert.IsFalse(DiffPlexDiffer.AreFilesIdentical(missing, existing));
	}

	/// <summary>
	/// The raw line diff reports the changed block with its positions in each file.
	/// </summary>
	[TestMethod]
	public void CreateLineDiffs_ReturnsTheChangedBlock()
	{
		string file1 = CreateFile("a.txt", "one\ntwo\nthree\n");
		string file2 = CreateFile("b.txt", "one\nTWO\nthree\n");

		DiffResult result = DiffPlexDiffer.CreateLineDiffs(file1, file2);

		Assert.HasCount(1, result.DiffBlocks);
		DiffPlex.Model.DiffBlock block = result.DiffBlocks[0];
		Assert.AreEqual(1, block.DeleteStartA);
		Assert.AreEqual(1, block.DeleteCountA);
		Assert.AreEqual(1, block.InsertStartB);
		Assert.AreEqual(1, block.InsertCountB);
		Assert.AreEqual("two", result.PiecesOld[1]);
		Assert.AreEqual("TWO", result.PiecesNew[1]);
	}

	/// <summary>
	/// Identical files give a line diff with no blocks.
	/// </summary>
	[TestMethod]
	public void CreateLineDiffs_IdenticalFiles_HaveNoBlocks()
	{
		string file1 = CreateFile("a.txt", "same\n");
		string file2 = CreateFile("b.txt", "same\n");

		Assert.IsEmpty(DiffPlexDiffer.CreateLineDiffs(file1, file2).DiffBlocks);
	}

	/// <summary>
	/// Each operation that reads both files refuses a missing one with the same message.
	/// </summary>
	[TestMethod]
	public void Operations_MissingFile_ThrowFileNotFound()
	{
		string existing = CreateFile("present.txt", "x");
		string missing = Path.Join(TestDirectory, "missing.txt");

		Assert.AreEqual(MissingFilesMessage, Assert.ThrowsExactly<FileNotFoundException>(() => DiffPlexDiffer.CreateLineDiffs(existing, missing)).Message);
		Assert.AreEqual(MissingFilesMessage, Assert.ThrowsExactly<FileNotFoundException>(() => DiffPlexDiffer.GenerateColoredDiff(missing, existing)).Message);
		Assert.AreEqual(MissingFilesMessage, Assert.ThrowsExactly<FileNotFoundException>(() => DiffPlexDiffer.FindDifferences(existing, missing)).Message);
		Assert.AreEqual(MissingFilesMessage, Assert.ThrowsExactly<FileNotFoundException>(() => DiffPlexDiffer.GenerateSideBySideDiff(missing, existing)).Message);
	}

	/// <summary>
	/// Files differing only in CR versus LF line endings are described by name.
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_CrVersusLf_NamesBothStyles()
	{
		string file1 = CreateFile("cr.txt", "a\rb\r");
		string file2 = CreateFile("lf.txt", "a\nb\n");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(file1, file2);

		string[] lines = diff.Split(Environment.NewLine);
		Assert.AreSequenceEqual([$"--- {file1}", $"+++ {file2}", "Files differ only in line endings: CR vs LF"], lines);
	}

	/// <summary>
	/// A hunk that only deletes is anchored after the last surviving line of the new file.
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_DeletionOnlyHunk_IsAnchoredAfterPrecedingLine()
	{
		string file1 = CreateFile("a.txt", "a\nb\nc\n");
		string file2 = CreateFile("b.txt", "a\nc\n");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(file1, file2, contextLines: 0);

		string[] lines = diff.Split(Environment.NewLine);
		Assert.AreSequenceEqual([$"--- {file1}", $"+++ {file2}", "@@ -2,1 +1,0 @@", "-b"], lines);
	}
}
