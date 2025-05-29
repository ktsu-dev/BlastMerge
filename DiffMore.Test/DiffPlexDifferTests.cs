// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for DiffPlex-based diffing functionality
/// </summary>
[TestClass]
public class DiffPlexDifferTests
{
	private string _testDirectory = string.Empty;
	private string _file1 = string.Empty;
	private string _file2 = string.Empty;
	private string _identicalFile = string.Empty;

	/// <summary>
	/// Sets up test environment before each test
	/// </summary>
	[TestInitialize]
	public void TestInitialize()
	{
		// Create a unique test directory for each test
		_testDirectory = Path.Combine(Path.GetTempPath(), $"DiffPlexDifferTests_{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDirectory);

		// Create test files
		_file1 = Path.Combine(_testDirectory, "file1.txt");
		_file2 = Path.Combine(_testDirectory, "file2.txt");
		_identicalFile = Path.Combine(_testDirectory, "identical.txt");

		File.WriteAllText(_file1, """
			Line 1
			Line 2
			Line 3
			Line 4
			""");

		File.WriteAllText(_file2, """
			Line 1
			Modified Line 2
			Line 3
			New Line 4
			Line 5
			""");

		File.WriteAllText(_identicalFile, """
			Line 1
			Line 2
			Line 3
			Line 4
			""");
	}

	/// <summary>
	/// Cleans up test environment after each test
	/// </summary>
	[TestCleanup]
	public void TestCleanup()
	{
		if (Directory.Exists(_testDirectory))
		{
			Directory.Delete(_testDirectory, recursive: true);
		}
	}

	/// <summary>
	/// Tests that identical files are correctly identified
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_IdenticalFiles_ReturnsTrue()
	{
		var result = DiffPlexDiffer.AreFilesIdentical(_file1, _identicalFile);
		Assert.IsTrue(result);
	}

	/// <summary>
	/// Tests that different files are correctly identified
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_DifferentFiles_ReturnsFalse()
	{
		var result = DiffPlexDiffer.AreFilesIdentical(_file1, _file2);
		Assert.IsFalse(result);
	}

	/// <summary>
	/// Tests unified diff generation for different files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_DifferentFiles_ReturnsValidDiff()
	{
		var diff = DiffPlexDiffer.GenerateUnifiedDiff(_file1, _file2);

		Assert.IsFalse(string.IsNullOrEmpty(diff));
		Assert.IsTrue(diff.Contains($"--- {_file1}"));
		Assert.IsTrue(diff.Contains($"+++ {_file2}"));
		Assert.IsTrue(diff.Contains("@@"));
	}

	/// <summary>
	/// Tests unified diff generation for identical files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_IdenticalFiles_ReturnsHeaderOnly()
	{
		var diff = DiffPlexDiffer.GenerateUnifiedDiff(_file1, _identicalFile);

		Assert.IsTrue(diff.Contains($"--- {_file1}"));
		Assert.IsTrue(diff.Contains($"+++ {_identicalFile}"));
		// Should not contain any change blocks for identical files
		Assert.IsFalse(diff.Contains("@@"));
	}

	/// <summary>
	/// Tests colored diff generation
	/// </summary>
	[TestMethod]
	public void GenerateColoredDiff_DifferentFiles_ReturnsColoredLines()
	{
		var coloredDiff = DiffPlexDiffer.GenerateColoredDiff(_file1, _file2);

		Assert.IsTrue(coloredDiff.Count > 0);

		// Should contain file headers
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.FileHeader));

		// Should contain additions and deletions
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.Addition));
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.Deletion));
	}

	/// <summary>
	/// Tests finding differences between files
	/// </summary>
	[TestMethod]
	public void FindDifferences_DifferentFiles_ReturnsCorrectDifferences()
	{
		var differences = DiffPlexDiffer.FindDifferences(_file1, _file2);

		Assert.IsTrue(differences.Count > 0);

		// Should find both additions and deletions
		Assert.IsTrue(differences.Any(d => d.Type == LineDifferenceType.Added));
		Assert.IsTrue(differences.Any(d => d.Type == LineDifferenceType.Deleted));
	}

	/// <summary>
	/// Tests side-by-side diff generation
	/// </summary>
	[TestMethod]
	public void GenerateSideBySideDiff_DifferentFiles_ReturnsValidModel()
	{
		var sideBySide = DiffPlexDiffer.GenerateSideBySideDiff(_file1, _file2);

		Assert.IsNotNull(sideBySide);
		Assert.IsNotNull(sideBySide.OldText);
		Assert.IsNotNull(sideBySide.NewText);
		Assert.IsTrue(sideBySide.OldText.Lines.Count > 0);
		Assert.IsTrue(sideBySide.NewText.Lines.Count > 0);
	}

	/// <summary>
	/// Tests change summary generation
	/// </summary>
	[TestMethod]
	public void GenerateChangeSummary_DifferentFiles_ReturnsOnlyChanges()
	{
		var changeSummary = DiffPlexDiffer.GenerateChangeSummary(_file1, _file2);

		Assert.IsTrue(changeSummary.Count > 0);

		// Should not contain unchanged lines (default color)
		var unchangedLines = changeSummary.Where(line => line.Color == DiffColor.Default).ToList();
		// File headers might have default color, but content should not
		Assert.IsTrue(unchangedLines.Count == 0 || unchangedLines.All(line =>
			line.Content.Contains(_file1) || line.Content.Contains(_file2) || string.IsNullOrEmpty(line.Content.Trim())));
	}

	/// <summary>
	/// Tests error handling for non-existent files
	/// </summary>
	[TestMethod]
	[ExpectedException(typeof(FileNotFoundException))]
	public void GenerateUnifiedDiff_NonExistentFile_ThrowsException()
	{
		var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");
		DiffPlexDiffer.GenerateUnifiedDiff(_file1, nonExistentFile);
	}

	/// <summary>
	/// Tests handling of empty files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_EmptyFiles_HandlesCorrectly()
	{
		var emptyFile1 = Path.Combine(_testDirectory, "empty1.txt");
		var emptyFile2 = Path.Combine(_testDirectory, "empty2.txt");

		File.WriteAllText(emptyFile1, string.Empty);
		File.WriteAllText(emptyFile2, string.Empty);

		var diff = DiffPlexDiffer.GenerateUnifiedDiff(emptyFile1, emptyFile2);
		Assert.IsNotNull(diff);

		// Should identify as identical
		var areIdentical = DiffPlexDiffer.AreFilesIdentical(emptyFile1, emptyFile2);
		Assert.IsTrue(areIdentical);
	}

	/// <summary>
	/// Tests handling of binary-like files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_BinaryFiles_HandlesCorrectly()
	{
		var binaryFile1 = Path.Combine(_testDirectory, "binary1.bin");
		var binaryFile2 = Path.Combine(_testDirectory, "binary2.bin");

		// Create files with binary-like content
		File.WriteAllBytes(binaryFile1, [0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE]);
		File.WriteAllBytes(binaryFile2, [0x00, 0x01, 0x04, 0x05, 0xFF, 0xFE]);

		var diff = DiffPlexDiffer.GenerateUnifiedDiff(binaryFile1, binaryFile2);
		Assert.IsNotNull(diff);

		// DiffPlex should handle binary files as text and show differences
		Assert.IsTrue(diff.Length > 0);
	}
}
