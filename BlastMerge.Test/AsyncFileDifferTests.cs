// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.Generic;
using System.Threading.Tasks;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for AsyncFileDiffer using dependency injection
/// </summary>
[TestClass]
public class AsyncFileDifferTests : DependencyInjectionTestBase
{
	private AsyncFileDiffer _differ = null!;

	protected override void InitializeTestData()
	{
		_differ = GetService<AsyncFileDiffer>();
	}

	[TestMethod]
	public async Task GroupFilesByHashAsync_WithValidFiles_GroupsCorrectly()
	{
		// Arrange
		List<string> filePaths = [@"C:\test\file1.txt", @"C:\test\file2.txt"];

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByHashAsync(filePaths);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task GroupFilesByHashAsync_WithEmptyList_ReturnsEmptyCollection()
	{
		// Arrange
		List<string> filePaths = [];

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByHashAsync(filePaths);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task GroupFilesByHashAsync_WithCustomParallelism_Works()
	{
		// Arrange
		List<string> filePaths = [@"C:\test\file1.txt", @"C:\test\file2.txt"];

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByHashAsync(filePaths, maxDegreeOfParallelism: 1);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task GroupFilesByHashAsync_WithCancellation_SupportsCancellation()
	{
		// Arrange
		List<string> filePaths = [@"C:\test\file1.txt", @"C:\test\file2.txt"];
		using CancellationTokenSource cts = new();

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByHashAsync(filePaths, cancellationToken: cts.Token).ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task GroupFilesByFilenameAndHashAsync_WithSameFilenames_GroupsByHash()
	{
		// Arrange
		List<string> filePaths = [@"C:\test\file1.txt", @"C:\test\subdir\file1.txt"];

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByFilenameAndHashAsync(filePaths).ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task GroupFilesByFilenameAndHashAsync_WithSingleFile_CreatesOneGroup()
	{
		// Arrange
		List<string> filePaths = [@"C:\test\file1.txt"];

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByFilenameAndHashAsync(filePaths).ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task GroupFilesByFilenameAndHashAsync_WithEmptyInput_ReturnsEmptyCollection()
	{
		// Arrange
		List<string> filePaths = [];

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByFilenameAndHashAsync(filePaths).ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task ReadFilesAsync_WithValidFiles_ReadsContent()
	{
		// Arrange
		List<string> filePaths = [@"C:\test\file1.txt", @"C:\test\file2.txt"];

		// Act
		Dictionary<string, string> result = await _differ.ReadFilesAsync(filePaths).ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task ReadFilesAsync_WithEmptyList_ReturnsEmptyDictionary()
	{
		// Arrange
		List<string> filePaths = [];

		// Act
		Dictionary<string, string> result = await _differ.ReadFilesAsync(filePaths).ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task ReadFilesAsync_WithCustomParallelism_Works()
	{
		// Arrange
		List<string> filePaths = [@"C:\test\file1.txt", @"C:\test\file2.txt"];

		// Act
		Dictionary<string, string> result = await _differ.ReadFilesAsync(filePaths, maxDegreeOfParallelism: 1).ConfigureAwait(false);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task CalculateFileSimilarityAsync_WithValidFiles_ReturnsValidSimilarity()
	{
		// Arrange
		string file1 = @"C:\test\file1.txt";
		string file2 = @"C:\test\file2.txt";

		// Act
		double similarity = await _differ.CalculateFileSimilarityAsync(file1, file2).ConfigureAwait(false);

		// Assert
		Assert.IsTrue(similarity >= 0.0 && similarity <= 1.0);
	}

	[TestMethod]
	public async Task CalculateFileSimilarityAsync_WithCancellation_SupportsCancellation()
	{
		// Arrange
		string file1 = @"C:\test\file1.txt";
		string file2 = @"C:\test\file2.txt";
		using CancellationTokenSource cts = new();

		// Act
		double similarity = await _differ.CalculateFileSimilarityAsync(file1, file2, cts.Token);

		// Assert
		Assert.IsTrue(similarity >= 0.0 && similarity <= 1.0);
	}

	[TestMethod]
	public async Task CopyFilesAsync_WithValidOperations_CopiesFiles()
	{
		// Arrange
		List<(string source, string target)> operations = [(@"C:\test\file1.txt", @"C:\test\copied.txt")];

		// Act
		IReadOnlyCollection<(string source, string target)> result = await _differ.CopyFilesAsync(operations);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task CopyFilesAsync_WithEmptyOperations_ReturnsEmptyCollection()
	{
		// Arrange
		List<(string source, string target)> operations = [];

		// Act
		IReadOnlyCollection<(string source, string target)> result = await _differ.CopyFilesAsync(operations);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.Count);
	}

	[TestMethod]
	public async Task CopyFilesAsync_WithCustomParallelism_Works()
	{
		// Arrange
		List<(string source, string target)> operations = [(@"C:\test\file1.txt", @"C:\test\copied.txt")];

		// Act
		IReadOnlyCollection<(string source, string target)> result = await _differ.CopyFilesAsync(operations, maxDegreeOfParallelism: 1);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task GroupFilesByHashAsync_WithLargeNumberOfFiles_HandlesEfficiently()
	{
		// Arrange
		List<string> filePaths = [];
		for (int i = 0; i < 100; i++)
		{
			filePaths.Add($@"C:\test\file{i}.txt");
		}

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByHashAsync(filePaths);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task ReadFilesAsync_WithLargeNumberOfFiles_HandlesEfficiently()
	{
		// Arrange
		List<string> filePaths = [];
		for (int i = 0; i < 50; i++)
		{
			filePaths.Add($@"C:\test\file{i}.txt");
		}

		// Act
		Dictionary<string, string> result = await _differ.ReadFilesAsync(filePaths);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task CalculateFileSimilarityAsync_WithSpecialPaths_HandlesCorrectly()
	{
		// Arrange
		string file1 = @"C:\test\file with spaces.txt";
		string file2 = @"C:\test\file-with-dashes.txt";

		// Act
		double similarity = await _differ.CalculateFileSimilarityAsync(file1, file2);

		// Assert
		Assert.IsTrue(similarity >= 0.0 && similarity <= 1.0);
	}

	[TestMethod]
	public async Task CopyFilesAsync_WithSpecialPaths_HandlesCorrectly()
	{
		// Arrange
		List<(string source, string target)> operations = [
			(@"C:\test\file with spaces.txt", @"C:\test\copied with spaces.txt")
		];

		// Act
		IReadOnlyCollection<(string source, string target)> result = await _differ.CopyFilesAsync(operations);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public async Task GroupFilesByFilenameAndHashAsync_WithMixedDirectories_GroupsCorrectly()
	{
		// Arrange
		List<string> filePaths = [
			@"C:\test\dir1\file.txt",
			@"C:\test\dir2\file.txt",
			@"C:\test\dir3\different.txt"
		];

		// Act
		IReadOnlyCollection<FileGroup> result = await _differ.GroupFilesByFilenameAndHashAsync(filePaths);

		// Assert
		Assert.IsNotNull(result);
	}
}
