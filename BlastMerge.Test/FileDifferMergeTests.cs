// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="FileDiffer.MergeLines(string[], string[], string?)"/>.
/// </summary>
/// <remarks>
/// The merge must never drop or duplicate a line, whatever shape the diff takes. Pure
/// insertions and pure deletions are the cases that regressed previously, so they are
/// covered alongside same-line modifications. These cases operate purely on in-memory
/// lines, so they deliberately do not use a mock file system.
/// </remarks>
[TestClass]
public class FileDifferMergeTests
{
	[TestMethod]
	public void MergeLines_WithNullLines1_ThrowsArgumentNullException()
	{
		// Arrange
		string[] lines2 = ["a"];

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => FileDiffer.MergeLines(null!, lines2));
	}

	[TestMethod]
	public void MergeLines_WithNullLines2_ThrowsArgumentNullException()
	{
		// Arrange
		string[] lines1 = ["a"];

		// Act & Assert
		Assert.ThrowsExactly<ArgumentNullException>(() => FileDiffer.MergeLines(lines1, null!));
	}

	[TestMethod]
	public void MergeLines_WithIdenticalLines_ReturnsOriginalContentWithoutConflicts()
	{
		// Arrange
		string[] lines1 = ["a", "b", "c"];
		string[] lines2 = ["a", "b", "c"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, lines2);

		// Assert
		CollectionAssert.AreEqual(lines1, result.MergedLines.ToArray());
		Assert.AreEqual(0, result.Conflicts.Count);
	}

	[TestMethod]
	public void MergeLines_WithBothSidesEmpty_ReturnsEmptyResult()
	{
		// Act
		MergeResult result = FileDiffer.MergeLines([], []);

		// Assert
		Assert.AreEqual(0, result.MergedLines.Count);
		Assert.AreEqual(0, result.Conflicts.Count);
	}

	[TestMethod]
	public void MergeLines_WithPureDeletion_KeepsLeadingLineAndDoesNotDuplicateTrailingLines()
	{
		// Arrange - "b" is deleted; every other line must survive exactly once.
		string[] lines1 = ["a", "b", "c", "d"];
		string[] lines2 = ["a", "c", "d"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, lines2);

		// Assert
		string[] expected =
		[
			"a",
			"<<<<<<< Version 1 (deleted)",
			"b",
			"=======",
			">>>>>>> Version 2 (not present)",
			"c",
			"d"
		];
		CollectionAssert.AreEqual(expected, result.MergedLines.ToArray());

		Assert.AreEqual(1, result.Conflicts.Count);
		MergeConflict conflict = result.Conflicts.First();
		Assert.AreEqual("b", conflict.Content1);
		Assert.IsNull(conflict.Content2);
	}

	[TestMethod]
	public void MergeLines_WithPureInsertion_PlacesConflictAtTheInsertionPoint()
	{
		// Arrange - "X" is inserted between "a" and "b", so the conflict belongs after "a".
		string[] lines1 = ["a", "b", "c"];
		string[] lines2 = ["a", "X", "b", "c"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, lines2);

		// Assert
		string[] expected =
		[
			"a",
			"<<<<<<< Version 1 (not present)",
			"=======",
			"X",
			">>>>>>> Version 2 (added)",
			"b",
			"c"
		];
		CollectionAssert.AreEqual(expected, result.MergedLines.ToArray());

		Assert.AreEqual(1, result.Conflicts.Count);
		MergeConflict conflict = result.Conflicts.First();
		Assert.IsNull(conflict.Content1);
		Assert.AreEqual("X", conflict.Content2);
	}

	[TestMethod]
	public void MergeLines_WithSameLineModification_PlacesConflictBetweenSurroundingLines()
	{
		// Arrange
		string[] lines1 = ["a", "b", "c"];
		string[] lines2 = ["a", "B", "c"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, lines2);

		// Assert
		string[] expected =
		[
			"a",
			"<<<<<<< Version 1",
			"b",
			"=======",
			"B",
			">>>>>>> Version 2",
			"c"
		];
		CollectionAssert.AreEqual(expected, result.MergedLines.ToArray());

		Assert.AreEqual(1, result.Conflicts.Count);
		MergeConflict conflict = result.Conflicts.First();
		Assert.AreEqual("b", conflict.Content1);
		Assert.AreEqual("B", conflict.Content2);
	}

	[TestMethod]
	public void MergeLines_WithModificationInsertionAndDeletion_PreservesEveryUnchangedLineInOrder()
	{
		// Arrange - "b" becomes "X", "d" is deleted, and "f" is appended.
		string[] lines1 = ["a", "b", "c", "d", "e"];
		string[] lines2 = ["a", "X", "c", "e", "f"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, lines2);

		// Assert
		string[] expected =
		[
			"a",
			"<<<<<<< Version 1",
			"b",
			"=======",
			"X",
			">>>>>>> Version 2",
			"c",
			"<<<<<<< Version 1 (deleted)",
			"d",
			"=======",
			">>>>>>> Version 2 (not present)",
			"e",
			"<<<<<<< Version 1 (not present)",
			"=======",
			"f",
			">>>>>>> Version 2 (added)"
		];
		CollectionAssert.AreEqual(expected, result.MergedLines.ToArray());
		Assert.AreEqual(3, result.Conflicts.Count);
	}

	[TestMethod]
	public void MergeLines_WithMultiLineDeletion_RecordsEveryDeletedLine()
	{
		// Arrange
		string[] lines1 = ["a", "b", "c", "d"];
		string[] lines2 = ["a", "d"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, lines2);

		// Assert
		string[] expected =
		[
			"a",
			"<<<<<<< Version 1 (deleted)",
			"b",
			"c",
			"=======",
			">>>>>>> Version 2 (not present)",
			"d"
		];
		CollectionAssert.AreEqual(expected, result.MergedLines.ToArray());

		Assert.AreEqual(1, result.Conflicts.Count);
		MergeConflict conflict = result.Conflicts.First();
		Assert.AreEqual($"b{Environment.NewLine}c", conflict.Content1);
		Assert.IsNull(conflict.Content2);
	}

	[TestMethod]
	public void MergeLines_WithInsertionAtTheStart_PlacesConflictBeforeTheFirstLine()
	{
		// Arrange
		string[] lines1 = ["a", "b"];
		string[] lines2 = ["X", "a", "b"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, lines2);

		// Assert
		string[] expected =
		[
			"<<<<<<< Version 1 (not present)",
			"=======",
			"X",
			">>>>>>> Version 2 (added)",
			"a",
			"b"
		];
		CollectionAssert.AreEqual(expected, result.MergedLines.ToArray());
		Assert.AreEqual(1, result.Conflicts.Count);
	}

	[TestMethod]
	public void MergeLines_WithSecondVersionEmpty_ReportsASingleDeletionOfEveryLine()
	{
		// Arrange
		string[] lines1 = ["a", "b"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, []);

		// Assert
		string[] expected =
		[
			"<<<<<<< Version 1 (deleted)",
			"a",
			"b",
			"=======",
			">>>>>>> Version 2 (not present)"
		];
		CollectionAssert.AreEqual(expected, result.MergedLines.ToArray());
		Assert.AreEqual(1, result.Conflicts.Count);
	}

	[TestMethod]
	public void MergeLines_ConflictLineNumbers_PointAtTheConflictMarkerInTheOutput()
	{
		// Arrange
		string[] lines1 = ["a", "b", "c", "d", "e"];
		string[] lines2 = ["a", "X", "c", "e", "f"];

		// Act
		MergeResult result = FileDiffer.MergeLines(lines1, lines2);

		// Assert - each recorded line number is the 1-based position of that conflict's
		// opening marker in MergedLines.
		foreach (MergeConflict conflict in result.Conflicts)
		{
			string markerLine = result.MergedLines[conflict.LineNumber - 1];
			Assert.IsTrue(
				markerLine.StartsWith("<<<<<<<", StringComparison.Ordinal),
				$"Expected a conflict start marker at line {conflict.LineNumber}, found '{markerLine}'.");
		}
	}
}

/// <summary>
/// Tests for <see cref="FileDiffer.MergeFiles(string, string, System.IO.Abstractions.IFileSystem?)"/>,
/// the file-backed entry point onto the same merge.
/// </summary>
[TestClass]
public class FileDifferMergeFilesTests : MockFileSystemTestBase
{
	[TestMethod]
	public void MergeFiles_WithPureDeletion_ProducesTheSameResultAsMergeLines()
	{
		// Arrange
		string file1 = CreateFile("version1.txt", string.Join(Environment.NewLine, ["a", "b", "c", "d"]));
		string file2 = CreateFile("version2.txt", string.Join(Environment.NewLine, ["a", "c", "d"]));

		// Act
		MergeResult result = FileDiffer.MergeFiles(file1, file2, MockFileSystem);

		// Assert
		string[] expected =
		[
			"a",
			"<<<<<<< Version 1 (deleted)",
			"b",
			"=======",
			">>>>>>> Version 2 (not present)",
			"c",
			"d"
		];
		CollectionAssert.AreEqual(expected, result.MergedLines.ToArray());
		Assert.AreEqual(1, result.Conflicts.Count);
	}

	[TestMethod]
	public void MergeFiles_WithIdenticalFiles_ReturnsOriginalContentWithoutConflicts()
	{
		// Arrange
		string content = string.Join(Environment.NewLine, ["a", "b", "c"]);
		string file1 = CreateFile("same1.txt", content);
		string file2 = CreateFile("same2.txt", content);

		// Act
		MergeResult result = FileDiffer.MergeFiles(file1, file2, MockFileSystem);

		// Assert
		CollectionAssert.AreEqual(new[] { "a", "b", "c" }, result.MergedLines.ToArray());
		Assert.AreEqual(0, result.Conflicts.Count);
	}
}
