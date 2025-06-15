// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.IO.Abstractions.TestingHelpers;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for SecureTempFileHelper
/// </summary>
[TestClass]
public class SecureTempFileHelperTests : MockFileSystemTestBase
{
	protected override void InitializeFileSystem()
	{
		// Set up a mock temp directory
		string tempPath = @"C:\temp";
		MockFileSystem.Directory.CreateDirectory(tempPath);

		// Mock Path.GetTempPath() behavior by ensuring temp directory exists
		MockFileSystem.Directory.CreateDirectory(tempPath);
	}

	[TestMethod]
	public void CreateTempFile_WithDefaultExtension_CreatesFileWithTxtExtension()
	{
		// Act
		string tempFile = SecureTempFileHelper.CreateTempFile();

		// Assert
		Assert.IsTrue(MockFileSystem.File.Exists(tempFile));
		Assert.IsTrue(tempFile.StartsWith(@"C:\temp"));
	}

	[TestMethod]
	public void CreateTempFile_WithCustomExtension_CreatesFileWithSpecifiedExtension()
	{
		// Arrange
		string extension = ".log";

		// Act
		string tempFile = SecureTempFileHelper.CreateTempFile(extension);

		// Assert
		Assert.IsTrue(MockFileSystem.File.Exists(tempFile));
		Assert.IsTrue(tempFile.EndsWith(extension));
		Assert.IsTrue(tempFile.StartsWith(@"C:\temp"));
	}

	[TestMethod]
	public void CreateTempFile_CalledMultipleTimes_CreatesUniqueFiles()
	{
		// Act
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

		// Assert
		Assert.AreNotEqual(tempFile1, tempFile2);
		Assert.IsTrue(MockFileSystem.File.Exists(tempFile1));
		Assert.IsTrue(MockFileSystem.File.Exists(tempFile2));

		// Cleanup
		SecureTempFileHelper.SafeDeleteTempFiles(MockFileSystem, tempFile1, tempFile2);
	}

	[TestMethod]
	public void CreateTempDirectory_CreatesUniqueDirectory()
	{
		// Act
		string tempDir = SecureTempFileHelper.CreateTempDirectory();

		// Assert
		Assert.IsTrue(MockFileSystem.Directory.Exists(tempDir));
		Assert.IsTrue(tempDir.StartsWith(@"C:\temp"));
	}

	[TestMethod]
	public void CreateTempDirectory_CalledMultipleTimes_CreatesUniqueDirectories()
	{
		// Act
		string tempDir1 = SecureTempFileHelper.CreateTempDirectory();
		string tempDir2 = SecureTempFileHelper.CreateTempDirectory();

		// Assert
		Assert.AreNotEqual(tempDir1, tempDir2);
		Assert.IsTrue(MockFileSystem.Directory.Exists(tempDir1));
		Assert.IsTrue(MockFileSystem.Directory.Exists(tempDir2));
	}

	[TestMethod]
	public void SafeDeleteTempFiles_WithExistingFile_DeletesFile()
	{
		// Arrange
		string tempFile = SecureTempFileHelper.CreateTempFile();
		Assert.IsTrue(MockFileSystem.File.Exists(tempFile));

		// Act
		SecureTempFileHelper.SafeDeleteTempFiles(MockFileSystem, tempFile);

		// Assert
		Assert.IsFalse(MockFileSystem.File.Exists(tempFile));
	}

	[TestMethod]
	public void SafeDeleteTempFiles_WithNonExistentFile_DoesNotThrow()
	{
		// Arrange
		string nonExistentFile = @"C:\temp\nonexistent.txt";

		// Act & Assert (should not throw)
		SecureTempFileHelper.SafeDeleteTempFiles(MockFileSystem, nonExistentFile);
	}

	[TestMethod]
	public void SafeDeleteTempFiles_WithMultipleFiles_DeletesAllFiles()
	{
		// Arrange
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();
		Assert.IsTrue(MockFileSystem.File.Exists(tempFile1));
		Assert.IsTrue(MockFileSystem.File.Exists(tempFile2));

		// Act
		SecureTempFileHelper.SafeDeleteTempFiles(MockFileSystem, tempFile1, tempFile2);

		// Assert
		Assert.IsFalse(MockFileSystem.File.Exists(tempFile1));
		Assert.IsFalse(MockFileSystem.File.Exists(tempFile2));
	}

	[TestMethod]
	public void SafeDeleteTempFiles_WithMixedExistentAndNonExistentFiles_DeletesExistingFiles()
	{
		// Arrange
		string tempFile = SecureTempFileHelper.CreateTempFile();
		string nonExistentFile = @"C:\temp\nonexistent.txt";

		// Act
		SecureTempFileHelper.SafeDeleteTempFiles(MockFileSystem, tempFile, nonExistentFile, null, string.Empty);

		// Assert
		Assert.IsFalse(MockFileSystem.File.Exists(tempFile));
	}

	[TestMethod]
	public void SafeDeleteTempDirectory_WithExistingDirectory_DeletesDirectory()
	{
		// Arrange
		string tempDir = SecureTempFileHelper.CreateTempDirectory();
		Assert.IsTrue(MockFileSystem.Directory.Exists(tempDir));

		// Act
		SecureTempFileHelper.SafeDeleteTempDirectory(tempDir, MockFileSystem);

		// Assert
		Assert.IsFalse(MockFileSystem.Directory.Exists(tempDir));
	}

	[TestMethod]
	public void SafeDeleteTempDirectory_WithDirectoryContainingFiles_DeletesDirectoryAndContents()
	{
		// Arrange
		string tempDir = SecureTempFileHelper.CreateTempDirectory();
		string fileInside = MockFileSystem.Path.Combine(tempDir, "test.txt");
		MockFileSystem.File.WriteAllText(fileInside, "test content");

		string subDir = MockFileSystem.Path.Combine(tempDir, "subdir");
		MockFileSystem.Directory.CreateDirectory(subDir);
		string fileInSubDir = MockFileSystem.Path.Combine(subDir, "subtest.txt");
		MockFileSystem.File.WriteAllText(fileInSubDir, "subtest content");

		Assert.IsTrue(MockFileSystem.Directory.Exists(tempDir));
		Assert.IsTrue(MockFileSystem.File.Exists(fileInside));
		Assert.IsTrue(MockFileSystem.Directory.Exists(subDir));
		Assert.IsTrue(MockFileSystem.File.Exists(fileInSubDir));

		// Act
		SecureTempFileHelper.SafeDeleteTempDirectory(tempDir, MockFileSystem);

		// Assert
		Assert.IsFalse(MockFileSystem.Directory.Exists(tempDir));
	}

	[TestMethod]
	public void SafeDeleteTempDirectory_WithMultipleDirectories_DeletesAllDirectories()
	{
		// Arrange
		string tempDir1 = SecureTempFileHelper.CreateTempDirectory();
		string tempDir2 = SecureTempFileHelper.CreateTempDirectory();

		// Act
		SecureTempFileHelper.SafeDeleteTempDirectory(tempDir1, MockFileSystem);
		SecureTempFileHelper.SafeDeleteTempDirectory(tempDir2, MockFileSystem);

		// Assert
		Assert.IsFalse(MockFileSystem.Directory.Exists(tempDir1));
		Assert.IsFalse(MockFileSystem.Directory.Exists(tempDir2));
	}

	[TestMethod]
	public void SafeDeleteTempDirectory_WithNonExistentDirectory_DoesNotThrow()
	{
		// Arrange
		string nonExistentDir = @"C:\temp\nonexistent";

		// Act & Assert (should not throw)
		SecureTempFileHelper.SafeDeleteTempDirectory(nonExistentDir, MockFileSystem);
	}

	[TestMethod]
	public void SafeDeleteTempFiles_WithNullFileSystemAndNullFilePaths_DoesNotThrow()
	{
		// Act & Assert (should not throw)
		SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, filePaths: null!);
	}

	[TestMethod]
	public void CreateTempFile_WithFileSystemParameter_UsesProvidedFileSystem()
	{
		// Arrange
		string extension = ".test";

		// Act
		string tempFile = SecureTempFileHelper.CreateTempFile(extension, MockFileSystem);

		// Assert
		Assert.IsTrue(tempFile.EndsWith(extension), $"File should end with {extension}");
		Assert.IsTrue(MockFileSystem.File.Exists(tempFile));
	}
}
