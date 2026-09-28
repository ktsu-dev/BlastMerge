// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System.IO;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for DiffPlex-based diffing functionality
/// </summary>
[TestClass]
public class DiffPlexDifferTests : MockFileSystemTestBase
{
	private string _file1 = string.Empty;
	private string _file2 = string.Empty;
	private string _identicalFile = string.Empty;

	/// <summary>
	/// Sets up test files in the mock file system
	/// </summary>
	protected override void InitializeFileSystem()
	{
		// Create test files in mock file system
		_file1 = CreateFile("file1.txt", """
			Line 1
			Line 2
			Line 3
			Line 4
			""");

		_file2 = CreateFile("file2.txt", """
			Line 1
			Modified Line 2
			Line 3
			New Line 4
			Line 5
			""");

		_identicalFile = CreateFile("identical.txt", """
			Line 1
			Line 2
			Line 3
			Line 4
			""");
	}

	/// <summary>
	/// Tests that identical files are correctly identified
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_IdenticalFiles_ReturnsTrue()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

		try
		{
			MockFileSystem.File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			MockFileSystem.File.WriteAllText(tempFile2, MockFileSystem.File.ReadAllText(_identicalFile));

			bool result = DiffPlexDiffer.AreFilesIdentical(tempFile1, tempFile2);
			Assert.IsTrue(result, "Identical files should be detected as identical");
		}
		finally
		{
			SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, tempFile1, tempFile2);
		}
	}

	/// <summary>
	/// Tests that different files are correctly identified
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_DifferentFiles_ReturnsFalse()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		bool result = DiffPlexDiffer.AreFilesIdentical(_file1, _file2);
		Assert.IsFalse(result, "Different files should not be detected as identical");
	}

	/// <summary>
	/// Tests unified diff generation for different files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_DifferentFiles_ReturnsValidDiff()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		string diff = DiffPlexDiffer.GenerateUnifiedDiff(_file1, _file2);

		Assert.IsFalse(string.IsNullOrEmpty(diff), "Unified diff should not be empty for different files");
		Assert.IsTrue(diff.Contains($"--- {_file1}"), "Unified diff should contain source file header");
		Assert.IsTrue(diff.Contains($"+++ {_file2}"), "Unified diff should contain target file header");
		Assert.IsTrue(diff.Contains("@@"), "Unified diff should contain hunk headers");
	}

	/// <summary>
	/// Tests unified diff generation for identical files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_IdenticalFiles_ReturnsEmptyString()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		string diff = DiffPlexDiffer.GenerateUnifiedDiff(_file1, _identicalFile);

		// For identical files, git returns empty string
		Assert.AreEqual(string.Empty, diff, "Unified diff should be empty for identical files");
	}

	/// <summary>
	/// Tests that a single deletion produces the hunk header and body that GNU diff -u produces,
	/// rather than duplicating the leading context as trailing context
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_SingleDeletion_MatchesUnifiedDiffFormat()
	{
		string source = CreateFile("seq1.txt", "a\nb\nc\nd\ne\nf\ng");
		string target = CreateFile("seq2.txt", "a\nb\nc\ne\nf\ng");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] expected =
		[
			$"--- {source}",
			$"+++ {target}",
			"@@ -1,7 +1,6 @@",
			" a",
			" b",
			" c",
			"-d",
			" e",
			" f",
			" g",
		];

		Assert.AreSequenceEqual(expected, SplitLines(diff));
	}

	/// <summary>
	/// Tests that the trailing context is the lines that follow the change, so no line of the source
	/// file appears in the hunk body more than once
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_SingleDeletion_DoesNotDuplicateContextLines()
	{
		string source = CreateFile("dup1.txt", "a\nb\nc\nd\ne\nf\ng");
		string target = CreateFile("dup2.txt", "a\nb\nc\ne\nf\ng");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] body = [.. SplitLines(diff).Where(line => line.StartsWith(' '))];

		Assert.AreAllDistinct(body, "A context line must not be emitted twice in one hunk");
		Assert.AreSequenceEqual([" a", " b", " c", " e", " f", " g"], body);
	}

	/// <summary>
	/// Tests that two changes further apart than twice the context are emitted as separate hunks,
	/// each with its own correct line numbers
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_TwoDistantChanges_ProducesTwoCorrectlyNumberedHunks()
	{
		string source = CreateFile("far1.txt", "1\n2\n3\n4\n5\n6\n7\n8\n9\n10\n11\n12\n13\n14\n15\n16\n17\n18\n19\n20");
		string target = CreateFile("far2.txt", "1\n2\nX\n4\n5\n6\n7\n8\n9\n10\n11\n12\n13\n14\n15\n16\n17\nY\n19\n20");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] headers = [.. SplitLines(diff).Where(line => line.StartsWith("@@", StringComparison.Ordinal))];

		Assert.AreSequenceEqual(["@@ -1,6 +1,6 @@", "@@ -15,6 +15,6 @@"], headers);
	}

	/// <summary>
	/// Tests that changes close enough for their context regions to meet are merged into one hunk
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_NearbyChanges_ProducesOneMergedHunk()
	{
		string source = CreateFile("near1.txt", "1\n2\n3\n4\n5\n6\n7\n8");
		string target = CreateFile("near2.txt", "1\n2\nX\n4\n5\nY\n7\n8");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] headers = [.. SplitLines(diff).Where(line => line.StartsWith("@@", StringComparison.Ordinal))];

		Assert.HasCount(1, headers, "Changes within twice the context should share one hunk");
		Assert.AreEqual("@@ -1,8 +1,8 @@", headers[0]);
	}

	/// <summary>
	/// Tests that appended lines are anchored after the last shared line
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_AppendedLines_AnchorsHunkAfterSharedContext()
	{
		string source = CreateFile("app1.txt", "a\nb\nc");
		string target = CreateFile("app2.txt", "a\nb\nc\nd\ne");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] expected =
		[
			$"--- {source}",
			$"+++ {target}",
			"@@ -1,3 +1,5 @@",
			" a",
			" b",
			" c",
			"+d",
			"+e",
		];

		Assert.AreSequenceEqual(expected, SplitLines(diff));
	}

	/// <summary>
	/// Tests that a hunk with no lines on one side reports a zero length there, anchored after the
	/// last line that does exist
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_ZeroContext_ReportsZeroLengthRangeForOneSidedHunk()
	{
		string source = CreateFile("zero1.txt", "a\nb\nc");
		string target = CreateFile("zero2.txt", "a\nb\nc\nd\ne");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target, 0);

		string[] expected =
		[
			$"--- {source}",
			$"+++ {target}",
			"@@ -3,0 +4,2 @@",
			"+d",
			"+e",
		];

		Assert.AreSequenceEqual(expected, SplitLines(diff));
	}

	/// <summary>
	/// Tests that when both files end with a line ending, lines appended at the end produce the hunk
	/// GNU diff -u produces, with no empty context line after the last real line
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_BothSidesNewlineTerminated_AppendAtEndHasNoPhantomLine()
	{
		string source = CreateFile("eof1.txt", "a\ne\n");
		string target = CreateFile("eof2.txt", "a\ne\nc\nd\n");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] expected =
		[
			$"--- {source}",
			$"+++ {target}",
			"@@ -1,2 +1,4 @@",
			" a",
			" e",
			"+c",
			"+d",
		];

		Assert.AreSequenceEqual(expected, SplitLines(diff));
	}

	/// <summary>
	/// Tests that when both files end with a line ending, a change to the last line is counted from
	/// the real lines only, so the hunk applies with patch -F0
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_BothSidesNewlineTerminated_ChangeToLastLineCountsRealLinesOnly()
	{
		string source = CreateFile("last1.txt", "1\n2\n3\n4\n5\n6\n");
		string target = CreateFile("last2.txt", "1\n2\n3\n4\n5\nsix\n");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] expected =
		[
			$"--- {source}",
			$"+++ {target}",
			"@@ -3,4 +3,4 @@",
			" 3",
			" 4",
			" 5",
			"-6",
			"+six",
		];

		Assert.AreSequenceEqual(expected, SplitLines(diff));
	}

	/// <summary>
	/// Tests that an empty file, which has no unterminated line either, gains lines without an empty
	/// line trailing them
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_EmptyToNewlineTerminated_HasNoPhantomLine()
	{
		string source = CreateFile("fromempty1.txt", string.Empty);
		string target = CreateFile("fromempty2.txt", "e\n");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] expected =
		[
			$"--- {source}",
			$"+++ {target}",
			"@@ -0,0 +1,1 @@",
			"+e",
		];

		Assert.AreSequenceEqual(expected, SplitLines(diff));
	}

	/// <summary>
	/// Tests that CRLF-terminated files are treated the same way, since the final CRLF also
	/// terminates the last line rather than starting an empty one
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_BothSidesCrlfTerminated_AppendAtEndHasNoPhantomLine()
	{
		string source = CreateFile("crlf1.txt", "a\r\ne\r\n");
		string target = CreateFile("crlf2.txt", "a\r\ne\r\nc\r\n");

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(source, target);

		string[] headers = [.. SplitLines(diff).Where(line => line.StartsWith("@@", StringComparison.Ordinal))];

		Assert.AreSequenceEqual(["@@ -1,2 +1,3 @@"], headers);
		Assert.AreEqual("+c", SplitLines(diff)[^1]);
	}

	/// <summary>
	/// Tests that an insertion and an unrelated deletion elsewhere in the file are not paired as a
	/// modification just because their line numbers coincide, and that changes come back in file order
	/// </summary>
	[TestMethod]
	public void FindDifferences_InsertionAndUnrelatedDeletion_ReportsEachInFileOrder()
	{
		string file1 = CreateFile("p/1.txt", "A\nX\n");
		string file2 = CreateFile("p/2.txt", "I1\nI2\nA\n");

		LineDifference[] differences = [.. DiffPlexDiffer.FindDifferences(file1, file2)];

		LineDifference[] expected =
		[
			new(null, 1, null, "I1", LineDifferenceType.Added),
			new(null, 2, null, "I2", LineDifferenceType.Added),
			new(2, null, "X", null, LineDifferenceType.Deleted),
		];
		Assert.AreSequenceEqual(expected, differences);
	}

	/// <summary>
	/// Tests that a genuine in-place edit is still reported as a single modification
	/// </summary>
	[TestMethod]
	public void FindDifferences_InPlaceEdit_ReportsOneModification()
	{
		string file1 = CreateFile("edit/1.txt", "a\nb\nc");
		string file2 = CreateFile("edit/2.txt", "a\nB\nc");

		LineDifference[] differences = [.. DiffPlexDiffer.FindDifferences(file1, file2)];

		LineDifference[] expected = [new(2, 2, "b", "B", LineDifferenceType.Modified)];
		Assert.AreSequenceEqual(expected, differences);
	}

	/// <summary>
	/// Tests that a block replacing more lines than it inserts pairs what it can and reports the rest
	/// as deletions
	/// </summary>
	[TestMethod]
	public void FindDifferences_UnevenReplacement_PairsWithinTheBlockAndReportsTheRemainder()
	{
		string file1 = CreateFile("uneven/1.txt", "a\nb\nc\nd");
		string file2 = CreateFile("uneven/2.txt", "a\nB\nd");

		LineDifference[] differences = [.. DiffPlexDiffer.FindDifferences(file1, file2)];

		LineDifference[] expected =
		[
			new(2, 2, "b", "B", LineDifferenceType.Modified),
			new(3, null, "c", null, LineDifferenceType.Deleted),
		];
		Assert.AreSequenceEqual(expected, differences);
	}

	/// <summary>
	/// Splits a generated diff into its lines
	/// </summary>
	/// <param name="diff">The generated unified diff</param>
	/// <returns>The diff's lines</returns>
	private static string[] SplitLines(string diff) =>
		diff.Split(Environment.NewLine, StringSplitOptions.None);

	/// <summary>
	/// Tests colored diff generation
	/// </summary>
	[TestMethod]
	public void GenerateColoredDiff_DifferentFiles_ReturnsColoredLines()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		System.Collections.ObjectModel.Collection<ColoredDiffLine> coloredDiff = DiffPlexDiffer.GenerateColoredDiff(_file1, _file2);

		Assert.IsTrue(coloredDiff.Count > 0, "Colored diff should contain at least one line");

		// Should contain file headers
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.FileHeader), "Colored diff should contain file header lines");

		// Should contain additions and deletions
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.Addition), "Colored diff should contain addition lines");
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.Deletion), "Colored diff should contain deletion lines");
	}

	/// <summary>
	/// Tests side-by-side diff generation
	/// </summary>
	[TestMethod]
	public void GenerateSideBySideDiff_DifferentFiles_ReturnsValidModel()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		DiffPlex.DiffBuilder.Model.SideBySideDiffModel sideBySide = DiffPlexDiffer.GenerateSideBySideDiff(_file1, _file2);

		Assert.IsNotNull(sideBySide);
		Assert.IsNotNull(sideBySide.OldText);
		Assert.IsNotNull(sideBySide.NewText);
		Assert.IsTrue(sideBySide.OldText.Lines.Count > 0, "Side-by-side diff should have old text lines");
		Assert.IsTrue(sideBySide.NewText.Lines.Count > 0, "Side-by-side diff should have new text lines");
	}

	/// <summary>
	/// Tests change summary generation
	/// </summary>
	[TestMethod]
	public void GenerateChangeSummary_DifferentFiles_ReturnsOnlyChanges()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		System.Collections.ObjectModel.Collection<ColoredDiffLine> changeSummary = DiffPlexDiffer.GenerateChangeSummary(_file1, _file2);

		Assert.IsTrue(changeSummary.Count > 0, "Change summary should contain at least one line");

		// Should not contain unchanged lines (default color)
		List<ColoredDiffLine> unchangedLines = [.. changeSummary.Where(line => line.Color == DiffColor.Default)];
		// File headers might have default color, but content should not
		Assert.IsTrue(unchangedLines.Count == 0 || unchangedLines.All(line =>
			line.Content.Contains(_file1) || line.Content.Contains(_file2) || string.IsNullOrEmpty(line.Content.Trim())),
			"Change summary should only contain file headers or changes, not unchanged content lines");
	}

	/// <summary>
	/// Tests error handling for non-existent files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_NonExistentFile_ThrowsException()
	{
		string nonExistentFile = Path.GetTempFileName();
		File.Delete(nonExistentFile); // Ensure it doesn't exist

		string tempFile1 = Path.GetTempFileName();
		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			_ = Assert.ThrowsExactly<FileNotFoundException>(() => DiffPlexDiffer.GenerateUnifiedDiff(tempFile1, nonExistentFile));
		}
		finally
		{
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}
		}
	}

	/// <summary>
	/// Tests handling of empty files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_EmptyFiles_HandlesCorrectly()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		string emptyFile1 = CreateFile("empty1.txt", string.Empty);
		string emptyFile2 = CreateFile("empty2.txt", string.Empty);

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(emptyFile1, emptyFile2);
		Assert.IsNotNull(diff);

		// Should identify as identical
		bool areIdentical = DiffPlexDiffer.AreFilesIdentical(emptyFile1, emptyFile2);
		Assert.IsTrue(areIdentical, "Empty files should be detected as identical");
	}

	/// <summary>
	/// Tests handling of binary files
	/// </summary>
	/// <remarks>
	/// Binary content is reported as differing without being decoded. Splitting it into "lines" would
	/// cut the byte stream wherever a 0x0A happened to fall and show replacement characters in place
	/// of every byte that is not valid UTF-8, so the diff would describe content the files never held.
	/// </remarks>
	[TestMethod]
	public void GenerateUnifiedDiff_BinaryFiles_ReportsThemAsBinaryRatherThanAsLines()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		byte[] binaryData1 = [0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE];
		byte[] binaryData2 = [0x00, 0x01, 0x04, 0x05, 0xFF, 0xFE];

		string binaryFile1 = CreateBinaryFile("binary1.dat", binaryData1);
		string binaryFile2 = CreateBinaryFile("binary2.dat", binaryData2);

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(binaryFile1, binaryFile2);
		Assert.IsNotNull(diff);

		Assert.IsGreaterThan(0, diff.Length, "Binary file diff should produce output showing that the files differ");
		Assert.Contains("Binary content", diff, "Binary files should be reported as binary rather than diffed line by line");
	}
}
