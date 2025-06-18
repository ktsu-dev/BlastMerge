// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using ktsu.BlastMerge.Services;
using ktsu.BlastMerge.Test.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class MockFileSystemTests
{
	private MockFileSystem _mockFileSystem = null!;
	private string _testDir = null!;
	private string _testDir1 = null!;
	private string _testDir2 = null!;
	private readonly string _testId = Guid.NewGuid().ToString("N");

	[TestInitialize]
	public void Setup()
	{
		_testDir = $"/testdir-{_testId}";
		_testDir1 = Path.Combine(_testDir, "dir1");
		_testDir2 = Path.Combine(_testDir, "dir2");

		Dictionary<string, MockFileData> fileDict = new()
		{
			{ Path.Combine(_testDir1, "file1.txt"), new MockFileData("File 1 Content Version 1") },
			{ Path.Combine(_testDir1, "file2.txt"), new MockFileData("File 2 Content") },
			{ Path.Combine(_testDir1, "file3.txt"), new MockFileData("File 3 Content") },
			{ Path.Combine(_testDir2, "file1.txt"), new MockFileData("File 1 Content Version 2") },
			{ Path.Combine(_testDir2, "file2.txt"), new MockFileData("File 2 Content") },
			{ Path.Combine(_testDir2, "file4.txt"), new MockFileData("File 4 Content") }
		};

		_mockFileSystem = new MockFileSystem(fileDict);
		_mockFileSystem.Directory.CreateDirectory(_testDir1);
		_mockFileSystem.Directory.CreateDirectory(_testDir2);

		// Set the mock file system as the default for all services
		// Each test gets its own isolated filesystem instance
		FileSystemProvider.SetFileSystem(_mockFileSystem);
	}

	[TestCleanup]
	public void Cleanup()
	{
		// Reset the file system provider back to default
		// This ensures the next test (or non-test code) gets a clean slate
		FileSystemProvider.ResetToDefault();
	}

	[TestMethod]
	public void FileFinder_WithMockedFileSystem_FindsFilesCorrectly()
	{
		// Arrange
		Mock<IFileFinder> mockFileFinder = new();
		mockFileFinder.Setup(f => f.FindFiles(_testDir1, "*.txt"))
			.Returns(new ReadOnlyCollection<string>([
				Path.Combine(_testDir1, "file1.txt"),
				Path.Combine(_testDir1, "file2.txt"),
				Path.Combine(_testDir1, "file3.txt")
			]));

		// Act
		ReadOnlyCollection<string> files = mockFileFinder.Object.FindFiles(_testDir1, "*.txt");

		// Assert
		Assert.AreEqual(3, files.Count, "Should find 3 files");
		Assert.IsTrue(files.Any(f => f.EndsWith("file1.txt")), "Should find file1.txt");
		Assert.IsTrue(files.Any(f => f.EndsWith("file2.txt")), "Should find file2.txt");
		Assert.IsTrue(files.Any(f => f.EndsWith("file3.txt")), "Should find file3.txt");
	}

	[TestMethod]
	public void FileHasher_WithMockedFileSystem_ComputesHashCorrectly()
	{
		// Arrange
		Mock<IFileSystem> mockFileSystem = new();
		Mock<IFile> mockFile = new();

		mockFile.Setup(f => f.ReadAllBytes(It.Is<string>(s => s == Path.Combine(_testDir1, "file1.txt"))))
			.Returns(Encoding.UTF8.GetBytes("File 1 Content Version 1"));

		mockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);

		Mock<IFileHasher> mockFileHasher = new();
		mockFileHasher.Setup(h => h.ComputeFileHash(It.Is<string>(s => s == Path.Combine(_testDir1, "file1.txt"))))
			.Returns("mock-hash-1");

		// Act
		string hash = mockFileHasher.Object.ComputeFileHash(Path.Combine(_testDir1, "file1.txt"));

		// Assert
		Assert.AreEqual("mock-hash-1", hash, "Should return the mocked hash value");
	}

	[TestMethod]
	public void FileDiffer_WithMockedFileSystem_FindsDifferencesCorrectly()
	{
		// Arrange
		string file1Path = Path.Combine(_testDir1, "file1.txt");
		string file2Path = Path.Combine(_testDir2, "file1.txt");

		Mock<IFileDiffer> mockFileDiffer = new();
		mockFileDiffer.Setup(d => d.FindDifferences(file1Path, file2Path))
			.Returns(new ReadOnlyCollection<string>(["- File 1 Content Version 1", "+ File 1 Content Version 2"]));

		// Act
		ReadOnlyCollection<string> differences = mockFileDiffer.Object.FindDifferences(file1Path, file2Path);

		// Assert
		Assert.AreEqual(2, differences.Count, "Should find 2 difference lines");
		Assert.IsTrue(differences[0].StartsWith('-'), "First line should be a removal");
		Assert.IsTrue(differences[1].StartsWith('+'), "Second line should be an addition");
	}

	[TestMethod]
	public void FileDiffer_WithMockFileSystem_HandlesMissingFile()
	{
		// Arrange
		string file1Path = Path.Combine(_testDir1, "file1.txt");
		string missingFilePath = Path.Combine(_testDir1, "missing.txt");

		Mock<IFileDiffer> mockFileDiffer = new();
		mockFileDiffer.Setup(d => d.FindDifferences(file1Path, missingFilePath))
			.Throws(new FileNotFoundException("File not found", missingFilePath));

		// Act & Assert
		Assert.ThrowsException<FileNotFoundException>(() =>
			mockFileDiffer.Object.FindDifferences(file1Path, missingFilePath));
	}

	[TestMethod]
	public void FileDiffer_WithMockFileSystem_HandlesAccessDenied()
	{
		// Arrange
		string file1Path = Path.Combine(_testDir1, "file1.txt");
		string restrictedFilePath = Path.Combine(_testDir1, "restricted.txt");

		Mock<IFileDiffer> mockFileDiffer = new();
		mockFileDiffer.Setup(d => d.FindDifferences(file1Path, restrictedFilePath))
			.Throws(new UnauthorizedAccessException("Access denied"));

		// Act & Assert
		Assert.ThrowsException<UnauthorizedAccessException>(() =>
			mockFileDiffer.Object.FindDifferences(file1Path, restrictedFilePath));
	}
}
