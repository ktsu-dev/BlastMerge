// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class IterativeMergeOrchestratorTests : MockFileSystemTestBase
{
	protected override void InitializeFileSystem()
	{
		// Base setup will handle FileSystemProvider injection
	}

	[TestMethod]
	public void PrepareFileGroupsForMerging_WithSufficientFiles_ReturnsGroups()
	{
		// Arrange
		CreateFile("file1.txt", "content version 1");
		CreateFile("file2.txt", "content version 2");
		CreateFile("file3.txt", "content version 1"); // Same as file1

		// Act
		IReadOnlyCollection<FileGroup>? result = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(TestDirectory, "*.txt");

		// Assert
		Assert.IsNotNull(result);
		// FileDiffer.GroupFilesByFilenameAndHash groups by filename first, then by hash
		// So we get 3 groups: file1.txt (1 file), file2.txt (1 file), file3.txt (1 file)
		Assert.AreEqual(3, result.Count, "Should have 3 groups (one for each filename)");

		// Verify each file gets its own group since they have different names
		Assert.IsTrue(result.All(g => g.FilePaths.Count == 1), "Each file should be in its own group due to different filenames");
	}

	[TestMethod]
	public void PrepareFileGroupsForMerging_WithInsufficientFiles_ReturnsNull()
	{
		// Arrange - Only one file
		CreateFile("single.txt", "single content");

		// Act
		IReadOnlyCollection<FileGroup>? result = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(TestDirectory, "*.txt");

		// Assert
		Assert.IsNull(result, "Should return null when insufficient files for merging");
	}

	[TestMethod]
	public void PrepareFileGroupsForMerging_WithIdenticalFiles_ReturnsNull()
	{
		// Arrange - Multiple files with same content AND same filename
		CreateFile("subdir1/file.txt", "identical content");
		CreateFile("subdir2/file.txt", "identical content");
		CreateFile("subdir3/file.txt", "identical content");

		// Act
		IReadOnlyCollection<FileGroup>? result = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(TestDirectory, "*/file.txt");

		// Assert
		Assert.IsNull(result, "Should return null when all files with same name have identical content");
	}

	[TestMethod]
	public void PrepareFileGroupsForMerging_WithNonexistentDirectory_ReturnsNull()
	{
		// Arrange
		string nonexistentDir = Path.Combine(TestDirectory, "nonexistent");

		// Act
		IReadOnlyCollection<FileGroup>? result = IterativeMergeOrchestrator.PrepareFileGroupsForMerging(nonexistentDir, "*.txt");

		// Assert
		Assert.IsNull(result, "Should return null for nonexistent directory");
	}

	[TestMethod]
	public void PrepareFileGroupsForMerging_WithNullDirectory_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
		{
			IterativeMergeOrchestrator.PrepareFileGroupsForMerging(null!, "*.txt");
		});
	}

	[TestMethod]
	public void PrepareFileGroupsForMerging_WithNullFileName_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
		{
			IterativeMergeOrchestrator.PrepareFileGroupsForMerging(TestDirectory, null!);
		});
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithSuccessfulMerge_ReturnsCompletedResult()
	{
		// Arrange - Files with same name but different content to allow merging
		string file1 = CreateFile("dir1/test.txt", "line1\nline2");
		string file2 = CreateFile("dir2/test.txt", "line1\nline3");

		FileGroup group1 = new([file1]) { Hash = "hash1" };
		FileGroup group2 = new([file2]) { Hash = "hash2" };
		List<FileGroup> fileGroups = [group1, group2];

		bool mergeCallbackCalled = false;
		bool statusCallbackCalled = false;

		// Mock merge callback that returns a merged result
		MergeResult? MergeCallback(string f1, string f2, string? existing)
		{
			mergeCallbackCalled = true;
			return new MergeResult(["line1", "line2", "line3"], []);
		}

		// Mock status callback
		void StatusCallback(MergeSessionStatus status)
		{
			statusCallbackCalled = true;
			Assert.IsNotNull(status);
		}

		// Mock continuation callback that continues
		bool ContinuationCallback() => true;

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups, MergeCallback, StatusCallback, ContinuationCallback);

		// Assert
		Assert.IsTrue(result.IsSuccessful);
		Assert.IsTrue(mergeCallbackCalled);
		Assert.IsTrue(statusCallbackCalled);
		Assert.AreEqual(1, result.TotalMergeOperations);
		Assert.AreEqual(2, result.InitialFileGroups);
		Assert.AreEqual(2, result.TotalFilesMerged);
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithCancelledMerge_ReturnsFailedResult()
	{
		// Arrange - Files with same name to allow similarity check
		string file1 = CreateFile("dir1/test.txt", "content1");
		string file2 = CreateFile("dir2/test.txt", "content2");

		FileGroup group1 = new([file1]) { Hash = "hash1" };
		FileGroup group2 = new([file2]) { Hash = "hash2" };
		List<FileGroup> fileGroups = [group1, group2];

		// Mock merge callback that returns null (cancelled)
		MergeResult? MergeCallback(string f1, string f2, string? existing) => null;

		// Mock status and continuation callbacks
		void StatusCallback(MergeSessionStatus status)
		{ }
		bool ContinuationCallback() => true;

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups, MergeCallback, StatusCallback, ContinuationCallback);

		// Assert
		Assert.IsFalse(result.IsSuccessful);
		Assert.AreEqual("cancelled", result.OriginalFileName);
		Assert.AreEqual(0, result.TotalMergeOperations);
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithNullFileGroups_ThrowsArgumentNullException()
	{
		// Arrange
		MergeResult? MergeCallback(string f1, string f2, string? existing) => null;
		void StatusCallback(MergeSessionStatus status)
		{ }
		bool ContinuationCallback() => true;

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
		{
			IterativeMergeOrchestrator.StartIterativeMergeProcess(
				null!, MergeCallback, StatusCallback, ContinuationCallback);
		});
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithNullMergeCallback_ThrowsArgumentNullException()
	{
		// Arrange
		List<FileGroup> fileGroups = [];
		void StatusCallback(MergeSessionStatus status)
		{ }
		bool ContinuationCallback() => true;

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
		{
			IterativeMergeOrchestrator.StartIterativeMergeProcess(
				fileGroups, null!, StatusCallback, ContinuationCallback);
		});
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithSingleGroup_ReturnsSuccessImmediately()
	{
		// Arrange
		string file1 = CreateFile("file1.txt", "content");
		FileGroup singleGroup = new([file1]) { Hash = "hash1" };
		List<FileGroup> fileGroups = [singleGroup];

		bool mergeCallbackCalled = false;

		MergeResult? MergeCallback(string f1, string f2, string? existing)
		{
			mergeCallbackCalled = true;
			return new MergeResult(["content"], []);
		}

		void StatusCallback(MergeSessionStatus status)
		{ }
		bool ContinuationCallback() => true;

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups, MergeCallback, StatusCallback, ContinuationCallback);

		// Assert
		Assert.IsTrue(result.IsSuccessful);
		Assert.IsFalse(mergeCallbackCalled, "Merge callback should not be called with single group");
		Assert.AreEqual(0, result.TotalMergeOperations);
		Assert.AreEqual(1, result.InitialFileGroups);
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithNoSimilarFiles_ReturnsSafePreservation()
	{
		// Arrange - Files with completely different names to trigger safety check
		string file1 = CreateFile("completely_different_name_1.txt", "content1");
		string file2 = CreateFile("totally_unrelated_name_2.txt", "content2");

		FileGroup group1 = new([file1]) { Hash = "hash1" };
		FileGroup group2 = new([file2]) { Hash = "hash2" };
		List<FileGroup> fileGroups = [group1, group2];

		bool mergeCallbackCalled = false;

		MergeResult? MergeCallback(string f1, string f2, string? existing)
		{
			mergeCallbackCalled = true;
			return new MergeResult(["merged"], []);
		}

		void StatusCallback(MergeSessionStatus status)
		{ }
		bool ContinuationCallback() => true;

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups, MergeCallback, StatusCallback, ContinuationCallback);

		// Assert
		Assert.IsTrue(result.IsSuccessful, "Should succeed with safe preservation");
		Assert.IsFalse(mergeCallbackCalled, "Should not attempt merge when files are too different");
		// The actual message is "All files preserved safely - no merging needed.\nAll files have different names, so they remain separate as intended."
		Assert.IsTrue(result.OriginalFileName.Contains("All files preserved safely"), "Original filename should indicate safe preservation");
		Assert.AreEqual(0, result.TotalMergeOperations);
	}

	[TestMethod]
	public void StartIterativeMergeProcess_WithMultipleFiles_UpdatesAllFiles()
	{
		// Arrange - Multiple files with same content in each group
		string file1a = CreateFile("dir1/test.txt", "version 1");
		string file1b = CreateFile("dir2/test.txt", "version 1");
		string file2a = CreateFile("dir3/test.txt", "version 2");

		FileGroup group1 = new([file1a, file1b]) { Hash = "hash1" };
		FileGroup group2 = new([file2a]) { Hash = "hash2" };
		List<FileGroup> fileGroups = [group1, group2];

		MergeResult? MergeCallback(string f1, string f2, string? existing) =>
			new(["merged version"], []);

		void StatusCallback(MergeSessionStatus status)
		{ }
		bool ContinuationCallback() => true;

		// Act
		MergeCompletionResult result = IterativeMergeOrchestrator.StartIterativeMergeProcess(
			fileGroups, MergeCallback, StatusCallback, ContinuationCallback);

		// Assert
		Assert.IsTrue(result.IsSuccessful);

		// Verify all files were updated with merged content
		Assert.AreEqual("merged version", MockFileSystem.File.ReadAllText(file1a));
		Assert.AreEqual("merged version", MockFileSystem.File.ReadAllText(file1b));
		Assert.AreEqual("merged version", MockFileSystem.File.ReadAllText(file2a));
	}
}
