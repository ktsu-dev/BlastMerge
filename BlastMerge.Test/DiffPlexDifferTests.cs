// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			File.WriteAllText(tempFile2, MockFileSystem.File.ReadAllText(_identicalFile));

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
	[ExpectedException(typeof(FileNotFoundException))]
	public void GenerateUnifiedDiff_NonExistentFile_ThrowsException()
	{
		string nonExistentFile = Path.GetTempFileName();
		File.Delete(nonExistentFile); // Ensure it doesn't exist

		string tempFile1 = Path.GetTempFileName();
		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			DiffPlexDiffer.GenerateUnifiedDiff(tempFile1, nonExistentFile);
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

		string diff = DiffPlexDiffer.GenerateUnifiedDiff(binaryFile1, binaryFile2);
		Assert.IsNotNull(diff);

		// DiffPlex should handle binary files as text and show differences
		Assert.IsTrue(diff.Length > 0, "Binary file diff should produce output showing differences");
	}
}
