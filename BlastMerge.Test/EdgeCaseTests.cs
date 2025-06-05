// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.IO;
using System.Linq;
using System.Text;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
		string emptyFile1 = CreateFile("empty1.txt", string.Empty);
		string emptyFile2 = CreateFile("empty2.txt", string.Empty);

		// Act
		IReadOnlyCollection<LineDifference> differences = _fileDifferAdapter.FindDifferences(emptyFile1, emptyFile2);
		string gitDiff = _fileDifferAdapter.GenerateGitStyleDiff(emptyFile1, emptyFile2);

		// Assert
		Assert.AreEqual(0, differences.Count, "Empty files should have no differences");
		Assert.AreEqual(string.Empty, gitDiff, "Git diff for empty files should be empty string");
	}

	[TestMethod]
	public void FileHasher_IdenticalContentDifferentEncoding_SameHash()
	{
		// Arrange - Create files with same content but different encoding
		string utf8Content = "Test content with some unicode: äöü";
		byte[] utf8Bytes = Encoding.UTF8.GetBytes(utf8Content);
		byte[] utf16Bytes = Encoding.Unicode.GetBytes(utf8Content);

		string utf8File = CreateFile("utf8.txt", "");
		string utf16File = CreateFile("utf16.txt", "");

		// Write the bytes directly to simulate different encodings
		MockFileSystem.File.WriteAllBytes(utf8File, utf8Bytes);
		MockFileSystem.File.WriteAllBytes(utf16File, utf16Bytes);

		// Act
		string hash1 = _fileHasherAdapter.ComputeFileHash(utf8File);
		string hash2 = _fileHasherAdapter.ComputeFileHash(utf16File);

		// Assert
		// Note: We expect different hashes because the actual byte content is different
		// due to different encodings, even though the text appears the same
		Assert.AreNotEqual(hash1, hash2, "Files with same text but different encodings should have different hashes");
	}

	[TestMethod]
	public void FileFinder_SpecialCharactersInFilename_FindsCorrectly()
	{
		// Arrange - Create files with special characters in names
		string specialFileName = "test #$%^&.txt";
		string specialFilePath = CreateFile(specialFileName, "Test content");

		// Act
		IReadOnlyCollection<string> files = _fileFinderAdapter.FindFiles(TestDirectory, specialFileName);

		// Assert
		Assert.AreEqual(1, files.Count, "Should find the file with special characters in name");
		Assert.AreEqual(specialFilePath, files.First(), "Should return the correct file path");
	}

	[TestMethod]
	public void SyncFile_TargetDirectoryDoesNotExist_CreatesDirectory()
	{
		// Arrange
		string sourceFile = CreateFile("source.txt", "Test content");
		string targetDir = Path.Combine(TestDirectory, "NonExistentDir");
		string targetFile = Path.Combine(targetDir, "target.txt");

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
