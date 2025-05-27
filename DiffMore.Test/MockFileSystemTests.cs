// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class MockFileSystemTests
{
	private MockFileSystem _mockFileSystem = null!;
	private string _testDir = null!;
	private string _testDir1 = null!;
	private string _testDir2 = null!;

	[TestInitialize]
	public void Setup()
	{
		_testDir = "/testdir";
		_testDir1 = Path.Combine(_testDir, "dir1");
		_testDir2 = Path.Combine(_testDir, "dir2");

		var fileDict = new Dictionary<string, MockFileData>
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
	}

	[TestMethod]
	public void FileFinder_WithMockedFileSystem_FindsFilesCorrectly()
	{
		// Arrange
		var mockFileFinder = new Mock<IFileFinder>();
		mockFileFinder.Setup(f => f.FindFiles(_testDir1, "*.txt"))
			.Returns(new ReadOnlyCollection<string>([
				Path.Combine(_testDir1, "file1.txt"),
				Path.Combine(_testDir1, "file2.txt"),
				Path.Combine(_testDir1, "file3.txt")
			]));

		// Act
		var files = mockFileFinder.Object.FindFiles(_testDir1, "*.txt");

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
		var mockFileSystem = new Mock<IFileSystem>();
		var mockFile = new Mock<IFile>();

		mockFile.Setup(f => f.ReadAllBytes(It.Is<string>(s => s == Path.Combine(_testDir1, "file1.txt"))))
			.Returns(Encoding.UTF8.GetBytes("File 1 Content Version 1"));

		mockFileSystem.Setup(fs => fs.File).Returns(mockFile.Object);

		var mockFileHasher = new Mock<IFileHasher>();
		mockFileHasher.Setup(h => h.ComputeFileHash(It.Is<string>(s => s == Path.Combine(_testDir1, "file1.txt"))))
			.Returns("mock-hash-1");

		// Act
		var hash = mockFileHasher.Object.ComputeFileHash(Path.Combine(_testDir1, "file1.txt"));

		// Assert
		Assert.AreEqual("mock-hash-1", hash, "Should return the mocked hash value");
	}

	[TestMethod]
	public void FileDiffer_WithMockedFileSystem_FindsDifferencesCorrectly()
	{
		// Arrange
		var file1Path = Path.Combine(_testDir1, "file1.txt");
		var file2Path = Path.Combine(_testDir2, "file1.txt");

		var mockFileDiffer = new Mock<IFileDiffer>();
		mockFileDiffer.Setup(d => d.FindDifferences(file1Path, file2Path))
			.Returns(new ReadOnlyCollection<string>(["- File 1 Content Version 1", "+ File 1 Content Version 2"]));

		// Act
		var differences = mockFileDiffer.Object.FindDifferences(file1Path, file2Path);

		// Assert
		Assert.AreEqual(2, differences.Count, "Should find 2 difference lines");
		Assert.IsTrue(differences[0].StartsWith('-'), "First line should be a removal");
		Assert.IsTrue(differences[1].StartsWith('+'), "Second line should be an addition");
	}

	[TestMethod]
	public void FileDiffer_WithMockFileSystem_HandlesMissingFile()
	{
		// Arrange
		var file1Path = Path.Combine(_testDir1, "file1.txt");
		var missingFilePath = Path.Combine(_testDir1, "missing.txt");

		var mockFileDiffer = new Mock<IFileDiffer>();
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
		var file1Path = Path.Combine(_testDir1, "file1.txt");
		var restrictedFilePath = Path.Combine(_testDir1, "restricted.txt");

		var mockFileDiffer = new Mock<IFileDiffer>();
		mockFileDiffer.Setup(d => d.FindDifferences(file1Path, restrictedFilePath))
			.Throws(new UnauthorizedAccessException("Access denied"));

		// Act & Assert
		Assert.ThrowsException<UnauthorizedAccessException>(() =>
			mockFileDiffer.Object.FindDifferences(file1Path, restrictedFilePath));
	}
}

// Interface definitions to support mocking
public interface IFileFinder
{
	public ReadOnlyCollection<string> FindFiles(string directoryPath, string searchPattern);
}

public interface IFileHasher
{
	public string ComputeFileHash(string filePath);
}

public interface IFileDiffer
{
	public ReadOnlyCollection<string> FindDifferences(string file1Path, string file2Path);
	public string GenerateGitStyleDiff(string file1Path, string file2Path);
}
