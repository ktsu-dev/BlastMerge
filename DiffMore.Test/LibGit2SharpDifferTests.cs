// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.DiffMore.Core;

/// <summary>
/// Tests for LibGit2Sharp-based diffing functionality
/// </summary>
[TestClass]
public class LibGit2SharpDifferTests
{
	private string _testDirectory = null!;
	private string _file1 = null!;
	private string _file2 = null!;
	private string _identicalFile = null!;

	/// <summary>
	/// Initialize test environment
	/// </summary>
	[TestInitialize]
	public void Setup()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), $"LibGit2SharpDifferTests_{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDirectory);

		_file1 = Path.Combine(_testDirectory, "file1.txt");
		_file2 = Path.Combine(_testDirectory, "file2.txt");
		_identicalFile = Path.Combine(_testDirectory, "identical.txt");

		// Create test files
		File.WriteAllLines(_file1, [
			"Line 1",
			"Line 2",
			"Line 3",
			"Line 4"
		]);

		File.WriteAllLines(_file2, [
			"Line 1",
			"Modified Line 2",
			"Line 3",
			"Line 4",
			"Added Line 5"
		]);

		File.WriteAllLines(_identicalFile, [
			"Line 1",
			"Line 2",
			"Line 3",
			"Line 4"
		]);
	}

	/// <summary>
	/// Cleanup test environment
	/// </summary>
	[TestCleanup]
	public void Cleanup()
	{
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, recursive: true);
		}
	}

	/// <summary>
	/// Test that identical files are detected correctly
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_IdenticalFiles_ReturnsTrue()
	{
		// Act
		var result = LibGit2SharpDiffer.AreFilesIdentical(_file1, _identicalFile);

		// Assert
		Assert.IsTrue(result, "Identical files should be detected as identical");
	}

	/// <summary>
	/// Test that different files are detected correctly
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_DifferentFiles_ReturnsFalse()
	{
		// Act
		var result = LibGit2SharpDiffer.AreFilesIdentical(_file1, _file2);

		// Assert
		Assert.IsFalse(result, "Different files should be detected as different");
	}

	/// <summary>
	/// Test that git-style diff is generated correctly
	/// </summary>
	[TestMethod]
	public void GenerateGitStyleDiff_DifferentFiles_ReturnsValidDiff()
	{
		// Act
		var diff = LibGit2SharpDiffer.GenerateGitStyleDiff(_file1, _file2);

		// Assert
		Assert.IsFalse(string.IsNullOrEmpty(diff), "Diff should not be empty for different files");
		Assert.IsTrue(diff.Contains("-Line 2"), "Should show deletion of original line");
		Assert.IsTrue(diff.Contains("+Modified Line 2"), "Should show addition of modified line");
		Assert.IsTrue(diff.Contains("+Added Line 5"), "Should show addition of new line");
	}

	/// <summary>
	/// Test that identical files produce no diff
	/// </summary>
	[TestMethod]
	public void GenerateGitStyleDiff_IdenticalFiles_ReturnsEmptyDiff()
	{
		// Act
		var diff = LibGit2SharpDiffer.GenerateGitStyleDiff(_file1, _identicalFile);

		// Assert
		Assert.IsTrue(string.IsNullOrEmpty(diff), "Identical files should produce no diff output");
	}

	/// <summary>
	/// Test that colored diff includes appropriate colors
	/// </summary>
	[TestMethod]
	public void GenerateColoredDiff_DifferentFiles_ReturnsColoredLines()
	{
		// Act
		var coloredDiff = LibGit2SharpDiffer.GenerateColoredDiff(_file1, _file2);

		// Assert
		Assert.IsTrue(coloredDiff.Count > 0, "Should return colored diff lines");

		var hasAddition = coloredDiff.Any(line => line.Color == DiffColor.Addition);
		var hasDeletion = coloredDiff.Any(line => line.Color == DiffColor.Deletion);
		var hasHeader = coloredDiff.Any(line => line.Color == DiffColor.FileHeader);

		Assert.IsTrue(hasAddition, "Should have addition lines");
		Assert.IsTrue(hasDeletion, "Should have deletion lines");
		Assert.IsTrue(hasHeader, "Should have header lines");
	}

	/// <summary>
	/// Test that FindDifferences returns compatible format
	/// </summary>
	[TestMethod]
	public void FindDifferences_DifferentFiles_ReturnsLineDifferences()
	{
		// Act
		var differences = LibGit2SharpDiffer.FindDifferences(_file1, _file2);

		// Assert
		Assert.IsTrue(differences.Count > 0, "Should find differences between different files");

		// Check that we have both deletions and additions
		var hasDeletion = differences.Any(d => d.LineNumber1 > 0 && d.LineNumber2 == 0);
		var hasAddition = differences.Any(d => d.LineNumber1 == 0 && d.LineNumber2 > 0);

		Assert.IsTrue(hasDeletion, "Should have deletion differences");
		Assert.IsTrue(hasAddition, "Should have addition differences");
	}

	/// <summary>
	/// Test that context lines parameter works correctly
	/// </summary>
	[TestMethod]
	public void GenerateGitStyleDiff_WithContextLines_IncludesContext()
	{
		// Arrange
		var contextLines = 2;

		// Act
		var diff = LibGit2SharpDiffer.GenerateGitStyleDiff(_file1, _file2, contextLines);

		// Assert
		Assert.IsFalse(string.IsNullOrEmpty(diff), "Diff should not be empty");

		// Should include context lines around changes
		Assert.IsTrue(diff.Contains(" Line 1"), "Should include context line before change");
		Assert.IsTrue(diff.Contains(" Line 3"), "Should include context line after change");
	}

	/// <summary>
	/// Test that the LibGit2Sharp implementation produces compatible results with existing FileDiffer
	/// </summary>
	[TestMethod]
	public void FindDifferences_ComparedToFileDiffer_ProducesComparableResults()
	{
		// Act
		var libgit2Results = LibGit2SharpDiffer.FindDifferences(_file1, _file2);
		var originalResults = FileDiffer.FindDifferences(_file1, _file2);

		// Assert
		Assert.IsTrue(libgit2Results.Count > 0, "LibGit2Sharp should find differences");
		Assert.IsTrue(originalResults.Count > 0, "Original FileDiffer should find differences");

		// Both should detect that files are different
		Assert.IsTrue(libgit2Results.Count > 0 && originalResults.Count > 0,
			"Both implementations should detect differences");
	}

	/// <summary>
	/// Test error handling for non-existent files
	/// </summary>
	[TestMethod]
	[ExpectedException(typeof(FileNotFoundException))]
	public void GenerateGitStyleDiff_NonExistentFile_ThrowsException()
	{
		// Arrange
		var nonExistentFile = Path.Combine(_testDirectory, "does_not_exist.txt");

		// Act
		LibGit2SharpDiffer.GenerateGitStyleDiff(_file1, nonExistentFile);

		// Assert - expect exception
	}

	/// <summary>
	/// Test handling of binary files
	/// </summary>
	[TestMethod]
	public void GenerateGitStyleDiff_BinaryFiles_HandlesGracefully()
	{
		// Arrange
		var binaryFile1 = Path.Combine(_testDirectory, "binary1.bin");
		var binaryFile2 = Path.Combine(_testDirectory, "binary2.bin");

		// Create some binary data
		File.WriteAllBytes(binaryFile1, [0x00, 0x01, 0x02, 0x03, 0xFF]);
		File.WriteAllBytes(binaryFile2, [0x00, 0x01, 0x02, 0x04, 0xFF]);

		// Act
		var diff = LibGit2SharpDiffer.GenerateGitStyleDiff(binaryFile1, binaryFile2);

		// Assert
		Assert.IsFalse(string.IsNullOrEmpty(diff), "Should generate diff for binary files");
		// LibGit2Sharp should handle binary files appropriately
	}
}
