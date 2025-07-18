// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.IO.Abstractions;
using DiffPlex.Model;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the DiffPlexHelper utility class
/// </summary>
[TestClass]
public class DiffPlexHelperTests : MockFileSystemTestBase
{
	private string _file1Path = string.Empty;
	private string _file2Path = string.Empty;
	private string _identicalFilePath = string.Empty;

	protected override void InitializeFileSystem()
	{
		// Create temp directory in the mock file system
		MockFileSystem.Directory.CreateDirectory(@"C:\temp");

		// Create test files in the mock file system
		_file1Path = @"C:\temp\file1.txt";
		_file2Path = @"C:\temp\file2.txt";
		_identicalFilePath = @"C:\temp\identical.txt";

		// Write test content to files
		MockFileSystem.File.WriteAllText(_file1Path, """
			Line 1
			Line 2
			Line 3
			Line 4
			""");

		MockFileSystem.File.WriteAllText(_file2Path, """
			Line 1
			Modified Line 2
			Line 3
			New Line 4
			Line 5
			""");

		MockFileSystem.File.WriteAllText(_identicalFilePath, """
			Line 1
			Line 2
			Line 3
			Line 4
			""");
	}

	[TestMethod]
	public void Debug_FileSystemProvider_IsWorkingCorrectly()
	{
		// Verify that FileSystemProvider.Current is using our mock file system
		IFileSystem currentFileSystem = FileSystemProvider.Current;
		Assert.IsNotNull(currentFileSystem);
		Assert.IsTrue(currentFileSystem.GetType().Name.Contains("Mock"));

		// Verify that our test files exist in the mock file system
		Assert.IsTrue(MockFileSystem.File.Exists(_file1Path), $"File1 should exist at {_file1Path}");
		Assert.IsTrue(MockFileSystem.File.Exists(_file2Path), $"File2 should exist at {_file2Path}");
		Assert.IsTrue(MockFileSystem.File.Exists(_identicalFilePath), $"Identical file should exist at {_identicalFilePath}");

		// Verify that FileSystemProvider.Current can see our files
		Assert.IsTrue(currentFileSystem.File.Exists(_file1Path), $"FileSystemProvider.Current should see file1 at {_file1Path}");
		Assert.IsTrue(currentFileSystem.File.Exists(_file2Path), $"FileSystemProvider.Current should see file2 at {_file2Path}");

		// Read content to verify it's correct
		string content1 = currentFileSystem.File.ReadAllText(_file1Path);
		Assert.IsTrue(content1.Contains("Line 1"), "File1 should contain expected content");
	}

	[TestMethod]
	public void CreateLineDiffs_ValidFiles_ReturnsDiffResult()
	{
		// Act
		DiffResult result = DiffPlexHelper.CreateLineDiffs(_file1Path, _file2Path, MockFileSystem);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.DiffBlocks);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffs_IdenticalFiles_ReturnsNoDifferences()
	{
		// Act
		DiffResult result = DiffPlexHelper.CreateLineDiffs(_file1Path, _identicalFilePath, MockFileSystem);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.DiffBlocks);
		Assert.AreEqual(0, result.DiffBlocks.Count);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void CreateLineDiffs_NullFile1_ThrowsArgumentNullException()
	{
		// Act
		DiffPlexHelper.CreateLineDiffs(null!, _file2Path, MockFileSystem);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void CreateLineDiffs_NullFile2_ThrowsArgumentNullException()
	{
		// Act
		DiffPlexHelper.CreateLineDiffs(_file1Path, null!, MockFileSystem);
	}

	[TestMethod]
	[ExpectedException(typeof(FileNotFoundException))]
	public void CreateLineDiffs_NonExistentFile_ThrowsFileNotFoundException()
	{
		// Arrange
		string nonExistentFile = @"C:\temp\nonexistent.txt";

		// Act
		DiffPlexHelper.CreateLineDiffs(_file1Path, nonExistentFile, MockFileSystem);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_ValidContent_ReturnsDiffResult()
	{
		// Arrange
		string content1 = "Line 1\nLine 2\nLine 3";
		string content2 = "Line 1\nModified Line 2\nLine 3\nLine 4";

		// Act
		DiffResult result = DiffPlexHelper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.DiffBlocks);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_IdenticalContent_ReturnsNoDifferences()
	{
		// Arrange
		string content = "Line 1\nLine 2\nLine 3";

		// Act
		DiffResult result = DiffPlexHelper.CreateLineDiffsFromContent(content, content);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.DiffBlocks);
		Assert.AreEqual(0, result.DiffBlocks.Count);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void CreateLineDiffsFromContent_NullContent1_ThrowsArgumentNullException()
	{
		// Act
		DiffPlexHelper.CreateLineDiffsFromContent(null!, "content");
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void CreateLineDiffsFromContent_NullContent2_ThrowsArgumentNullException()
	{
		// Act
		DiffPlexHelper.CreateLineDiffsFromContent("content", null!);
	}

	[TestMethod]
	public void GetBlockContext_ValidInput_ReturnsBlockContext()
	{
		// Arrange
		string[] lines1 = ["Line 1", "Line 2", "Line 3", "Line 4", "Line 5"];
		string[] lines2 = ["Line 1", "Modified Line 2", "Line 3", "Line 4", "Line 5"];
		DiffResult diffResult = DiffPlexHelper.CreateLineDiffsFromContent(
			string.Join(Environment.NewLine, lines1),
			string.Join(Environment.NewLine, lines2));

		// Assume there's at least one diff block
		Assert.IsTrue(diffResult.DiffBlocks.Count > 0);
		DiffBlock diffBlock = diffResult.DiffBlocks[0];

		// Act
		BlockContext context = DiffPlexHelper.GetBlockContext(lines1, lines2, diffBlock, 2);

		// Assert
		Assert.IsNotNull(context);
		Assert.IsNotNull(context.ContextBefore1);
		Assert.IsNotNull(context.ContextAfter1);
		Assert.IsNotNull(context.ContextBefore2);
		Assert.IsNotNull(context.ContextAfter2);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void GetBlockContext_NullLines1_ThrowsArgumentNullException()
	{
		// Arrange
		string[] lines2 = ["Line 1", "Line 2"];
		DiffResult diffResult = DiffPlexHelper.CreateLineDiffsFromContent("content1", "content2");
		DiffBlock diffBlock = new(0, 1, 0, 1);

		// Act
		DiffPlexHelper.GetBlockContext(null!, lines2, diffBlock, 2);
	}

	[TestMethod]
	public void ApplyTakeLeft_ValidInput_ReturnsCorrectContent()
	{
		// Arrange
		string[] linesOld = ["Line 1", "Old Line 2", "Line 3"];
		string[] linesNew = ["Line 1", "New Line 2", "Line 3"];
		DiffBlock block = new(1, 1, 1, 1);

		// Act
		string result = DiffPlexHelper.ApplyTakeLeft(linesOld, linesNew, block);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.Contains("Old Line 2"));
		Assert.IsFalse(result.Contains("New Line 2"));
	}

	[TestMethod]
	public void ApplyTakeRight_ValidInput_ReturnsCorrectContent()
	{
		// Arrange
		string[] linesOld = ["Line 1", "Old Line 2", "Line 3"];
		string[] linesNew = ["Line 1", "New Line 2", "Line 3"];
		DiffBlock block = new(1, 1, 1, 1);

		// Act
		string result = DiffPlexHelper.ApplyTakeRight(linesOld, linesNew, block);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsFalse(result.Contains("Old Line 2"));
		Assert.IsTrue(result.Contains("New Line 2"));
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void ApplyTakeLeft_NullLinesOld_ThrowsArgumentNullException()
	{
		// Arrange
		string[] linesNew = ["Line 1", "Line 2"];
		DiffBlock block = new(0, 1, 0, 1);

		// Act
		DiffPlexHelper.ApplyTakeLeft(null!, linesNew, block);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void ApplyTakeRight_NullLinesNew_ThrowsArgumentNullException()
	{
		// Arrange
		string[] linesOld = ["Line 1", "Line 2"];
		DiffBlock block = new(0, 1, 0, 1);

		// Act
		DiffPlexHelper.ApplyTakeRight(linesOld, null!, block);
	}

	[TestMethod]
	public void CalculateDiffStatistics_ValidDiffResult_ReturnsStatistics()
	{
		// Arrange
		string content1 = "Line 1\nLine 2\nLine 3";
		string content2 = "Line 1\nModified Line 2\nLine 3\nLine 4";
		DiffResult diffResult = DiffPlexHelper.CreateLineDiffsFromContent(content1, content2);

		// Act
		DiffStatistics stats = DiffPlexHelper.CalculateDiffStatistics(diffResult);

		// Assert
		Assert.IsNotNull(stats);
		Assert.IsTrue(stats.Additions >= 0);
		Assert.IsTrue(stats.Deletions >= 0);
		Assert.IsTrue(stats.Modifications >= 0);
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))]
	public void CalculateDiffStatistics_NullDiffResult_ThrowsArgumentNullException()
	{
		// Act
		DiffPlexHelper.CalculateDiffStatistics(null!);
	}

	[TestMethod]
	public void CalculateDiffStatistics_NoDifferences_ReturnsZeroStatistics()
	{
		// Arrange
		string content = "Line 1\nLine 2\nLine 3";
		DiffResult diffResult = DiffPlexHelper.CreateLineDiffsFromContent(content, content);

		// Act
		DiffStatistics stats = DiffPlexHelper.CalculateDiffStatistics(diffResult);

		// Assert
		Assert.IsNotNull(stats);
		Assert.AreEqual(0, stats.Additions);
		Assert.AreEqual(0, stats.Deletions);
		Assert.AreEqual(0, stats.Modifications);
	}

	[TestMethod]
	public void CreateLineDiffs_EmptyFiles_HandlesCorrectly()
	{
		// Arrange
		string emptyFile1 = @"C:\temp\empty1.txt";
		string emptyFile2 = @"C:\temp\empty2.txt";
		MockFileSystem.File.WriteAllText(emptyFile1, string.Empty);
		MockFileSystem.File.WriteAllText(emptyFile2, string.Empty);

		// Act
		DiffResult result = DiffPlexHelper.CreateLineDiffs(emptyFile1, emptyFile2, MockFileSystem);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.DiffBlocks);
		Assert.AreEqual(0, result.DiffBlocks.Count);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_EmptyContent_HandlesCorrectly()
	{
		// Act
		DiffResult result = DiffPlexHelper.CreateLineDiffsFromContent(string.Empty, string.Empty);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.DiffBlocks);
		Assert.AreEqual(0, result.DiffBlocks.Count);
	}

	[TestMethod]
	public void GetBlockContext_WithLargeContextSize_DoesNotThrow()
	{
		// Arrange
		string[] lines1 = ["Line 1", "Line 2", "Line 3"];
		string[] lines2 = ["Line 1", "Modified Line 2", "Line 3"];
		DiffResult diffResult = DiffPlexHelper.CreateLineDiffsFromContent(
			string.Join(Environment.NewLine, lines1),
			string.Join(Environment.NewLine, lines2));

		// Assume there's at least one diff block
		if (diffResult.DiffBlocks.Count > 0)
		{
			DiffBlock diffBlock = diffResult.DiffBlocks[0];

			// Act & Assert - Should not throw even with large context size
			BlockContext context = DiffPlexHelper.GetBlockContext(lines1, lines2, diffBlock, 100);
			Assert.IsNotNull(context);
		}
	}
}
