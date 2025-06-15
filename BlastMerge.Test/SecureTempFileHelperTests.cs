// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.IO;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the SecureTempFileHelper utility class
/// </summary>
[TestClass]
public class SecureTempFileHelperTests
{
	[TestMethod]
	public void CreateTempFile_CreatesUniqueFile()
	{
		// Act
		string tempFile = SecureTempFileHelper.CreateTempFile();

		try
		{
			// Assert
			Assert.IsNotNull(tempFile);
			Assert.IsTrue(File.Exists(tempFile));
			Assert.IsTrue(tempFile.StartsWith(Path.GetTempPath()));
		}
		finally
		{
			SecureTempFileHelper.SafeDeleteTempFile(tempFile);
		}
	}

	[TestMethod]
	public void CreateTempFile_WithExtension_CreatesFileWithCorrectExtension()
	{
		// Arrange
		string extension = ".txt";

		// Act
		string tempFile = SecureTempFileHelper.CreateTempFile(extension);

		try
		{
			// Assert
			Assert.IsNotNull(tempFile);
			Assert.IsTrue(File.Exists(tempFile));
			Assert.IsTrue(tempFile.EndsWith(extension));
			Assert.IsTrue(tempFile.StartsWith(Path.GetTempPath()));
		}
		finally
		{
			SecureTempFileHelper.SafeDeleteTempFile(tempFile);
		}
	}

	[TestMethod]
	public void CreateTempFile_MultipleCallsCreateUniqueFiles()
	{
		// Act
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();

		try
		{
			// Assert
			Assert.AreNotEqual(tempFile1, tempFile2);
			Assert.IsTrue(File.Exists(tempFile1));
			Assert.IsTrue(File.Exists(tempFile2));
		}
		finally
		{
			SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, tempFile1, tempFile2);
		}
	}

	[TestMethod]
	public void CreateTempDirectory_CreatesUniqueDirectory()
	{
		// Act
		string tempDir = SecureTempFileHelper.CreateTempDirectory();

		try
		{
			// Assert
			Assert.IsNotNull(tempDir);
			Assert.IsTrue(Directory.Exists(tempDir));
			Assert.IsTrue(tempDir.StartsWith(Path.GetTempPath()));
		}
		finally
		{
			SecureTempFileHelper.SafeDeleteTempDirectory(tempDir);
		}
	}

	[TestMethod]
	public void CreateTempDirectory_MultipleCallsCreateUniqueDirectories()
	{
		// Act
		string tempDir1 = SecureTempFileHelper.CreateTempDirectory();
		string tempDir2 = SecureTempFileHelper.CreateTempDirectory();

		try
		{
			// Assert
			Assert.AreNotEqual(tempDir1, tempDir2);
			Assert.IsTrue(Directory.Exists(tempDir1));
			Assert.IsTrue(Directory.Exists(tempDir2));
		}
		finally
		{
			SecureTempFileHelper.SafeDeleteTempDirectory(tempDir1);
			SecureTempFileHelper.SafeDeleteTempDirectory(tempDir2);
		}
	}

	[TestMethod]
	public void SafeDeleteTempFile_DeletesExistingFile()
	{
		// Arrange
		string tempFile = SecureTempFileHelper.CreateTempFile();
		Assert.IsTrue(File.Exists(tempFile));

		// Act
		SecureTempFileHelper.SafeDeleteTempFile(tempFile);

		// Assert
		Assert.IsFalse(File.Exists(tempFile));
	}

	[TestMethod]
	public void SafeDeleteTempFile_WithNullPath_DoesNotThrow()
	{
		// Act & Assert - Should not throw
		SecureTempFileHelper.SafeDeleteTempFile(null);
		SecureTempFileHelper.SafeDeleteTempFile(string.Empty);
	}

	[TestMethod]
	public void SafeDeleteTempFile_WithNonExistentFile_DoesNotThrow()
	{
		// Arrange
		string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		// Act & Assert - Should not throw
		SecureTempFileHelper.SafeDeleteTempFile(nonExistentFile);
	}

	[TestMethod]
	public void SafeDeleteTempFiles_DeletesMultipleFiles()
	{
		// Arrange
		string tempFile1 = SecureTempFileHelper.CreateTempFile();
		string tempFile2 = SecureTempFileHelper.CreateTempFile();
		Assert.IsTrue(File.Exists(tempFile1));
		Assert.IsTrue(File.Exists(tempFile2));

		// Act
		SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, tempFile1, tempFile2);

		// Assert
		Assert.IsFalse(File.Exists(tempFile1));
		Assert.IsFalse(File.Exists(tempFile2));
	}

	[TestMethod]
	public void SafeDeleteTempFiles_WithMixedValidAndInvalidPaths_DoesNotThrow()
	{
		// Arrange
		string tempFile = SecureTempFileHelper.CreateTempFile();
		string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		// Act & Assert - Should not throw
		SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, tempFile, nonExistentFile, null, string.Empty);

		// Verify the valid file was deleted
		Assert.IsFalse(File.Exists(tempFile));
	}

	[TestMethod]
	public void SafeDeleteTempDirectory_DeletesExistingDirectory()
	{
		// Arrange
		string tempDir = SecureTempFileHelper.CreateTempDirectory();
		Assert.IsTrue(Directory.Exists(tempDir));

		// Act
		SecureTempFileHelper.SafeDeleteTempDirectory(tempDir);

		// Assert
		Assert.IsFalse(Directory.Exists(tempDir));
	}

	[TestMethod]
	public void SafeDeleteTempDirectory_WithFilesInside_DeletesRecursively()
	{
		// Arrange
		string tempDir = SecureTempFileHelper.CreateTempDirectory();
		string fileInside = Path.Combine(tempDir, "test.txt");
		File.WriteAllText(fileInside, "test content");

		string subDir = Path.Combine(tempDir, "subdir");
		Directory.CreateDirectory(subDir);
		string fileInSubDir = Path.Combine(subDir, "subtest.txt");
		File.WriteAllText(fileInSubDir, "subtest content");

		Assert.IsTrue(Directory.Exists(tempDir));
		Assert.IsTrue(File.Exists(fileInside));
		Assert.IsTrue(Directory.Exists(subDir));
		Assert.IsTrue(File.Exists(fileInSubDir));

		// Act
		SecureTempFileHelper.SafeDeleteTempDirectory(tempDir);

		// Assert
		Assert.IsFalse(Directory.Exists(tempDir));
	}

	[TestMethod]
	public void SafeDeleteTempDirectory_WithNullPath_DoesNotThrow()
	{
		// Act & Assert - Should not throw
		SecureTempFileHelper.SafeDeleteTempDirectory(null);
		SecureTempFileHelper.SafeDeleteTempDirectory(string.Empty);
	}

	[TestMethod]
	public void SafeDeleteTempDirectory_WithNonExistentDirectory_DoesNotThrow()
	{
		// Arrange
		string nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

		// Act & Assert - Should not throw
		SecureTempFileHelper.SafeDeleteTempDirectory(nonExistentDir);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void CreateTempFile_WithNullExtension_ThrowsArgumentNullException()
	{
		// Act
		SecureTempFileHelper.CreateTempFile((string)null!);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void SafeDeleteTempFiles_WithNullArray_ThrowsArgumentNullException()
	{
		// Act
		SecureTempFileHelper.SafeDeleteTempFiles(fileSystem: null, filePaths: null!);
	}

	[TestMethod]
	public void CreateTempFile_WithCustomExtension_CreatesFileWithCorrectExtension()
	{
		// Arrange
		string[] extensions = [".cs", ".json", ".xml", ".config"];

		foreach (string extension in extensions)
		{
			// Act
			string tempFile = SecureTempFileHelper.CreateTempFile(extension);

			try
			{
				// Assert
				Assert.IsTrue(tempFile.EndsWith(extension), $"File should end with {extension}");
				Assert.IsTrue(File.Exists(tempFile));
			}
			finally
			{
				SecureTempFileHelper.SafeDeleteTempFile(tempFile);
			}
		}
	}
}
