// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using ktsu.DiffMore.Core;
using ktsu.DiffMore.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileHasherTests : MockFileSystemTestBase
{
	private string _testFilePath1 = null!;
	private string _testFilePath2 = null!;
	private string _testFilePath3 = null!;
	private FileHasherAdapter _fileHasherAdapter = null!;

	protected override void InitializeFileSystem()
	{
		// Create test files with different content
		_testFilePath1 = CreateFile("test1.txt", "This is test file 1");
		_testFilePath2 = CreateFile("test2.txt", "This is test file 1"); // Same content as file 1
		_testFilePath3 = CreateFile("test3.txt", "This is test file 3"); // Different content

		// Initialize the adapter
		_fileHasherAdapter = new FileHasherAdapter(MockFileSystem);
	}

	[TestMethod]
	public void ComputeFileHash_SameContent_ReturnsSameHash()
	{
		// Act
		var hash1 = _fileHasherAdapter.ComputeFileHash(_testFilePath1);
		var hash2 = _fileHasherAdapter.ComputeFileHash(_testFilePath2);

		// Assert
		Assert.AreEqual(hash1, hash2, "Files with identical content should have the same hash");
	}

	[TestMethod]
	public void ComputeFileHash_DifferentContent_ReturnsDifferentHash()
	{
		// Act
		var hash1 = _fileHasherAdapter.ComputeFileHash(_testFilePath1);
		var hash3 = _fileHasherAdapter.ComputeFileHash(_testFilePath3);

		// Assert
		Assert.AreNotEqual(hash1, hash3, "Files with different content should have different hashes");
	}

	[TestMethod]
	public void ComputeFileHash_EmptyFile_ReturnsValidHash()
	{
		// Arrange
		var emptyFilePath = CreateFile("empty.txt", string.Empty);

		// Act
		var hash = _fileHasherAdapter.ComputeFileHash(emptyFilePath);

		// Assert
		Assert.IsNotNull(hash);
		Assert.AreEqual(16, hash.Length, "Hash should be 16 characters long (64-bit FNV hash as hex)");
	}

	[TestMethod]
	[ExpectedException(typeof(FileNotFoundException))]
	public void ComputeFileHash_NonexistentFile_ThrowsFileNotFoundException()
	{
		// Act
		_fileHasherAdapter.ComputeFileHash(Path.Combine(TestDirectory, "nonexistent.txt"));

		// Assert is handled by ExpectedException attribute
	}
}
