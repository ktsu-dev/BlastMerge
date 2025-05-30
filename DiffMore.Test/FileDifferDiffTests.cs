// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileDifferDiffTests
{
	private readonly string _testDirectory;
	private readonly string _testFile1;
	private readonly string _testFile2;
	private readonly string _testFile3;
	private readonly string _identicalFile;

	public FileDifferDiffTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), "DiffMoreTests_Diff");
		_testFile1 = Path.Combine(_testDirectory, "file1.txt");
		_testFile2 = Path.Combine(_testDirectory, "file2.txt");
		_testFile3 = Path.Combine(_testDirectory, "file3.txt");
		_identicalFile = Path.Combine(_testDirectory, "identical.txt");
	}

	[TestInitialize]
	public void Setup()
	{
		// Create test directory
		if (!Directory.Exists(_testDirectory))
		{
			Directory.CreateDirectory(_testDirectory);
		}

		// Create test files with known differences
		File.WriteAllLines(_testFile1,
		[
			"Line 1",
			"Line 2",
			"Line 3",
			"Line 4",
			"Line 5"
		]);

		File.WriteAllLines(_testFile2,
		[
			"Line 1",
			"Line 2 modified",  // Changed line
                "Line 3",
			"New line inserted", // Added line
                "Line 4",
			"Line 5"
		]);

		File.WriteAllLines(_testFile3,
		[
			"Line 1",
			"Line 3",           // Line 2 removed
                "Line 4",
			"Line 5",
			"Line 6 added"      // Added line at end
            ]);

		// Create identical copy of file1
		File.Copy(_testFile1, _identicalFile, true);
	}

	[TestCleanup]
	public void Cleanup()
	{
		// Clean up test directory
		if (Directory.Exists(_testDirectory))
		{
			try
			{
				Directory.Delete(_testDirectory, true);
			}
			catch (IOException)
			{
				// Ignore IO exceptions during cleanup
			}
			catch (UnauthorizedAccessException)
			{
				// Ignore access exceptions during cleanup
			}
		}
	}

	[TestMethod]
	public void FindDifferences_IdenticalFiles_ReturnsEmptyCollection()
	{
		// Act
		var differences = FileDiffer.FindDifferences(_testFile1, _identicalFile);

		// Assert
		Assert.AreEqual(0, differences.Count, "Should return no differences for identical files");
	}

	[TestMethod]
	public void GenerateGitStyleDiff_IdenticalFiles_ReturnsEmptyString()
	{
		// Act
		var diff = FileDiffer.GenerateGitStyleDiff(_testFile1, _identicalFile);

		// Assert
		Assert.AreEqual(string.Empty, diff, "Git style diff should be empty for identical files");
	}

	[TestMethod]
	public void GenerateGitStyleDiff_WithDifferences_ReturnsNonEmptyString()
	{
		// Act
		var diff = FileDiffer.GenerateGitStyleDiff(_testFile1, _testFile2);

		// Assert
		Assert.IsFalse(string.IsNullOrEmpty(diff), "Git style diff should not be empty when files differ");
		Assert.IsTrue(diff.Contains("Line 2"), "Diff should contain the original line");
		Assert.IsTrue(diff.Contains("Line 2 modified"), "Diff should contain the modified line");
		Assert.IsTrue(diff.Contains("New line inserted"), "Diff should contain the added line");
	}

	[TestMethod]
	public void GenerateColoredDiff_IdenticalFiles_OnlyContainsDefaultColorLines()
	{
		// Arrange
		var lines1 = File.ReadAllLines(_testFile1);
		var lines2 = File.ReadAllLines(_identicalFile);

		// Act
		var coloredDiff = FileDiffer.GenerateColoredDiff(_testFile1, _identicalFile, lines1, lines2);

		// Assert
		Assert.IsTrue(coloredDiff.All(l => l.Color is DiffColor.Default or DiffColor.FileHeader),
			"All lines should have Default or FileHeader color for identical files");
	}

	[TestMethod]
	public void GenerateColoredDiff_WithDifferences_ContainsColoredLines()
	{
		// Arrange
		var lines1 = File.ReadAllLines(_testFile1);
		var lines2 = File.ReadAllLines(_testFile2);

		// Act
		var coloredDiff = FileDiffer.GenerateColoredDiff(_testFile1, _testFile2, lines1, lines2);

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
		FileDiffer.FindDifferences(_testFile1, Path.Combine(_testDirectory, "nonexistent.txt"));
	}

	[TestMethod]
	public void SyncFile_CopiesContent()
	{
		// Arrange
		var targetFile = Path.Combine(_testDirectory, "target.txt");
		File.WriteAllText(targetFile, "Original content");

		// Act
		FileDiffer.SyncFile(_testFile1, targetFile);

		// Assert
		var sourceContent = File.ReadAllText(_testFile1);
		var targetContent = File.ReadAllText(targetFile);
		Assert.AreEqual(sourceContent, targetContent, "Target file should have the same content as source");
	}
}
