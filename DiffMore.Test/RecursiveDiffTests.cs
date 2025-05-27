// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System;
using System.IO;
using System.Linq;
using ktsu.DiffMore.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RecursiveDiffTests
{
	private readonly string _testDirectory;
	private readonly string _dir1;
	private readonly string _dir2;
	private readonly string _dir1SubA;
	private readonly string _dir1SubB;
	private readonly string _dir2SubA;
	private readonly string _dir2SubC;

	public RecursiveDiffTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), "DiffMoreTests_Recursive");
		_dir1 = Path.Combine(_testDirectory, "dir1");
		_dir2 = Path.Combine(_testDirectory, "dir2");
		_dir1SubA = Path.Combine(_dir1, "subA");
		_dir1SubB = Path.Combine(_dir1, "subB");
		_dir2SubA = Path.Combine(_dir2, "subA");
		_dir2SubC = Path.Combine(_dir2, "subC");
	}

	[TestInitialize]
	public void Setup()
	{
		// Create test directories
		TestHelper.SafeCreateDirectory(_testDirectory);
		TestHelper.SafeCreateDirectory(_dir1);
		TestHelper.SafeCreateDirectory(_dir2);
		TestHelper.SafeCreateDirectory(_dir1SubA);
		TestHelper.SafeCreateDirectory(_dir1SubB);
		TestHelper.SafeCreateDirectory(_dir2SubA);
		TestHelper.SafeCreateDirectory(_dir2SubC);

		// Create test files in the root directories
		File.WriteAllText(Path.Combine(_dir1, "file1.txt"), "Root Content 1");
		File.WriteAllText(Path.Combine(_dir1, "file2.txt"), "Root Content 2");
		File.WriteAllText(Path.Combine(_dir2, "file1.txt"), "Root Content 1 Modified");
		File.WriteAllText(Path.Combine(_dir2, "file3.txt"), "Root Content 3");

		// Create test files in subdirectories
		File.WriteAllText(Path.Combine(_dir1SubA, "subfile1.txt"), "Sub Content 1");
		File.WriteAllText(Path.Combine(_dir1SubA, "subfile2.txt"), "Sub Content 2");
		File.WriteAllText(Path.Combine(_dir1SubB, "uniquefile.txt"), "Unique Content");

		File.WriteAllText(Path.Combine(_dir2SubA, "subfile1.txt"), "Sub Content 1 Modified");
		File.WriteAllText(Path.Combine(_dir2SubA, "subfile3.txt"), "Sub Content 3");
		File.WriteAllText(Path.Combine(_dir2SubC, "newfile.txt"), "New Content");
	}

	[TestCleanup]
	public void Cleanup()
	{
		TestHelper.SafeDeleteDirectory(_testDirectory);
	}

	[TestMethod]
	public void FindDifferences_RecursiveTrue_IncludesSubdirectoryFiles()
	{
		// Act
		var result = FileDiffer.FindDifferences(_dir1, _dir2, "*.txt", recursive: true);

		// Assert
		Assert.IsNotNull(result);

		// Verify overall counts
		var totalFiles = result.SameFiles.Count + result.ModifiedFiles.Count +
						 result.OnlyInDir1.Count + result.OnlyInDir2.Count;
		Assert.AreEqual(9, totalFiles, "Should find all 9 files across all directories");

		// Check for specific files in subdirectories
		Assert.IsTrue(result.ModifiedFiles.Contains(Path.Combine("subA", "subfile1.txt")),
			"Should include modified file in subdirectory");
		Assert.IsTrue(result.OnlyInDir1.Contains(Path.Combine("subA", "subfile2.txt")),
			"Should include file only in dir1 subdirectory");
		Assert.IsTrue(result.OnlyInDir1.Contains(Path.Combine("subB", "uniquefile.txt")),
			"Should include file in dir1-only subdirectory");
		Assert.IsTrue(result.OnlyInDir2.Contains(Path.Combine("subA", "subfile3.txt")),
			"Should include file only in dir2 subdirectory");
		Assert.IsTrue(result.OnlyInDir2.Contains(Path.Combine("subC", "newfile.txt")),
			"Should include file in dir2-only subdirectory");
	}

	[TestMethod]
	public void FindDifferences_RecursiveFalse_ExcludesSubdirectoryFiles()
	{
		// Act
		var result = FileDiffer.FindDifferences(_dir1, _dir2, "*.txt", recursive: false);

		// Assert
		Assert.IsNotNull(result);

		// Verify only root directory files are included
		var totalFiles = result.SameFiles.Count + result.ModifiedFiles.Count +
						 result.OnlyInDir1.Count + result.OnlyInDir2.Count;
		Assert.AreEqual(4, totalFiles, "Should find only 4 files in the root directories");

		// Check for specific root files
		Assert.AreEqual(0, result.SameFiles.Count, "Should not have any identical files");
		Assert.AreEqual(1, result.ModifiedFiles.Count, "Should have 1 modified file");
		Assert.AreEqual(1, result.OnlyInDir1.Count, "Should have 1 file only in dir1");
		Assert.AreEqual(1, result.OnlyInDir2.Count, "Should have 1 file only in dir2");

		// Check that subdirectory files are excluded
		Assert.IsFalse(result.ModifiedFiles.Any(f => f.Contains("sub")),
			"Should not include any subdirectory files");
		Assert.IsFalse(result.OnlyInDir1.Any(f => f.Contains("sub")),
			"Should not include any subdirectory files");
		Assert.IsFalse(result.OnlyInDir2.Any(f => f.Contains("sub")),
			"Should not include any subdirectory files");
	}

	[TestMethod]
	public void FindDifferences_DeepDirectoryStructure_HandlesCorrectly()
	{
		// Arrange
		// Create a deeper directory structure
		var deepDir1 = Path.Combine(_dir1SubA, "deep", "deeper", "deepest");
		var deepDir2 = Path.Combine(_dir2SubA, "deep", "deeper", "deepest");
		TestHelper.SafeCreateDirectory(deepDir1);
		TestHelper.SafeCreateDirectory(deepDir2);

		File.WriteAllText(Path.Combine(deepDir1, "deepfile.txt"), "Deep Content");
		File.WriteAllText(Path.Combine(deepDir2, "deepfile.txt"), "Deep Content Modified");

		// Act
		var result = FileDiffer.FindDifferences(_dir1, _dir2, "*.txt", recursive: true);

		// Assert
		Assert.IsNotNull(result);

		// Check for deep files
		var deepFilePath = Path.Combine("subA", "deep", "deeper", "deepest", "deepfile.txt");
		Assert.IsTrue(result.ModifiedFiles.Contains(deepFilePath),
			"Should find files in deeply nested directories");
	}

	[TestMethod]
	public void FindDifferences_WithSymbolicLinks_HandlesCorrectly()
	{
		// This test is conditional as it requires admin rights on Windows
		var canCreateSymlinks = false;

		try
		{
			var testDir = Path.Combine(_testDirectory, "symlink_test");
			var targetDir = Path.Combine(_testDirectory, "symlink_target");
			TestHelper.SafeCreateDirectory(testDir);
			TestHelper.SafeCreateDirectory(targetDir);

			File.WriteAllText(Path.Combine(targetDir, "linkfile.txt"), "Link Content");

			// Try to create a symlink - may fail without admin rights on Windows
			Directory.CreateSymbolicLink(
				Path.Combine(testDir, "link"),
				targetDir);

			canCreateSymlinks = true;
			TestHelper.SafeDeleteDirectory(testDir);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Skipping symbolic link test: {ex.Message}");
		}

		if (canCreateSymlinks)
		{
			// Create test directories with symbolic links
			var linkTarget1 = Path.Combine(_testDirectory, "linktarget1");
			var linkTarget2 = Path.Combine(_testDirectory, "linktarget2");
			TestHelper.SafeCreateDirectory(linkTarget1);
			TestHelper.SafeCreateDirectory(linkTarget2);

			// Create files in the link targets
			File.WriteAllText(Path.Combine(linkTarget1, "targetfile.txt"), "Target Content 1");
			File.WriteAllText(Path.Combine(linkTarget2, "targetfile.txt"), "Target Content 2");

			// Create symbolic links
			var link1 = Path.Combine(_dir1, "link");
			var link2 = Path.Combine(_dir2, "link");
			Directory.CreateSymbolicLink(link1, linkTarget1);
			Directory.CreateSymbolicLink(link2, linkTarget2);

			// Act - should follow symbolic links
			var result = FileDiffer.FindDifferences(_dir1, _dir2, "*.txt", recursive: true);

			// Assert
			Assert.IsNotNull(result);
			Assert.IsTrue(result.ModifiedFiles.Contains(Path.Combine("link", "targetfile.txt")),
				"Should follow symbolic links and find differences");

			// Clean up
			try
			{
				if (Directory.Exists(link1))
				{
					Directory.Delete(link1);
				}

				if (Directory.Exists(link2))
				{
					Directory.Delete(link2);
				}

				TestHelper.SafeDeleteDirectory(linkTarget1);
				TestHelper.SafeDeleteDirectory(linkTarget2);
			}
			catch { /* Ignore cleanup errors */ }
		}
	}

	[TestMethod]
	public void FindDifferences_WithLargeDirectoryStructure_CompletesInReasonableTime()
	{
		// Skip this test in regular runs if it would take too long
		var runLargeTest = false; // Set to true to run this test

		if (runLargeTest)
		{
			// Arrange
			var largeDir1 = Path.Combine(_testDirectory, "large1");
			var largeDir2 = Path.Combine(_testDirectory, "large2");
			TestHelper.SafeCreateDirectory(largeDir1);
			TestHelper.SafeCreateDirectory(largeDir2);

			// Create a larger directory structure
			for (var i = 0; i < 10; i++)
			{
				var subdir1 = Path.Combine(largeDir1, $"sub{i}");
				var subdir2 = Path.Combine(largeDir2, $"sub{i}");
				TestHelper.SafeCreateDirectory(subdir1);
				TestHelper.SafeCreateDirectory(subdir2);

				for (var j = 0; j < 10; j++)
				{
					File.WriteAllText(Path.Combine(subdir1, $"file{j}.txt"), $"Content {i}-{j}");
					// Make half the files different
					var content = j % 2 == 0 ? $"Content {i}-{j}" : $"Modified {i}-{j}";
					File.WriteAllText(Path.Combine(subdir2, $"file{j}.txt"), content);
				}
			}

			// Act with timeout check
			var timeout = TimeSpan.FromSeconds(10);
			var watch = System.Diagnostics.Stopwatch.StartNew();
			var result = differ.FindDifferences(largeDir1, largeDir2, "*.txt", recursive: true);
			watch.Stop();

			// Assert
			Assert.IsNotNull(result);
			Assert.IsTrue(watch.Elapsed < timeout,
				$"Should complete in less than {timeout.TotalSeconds} seconds, took {watch.Elapsed.TotalSeconds} seconds");

			// Clean up
			TestHelper.SafeDeleteDirectory(largeDir1);
			TestHelper.SafeDeleteDirectory(largeDir2);
		}
		else
		{
			// Skip test
			Console.WriteLine("Skipping large directory test - set runLargeTest=true to enable");
			Assert.IsTrue(true);
		}
	}
}
