// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiffPlex.DiffBuilder.Model;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for the DiffPlexDiffer service
/// </summary>
[TestClass]
public class DiffPlexDifferTests : DependencyInjectionTestBase
{
	private string _file1 = string.Empty;
	private string _file2 = string.Empty;
	private string _identicalFile = string.Empty;
	private DiffPlexDiffer _diffPlexDiffer = null!;

	/// <summary>
	/// Sets up test files in the mock file system
	/// </summary>
	protected override void InitializeTestData()
	{
		// Get the DiffPlexDiffer service from DI
		_diffPlexDiffer = GetService<DiffPlexDiffer>();

		// Create test files in the mock file system
		_file1 = CreateFile("file1.txt", "Line 1\nLine 2\nLine 3");
		_file2 = CreateFile("file2.txt", "Line 1\nModified Line 2\nLine 3\nLine 4");
		_identicalFile = CreateFile("identical.txt", "Line 1\nLine 2\nLine 3");
	}

	/// <summary>
	/// Tests that identical files are correctly identified
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_IdenticalFiles_ReturnsTrue()
	{
		// Create temp files in the mock file system instead of real file system
		string tempFile1 = CreateFile("temp1.txt", MockFileSystem.File.ReadAllText(_file1));
		string tempFile2 = CreateFile("temp2.txt", MockFileSystem.File.ReadAllText(_identicalFile));

		bool result = _diffPlexDiffer.AreFilesIdentical(tempFile1, tempFile2);
		Assert.IsTrue(result);
	}

	/// <summary>
	/// Tests that different files are correctly identified
	/// </summary>
	[TestMethod]
	public void AreFilesIdentical_DifferentFiles_ReturnsFalse()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		bool result = _diffPlexDiffer.AreFilesIdentical(_file1, _file2);
		Assert.IsFalse(result);
	}

	/// <summary>
	/// Tests unified diff generation for different files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_DifferentFiles_ReturnsValidDiff()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		string diff = _diffPlexDiffer.GenerateUnifiedDiff(_file1, _file2);

		Assert.IsFalse(string.IsNullOrEmpty(diff));
		Assert.IsTrue(diff.Contains($"--- {_file1}"));
		Assert.IsTrue(diff.Contains($"+++ {_file2}"));
		Assert.IsTrue(diff.Contains("@@"));
	}

	/// <summary>
	/// Tests unified diff generation for identical files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_IdenticalFiles_ReturnsEmptyString()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		string diff = _diffPlexDiffer.GenerateUnifiedDiff(_file1, _identicalFile);

		// For identical files, git returns empty string
		Assert.AreEqual(string.Empty, diff, "Unified diff should be empty for identical files");
	}

	/// <summary>
	/// Tests colored diff generation
	/// </summary>
	[TestMethod]
	public void GenerateColoredDiff_DifferentFiles_ReturnsColoredLines()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		System.Collections.ObjectModel.Collection<ColoredDiffLine> coloredDiff = _diffPlexDiffer.GenerateColoredDiff(_file1, _file2);

		Assert.IsTrue(coloredDiff.Count > 0);

		// Should contain file headers
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.FileHeader));

		// Should contain additions and deletions
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.Addition));
		Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.Deletion));
	}

	/// <summary>
	/// Tests side-by-side diff generation
	/// </summary>
	[TestMethod]
	public void GenerateSideBySideDiff_DifferentFiles_ReturnsValidModel()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		SideBySideDiffModel sideBySide = _diffPlexDiffer.GenerateSideBySideDiff(_file1, _file2);

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
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		System.Collections.ObjectModel.Collection<ColoredDiffLine> changeSummary = _diffPlexDiffer.GenerateChangeSummary(_file1, _file2);

		Assert.IsTrue(changeSummary.Count > 0);

		// Should not contain unchanged lines (default color)
		List<ColoredDiffLine> unchangedLines = [.. changeSummary.Where(line => line.Color == DiffColor.Default)];
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
		// Use mock file system consistently - create a file that exists and one that doesn't exist in the mock file system
		string existingFile = CreateFile("existing.txt", MockFileSystem.File.ReadAllText(_file1));
		string nonExistentFile = MockFileSystem.Path.Combine(TestDirectory, "nonexistent.txt");
		// Don't create the nonExistentFile, so it won't exist in the mock file system

		_diffPlexDiffer.GenerateUnifiedDiff(existingFile, nonExistentFile);
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

		string diff = _diffPlexDiffer.GenerateUnifiedDiff(emptyFile1, emptyFile2);
		Assert.IsNotNull(diff);

		// Should identify as identical
		bool areIdentical = _diffPlexDiffer.AreFilesIdentical(emptyFile1, emptyFile2);
		Assert.IsTrue(areIdentical);
	}

	/// <summary>
	/// Tests handling of binary-like files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_BinaryFiles_HandlesCorrectly()
	{
		// Use mock file system directly since DiffPlexDiffer uses FileSystemProvider.Current
		// Create files with binary-like content using mock file system
		byte[] binaryData1 = [0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE];
		byte[] binaryData2 = [0x00, 0x01, 0x04, 0x05, 0xFF, 0xFE];

		string binaryFile1 = CreateFile("binary1.dat", System.Text.Encoding.UTF8.GetString(binaryData1));
		string binaryFile2 = CreateFile("binary2.dat", System.Text.Encoding.UTF8.GetString(binaryData2));

		string diff = _diffPlexDiffer.GenerateUnifiedDiff(binaryFile1, binaryFile2);
		Assert.IsNotNull(diff);

		// DiffPlex should handle binary files as text and show differences
		Assert.IsTrue(diff.Length > 0);
	}

	/// <summary>
	/// Tests the new in-memory API for finding differences from string arrays
	/// </summary>
	[TestMethod]
	public void FindDifferencesFromLines_DifferentArrays_ReturnsCorrectDifferences()
	{
		// Arrange
		string[] lines1 = ["Line 1", "Line 2", "Line 3"];
		string[] lines2 = ["Line 1", "Modified Line 2", "Line 3", "New Line 4"];

		// Act
		IReadOnlyCollection<LineDifference> differences = DiffPlexDiffer.FindDifferencesFromLines(lines1, lines2);

		// Assert
		Assert.IsTrue(differences.Count > 0);
		Assert.IsTrue(differences.Any(d => d.Type == LineDifferenceType.Modified));
		Assert.IsTrue(differences.Any(d => d.Type == LineDifferenceType.Added));
	}

	/// <summary>
	/// Tests the new in-memory API for finding differences from content strings
	/// </summary>
	[TestMethod]
	public void FindDifferencesFromContent_DifferentContent_ReturnsCorrectDifferences()
	{
		// Arrange
		string content1 = "Line 1\nLine 2\nLine 3";
		string content2 = "Line 1\nModified Line 2\nLine 3\nNew Line 4";

		// Act
		IReadOnlyCollection<LineDifference> differences = DiffPlexDiffer.FindDifferencesFromContent(content1, content2);

		// Assert
		Assert.IsTrue(differences.Count > 0);
		Assert.IsTrue(differences.Any(d => d.Type == LineDifferenceType.Modified));
		Assert.IsTrue(differences.Any(d => d.Type == LineDifferenceType.Added));
	}

	/// <summary>
	/// Tests that identical content returns no differences
	/// </summary>
	[TestMethod]
	public void FindDifferencesFromContent_IdenticalContent_ReturnsNoDifferences()
	{
		// Arrange
		string content = "Line 1\nLine 2\nLine 3";

		// Act
		IReadOnlyCollection<LineDifference> differences = DiffPlexDiffer.FindDifferencesFromContent(content, content);

		// Assert
		Assert.AreEqual(0, differences.Count);
	}
}
