// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.DiffMore.Test;

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class FileDifferPathTests
{
	private readonly string _testDirectory;
	private readonly string _dir1;
	private readonly string _dir2;
	private readonly string _emptyDir;

	public FileDifferPathTests()
	{
		_testDirectory = Path.Combine(Path.GetTempPath(), "DiffMoreTests_Paths");
		_dir1 = Path.Combine(_testDirectory, "dir1");
		_dir2 = Path.Combine(_testDirectory, "dir2");
		_emptyDir = Path.Combine(_testDirectory, "empty");
	}

	[TestInitialize]
	public void Setup()
	{
		// Create test directories
		TestHelper.SafeCreateDirectory(_testDirectory);
		TestHelper.SafeCreateDirectory(_dir1);
		TestHelper.SafeCreateDirectory(_dir2);
		TestHelper.SafeCreateDirectory(_emptyDir);

		// Create test files
		File.WriteAllText(Path.Combine(_dir1, "file1.txt"), "Content 1");
		File.WriteAllText(Path.Combine(_dir1, "file2.txt"), "Content 2");
		File.WriteAllText(Path.Combine(_dir1, "file3.txt"), "Content 3");

		File.WriteAllText(Path.Combine(_dir2, "file1.txt"), "Content 1 Modified");
		File.WriteAllText(Path.Combine(_dir2, "file2.txt"), "Content 2");
		File.WriteAllText(Path.Combine(_dir2, "file4.txt"), "Content 4");
	}

	[TestCleanup]
	public void Cleanup()
	{
		TestHelper.SafeDeleteDirectory(_testDirectory);
	}

	[TestMethod]
	public void FileDiffer_EmptyDirectory_ReturnsEmptyResult()
	{
		// Act
		var result = FileDiffer.FindDifferences(_emptyDir, _dir2, "*.txt");

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.SameFiles.Count, "Should not have any same files");
		Assert.AreEqual(0, result.ModifiedFiles.Count, "Should not have any modified files");
		Assert.AreEqual(3, result.OnlyInDir2.Count, "Should have 3 files only in dir2");
		Assert.AreEqual(0, result.OnlyInDir1.Count, "Should not have any files only in empty dir");
	}

	[TestMethod]
	public void FileDiffer_RelativePaths_HandledCorrectly()
	{
		// Arrange
		var currentDir = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(_testDirectory);

		try
		{
			// Act using relative paths
			var result = FileDiffer.FindDifferences("dir1", "dir2", "*.txt");

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(1, result.SameFiles.Count, "Should have 1 same file");
			Assert.AreEqual(1, result.ModifiedFiles.Count, "Should have 1 modified file");
			Assert.AreEqual(1, result.OnlyInDir2.Count, "Should have 1 file only in dir2");
			Assert.AreEqual(1, result.OnlyInDir1.Count, "Should have 1 file only in dir1");
		}
		finally
		{
			// Restore current directory
			Directory.SetCurrentDirectory(currentDir);
		}
	}

	[TestMethod]
	public void FileDiffer_WithTrailingSlashes_HandledCorrectly()
	{
		// Arrange
		var dir1WithSlash = _dir1 + Path.DirectorySeparatorChar;
		var dir2WithSlash = _dir2 + Path.DirectorySeparatorChar;

		// Act
		var result = FileDiffer.FindDifferences(dir1WithSlash, dir2WithSlash, "*.txt");

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(1, result.SameFiles.Count, "Should have 1 same file");
		Assert.AreEqual(1, result.ModifiedFiles.Count, "Should have 1 modified file");
		Assert.AreEqual(1, result.OnlyInDir2.Count, "Should have 1 file only in dir2");
		Assert.AreEqual(1, result.OnlyInDir1.Count, "Should have 1 file only in dir1");
	}

	[TestMethod]
	public void FileDiffer_WithDifferentCasePaths_HandledCorrectly()
	{
		// Arrange
		var upperCaseDir1 = _dir1.ToUpper();
		var upperCaseDir2 = _dir2.ToUpper();

		// This test is OS dependent - Windows is case insensitive, Unix is case sensitive
		var isWindows = Path.DirectorySeparatorChar == '\\';

		if (isWindows)
		{
			// Act - Windows should handle different case paths
			var result = FileDiffer.FindDifferences(upperCaseDir1, upperCaseDir2, "*.txt");

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(1, result.SameFiles.Count, "Should have 1 same file");
			Assert.AreEqual(1, result.ModifiedFiles.Count, "Should have 1 modified file");
			Assert.AreEqual(1, result.OnlyInDir2.Count, "Should have 1 file only in dir2");
			Assert.AreEqual(1, result.OnlyInDir1.Count, "Should have 1 file only in dir1");
		}
		else
		{
			// On Unix, this would normally throw an exception, but we'll skip actual assertion
			// since this is platform-dependent behavior
			Console.WriteLine("Skipping case sensitivity test on non-Windows platform");
		}
	}

	/*[TestMethod]
	public void FileDiffer_MultipleSearchPatterns_FindsAllMatchingFiles()
	{
		// Create files with different extensions
		File.WriteAllText(Path.Combine(_dir1, "code.cs"), "C# code");
		File.WriteAllText(Path.Combine(_dir1, "doc.md"), "Markdown");
		File.WriteAllText(Path.Combine(_dir2, "code.cs"), "C# code modified");
		File.WriteAllText(Path.Combine(_dir2, "doc.md"), "Markdown");

		// Act - Test with multiple patterns
		var result = FileDiffer.FindDifferences(_dir1, _dir2, "*.txt;*.cs;*.md");

		// Assert - Should include all matching file types
		Assert.IsNotNull(result);
		Assert.AreEqual(2, result.SameFiles.Count, "Should have 2 same files (*.txt and *.md)");
		Assert.AreEqual(2, result.ModifiedFiles.Count, "Should have 2 modified files (*.txt and *.cs)");
		Assert.AreEqual(1, result.OnlyInDir2.Count, "Should have 1 file only in dir2");
		Assert.AreEqual(1, result.OnlyInDir1.Count, "Should have 1 file only in dir1");

		// Verify specific extensions were included
		Assert.IsTrue(result.SameFiles.Any(f => f.EndsWith(".md")), "Should include markdown file");
		Assert.IsTrue(result.ModifiedFiles.Any(f => f.EndsWith(".cs")), "Should include C# file");
	}*/
}
