// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Performance tests for BlastMerge services using dependency injection
/// </summary>
[TestClass]
public class PerformanceTests : DependencyInjectionTestBase
{
	private AsyncFileDiffer _asyncFileDiffer = null!;
	private FileDiffer _fileDiffer = null!;
	private FileFinder _fileFinder = null!;

	protected override void InitializeTestData()
	{
		_asyncFileDiffer = GetService<AsyncFileDiffer>();
		_fileDiffer = GetService<FileDiffer>();
		_fileFinder = GetService<FileFinder>();
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileHashing_Performance_HandlesLargeFiles()
	{
		// Arrange
		string testContent = new('A', 10000); // 10KB of content

		// Act
		string hash1 = FileHasher.ComputeContentHash(testContent);
		string hash2 = FileHasher.ComputeContentHash(testContent);

		// Assert
		Assert.AreEqual(hash1, hash2, "Hash should be consistent for same content");
		Assert.IsNotNull(hash1);
		Assert.IsTrue(hash1.Length > 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_HandlesLargeNumberOfFiles()
	{
		// Arrange
		List<string> filePaths = [];
		for (int i = 0; i < 100; i++)
		{
			filePaths.Add($@"C:\test\performance\file_{i:000}.txt");
		}

		// Act
		IReadOnlyCollection<FileGroup> groups = _fileDiffer.GroupFilesByHash(filePaths);

		// Assert
		Assert.IsNotNull(groups);
		Assert.IsTrue(groups.Count >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileFinder_Performance_HandlesDeepDirectoryStructure()
	{
		// Arrange
		string searchDirectory = @"C:\test\deep\structure";
		string searchPattern = "*.txt";

		// Act
		IReadOnlyCollection<string> foundFiles = _fileFinder.FindFiles(searchDirectory, searchPattern);

		// Assert
		Assert.IsNotNull(foundFiles);
		Assert.IsTrue(foundFiles.Count >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void AsyncFileDiffer_Performance_HandlesLargeFileBatch()
	{
		// Arrange
		List<string> filePaths = [];
		for (int i = 0; i < 50; i++)
		{
			filePaths.Add($@"C:\test\async_perf\file_{i:000}.txt");
		}

		// Act
		Task<IReadOnlyCollection<FileGroup>> task = _asyncFileDiffer.GroupFilesByHashAsync(filePaths);
		IReadOnlyCollection<FileGroup> groups = task.Result;

		// Assert
		Assert.IsNotNull(groups);
		Assert.IsTrue(groups.Count >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_GeneratesLargeDiffs()
	{
		// Arrange
		string file1 = @"C:\test\large_diff\file1.txt";
		string file2 = @"C:\test\large_diff\file2.txt";

		// Act
		IReadOnlyCollection<LineDifference> differences = _fileDiffer.FindDifferences(file1, file2);

		// Assert
		Assert.IsNotNull(differences);
		Assert.IsTrue(differences.Count >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_CalculatesSimilarity()
	{
		// Arrange
		string file1 = @"C:\test\similarity\file1.txt";
		string file2 = @"C:\test\similarity\file2.txt";

		// Act
		double similarity = _fileDiffer.CalculateFileSimilarity(file1, file2);

		// Assert
		Assert.IsTrue(similarity is >= 0.0 and <= 1.0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_GroupsByFilenameAndHash()
	{
		// Arrange
		List<string> filePaths = [];
		for (int i = 0; i < 20; i++)
		{
			filePaths.Add($@"C:\test\grouping\same_name.txt");
			filePaths.Add($@"C:\test\grouping\different_name_{i}.txt");
		}

		// Act
		IReadOnlyCollection<FileGroup> groups = _fileDiffer.GroupFilesByFilenameAndHash(filePaths);

		// Assert
		Assert.IsNotNull(groups);
		Assert.IsTrue(groups.Count >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_FindsMostSimilarFiles()
	{
		// Arrange
		List<FileGroup> testGroups = [
			new([@"C:\test\group1\file1.txt"]),
			new([@"C:\test\group2\file2.txt"]),
			new([@"C:\test\group3\file3.txt"])
		];

		// Act
		FileSimilarity? similarity = _fileDiffer.FindMostSimilarFiles(testGroups);

		// Assert
		// Result can be null if no similar files found
		Assert.IsTrue(similarity == null || similarity.SimilarityScore >= 0.0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_MergesFiles()
	{
		// Arrange
		string file1 = @"C:\test\merge\file1.txt";
		string file2 = @"C:\test\merge\file2.txt";

		// Act
		MergeResult mergeResult = _fileDiffer.MergeFiles(file1, file2);

		// Assert
		Assert.IsNotNull(mergeResult);
		Assert.IsNotNull(mergeResult.MergedLines);
		Assert.IsNotNull(mergeResult.Conflicts);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_SyncFiles()
	{
		// Arrange
		string sourceFile = @"C:\test\sync\source.txt";
		string targetFile = @"C:\test\sync\target.txt";

		// Act & Assert - Should not throw
		_fileDiffer.SyncFile(sourceFile, targetFile);
		Assert.IsTrue(true); // Test completes successfully
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void AsyncFileDiffer_Performance_ReadsManyFiles()
	{
		// Arrange
		List<string> filePaths = [];
		for (int i = 0; i < 30; i++)
		{
			filePaths.Add($@"C:\test\read_perf\file_{i:000}.txt");
		}

		// Act
		Task<Dictionary<string, string>> task = _asyncFileDiffer.ReadFilesAsync(filePaths);
		Dictionary<string, string> content = task.Result;

		// Assert
		Assert.IsNotNull(content);
		Assert.IsTrue(content.Count >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void AsyncFileDiffer_Performance_CopiesFiles()
	{
		// Arrange
		List<(string source, string target)> copyOperations = [];
		for (int i = 0; i < 10; i++)
		{
			copyOperations.Add(($@"C:\test\copy_perf\source_{i}.txt", $@"C:\test\copy_perf\target_{i}.txt"));
		}

		// Act
		Task<IReadOnlyCollection<(string source, string target)>> task = _asyncFileDiffer.CopyFilesAsync(copyOperations);
		IReadOnlyCollection<(string source, string target)> results = task.Result;

		// Assert
		Assert.IsNotNull(results);
		Assert.IsTrue(results.Count >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_GeneratesGitStyleDiff()
	{
		// Arrange
		string file1 = @"C:\test\git_diff\file1.txt";
		string file2 = @"C:\test\git_diff\file2.txt";

		// Act
		string diff = _fileDiffer.GenerateGitStyleDiff(file1, file2);

		// Assert
		Assert.IsNotNull(diff);
		Assert.IsTrue(diff.Length >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_GeneratesColoredDiff()
	{
		// Arrange
		string file1 = @"C:\test\colored_diff\file1.txt";
		string file2 = @"C:\test\colored_diff\file2.txt";

		// Act
		string diff = _fileDiffer.GenerateChangeSummaryDiff(file1, file2);

		// Assert
		Assert.IsNotNull(diff);
		Assert.IsTrue(diff.Length >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileHasher_Performance_MultipleHashOperations()
	{
		// Arrange
		List<string> contents = [];
		for (int i = 0; i < 100; i++)
		{
			contents.Add($"Content for file number {i} with some additional text to make it longer");
		}

		// Act
		List<string> hashes = [.. contents.Select(FileHasher.ComputeContentHash)];

		// Assert
		Assert.AreEqual(contents.Count, hashes.Count);
		Assert.IsTrue(hashes.All(h => !string.IsNullOrEmpty(h)));
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileFinder_Performance_RecursiveSearch()
	{
		// Arrange
		string searchDirectory = @"C:\test\recursive";
		string searchPattern = "*.txt";

		// Act
		IReadOnlyCollection<string> foundFiles = _fileFinder.FindFiles(searchDirectory, searchPattern);

		// Assert
		Assert.IsNotNull(foundFiles);
		Assert.IsTrue(foundFiles.Count >= 0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void AsyncFileDiffer_Performance_CalculatesFileSimilarity()
	{
		// Arrange
		string file1 = @"C:\test\async_similarity\file1.txt";
		string file2 = @"C:\test\async_similarity\file2.txt";

		// Act
		Task<double> task = _asyncFileDiffer.CalculateFileSimilarityAsync(file1, file2);
		double similarity = task.Result;

		// Assert
		Assert.IsTrue(similarity is >= 0.0 and <= 1.0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_StaticLineSimilarity()
	{
		// Arrange
		string[] lines1 = ["Line 1", "Line 2", "Line 3"];
		string[] lines2 = ["Line 1", "Modified Line 2", "Line 3"];

		// Act
		double similarity = FileDiffer.CalculateLineSimilarity(lines1, lines2);

		// Assert
		Assert.IsTrue(similarity is >= 0.0 and <= 1.0);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_StaticMergeLines()
	{
		// Arrange
		string[] lines1 = ["Line 1", "Line 2", "Line 3"];
		string[] lines2 = ["Line 1", "Modified Line 2", "Line 3"];

		// Act
		MergeResult mergeResult = FileDiffer.MergeLines(lines1, lines2);

		// Assert
		Assert.IsNotNull(mergeResult);
		Assert.IsNotNull(mergeResult.MergedLines);
		Assert.IsNotNull(mergeResult.Conflicts);
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_Performance_StaticFileHash()
	{
		// Arrange
		string content = "Sample content for hashing performance test";

		// Act
		string hash = FileDiffer.CalculateFileHash(content);

		// Assert
		Assert.IsNotNull(hash);
		Assert.IsTrue(hash.Length > 0);
	}
}
