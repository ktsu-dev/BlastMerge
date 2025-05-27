// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ktsu.DiffMore.Core;

[TestClass]
public class FileDifferGroupingTests
{
	private readonly string _testDirectory;
	private readonly string _testFile1;
	private readonly string _testFile2;
	private readonly string _testFile3;
	private readonly string _testFile4;

	public FileDifferGroupingTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), "DiffMoreTests_Grouping");
		_testFile1 = Path.Combine(_testDirectory, "file1.txt");
		_testFile2 = Path.Combine(_testDirectory, "file2.txt");
		_testFile3 = Path.Combine(_testDirectory, "file3.txt");
		_testFile4 = Path.Combine(_testDirectory, "file4.txt");
	}

	[TestInitialize]
	public void Setup()
	{
		// Create test directory
		if (!Directory.Exists(_testDirectory))
		{
			Directory.CreateDirectory(_testDirectory);
		}

		// Create test files (file1 and file2 have identical content)
		File.WriteAllText(_testFile1, "This is test content 1");
		File.WriteAllText(_testFile2, "This is test content 1"); // Same as file1
		File.WriteAllText(_testFile3, "This is test content 3"); // Different
		File.WriteAllText(_testFile4, "This is test content 3"); // Same as file3
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
	public void GroupFilesByHash_WithIdenticalFiles_GroupsCorrectly()
	{
		// Arrange
		var files = new List<string> { _testFile1, _testFile2, _testFile3, _testFile4 };

		// Act
		var groups = FileDiffer.GroupFilesByHash(files);

		// Assert
		Assert.AreEqual(2, groups.Count, "Should create two groups for two unique file contents");

		// Find group containing file1/file2
		var group1 = groups.First(g => g.FilePaths.Contains(_testFile1));
		Assert.AreEqual(2, group1.FilePaths.Count, "First group should contain 2 files");
		Assert.IsTrue(group1.FilePaths.Contains(_testFile1), "First group should contain file1");
		Assert.IsTrue(group1.FilePaths.Contains(_testFile2), "First group should contain file2");

		// Find group containing file3/file4
		var group2 = groups.First(g => g.FilePaths.Contains(_testFile3));
		Assert.AreEqual(2, group2.FilePaths.Count, "Second group should contain 2 files");
		Assert.IsTrue(group2.FilePaths.Contains(_testFile3), "Second group should contain file3");
		Assert.IsTrue(group2.FilePaths.Contains(_testFile4), "Second group should contain file4");

		// Verify the hashes are different
		Assert.AreNotEqual(group1.Hash, group2.Hash, "The two groups should have different hashes");
	}

	[TestMethod]
	public void GroupFilesByHash_WithUniqueFiles_CreatesSeperateGroups()
	{
		// Arrange
		var uniqueFile = Path.Combine(_testDirectory, "unique.txt");
		File.WriteAllText(uniqueFile, "This is unique content");
		var files = new List<string> { _testFile1, _testFile3, uniqueFile };

		// Act
		var groups = FileDiffer.GroupFilesByHash(files);

		// Assert
		Assert.AreEqual(3, groups.Count, "Should create three groups for three unique file contents");

		// Check each group has only one file
		var uniqueFileGroup = groups.First(g => g.FilePaths.Contains(uniqueFile));
		Assert.AreEqual(1, uniqueFileGroup.FilePaths.Count, "Unique file should be in its own group");
	}

	[TestMethod]
	public void GroupFilesByHash_WithEmptyFiles_GroupsCorrectly()
	{
		// Arrange
		var emptyFile1 = Path.Combine(_testDirectory, "empty1.txt");
		var emptyFile2 = Path.Combine(_testDirectory, "empty2.txt");
		File.WriteAllText(emptyFile1, string.Empty);
		File.WriteAllText(emptyFile2, string.Empty);
		var files = new List<string> { emptyFile1, emptyFile2, _testFile1 };

		// Act
		var groups = FileDiffer.GroupFilesByHash(files);

		// Assert
		Assert.AreEqual(2, groups.Count, "Should create two groups (empty files and non-empty file)");

		// Find empty file group
		var emptyGroup = groups.First(g => g.FilePaths.Contains(emptyFile1));
		Assert.AreEqual(2, emptyGroup.FilePaths.Count, "Empty file group should contain 2 files");
		Assert.IsTrue(emptyGroup.FilePaths.Contains(emptyFile2), "Empty file group should contain both empty files");
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void GroupFilesByHash_WithNullInput_ThrowsArgumentNullException()
	{
		// Act
		FileDiffer.GroupFilesByHash(null!);
	}

	[TestMethod]
	public void GroupFilesByHash_WithEmptyList_ReturnsEmptyCollection()
	{
		// Act
		var groups = FileDiffer.GroupFilesByHash([]);

		// Assert
		Assert.AreEqual(0, groups.Count, "Should return empty collection for empty input");
	}

	[TestMethod]
	public void FileGroup_AddFilePath_AddsCorrectly()
	{
		// Arrange
		var group = new FileGroup { Hash = "test-hash" };
		var filePath = "test/path.txt";

		// Act
		group.AddFilePath(filePath);

		// Assert
		Assert.AreEqual(1, group.FilePaths.Count, "Group should have one file path");
		Assert.AreEqual(filePath, group.FilePaths.First(), "Group should contain the added file path");
	}
}
