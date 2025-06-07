// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileDifferDiffTests : MockFileSystemTestBase
{
	private string _testFile1 = null!;
	private string _testFile2 = null!;
	private string _identicalFile = null!;
	private FileDifferAdapter _fileDifferAdapter = null!;

	protected override void InitializeFileSystem()
	{
		// Initialize adapter
		_fileDifferAdapter = new FileDifferAdapter(MockFileSystem);

		// Create test files with known differences
		_testFile1 = CreateFile("file1.txt", string.Join('\n', [
			"Line 1",
			"Line 2",
			"Line 3",
			"Line 4",
			"Line 5"
		]));

		_testFile2 = CreateFile("file2.txt", string.Join('\n', [
			"Line 1",
			"Line 2 modified",  // Changed line
			"Line 3",
			"New line inserted", // Added line
			"Line 4",
			"Line 5"
		]));

		// Create identical copy of file1
		_identicalFile = CreateFile("identical.txt", string.Join('\n', [
			"Line 1",
			"Line 2",
			"Line 3",
			"Line 4",
			"Line 5"
		]));
	}

	[TestMethod]
	public void FindDifferences_IdenticalFiles_ReturnsEmptyCollection()
	{
		// Act
		IReadOnlyCollection<LineDifference> differences = _fileDifferAdapter.FindDifferences(_testFile1, _identicalFile);

		// Assert
		Assert.AreEqual(0, differences.Count, "Should return no differences for identical files");
	}

	[TestMethod]
	public void GenerateGitStyleDiff_IdenticalFiles_ReturnsEmptyString()
	{
		// Act
		string diff = _fileDifferAdapter.GenerateGitStyleDiff(_testFile1, _identicalFile);

		// Assert
		Assert.AreEqual(string.Empty, diff, "Git style diff should be empty for identical files");
	}

	[TestMethod]
	public void GenerateGitStyleDiff_WithDifferences_ReturnsNonEmptyString()
	{
		// Act
		string diff = _fileDifferAdapter.GenerateGitStyleDiff(_testFile1, _testFile2);

		// Assert
		Assert.IsFalse(string.IsNullOrEmpty(diff), "Git style diff should not be empty when files differ");
		Assert.IsTrue(diff.Contains("Line 2"), "Diff should contain the original line");
		Assert.IsTrue(diff.Contains("Line 2 modified"), "Diff should contain the modified line");
		Assert.IsTrue(diff.Contains("New line inserted"), "Diff should contain the added line");
	}

	[TestMethod]
	public void GenerateColoredDiff_IdenticalFiles_OnlyContainsDefaultColorLines()
	{
		// Act
		System.Collections.ObjectModel.Collection<ColoredDiffLine> coloredDiff = _fileDifferAdapter.GenerateColoredDiff(_testFile1, _identicalFile);

		// Assert
		Assert.IsTrue(coloredDiff.All(l => l.Color is DiffColor.Default or DiffColor.FileHeader),
			"All lines should have Default or FileHeader color for identical files");
	}

	[TestMethod]
	public void GenerateColoredDiff_WithDifferences_ContainsColoredLines()
	{
		// Act
		System.Collections.ObjectModel.Collection<ColoredDiffLine> coloredDiff = _fileDifferAdapter.GenerateColoredDiff(_testFile1, _testFile2);

		// Assert
		Assert.IsTrue(coloredDiff.Any(l => l.Color == DiffColor.Addition),
			"Should contain lines with Addition color");
		Assert.IsTrue(coloredDiff.Any(l => l.Color == DiffColor.Deletion),
			"Should contain lines with Deletion color");

		// Verify specific content
		Assert.IsTrue(coloredDiff.Any(l => l.Content.Contains("Line 2") && l.Color == DiffColor.Deletion),
			"Original 'Line 2' should be marked as deleted");
		Assert.IsTrue(coloredDiff.Any(l => l.Content.Contains("Line 2 modified") && l.Color == DiffColor.Addition),
			"Modified 'Line 2 modified' should be marked as added");
	}

	[TestMethod]
	[ExpectedException(typeof(FileNotFoundException))]
	public void FindDifferences_NonExistentFile_ThrowsFileNotFoundException()
	{
		// Act
		_fileDifferAdapter.FindDifferences(_testFile1, Path.Combine(TestDirectory, "nonexistent.txt"));
	}

	[TestMethod]
	public void SyncFile_CopiesContent()
	{
		// Arrange
		string targetFile = CreateFile("target.txt", "Original content");

		// Act
		_fileDifferAdapter.SyncFile(_testFile1, targetFile);

		// Assert
		string sourceContent = MockFileSystem.File.ReadAllText(_testFile1);
		string targetContent = MockFileSystem.File.ReadAllText(targetFile);
		Assert.AreEqual(sourceContent, targetContent, "Target file should have the same content as source");
	}
}
