// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.BlastMerge.Test.Adapters;

[TestClass]
public class EdgeCaseTests : MockFileSystemTestBase
{
	private FileDifferAdapter _fileDifferAdapter = null!;
	private FileHasherAdapter _fileHasherAdapter = null!;
	private FileFinderAdapter _fileFinderAdapter = null!;

	protected override void InitializeFileSystem()
	{
		// Initialize adapters
		_fileDifferAdapter = new FileDifferAdapter(MockFileSystem);
		_fileHasherAdapter = new FileHasherAdapter(MockFileSystem);
		_fileFinderAdapter = new FileFinderAdapter(MockFileSystem);
	}

	[TestMethod]
	public void FileDiffer_EmptyFiles_ProducesCorrectDiff()
	{
		// Arrange
		var emptyFile1 = CreateFile("empty1.txt", string.Empty);
		var emptyFile2 = CreateFile("empty2.txt", string.Empty);

		// Act
		var differences = _fileDifferAdapter.FindDifferences(emptyFile1, emptyFile2);
		var gitDiff = _fileDifferAdapter.GenerateGitStyleDiff(emptyFile1, emptyFile2);

		// Assert
		Assert.AreEqual(0, differences.Count, "Empty files should have no differences");
		Assert.AreEqual(string.Empty, gitDiff, "Git diff for empty files should be empty string");
	}

	[TestMethod]
	public void FileHasher_IdenticalContentDifferentEncoding_SameHash()
	{
		// Arrange - Create files with same content but different encoding
		var utf8Content = "Test content with some unicode: äöü";
		var utf8Bytes = Encoding.UTF8.GetBytes(utf8Content);
		var utf16Bytes = Encoding.Unicode.GetBytes(utf8Content);

		var utf8File = CreateFile("utf8.txt", "");
		var utf16File = CreateFile("utf16.txt", "");

		// Write the bytes directly to simulate different encodings
		MockFileSystem.File.WriteAllBytes(utf8File, utf8Bytes);
		MockFileSystem.File.WriteAllBytes(utf16File, utf16Bytes);

		// Act
		var hash1 = _fileHasherAdapter.ComputeFileHash(utf8File);
		var hash2 = _fileHasherAdapter.ComputeFileHash(utf16File);

		// Assert
		// Note: We expect different hashes because the actual byte content is different
		// due to different encodings, even though the text appears the same
		Assert.AreNotEqual(hash1, hash2, "Files with same text but different encodings should have different hashes");
	}

	[TestMethod]
	public void FileFinder_SpecialCharactersInFilename_FindsCorrectly()
	{
		// Arrange - Create files with special characters in names
		var specialFileName = "test #$%^&.txt";
		var specialFilePath = CreateFile(specialFileName, "Test content");

		// Act
		var files = _fileFinderAdapter.FindFiles(TestDirectory, specialFileName);

		// Assert
		Assert.AreEqual(1, files.Count, "Should find the file with special characters in name");
		Assert.AreEqual(specialFilePath, files.First(), "Should return the correct file path");
	}

	[TestMethod]
	public void SyncFile_TargetDirectoryDoesNotExist_CreatesDirectory()
	{
		// Arrange
		var sourceFile = CreateFile("source.txt", "Test content");
		var targetDir = Path.Combine(TestDirectory, "NonExistentDir");
		var targetFile = Path.Combine(targetDir, "target.txt");

		// Ensure directory doesn't exist in mock file system
		Assert.IsFalse(MockFileSystem.Directory.Exists(targetDir));

		// Act
		_fileDifferAdapter.SyncFile(sourceFile, targetFile);

		// Assert
		Assert.IsTrue(MockFileSystem.Directory.Exists(targetDir), "Target directory should be created");
		Assert.IsTrue(MockFileSystem.File.Exists(targetFile), "Target file should exist");
		Assert.AreEqual("Test content", MockFileSystem.File.ReadAllText(targetFile), "Content should be copied correctly");
	}
}
