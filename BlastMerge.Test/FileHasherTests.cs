// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ktsu.BlastMerge.Services;
using ktsu.BlastMerge.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileHasherTests : DependencyInjectionTestBase
{
	private string _testFilePath1 = null!;
	private string _testFilePath2 = null!;
	private string _testFilePath3 = null!;
	private FileHasherAdapter _fileHasherAdapter = null!;

	protected override void InitializeTestData()
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
		string hash1 = _fileHasherAdapter.ComputeFileHash(_testFilePath1);
		string hash2 = _fileHasherAdapter.ComputeFileHash(_testFilePath2);

		// Assert
		Assert.AreEqual(hash1, hash2, "Files with identical content should have the same hash");
	}

	[TestMethod]
	public void ComputeFileHash_DifferentContent_ReturnsDifferentHash()
	{
		// Act
		string hash1 = _fileHasherAdapter.ComputeFileHash(_testFilePath1);
		string hash3 = _fileHasherAdapter.ComputeFileHash(_testFilePath3);

		// Assert
		Assert.AreNotEqual(hash1, hash3, "Files with different content should have different hashes");
	}

	[TestMethod]
	public void ComputeFileHash_EmptyFile_ReturnsValidHash()
	{
		// Arrange
		string emptyFilePath = CreateFile("empty.txt", string.Empty);

		// Act
		string hash = _fileHasherAdapter.ComputeFileHash(emptyFilePath);

		// Assert
		Assert.IsNotNull(hash);
		Assert.AreEqual(64, hash.Length, "Hash should be 64 characters long (SHA256 hash as hex)");
	}

	[TestMethod]
	public void ComputeFileHash_NonexistentFile_ThrowsFileNotFoundException()
	{
		// Act & Assert
		Assert.ThrowsException<FileNotFoundException>(() =>
			_fileHasherAdapter.ComputeFileHash(MockFileSystem.Path.Combine(TestDirectory, "nonexistent.txt")));
	}

	// NEW TESTS FOR IMPROVED COVERAGE - Testing static methods directly

	[TestMethod]
	public async Task ComputeFileHashAsync_SameContent_ReturnsSameHash()
	{
		// Act
		string hash1 = await FileHasher.ComputeFileHashAsync(_testFilePath1, MockFileSystem).ConfigureAwait(false);
		string hash2 = await FileHasher.ComputeFileHashAsync(_testFilePath2, MockFileSystem).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(hash1, hash2, "Files with identical content should have the same hash when computed asynchronously");
	}

	[TestMethod]
	public async Task ComputeFileHashAsync_DifferentContent_ReturnsDifferentHash()
	{
		// Act
		string hash1 = await FileHasher.ComputeFileHashAsync(_testFilePath1, MockFileSystem).ConfigureAwait(false);
		string hash3 = await FileHasher.ComputeFileHashAsync(_testFilePath3, MockFileSystem).ConfigureAwait(false);

		// Assert
		Assert.AreNotEqual(hash1, hash3, "Files with different content should have different hashes when computed asynchronously");
	}

	[TestMethod]
	public async Task ComputeFileHashesAsync_MultipleFiles_ReturnsCorrectHashes()
	{
		// Arrange
		List<string> filePaths = [_testFilePath1, _testFilePath2, _testFilePath3];

		// Act
		Dictionary<string, string> results = await FileHasher.ComputeFileHashesAsync(filePaths, MockFileSystem).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results.ContainsKey(_testFilePath1));
		Assert.IsTrue(results.ContainsKey(_testFilePath2));
		Assert.IsTrue(results.ContainsKey(_testFilePath3));
		Assert.AreEqual(results[_testFilePath1], results[_testFilePath2]); // Same content
		Assert.AreNotEqual(results[_testFilePath1], results[_testFilePath3]); // Different content
	}

	[TestMethod]
	public async Task ComputeFileHashesAsync_WithMaxParallelism_ReturnsCorrectHashes()
	{
		// Arrange
		List<string> filePaths = [_testFilePath1, _testFilePath2, _testFilePath3];

		// Act
		Dictionary<string, string> results = await FileHasher.ComputeFileHashesAsync(filePaths, MockFileSystem, maxDegreeOfParallelism: 1).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(3, results.Count);
		Assert.AreEqual(results[_testFilePath1], results[_testFilePath2]); // Same content
	}

	[TestMethod]
	public async Task ComputeFileHashesAsync_NullFilePaths_ThrowsArgumentNullException()
	{
		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await FileHasher.ComputeFileHashesAsync(null!, MockFileSystem).ConfigureAwait(false)).ConfigureAwait(false);
	}

	[TestMethod]
	public void ComputeContentHash_SameContent_ReturnsSameHash()
	{
		// Arrange
		string content1 = "Hello World";
		string content2 = "Hello World";

		// Act
		string hash1 = FileHasher.ComputeContentHash(content1);
		string hash2 = FileHasher.ComputeContentHash(content2);

		// Assert
		Assert.AreEqual(hash1, hash2, "Same content should produce same hash");
	}

	[TestMethod]
	public void ComputeContentHash_DifferentContent_ReturnsDifferentHash()
	{
		// Arrange
		string content1 = "Hello World";
		string content2 = "Goodbye World";

		// Act
		string hash1 = FileHasher.ComputeContentHash(content1);
		string hash2 = FileHasher.ComputeContentHash(content2);

		// Assert
		Assert.AreNotEqual(hash1, hash2, "Different content should produce different hashes");
	}

	[TestMethod]
	public void ComputeContentHash_EmptyString_ReturnsValidHash()
	{
		// Act
		string hash = FileHasher.ComputeContentHash(string.Empty);

		// Assert
		Assert.IsNotNull(hash);
		Assert.AreEqual(64, hash.Length, "Hash should be 64 characters long");
	}

	[TestMethod]
	public void ComputeContentHash_NullContent_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
		{
			FileHasher.ComputeContentHash(null!);
		});
	}

	[TestMethod]
	public async Task ComputeContentHashAsync_SameContent_ReturnsSameHash()
	{
		// Arrange
		string content1 = "Hello World";
		string content2 = "Hello World";

		// Act
		string hash1 = await FileHasher.ComputeContentHashAsync(content1).ConfigureAwait(false);
		string hash2 = await FileHasher.ComputeContentHashAsync(content2).ConfigureAwait(false);

		// Assert
		Assert.AreEqual(hash1, hash2, "Same content should produce same hash when computed asynchronously");
	}

	[TestMethod]
	public async Task ComputeContentHashAsync_DifferentContent_ReturnsDifferentHash()
	{
		// Arrange
		string content1 = "Hello World";
		string content2 = "Goodbye World";

		// Act
		string hash1 = await FileHasher.ComputeContentHashAsync(content1).ConfigureAwait(false);
		string hash2 = await FileHasher.ComputeContentHashAsync(content2).ConfigureAwait(false);

		// Assert
		Assert.AreNotEqual(hash1, hash2, "Different content should produce different hashes when computed asynchronously");
	}

	[TestMethod]
	public async Task ComputeContentHashAsync_NullContent_ThrowsArgumentNullException()
	{
		// Act & Assert
		await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await FileHasher.ComputeContentHashAsync(null!).ConfigureAwait(false)).ConfigureAwait(false);
	}

	[TestMethod]
	public void ComputeFileHash_LargeFile_ReturnsValidHash()
	{
		// Arrange
		string largeContent = new('A', 10000); // 10KB of 'A' characters
		string largeFilePath = CreateFile("large.txt", largeContent);

		// Act
		string hash = _fileHasherAdapter.ComputeFileHash(largeFilePath);

		// Assert
		Assert.IsNotNull(hash);
		Assert.AreEqual(64, hash.Length, "Hash should be 64 characters long");
	}

	[TestMethod]
	public void ComputeContentHash_UnicodeContent_ReturnsValidHash()
	{
		// Arrange
		string unicodeContent = "Hello 世界 🌍 Мир";

		// Act
		string hash = FileHasher.ComputeContentHash(unicodeContent);

		// Assert
		Assert.IsNotNull(hash);
		Assert.AreEqual(64, hash.Length, "Hash should be 64 characters long");
	}

	[TestMethod]
	public async Task ComputeFileHashesAsync_EmptyList_ReturnsEmptyDictionary()
	{
		// Arrange
		List<string> emptyList = [];

		// Act
		Dictionary<string, string> results = await FileHasher.ComputeFileHashesAsync(emptyList, MockFileSystem).ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(results);
		Assert.AreEqual(0, results.Count);
	}
}
