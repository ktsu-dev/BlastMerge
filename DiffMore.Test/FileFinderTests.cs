// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System.IO;
using ktsu.DiffMore.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileFinderTests : MockFileSystemTestBase
{
	private FileFinderAdapter _fileFinderAdapter = null!;

	protected override void InitializeFileSystem()
	{
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
	public void FindFiles_NonExistingFileName_ReturnsEmptyCollection()
	{
		// Act
		var files = _fileFinderAdapter.FindFiles(TestDirectory, "nonexistent.txt");

		// Assert
		Assert.AreEqual(0, files.Count, "Should return empty collection for non-existent files");
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
