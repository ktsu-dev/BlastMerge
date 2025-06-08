// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Diagnostics;
using System.IO;
using ktsu.BlastMerge.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PerformanceTests
{
	private readonly string _testDirectory;
	private readonly string _largeFile1;
	private readonly string _largeFile2;
	private readonly string _mediumFile1;
	private readonly string _mediumFile2;
	private readonly string _smallFile1;
	private readonly string _smallFile2;

	public PerformanceTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), $"DiffMoreTests_Performance_{Guid.NewGuid()}");
		_largeFile1 = Path.Combine(_testDirectory, "large1.txt");
		_largeFile2 = Path.Combine(_testDirectory, "large2.txt");
		_mediumFile1 = Path.Combine(_testDirectory, "medium1.txt");
		_mediumFile2 = Path.Combine(_testDirectory, "medium2.txt");
		_smallFile1 = Path.Combine(_testDirectory, "small1.txt");
		_smallFile2 = Path.Combine(_testDirectory, "small2.txt");
	}

	[TestInitialize]
	public void Setup()
	{
		// Create test directory
		if (!Directory.Exists(_testDirectory))
		{
			Directory.CreateDirectory(_testDirectory);
		}

		// Create small files (~5KB)
		CreateTestFile(_smallFile1, 100, 0);
		CreateTestFile(_smallFile2, 100, 5); // 5% differences

		// Create medium files (~50KB)
		CreateTestFile(_mediumFile1, 1000, 0);
		CreateTestFile(_mediumFile2, 1000, 5); // 5% differences

		// Create large files (~500KB)
		CreateTestFile(_largeFile1, 10000, 0);
		CreateTestFile(_largeFile2, 10000, 5); // 5% differences
	}

	[TestCleanup]
	public void Cleanup()
	{
		TestHelper.SafeDeleteDirectory(_testDirectory);
	}

	private static void CreateTestFile(string path, int lines, int percentDifferent)
	{
		using StreamWriter writer = new(path);
		for (int i = 0; i < lines; i++)
		{
			// Add some differences based on the percentDifferent parameter
			if (percentDifferent > 0 && i % (100 / percentDifferent) == 0)
			{
				writer.WriteLine($"Different line {i} for testing purposes");
			}
			else
			{
				writer.WriteLine($"Line {i} of test content for performance testing");
			}
		}
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileHasher_LargeFile_PerformanceTest()
	{
		// Arrange
		Stopwatch stopwatch = new();

		// Act
		stopwatch.Start();
		string hash = FileHasher.ComputeFileHash(_largeFile1);
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
		_ = FileDiffer.FindDifferences(_smallFile1, _smallFile2);
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
		_ = FileDiffer.FindDifferences(_mediumFile1, _mediumFile2);
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
		_ = FileDiffer.FindDifferences(_largeFile1, _largeFile2);
		stopwatch.Stop();

		// Assert
		Console.WriteLine($"Time to diff large files (~500KB): {stopwatch.ElapsedMilliseconds}ms");
		Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000,
			"Diffing large files should take less than 10 seconds");
	}

	[TestMethod]
	[TestCategory("Performance")]
	public void FileFinder_LargeDirectory_PerformanceTest()
	{
		// Arrange
		string largeDir = Path.Combine(_testDirectory, "LargeDir");
		if (!Directory.Exists(largeDir))
		{
			Directory.CreateDirectory(largeDir);
		}

		// Create 100 files
		for (int i = 0; i < 100; i++)
		{
			File.WriteAllText(Path.Combine(largeDir, $"file{i}.txt"), $"Content for file {i}");
		}

		Stopwatch stopwatch = new();

		// Act
		stopwatch.Start();
		IReadOnlyCollection<string> files = FileFinder.FindFiles(largeDir, "*.txt", fileSystem: null);
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
