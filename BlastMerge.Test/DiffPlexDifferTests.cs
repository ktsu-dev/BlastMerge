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
			Assert.IsTrue(result);
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
		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			File.WriteAllText(tempFile2, MockFileSystem.File.ReadAllText(_file2));

			bool result = DiffPlexDiffer.AreFilesIdentical(tempFile1, tempFile2);
			Assert.IsFalse(result);
		}
		finally
		{
			SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, tempFile1, tempFile2);
		}
	}

	/// <summary>
	/// Tests unified diff generation for different files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_DifferentFiles_ReturnsValidDiff()
	{
		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			File.WriteAllText(tempFile2, MockFileSystem.File.ReadAllText(_file2));

			string diff = DiffPlexDiffer.GenerateUnifiedDiff(tempFile1, tempFile2);

			Assert.IsFalse(string.IsNullOrEmpty(diff));
			Assert.IsTrue(diff.Contains($"--- {tempFile1}"));
			Assert.IsTrue(diff.Contains($"+++ {tempFile2}"));
			Assert.IsTrue(diff.Contains("@@"));
		}
		finally
		{
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}

	/// <summary>
	/// Tests unified diff generation for identical files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_IdenticalFiles_ReturnsEmptyString()
	{
		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			File.WriteAllText(tempFile2, MockFileSystem.File.ReadAllText(_identicalFile));

			string diff = DiffPlexDiffer.GenerateUnifiedDiff(tempFile1, tempFile2);

			// For identical files, git returns empty string
			Assert.AreEqual(string.Empty, diff, "Unified diff should be empty for identical files");
		}
		finally
		{
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}

	/// <summary>
	/// Tests colored diff generation
	/// </summary>
	[TestMethod]
	public void GenerateColoredDiff_DifferentFiles_ReturnsColoredLines()
	{
		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			File.WriteAllText(tempFile2, MockFileSystem.File.ReadAllText(_file2));

			System.Collections.ObjectModel.Collection<ColoredDiffLine> coloredDiff = DiffPlexDiffer.GenerateColoredDiff(tempFile1, tempFile2);

			Assert.IsTrue(coloredDiff.Count > 0);

			// Should contain file headers
			Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.FileHeader));

			// Should contain additions and deletions
			Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.Addition));
			Assert.IsTrue(coloredDiff.Any(line => line.Color == DiffColor.Deletion));
		}
		finally
		{
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}

	/// <summary>
	/// Tests side-by-side diff generation
	/// </summary>
	[TestMethod]
	public void GenerateSideBySideDiff_DifferentFiles_ReturnsValidModel()
	{
		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			File.WriteAllText(tempFile2, MockFileSystem.File.ReadAllText(_file2));

			DiffPlex.DiffBuilder.Model.SideBySideDiffModel sideBySide = DiffPlexDiffer.GenerateSideBySideDiff(tempFile1, tempFile2);

			Assert.IsNotNull(sideBySide);
			Assert.IsNotNull(sideBySide.OldText);
			Assert.IsNotNull(sideBySide.NewText);
			Assert.IsTrue(sideBySide.OldText.Lines.Count > 0);
			Assert.IsTrue(sideBySide.NewText.Lines.Count > 0);
		}
		finally
		{
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}

	/// <summary>
	/// Tests change summary generation
	/// </summary>
	[TestMethod]
	public void GenerateChangeSummary_DifferentFiles_ReturnsOnlyChanges()
	{
		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllText(tempFile1, MockFileSystem.File.ReadAllText(_file1));
			File.WriteAllText(tempFile2, MockFileSystem.File.ReadAllText(_file2));

			System.Collections.ObjectModel.Collection<ColoredDiffLine> changeSummary = DiffPlexDiffer.GenerateChangeSummary(tempFile1, tempFile2);

			Assert.IsTrue(changeSummary.Count > 0);

			// Should not contain unchanged lines (default color)
			List<ColoredDiffLine> unchangedLines = [.. changeSummary.Where(line => line.Color == DiffColor.Default)];
			// File headers might have default color, but content should not
			Assert.IsTrue(unchangedLines.Count == 0 || unchangedLines.All(line =>
				line.Content.Contains(tempFile1) || line.Content.Contains(tempFile2) || string.IsNullOrEmpty(line.Content.Trim())));
		}
		finally
		{
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
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
		string emptyFile1 = CreateFile("empty1.txt", string.Empty);
		string emptyFile2 = CreateFile("empty2.txt", string.Empty);

		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			File.WriteAllText(tempFile1, string.Empty);
			File.WriteAllText(tempFile2, string.Empty);

			string diff = DiffPlexDiffer.GenerateUnifiedDiff(tempFile1, tempFile2);
			Assert.IsNotNull(diff);

			// Should identify as identical
			bool areIdentical = DiffPlexDiffer.AreFilesIdentical(tempFile1, tempFile2);
			Assert.IsTrue(areIdentical);
		}
		finally
		{
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}

	/// <summary>
	/// Tests handling of binary-like files
	/// </summary>
	[TestMethod]
	public void GenerateUnifiedDiff_BinaryFiles_HandlesCorrectly()
	{
		// Create temporary real files for testing since DiffPlexDiffer uses real file system
		string tempFile1 = Path.GetTempFileName();
		string tempFile2 = Path.GetTempFileName();

		try
		{
			// Create files with binary-like content
			File.WriteAllBytes(tempFile1, [0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE]);
			File.WriteAllBytes(tempFile2, [0x00, 0x01, 0x04, 0x05, 0xFF, 0xFE]);

			string diff = DiffPlexDiffer.GenerateUnifiedDiff(tempFile1, tempFile2);
			Assert.IsNotNull(diff);

			// DiffPlex should handle binary files as text and show differences
			Assert.IsTrue(diff.Length > 0);
		}
		finally
		{
			if (File.Exists(tempFile1))
			{
				File.Delete(tempFile1);
			}

			if (File.Exists(tempFile2))
			{
				File.Delete(tempFile2);
			}
		}
	}
}
