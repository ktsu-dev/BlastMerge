// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for ApplicationService base class functionality
/// </summary>
[TestClass]
public class ApplicationServiceTests : MockFileSystemTestBase
{
	private TestApplicationService _applicationService = null!;
	private string _testDirectory = null!;

	/// <summary>
	/// Test implementation of ApplicationService for testing base functionality
	/// </summary>
	private sealed class TestApplicationService(IFileSystem? fileSystem) : ApplicationService(fileSystem)
	{
		public override void ProcessFiles(string directory, string fileName)
		{
			// Test implementation - just validate parameters
			ValidateDirectoryAndFileName(directory, fileName);
		}

		public override void ProcessBatch(string directory, string batchName)
		{
			// Test implementation - just validate directory
			ValidateDirectoryExists(directory);
		}

		public override void RunIterativeMerge(string directory, string fileName)
		{
			// Test implementation - just validate parameters
			ValidateDirectoryAndFileName(directory, fileName);
		}

		public override void ListBatches()
		{
			// Test implementation - no-op
		}

		public override void StartInteractiveMode()
		{
			// Test implementation - no-op
		}

		// Expose protected methods for testing
		public void TestValidateDirectoryAndFileName(string directory, string fileName)
		{
			ValidateDirectoryAndFileName(directory, fileName);
		}

		public void TestValidateDirectoryExists(string directory)
		{
			ValidateDirectoryExists(directory);
		}
	}

	protected override void InitializeFileSystem()
	{
		_testDirectory = CreateDirectory("test_dir");
		CreateFile("test_dir/file1.txt", "Content 1");
		CreateFile("test_dir/file2.txt", "Content 2");
		CreateFile("test_dir/file3.txt", "Content 1"); // Same content as file1
	}

	[TestInitialize]
	public void Setup()
	{
		_applicationService = new TestApplicationService(MockFileSystem);
	}

	#region ValidateDirectoryAndFileName Tests

	/// <summary>
	/// Tests that ValidateDirectoryAndFileName succeeds with valid parameters.
	/// </summary>
	[TestMethod]
	public void ValidateDirectoryAndFileName_WithValidParameters_DoesNotThrow()
	{
		// Act & Assert - should not throw
		_applicationService.TestValidateDirectoryAndFileName(_testDirectory, "*.txt");
	}

	/// <summary>
	/// Tests that ValidateDirectoryAndFileName throws when directory is null.
	/// </summary>
	[TestMethod]
	public void ValidateDirectoryAndFileName_WithNullDirectory_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_applicationService.TestValidateDirectoryAndFileName(null!, "*.txt"));
	}

	/// <summary>
	/// Tests that ValidateDirectoryAndFileName throws when fileName is null.
	/// </summary>
	[TestMethod]
	public void ValidateDirectoryAndFileName_WithNullFileName_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_applicationService.TestValidateDirectoryAndFileName(_testDirectory, null!));
	}

	/// <summary>
	/// Tests that ValidateDirectoryAndFileName throws when directory doesn't exist.
	/// </summary>
	[TestMethod]
	public void ValidateDirectoryAndFileName_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
	{
		// Act & Assert
		Assert.ThrowsException<DirectoryNotFoundException>(() =>
			_applicationService.TestValidateDirectoryAndFileName("non_existent_dir", "*.txt"));
	}

	#endregion

	#region ValidateDirectoryExists Tests

	/// <summary>
	/// Tests that ValidateDirectoryExists succeeds with existing directory.
	/// </summary>
	[TestMethod]
	public void ValidateDirectoryExists_WithExistingDirectory_DoesNotThrow()
	{
		// Act & Assert - should not throw
		_applicationService.TestValidateDirectoryExists(_testDirectory);
	}

	/// <summary>
	/// Tests that ValidateDirectoryExists throws when directory doesn't exist.
	/// </summary>
	[TestMethod]
	public void ValidateDirectoryExists_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
	{
		// Act & Assert
		Assert.ThrowsException<DirectoryNotFoundException>(() =>
			_applicationService.TestValidateDirectoryExists("non_existent_dir"));
	}

	#endregion

	#region CompareFiles Tests

	/// <summary>
	/// Tests that CompareFiles returns correct file groups.
	/// </summary>
	[TestMethod]
	public void CompareFiles_WithValidParameters_ReturnsFileGroups()
	{
		// Act
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> result =
			_applicationService.CompareFiles(_testDirectory, "*.txt");

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.Count > 0);

		// Each file should be in its own group since they have different filenames
		// (GroupFilesByFilenameAndHash groups by filename first, then by content)
		Assert.AreEqual(3, result.Count, "Should have 3 groups for 3 files with different names");

		// Each group should contain exactly one file
		foreach (IReadOnlyCollection<string> group in result.Values)
		{
			Assert.AreEqual(1, group.Count, "Each group should contain exactly one file since all files have different names");
		}
	}

	/// <summary>
	/// Tests that CompareFiles throws with null directory.
	/// </summary>
	[TestMethod]
	public void CompareFiles_WithNullDirectory_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_applicationService.CompareFiles(null!, "*.txt"));
	}

	/// <summary>
	/// Tests that CompareFiles throws with null fileName.
	/// </summary>
	[TestMethod]
	public void CompareFiles_WithNullFileName_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_applicationService.CompareFiles(_testDirectory, null!));
	}

	/// <summary>
	/// Tests that CompareFiles throws with non-existent directory.
	/// </summary>
	[TestMethod]
	public void CompareFiles_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
	{
		// Act & Assert
		Assert.ThrowsException<DirectoryNotFoundException>(() =>
			_applicationService.CompareFiles("non_existent_dir", "*.txt"));
	}

	/// <summary>
	/// Tests that CompareFiles returns empty result when no files match pattern.
	/// </summary>
	[TestMethod]
	public void CompareFiles_WithNoMatchingFiles_ReturnsEmptyResult()
	{
		// Act
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> result =
			_applicationService.CompareFiles(_testDirectory, "*.nonexistent");

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.Count);
	}

	/// <summary>
	/// Tests that CompareFiles groups identical files correctly.
	/// </summary>
	[TestMethod]
	public void CompareFiles_WithIdenticalFiles_GroupsCorrectly()
	{
		// Arrange - Create additional files with same name and content
		CreateFile("test_dir/subdir1/file1.txt", "Content 1"); // Same name and content as original file1.txt
		CreateFile("test_dir/subdir2/file1.txt", "Content 1"); // Same name and content as original file1.txt

		// Act
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> result =
			_applicationService.CompareFiles(_testDirectory, "*.txt");

		// Assert
		Assert.IsNotNull(result);

		// Find the group with multiple files (should contain all file1.txt files with same content)
		IReadOnlyCollection<string>? multiFileGroup = result.Values.FirstOrDefault(group => group.Count > 1);
		Assert.IsNotNull(multiFileGroup, "Should have a group with multiple files with same name and content");
		Assert.AreEqual(3, multiFileGroup.Count, "Should have 3 file1.txt files with same content");

		// Verify all files in the group have the same filename
		List<string> fileNames = [.. multiFileGroup.Select(Path.GetFileName)];
		Assert.IsTrue(fileNames.All(name => name == "file1.txt"), "All files in the group should have the same name");
	}

	/// <summary>
	/// Tests that CompareFiles returns read-only dictionary.
	/// </summary>
	[TestMethod]
	public void CompareFiles_ReturnsReadOnlyDictionary()
	{
		// Act
		IReadOnlyDictionary<string, IReadOnlyCollection<string>> result =
			_applicationService.CompareFiles(_testDirectory, "*.txt");

		// Assert
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<IReadOnlyDictionary<string, IReadOnlyCollection<string>>>(result);

		// Verify collections within are also read-only
		foreach (IReadOnlyCollection<string> group in result.Values)
		{
			Assert.IsInstanceOfType<IReadOnlyCollection<string>>(group);
		}
	}

	#endregion

	#region Abstract Method Tests

	/// <summary>
	/// Tests that ProcessFiles calls validation.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_WithValidParameters_CallsValidation()
	{
		// Act & Assert - should not throw (validation passes)
		_applicationService.ProcessFiles(_testDirectory, "*.txt");
	}

	/// <summary>
	/// Tests that ProcessFiles throws with invalid parameters.
	/// </summary>
	[TestMethod]
	public void ProcessFiles_WithInvalidParameters_ThrowsException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_applicationService.ProcessFiles(null!, "*.txt"));
	}

	/// <summary>
	/// Tests that ProcessBatch calls validation.
	/// </summary>
	[TestMethod]
	public void ProcessBatch_WithValidParameters_CallsValidation()
	{
		// Act & Assert - should not throw (validation passes)
		_applicationService.ProcessBatch(_testDirectory, "TestBatch");
	}

	/// <summary>
	/// Tests that ProcessBatch throws with invalid directory.
	/// </summary>
	[TestMethod]
	public void ProcessBatch_WithInvalidDirectory_ThrowsException()
	{
		// Act & Assert
		Assert.ThrowsException<DirectoryNotFoundException>(() =>
			_applicationService.ProcessBatch("non_existent_dir", "TestBatch"));
	}

	/// <summary>
	/// Tests that RunIterativeMerge calls validation.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_WithValidParameters_CallsValidation()
	{
		// Act & Assert - should not throw (validation passes)
		_applicationService.RunIterativeMerge(_testDirectory, "*.txt");
	}

	/// <summary>
	/// Tests that RunIterativeMerge throws with invalid parameters.
	/// </summary>
	[TestMethod]
	public void RunIterativeMerge_WithInvalidParameters_ThrowsException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			_applicationService.RunIterativeMerge(_testDirectory, null!));
	}

	#endregion
}
