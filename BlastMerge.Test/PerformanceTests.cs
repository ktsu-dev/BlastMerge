// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PerformanceTests : DependencyInjectionTestBase
{
	private string _testDirectory = string.Empty;
	private string _largeFile1 = string.Empty;
	private string _largeFile2 = string.Empty;
	private string _mediumFile1 = string.Empty;
	private string _mediumFile2 = string.Empty;
	private string _smallFile1 = string.Empty;
	private string _smallFile2 = string.Empty;
	private FileDiffer _fileDiffer = null!;
	private FileHasher _fileHasher = null!;
	private FileFinder _fileFinder = null!;

	protected override void InitializeTestData()
	{
		// Get services from DI
		_fileDiffer = GetService<FileDiffer>();
		_fileHasher = GetService<FileHasher>();
		_fileFinder = GetService<FileFinder>();

		_testDirectory = TestDirectory;
		_largeFile1 = MockFileSystem.Path.Combine(_testDirectory, "large1.txt");
		_largeFile2 = MockFileSystem.Path.Combine(_testDirectory, "large2.txt");
		_mediumFile1 = MockFileSystem.Path.Combine(_testDirectory, "medium1.txt");
		_mediumFile2 = MockFileSystem.Path.Combine(_testDirectory, "medium2.txt");
		_smallFile1 = MockFileSystem.Path.Combine(_testDirectory, "small1.txt");
		_smallFile2 = MockFileSystem.Path.Combine(_testDirectory, "small2.txt");

		// Create test directory
		MockFileSystem.Directory.CreateDirectory(_testDirectory);

		CreateTestFiles();
	}

	private void CreateTestFiles()
	{
		// Create small files (1KB each)
		CreateTestFile(_smallFile1, 1024, "Small file 1 content");
		CreateTestFile(_smallFile2, 1024, "Small file 2 content");

		// Create medium files (100KB each)
		CreateTestFile(_mediumFile1, 100 * 1024, "Medium file 1 content");
		CreateTestFile(_mediumFile2, 100 * 1024, "Medium file 2 content");

		// Create large files (1MB each)
		CreateTestFile(_largeFile1, 1024 * 1024, "Large file 1 content");
		CreateTestFile(_largeFile2, 1024 * 1024, "Large file 2 content");
	}

	private void CreateTestFile(string filePath, int sizeInBytes, string baseContent)
	{
		StringBuilder content = new();
		Random random = new(42); // Fixed seed for reproducible tests

		while (content.Length < sizeInBytes)
		{
			content.AppendLine($"{baseContent} - Line {(content.Length / baseContent.Length) + 1} - Random: {random.Next()}");
		}

		// Trim to exact size
		if (content.Length > sizeInBytes)
		{
			content.Length = sizeInBytes;
		}

		MockFileSystem.File.WriteAllText(filePath, content.ToString());
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileHasher_LargeFile_PerformanceTest()
	{
		// Arrange
		Stopwatch stopwatch = new();

		// Act
		stopwatch.Start();
		string hash = _fileHasher.ComputeFileHash(_largeFile1);
		stopwatch.Stop();

		// Assert
		Console.WriteLine($"Time to hash large file (~500KB): {stopwatch.ElapsedMilliseconds}ms");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000,
			"Hashing a 500KB file should take less than 1 second");
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_SmallFiles_PerformanceTest()
	{
		// Arrange
		Stopwatch stopwatch = new();

		// Act
		stopwatch.Start();
		_ = _fileDiffer.FindDifferences(_smallFile1, _smallFile2);
		stopwatch.Stop();

		// Assert
		Console.WriteLine($"Time to diff small files (~5KB): {stopwatch.ElapsedMilliseconds}ms");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500,
			"Diffing small files should take less than 500ms");
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_MediumFiles_PerformanceTest()
	{
		// Arrange
		Stopwatch stopwatch = new();

		// Act
		stopwatch.Start();
		_ = _fileDiffer.FindDifferences(_mediumFile1, _mediumFile2);
		stopwatch.Stop();

		// Assert
		Console.WriteLine($"Time to diff medium files (~50KB): {stopwatch.ElapsedMilliseconds}ms");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000,
			"Diffing medium files should take less than 2 seconds");
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_LargeFiles_PerformanceTest()
	{
		// Arrange
		Stopwatch stopwatch = new();

		// Act
		stopwatch.Start();
		_ = _fileDiffer.FindDifferences(_largeFile1, _largeFile2);
		stopwatch.Stop();

		// Assert
		Console.WriteLine($"Time to diff large files (~500KB): {stopwatch.ElapsedMilliseconds}ms");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000,
			"Diffing large files should take less than 30 seconds");
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileFinder_LargeDirectory_PerformanceTest()
	{
		// Arrange
		string largeDir = MockFileSystem.Path.Combine(_testDirectory, "LargeDir");
		if (!MockFileSystem.Directory.Exists(largeDir))
		{
			MockFileSystem.Directory.CreateDirectory(largeDir);
		}

		// Create 100 files
		for (int i = 0; i < 100; i++)
		{
			MockFileSystem.File.WriteAllText(MockFileSystem.Path.Combine(largeDir, $"file{i}.txt"), $"Content for file {i}");
		}

		Stopwatch stopwatch = new();

		// Act
		stopwatch.Start();
		IReadOnlyCollection<string> files = _fileFinder.FindFiles(largeDir, "*.txt");
		stopwatch.Stop();

		// Assert
		Console.WriteLine($"Time to find 100 files: {stopwatch.ElapsedMilliseconds}ms");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000,
			"Finding 100 files should take less than 1 second");
		Assert.AreEqual(100, files.Count, "Should find all 100 files");
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileDiffer_GitStyleDiffPerformance_Test()
	{
		// Arrange
		Stopwatch stopwatch = new();

		// Act
		stopwatch.Start();
		string gitDiff = FileDiffer.GenerateGitStyleDiff(_mediumFile1, _mediumFile2);
		stopwatch.Stop();

		// Assert
		Console.WriteLine($"Time to generate git-style diff for medium files: {stopwatch.ElapsedMilliseconds}ms");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000,
			"Generating git-style diff for medium files should take less than 2 seconds");
	}
}
