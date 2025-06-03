// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.Linq;
using ktsu.BlastMerge.Core;
using ktsu.BlastMerge.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileDifferGroupingTests : MockFileSystemTestBase
{
	private string _testFile1 = null!;
	private string _testFile2 = null!;
	private string _testFile3 = null!;
	private string _testFile4 = null!;
	private FileDifferAdapter _fileDifferAdapter = null!;

	protected override void InitializeFileSystem()
	{
		// Initialize adapter
		_fileDifferAdapter = new FileDifferAdapter(MockFileSystem);

		// Create test files (file1 and file2 have identical content)
		_testFile1 = CreateFile("file1.txt", "This is test content 1");
		_testFile2 = CreateFile("file2.txt", "This is test content 1"); // Same as file1
		_testFile3 = CreateFile("file3.txt", "This is test content 3"); // Different
		_testFile4 = CreateFile("file4.txt", "This is test content 3"); // Same as file3
	}

	[TestMethod]
	public void GroupFilesByHash_WithIdenticalFiles_GroupsCorrectly()
	{
		// Arrange
		var files = new List<string> { _testFile1, _testFile2, _testFile3, _testFile4 };

		// Act
		var groups = _fileDifferAdapter.GroupFilesByHash(files);

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
		var uniqueFile = CreateFile("unique.txt", "This is unique content");
		var files = new List<string> { _testFile1, _testFile3, uniqueFile };

		// Act
		var groups = _fileDifferAdapter.GroupFilesByHash(files);

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
		var emptyFile1 = CreateFile("empty1.txt", string.Empty);
		var emptyFile2 = CreateFile("empty2.txt", string.Empty);
		var files = new List<string> { emptyFile1, emptyFile2, _testFile1 };

		// Act
		var groups = _fileDifferAdapter.GroupFilesByHash(files);

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
		_fileDifferAdapter.GroupFilesByHash(null!);
	}

	[TestMethod]
	public void GroupFilesByHash_WithEmptyList_ReturnsEmptyCollection()
	{
		// Act
		var groups = _fileDifferAdapter.GroupFilesByHash([]);

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
