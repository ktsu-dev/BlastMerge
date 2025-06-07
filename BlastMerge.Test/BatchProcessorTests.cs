// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.Core.Models;
using ktsu.BlastMerge.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BatchProcessorTests : MockFileSystemTestBase
{
	[TestMethod]
	public void CreateCustomBatch_WithValidParameters_ReturnsCorrectBatch()
	{
		// Arrange
		string name = "Test Batch";
		string[] patterns = ["*.txt", "*.cs"];
		string description = "Test Description";

		// Act
		BatchConfiguration result = BatchProcessor.CreateCustomBatch(name, patterns, description);

		// Assert
		Assert.AreEqual(name, result.Name);
		Assert.AreEqual(description, result.Description);
		CollectionAssert.AreEqual(patterns, result.FilePatterns.ToArray());
		Assert.IsFalse(result.PromptBeforeEachPattern);
		Assert.IsFalse(result.SkipEmptyPatterns);
	}

	[TestMethod]
	public void CreateCustomBatch_WithEmptyDescription_ReturnsCorrectBatch()
	{
		// Arrange
		string name = "Test Batch";
		string[] patterns = ["*.txt"];

		// Act
		BatchConfiguration result = BatchProcessor.CreateCustomBatch(name, patterns);

		// Assert
		Assert.AreEqual(name, result.Name);
		Assert.AreEqual(string.Empty, result.Description);
		CollectionAssert.AreEqual(patterns, result.FilePatterns.ToArray());
	}

	[TestMethod]
	public void ProcessBatch_WithNonExistentDirectory_ReturnsFailure()
	{
		// Arrange
		BatchConfiguration batch = new()
		{
			Name = "Test",
			FilePatterns = ["*.txt"]
		};
		string nonExistentDir = "/path/that/does/not/exist";

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			nonExistentDir,
			(a, b, c) => null,
			_ => { },
			() => true);

		// Assert
		Assert.IsFalse(result.Success);
		Assert.IsTrue(result.Summary.Contains("Directory does not exist"));
	}

	[TestMethod]
	public void ProcessBatch_WithNullParameters_ThrowsArgumentNullException()
	{
		// Arrange
		BatchConfiguration batch = new()
		{ Name = "Test", FilePatterns = ["*.txt"] };
		string directory = "/test";

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(null!, directory, (a, b, c) => null, _ => { }, () => true));

		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(batch, null!, (a, b, c) => null, _ => { }, () => true));

		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(batch, directory, null!, _ => { }, () => true));

		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(batch, directory, (a, b, c) => null, null!, () => true));

		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatch(batch, directory, (a, b, c) => null, _ => { }, null!));
	}

	[TestMethod]
	public void ProcessBatch_WithValidDirectory_ProcessesPatterns()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"]
		};

		List<(string, string, string?)> mergeResults = [];
		List<MergeSessionStatus> statusUpdates = [];

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(path1, path2, output) =>
			{
				mergeResults.Add((path1, path2, output));
				return new MergeResult(["merged content"], []);
			},
			statusUpdates.Add,
			() => true);

		// Assert
		Assert.IsTrue(result.Success);
		Assert.AreEqual("Test Batch", result.BatchName);
		Assert.AreEqual(1, result.TotalPatternsProcessed);
		Assert.AreEqual(1, result.SuccessfulPatterns);
		Assert.IsTrue(result.PatternResults.Count != 0);
	}

	[TestMethod]
	public void ProcessBatch_WithNoMatchingFiles_HandlesEmptyPattern()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file.cs"), new("content"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"], // No .txt files exist
			SkipEmptyPatterns = false
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true);

		// Assert
		Assert.IsFalse(result.Success); // Should fail when SkipEmptyPatterns is false
		Assert.AreEqual(1, result.TotalPatternsProcessed);
		Assert.AreEqual(0, result.SuccessfulPatterns);
	}

	[TestMethod]
	public void ProcessBatch_WithSkipEmptyPatterns_SkipsEmptyPatterns()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file.cs"), new("content"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"], // No .txt files exist
			SkipEmptyPatterns = true
		};

		// Act
		BatchResult result = BatchProcessor.ProcessBatch(
			batch,
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true);

		// Assert
		Assert.IsTrue(result.Success); // Should succeed when SkipEmptyPatterns is true
		Assert.AreEqual(1, result.TotalPatternsProcessed);
		Assert.AreEqual(1, result.SuccessfulPatterns);
		Assert.AreEqual("No files found (skipped)", result.PatternResults.First().Message);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithMultipleFiles_ProcessesMerge()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		bool mergeCallbackCalled = false;

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(path1, path2, output) =>
			{
				mergeCallbackCalled = true;
				return new MergeResult(["merged content"], []);
			},
			_ => { },
			() => true);

		// Assert
		Assert.IsTrue(result.Success);
		Assert.AreEqual("*.txt", result.Pattern);
		Assert.IsTrue(mergeCallbackCalled);
		Assert.AreEqual(2, result.FilesFound);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithSingleFile_ReturnsOnlyOneFileMessage()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true);

		// Assert
		Assert.IsTrue(result.Success);
		Assert.IsTrue(result.Message.Contains("Only one file found"));
		Assert.AreEqual(1, result.FilesFound);
	}

	[TestMethod]
	public void ProcessSinglePattern_WithNoFiles_ReturnsNoFilesMessage()
	{
		// Arrange
		string testDir = CreateTestDirectory();

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePattern(
			"*.txt",
			testDir,
			(a, b, c) => null,
			_ => { },
			() => true);

		// Assert
		Assert.IsFalse(result.Success);
		Assert.IsTrue(result.Message.Contains("No files found"));
		Assert.AreEqual(0, result.FilesFound);
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithValidData_ProcessesSuccessfully()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		MockFileSystem.AddFile(Path.Combine(testDir, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(testDir, "file2.txt"), new("content2"));

		BatchConfiguration batch = new()
		{
			Name = "Test Batch",
			FilePatterns = ["*.txt"]
		};

		List<string> progressUpdates = [];
		bool mergeCallbackCalled = false;

		// Act
		BatchResult result = BatchProcessor.ProcessBatchWithDiscretePhases(
			batch,
			testDir,
			(path1, path2, output) =>
			{
				mergeCallbackCalled = true;
				return new MergeResult(["merged content"], []);
			},
			_ => { },
			() => true,
			progressUpdates.Add);

		// Assert
		Assert.IsTrue(result.Success);
		Assert.IsTrue(mergeCallbackCalled);
		Assert.IsTrue(progressUpdates.Any(msg => msg.Contains("Gathering")));
	}

	[TestMethod]
	public void ProcessBatchWithDiscretePhases_WithNullParameters_ThrowsArgumentNullException()
	{
		// Arrange
		BatchConfiguration batch = new()
		{ Name = "Test", FilePatterns = ["*.txt"] };
		string directory = "/test";

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(null!, directory, (a, b, c) => null, _ => { }, () => true));

		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(batch, null!, (a, b, c) => null, _ => { }, () => true));

		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(batch, directory, null!, _ => { }, () => true));

		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(batch, directory, (a, b, c) => null, null!, () => true));

		Assert.ThrowsException<ArgumentNullException>(() =>
			BatchProcessor.ProcessBatchWithDiscretePhases(batch, directory, (a, b, c) => null, _ => { }, null!));
	}

	[TestMethod]
	public void ProcessSinglePatternWithPaths_WithSearchPaths_FindsFilesInSpecifiedPaths()
	{
		// Arrange
		string testDir = CreateTestDirectory();
		string subDir1 = Path.Combine(testDir, "sub1");
		string subDir2 = Path.Combine(testDir, "sub2");

		MockFileSystem.AddDirectory(subDir1);
		MockFileSystem.AddDirectory(subDir2);
		MockFileSystem.AddFile(Path.Combine(subDir1, "file1.txt"), new("content1"));
		MockFileSystem.AddFile(Path.Combine(subDir2, "file2.txt"), new("content2"));

		string[] searchPaths = [subDir1, subDir2];

		// Act
		PatternResult result = BatchProcessor.ProcessSinglePatternWithPaths(
			"*.txt",
			searchPaths,
			testDir,
			[],
			(a, b, c) => new MergeResult(["merged content"], []),
			_ => { },
			() => true);

		// Assert
		Assert.IsTrue(result.Success);
		Assert.AreEqual(2, result.FilesFound);
	}

	private string CreateTestDirectory()
	{
		string testDir = "/test_" + Guid.NewGuid().ToString("N")[..8];
		MockFileSystem.AddDirectory(testDir);
		return testDir;
	}
}
