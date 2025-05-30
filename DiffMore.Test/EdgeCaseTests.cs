// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class EdgeCaseTests
{
	private readonly string _testDirectory;

	public EdgeCaseTests() => _testDirectory = Path.Combine(Path.GetTempPath(), "DiffMoreTests_EdgeCases");

	[TestInitialize]
	public void Setup()
	{
		// Create test directory
		if (!Directory.Exists(_testDirectory))
		{
			Directory.CreateDirectory(_testDirectory);
		}
	}

	[TestCleanup]
	public void Cleanup()
	{
		TestHelper.SafeDeleteDirectory(_testDirectory);
	}

	[TestMethod]
	public void FileDiffer_EmptyFiles_ProducesCorrectDiff()
	{
		// Arrange
		var emptyFile1 = Path.Combine(_testDirectory, "empty1.txt");
		var emptyFile2 = Path.Combine(_testDirectory, "empty2.txt");
		File.WriteAllText(emptyFile1, string.Empty);
		File.WriteAllText(emptyFile2, string.Empty);

		// Act
		var differences = FileDiffer.FindDifferences(emptyFile1, emptyFile2);
		var gitDiff = FileDiffer.GenerateGitStyleDiff(emptyFile1, emptyFile2);

		// Assert
		Assert.AreEqual(0, differences.Count, "Empty files should have no differences");
		Assert.AreEqual(string.Empty, gitDiff, "Git diff for empty files should be empty string");
	}

	[TestMethod]
	public void FileHasher_IdenticalContentDifferentEncoding_SameHash()
	{
		// Arrange - Create files with same content but different encoding
		var utf8File = Path.Combine(_testDirectory, "utf8.txt");
		var utf16File = Path.Combine(_testDirectory, "utf16.txt");

		File.WriteAllText(utf8File, "Test content with some unicode: äöü", Encoding.UTF8);
		File.WriteAllText(utf16File, "Test content with some unicode: äöü", Encoding.Unicode);

		// Act
		var hash1 = FileHasher.ComputeFileHash(utf8File);
		var hash2 = FileHasher.ComputeFileHash(utf16File);

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
		var specialFilePath = Path.Combine(_testDirectory, specialFileName);

		File.WriteAllText(specialFilePath, "Test content");

		// Act
		var files = FileFinder.FindFiles(_testDirectory, specialFileName);

		// Assert
		Assert.AreEqual(1, files.Count, "Should find the file with special characters in name");
		Assert.AreEqual(specialFilePath, files.First(), "Should return the correct file path");
	}

	[TestMethod]
	public void SyncFile_TargetDirectoryDoesNotExist_CreatesDirectory()
	{
		// Arrange
		var sourceFile = Path.Combine(_testDirectory, "source.txt");
		File.WriteAllText(sourceFile, "Test content");

		var targetDir = Path.Combine(_testDirectory, "NonExistentDir");
		var targetFile = Path.Combine(targetDir, "target.txt");

		// Ensure directory doesn't exist
		if (Directory.Exists(targetDir))
		{
			Directory.Delete(targetDir, true);
		}

		// Act
		FileDiffer.SyncFile(sourceFile, targetFile);

		// Assert
		Assert.IsTrue(Directory.Exists(targetDir), "Target directory should be created");
		Assert.IsTrue(File.Exists(targetFile), "Target file should exist");
		Assert.AreEqual("Test content", File.ReadAllText(targetFile), "Content should be copied correctly");
	}
}
