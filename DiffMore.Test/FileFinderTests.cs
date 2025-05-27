// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileFinderTests : MockFileSystemTestBase
{
	private required string _testSubdir1;
	private required string _testSubdir2;
	private required string _testNestedSubdir;
	private required FileFinderAdapter _fileFinderAdapter;

	protected override void InitializeFileSystem()
	{
		// Set up directories
		_testSubdir1 = CreateDirectory("Subdir1");
		_testSubdir2 = CreateDirectory("Subdir2");
		_testNestedSubdir = CreateDirectory(Path.Combine("Subdir1", "NestedSubdir"));

		// Create test files with the same name in different directories
		CreateFile(Path.Combine("test.txt"), "Root test file");
		CreateFile(Path.Combine("Subdir1", "test.txt"), "Subdir1 test file");
		CreateFile(Path.Combine("Subdir2", "test.txt"), "Subdir2 test file");
		CreateFile(Path.Combine("Subdir1", "NestedSubdir", "test.txt"), "Nested test file");

		// Create some files with different names
		CreateFile(Path.Combine("other.txt"), "Other file");
		CreateFile(Path.Combine("Subdir1", "different.txt"), "Different file");

		// Initialize the adapter
		_fileFinderAdapter = new FileFinderAdapter(MockFileSystem);
	}

	[TestMethod]
	public void FindFiles_ExistingFileName_ReturnsAllMatches()
	{
		// Act
		var files = _fileFinderAdapter.FindFiles(TestDirectory, "test.txt");

		// Assert
		Assert.AreEqual(4, files.Count, "Should find all 4 test.txt files");

		// Verify specific files are found
		Assert.IsTrue(files.Contains(Path.Combine(TestDirectory, "test.txt")), "Root file should be found");
		Assert.IsTrue(files.Contains(Path.Combine(_testSubdir1, "test.txt")), "Subdir1 file should be found");
		Assert.IsTrue(files.Contains(Path.Combine(_testSubdir2, "test.txt")), "Subdir2 file should be found");
		Assert.IsTrue(files.Contains(Path.Combine(_testNestedSubdir, "test.txt")), "Nested file should be found");
	}

	[TestMethod]
	public void FindFiles_NonExistingFileName_ReturnsEmptyCollection()
	{
		// Act
		var files = _fileFinderAdapter.FindFiles(TestDirectory, "nonexistent.txt");

		// Assert
		Assert.AreEqual(0, files.Count, "Should return empty collection for non-existent files");
	}

	[TestMethod]
	public void FindFiles_UniqueFileName_ReturnsSingleFile()
	{
		// Act
		var files = _fileFinderAdapter.FindFiles(TestDirectory, "different.txt");

		// Assert
		Assert.AreEqual(1, files.Count, "Should find exactly one file");
		Assert.AreEqual(Path.Combine(_testSubdir1, "different.txt"), files.First(), "Should find the correct file");
	}

	[TestMethod]
	public void FindFiles_NonExistentDirectory_ReturnsEmptyCollection()
	{
		// Act
		var files = _fileFinderAdapter.FindFiles(Path.Combine(TestDirectory, "NonExistentDir"), "test.txt");

		// Assert
		Assert.AreEqual(0, files.Count, "Should return empty collection for non-existent directory");
	}

	[TestMethod]
	public void FindFiles_WithWildcard_ReturnsAllMatches()
	{
		// Act
		var files = _fileFinderAdapter.FindFiles(TestDirectory, "*.txt");

		// Assert
		Assert.AreEqual(6, files.Count, "Should find all 6 .txt files");
	}
}
