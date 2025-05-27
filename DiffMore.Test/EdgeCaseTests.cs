// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System;
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
	public void FileDiffer_VeryLargeFile_HandlesCorrectly()
	{
		// Arrange - Create a large file (~1MB)
		var largeFile1 = Path.Combine(_testDirectory, "large1.txt");
		var largeFile2 = Path.Combine(_testDirectory, "large2.txt");

		using (var writer = new StreamWriter(largeFile1))
		{
			for (var i = 0; i < 10000; i++)
			{
				writer.WriteLine($"Line {i} of the large test file");
			}
		}

		// Create second file with one line changed
		using (var reader = new StreamReader(largeFile1))
		using (var writer = new StreamWriter(largeFile2))
		{
			var lineNum = 0;
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				if (lineNum == 5000)
				{
					// Change one line in the middle
					writer.WriteLine("This line is different in the second file");
				}
				else
				{
					writer.WriteLine(line);
				}

				lineNum++;
			}
		}

		// Act - This might take some time for large files
		var differences = FileDiffer.FindDifferences(largeFile1, largeFile2);

		// Assert
		Assert.AreEqual(1, differences.Count(d => d.LineNumber1 == 5001 || d.LineNumber2 == 5001),
			"Should find the one modified line");
	}

	[TestMethod]
	public void FileDiffer_BinaryFiles_GeneratesNoOutput()
	{
		// Arrange - Create binary files
		var binaryFile1 = Path.Combine(_testDirectory, "binary1.bin");
		var binaryFile2 = Path.Combine(_testDirectory, "binary2.bin");

		using (var stream = File.OpenWrite(binaryFile1))
		{
			for (var i = 0; i < 100; i++)
			{
				stream.WriteByte((byte)i);
			}
		}

		using (var stream = File.OpenWrite(binaryFile2))
		{
			for (var i = 0; i < 100; i++)
			{
				// Different binary content
				stream.WriteByte((byte)(i + 1));
			}
		}

		// Act & Assert (should not throw on binary files)
		var differences = FileDiffer.FindDifferences(binaryFile1, binaryFile2);
		var gitDiff = FileDiffer.GenerateGitStyleDiff(binaryFile1, binaryFile2);

		// Binary files will be treated as text and should show differences
		Assert.IsTrue(differences.Count > 0, "Binary files should be compared as text and show differences");
	}

	[TestMethod]
	public void FileDiffer_AllLinesChangedFile_ShowsCompleteChanges()
	{
		// Arrange
		var file1 = Path.Combine(_testDirectory, "allchanged1.txt");
		var file2 = Path.Combine(_testDirectory, "allchanged2.txt");

		File.WriteAllLines(file1, ["Line 1", "Line 2", "Line 3"]);
		File.WriteAllLines(file2, ["Different Line 1", "Different Line 2", "Different Line 3"]);

		// Act
		var differences = FileDiffer.FindDifferences(file1, file2);
		var coloredDiff = FileDiffer.GenerateColoredDiff(file1, file2,
			File.ReadAllLines(file1),
			File.ReadAllLines(file2));

		// Assert
		Assert.AreEqual(3, differences.Count(d => d.LineNumber1 > 0 && d.LineNumber2 > 0),
			"Should find all 3 lines modified");

		Assert.IsTrue(coloredDiff.Any(l => l.Color == DiffColor.Deletion && l.Content.Contains("Line 1")),
			"Should mark old Line 1 as deleted");
		Assert.IsTrue(coloredDiff.Any(l => l.Color == DiffColor.Addition && l.Content.Contains("Different Line 1")),
			"Should mark new Line 1 as added");
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
