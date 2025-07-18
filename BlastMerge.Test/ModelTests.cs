// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ModelTests
{
	#region DirectoryComparisonResult Tests

	[TestMethod]
	public void DirectoryComparisonResult_DefaultValues_AreEmpty()
	{
		// Act
		DirectoryComparisonResult result = new();

		// Assert
		Assert.IsNotNull(result.SameFiles);
		Assert.AreEqual(0, result.SameFiles.Count);
		Assert.IsNotNull(result.ModifiedFiles);
		Assert.AreEqual(0, result.ModifiedFiles.Count);
		Assert.IsNotNull(result.OnlyInDir1);
		Assert.AreEqual(0, result.OnlyInDir1.Count);
		Assert.IsNotNull(result.OnlyInDir2);
		Assert.AreEqual(0, result.OnlyInDir2.Count);
	}

	[TestMethod]
	public void DirectoryComparisonResult_WithValues_StoresCorrectly()
	{
		// Arrange
		List<string> sameFiles = ["same1.txt", "same2.txt"];
		List<string> modifiedFiles = ["modified.txt"];
		List<string> onlyInDir1 = ["only1.txt"];
		List<string> onlyInDir2 = ["only2.txt"];

		// Act
		DirectoryComparisonResult result = new()
		{
			SameFiles = sameFiles.AsReadOnly(),
			ModifiedFiles = modifiedFiles.AsReadOnly(),
			OnlyInDir1 = onlyInDir1.AsReadOnly(),
			OnlyInDir2 = onlyInDir2.AsReadOnly()
		};

		// Assert
		Assert.AreEqual(2, result.SameFiles.Count);
		Assert.IsTrue(result.SameFiles.Contains("same1.txt"));
		Assert.IsTrue(result.SameFiles.Contains("same2.txt"));
		Assert.AreEqual(1, result.ModifiedFiles.Count);
		Assert.IsTrue(result.ModifiedFiles.Contains("modified.txt"));
		Assert.AreEqual(1, result.OnlyInDir1.Count);
		Assert.IsTrue(result.OnlyInDir1.Contains("only1.txt"));
		Assert.AreEqual(1, result.OnlyInDir2.Count);
		Assert.IsTrue(result.OnlyInDir2.Contains("only2.txt"));
	}

	#endregion

	#region DiffStatistics Tests

	[TestMethod]
	public void DiffStatistics_TotalChanges_CalculatesCorrectly()
	{
		// Act
		DiffStatistics stats = new()
		{
			Additions = 5,
			Deletions = 3,
			Modifications = 2
		};

		// Assert
		Assert.AreEqual(10, stats.TotalChanges);
		Assert.IsTrue(stats.HasDifferences);
	}

	[TestMethod]
	public void DiffStatistics_HasDifferences_ReturnsFalseWhenNoChanges()
	{
		// Act
		DiffStatistics stats = new()
		{
			Additions = 0,
			Deletions = 0,
			Modifications = 0
		};

		// Assert
		Assert.AreEqual(0, stats.TotalChanges);
		Assert.IsFalse(stats.HasDifferences);
	}

	[TestMethod]
	public void DiffStatistics_HasDifferences_ReturnsTrueWhenChangesExist()
	{
		// Test with additions only
		DiffStatistics stats1 = new()
		{
			Additions = 1,
			Deletions = 0,
			Modifications = 0
		};
		Assert.IsTrue(stats1.HasDifferences);

		// Test with deletions only
		DiffStatistics stats2 = new()
		{
			Additions = 0,
			Deletions = 1,
			Modifications = 0
		};
		Assert.IsTrue(stats2.HasDifferences);

		// Test with modifications only
		DiffStatistics stats3 = new()
		{
			Additions = 0,
			Deletions = 0,
			Modifications = 1
		};
		Assert.IsTrue(stats3.HasDifferences);
	}

	#endregion

	#region FileOperationResult Tests

	[TestMethod]
	public void FileHashResult_Properties_StoreCorrectly()
	{
		// Act
		FileHashResult result = new()
		{
			FilePath = "/test/file.txt",
			Hash = "abc123"
		};

		// Assert
		Assert.AreEqual("/test/file.txt", result.FilePath);
		Assert.AreEqual("abc123", result.Hash);
	}

	[TestMethod]
	public void FileContentResult_Properties_StoreCorrectly()
	{
		// Act
		FileContentResult result = new()
		{
			FilePath = "/test/file.txt",
			Content = "file content"
		};

		// Assert
		Assert.AreEqual("/test/file.txt", result.FilePath);
		Assert.AreEqual("file content", result.Content);
	}

	[TestMethod]
	public void FileCopyOperation_Properties_StoreCorrectly()
	{
		// Act
		FileCopyOperation operation = new()
		{
			Source = "/source/file.txt",
			Target = "/target/file.txt"
		};

		// Assert
		Assert.AreEqual("/source/file.txt", operation.Source);
		Assert.AreEqual("/target/file.txt", operation.Target);
	}

	[TestMethod]
	public void FileCopyResult_Properties_StoreCorrectly()
	{
		// Act
		FileCopyResult result = new()
		{
			Source = "/source/file.txt",
			Target = "/target/file.txt",
			Success = true
		};

		// Assert
		Assert.AreEqual("/source/file.txt", result.Source);
		Assert.AreEqual("/target/file.txt", result.Target);
		Assert.IsTrue(result.Success);
	}

	#endregion

	#region MergeCompletionResult Tests

	[TestMethod]
	public void MergeCompletionResult_Constructor_SetsProperties()
	{
		// Act
		MergeCompletionResult result = new(
			IsSuccessful: true,
			FinalMergedContent: "merged content",
			FinalLineCount: 10,
			OriginalFileName: "test.txt"
		);

		// Assert
		Assert.IsTrue(result.IsSuccessful);
		Assert.AreEqual("merged content", result.FinalMergedContent);
		Assert.AreEqual(10, result.FinalLineCount);
		Assert.AreEqual("test.txt", result.OriginalFileName);
		Assert.AreEqual(0, result.TotalMergeOperations);
		Assert.AreEqual(0, result.InitialFileGroups);
		Assert.AreEqual(0, result.TotalFilesMerged);
		Assert.IsNotNull(result.Operations);
		Assert.AreEqual(0, result.Operations.Count);
	}

	[TestMethod]
	public void MergeCompletionResult_InitProperties_SetCorrectly()
	{
		// Arrange
		List<MergeOperationSummary> operations = [new() { OperationNumber = 1 }];

		// Act
		MergeCompletionResult result = new(true, "content", 5, "file.txt")
		{
			TotalMergeOperations = 2,
			InitialFileGroups = 3,
			TotalFilesMerged = 4,
			Operations = operations.AsReadOnly()
		};

		// Assert
		Assert.AreEqual(2, result.TotalMergeOperations);
		Assert.AreEqual(3, result.InitialFileGroups);
		Assert.AreEqual(4, result.TotalFilesMerged);
		Assert.AreEqual(1, result.Operations.Count);
		Assert.AreEqual(1, result.Operations[0].OperationNumber);
	}

	#endregion

	#region MergeOperationSummary Tests

	[TestMethod]
	public void MergeOperationSummary_DefaultValues_AreCorrect()
	{
		// Act
		MergeOperationSummary summary = new();

		// Assert
		Assert.AreEqual(0, summary.OperationNumber);
		Assert.AreEqual(string.Empty, summary.FilePath1);
		Assert.AreEqual(string.Empty, summary.FilePath2);
		Assert.AreEqual(0.0, summary.SimilarityScore);
		Assert.AreEqual(0, summary.FilesAffected);
		Assert.AreEqual(0, summary.ConflictsResolved);
		Assert.AreEqual(0, summary.MergedLineCount);
	}

	[TestMethod]
	public void MergeOperationSummary_InitProperties_SetCorrectly()
	{
		// Act
		MergeOperationSummary summary = new()
		{
			OperationNumber = 1,
			FilePath1 = "/path1/file.txt",
			FilePath2 = "/path2/file.txt",
			SimilarityScore = 0.85,
			FilesAffected = 2,
			ConflictsResolved = 3,
			MergedLineCount = 50
		};

		// Assert
		Assert.AreEqual(1, summary.OperationNumber);
		Assert.AreEqual("/path1/file.txt", summary.FilePath1);
		Assert.AreEqual("/path2/file.txt", summary.FilePath2);
		Assert.AreEqual(0.85, summary.SimilarityScore);
		Assert.AreEqual(2, summary.FilesAffected);
		Assert.AreEqual(3, summary.ConflictsResolved);
		Assert.AreEqual(50, summary.MergedLineCount);
	}

	#endregion

	#region PatternResult Tests

	[TestMethod]
	public void PatternResult_DefaultValues_AreCorrect()
	{
		// Act
		PatternResult result = new();

		// Assert
		Assert.AreEqual(string.Empty, result.Pattern);
		Assert.IsNull(result.FileName);
		Assert.IsFalse(result.Success);
		Assert.AreEqual(0, result.FilesFound);
		Assert.AreEqual(0, result.UniqueVersions);
		Assert.AreEqual(string.Empty, result.Message);
		Assert.IsNull(result.MergeResult);
	}

	[TestMethod]
	public void PatternResult_Properties_SetCorrectly()
	{
		// Arrange
		MergeCompletionResult mergeResult = new(true, "content", 10, "test.txt");

		// Act
		PatternResult result = new()
		{
			Pattern = "*.txt",
			FileName = "test.txt",
			Success = true,
			FilesFound = 5,
			UniqueVersions = 2,
			Message = "Success message",
			MergeResult = mergeResult
		};

		// Assert
		Assert.AreEqual("*.txt", result.Pattern);
		Assert.AreEqual("test.txt", result.FileName);
		Assert.IsTrue(result.Success);
		Assert.AreEqual(5, result.FilesFound);
		Assert.AreEqual(2, result.UniqueVersions);
		Assert.AreEqual("Success message", result.Message);
		Assert.IsNotNull(result.MergeResult);
		Assert.AreSame(mergeResult, result.MergeResult);
	}

	#endregion
}
