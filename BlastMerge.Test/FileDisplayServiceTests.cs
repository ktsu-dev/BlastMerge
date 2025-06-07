// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.ConsoleApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for FileDisplayService functionality.
/// </summary>
[TestClass]
public class FileDisplayServiceTests
{
	/// <summary>
	/// Tests that identical paths return just the filename.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_IdenticalPaths_ReturnsFilename()
	{
		// Arrange
		string path1 = @"C:\dev\project\file.txt";
		string path2 = @"C:\dev\project\file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("file.txt", result1);
		Assert.AreEqual("file.txt", result2);
	}

	/// <summary>
	/// Tests that paths with common leading components have them removed.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_CommonLeadingComponents_RemovesCommonParts()
	{
		// Arrange
		string path1 = @"C:\dev\ktsu-dev\AppDataStorage\.gitignore";
		string path2 = @"C:\dev\ktsu-dev\Shrapnel\.specstory\.gitignore";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("AppDataStorage/.gitignore", result1);
		Assert.AreEqual("Shrapnel/.specstory/.gitignore", result2);
	}

	/// <summary>
	/// Tests that paths with no common components return relative paths from root.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_NoCommonComponents_ReturnsFullRelativePaths()
	{
		// Arrange
		string path1 = @"C:\project1\file.txt";
		string path2 = @"D:\project2\file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("c/project1/file.txt", result1);
		Assert.AreEqual("d/project2/file.txt", result2);
	}

	/// <summary>
	/// Tests that matching internal components are ellipsized.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_MatchingInternalComponents_EllipsizesMatching()
	{
		// Arrange
		string path1 = @"C:\dev\project\src\common\utils\file.txt";
		string path2 = @"C:\dev\project\tests\common\utils\file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("src/.../file.txt", result1);
		Assert.AreEqual("tests/.../file.txt", result2);
	}

	/// <summary>
	/// Tests that different depth paths are handled correctly.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_DifferentDepthPaths_HandlesCorrectly()
	{
		// Arrange
		string path1 = @"C:\dev\project\file.txt";
		string path2 = @"C:\dev\project\src\deep\nested\file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("file.txt", result1);
		Assert.AreEqual("src/deep/nested/file.txt", result2);
	}

	/// <summary>
	/// Tests that single directory paths work correctly.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_SingleDirectoryPaths_WorksCorrectly()
	{
		// Arrange
		string path1 = @"C:\project1\file.txt";
		string path2 = @"C:\project2\file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("project1/file.txt", result1);
		Assert.AreEqual("project2/file.txt", result2);
	}

	/// <summary>
	/// Tests that case-insensitive comparison works for path components.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_CaseInsensitiveComparison_WorksCorrectly()
	{
		// Arrange
		string path1 = @"C:\DEV\Project\File.txt";
		string path2 = @"C:\dev\project\file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("File.txt", result1);
		Assert.AreEqual("file.txt", result2);
	}

	/// <summary>
	/// Tests that paths with only filename differences work correctly.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_OnlyFilenameDiffers_ReturnsFilenames()
	{
		// Arrange
		string path1 = @"C:\dev\project\file1.txt";
		string path2 = @"C:\dev\project\file2.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("file1.txt", result1);
		Assert.AreEqual("file2.txt", result2);
	}

	/// <summary>
	/// Tests that Unix-style paths are handled correctly.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_UnixStylePaths_WorksCorrectly()
	{
		// Arrange
		string path1 = "/home/user/project1/file.txt";
		string path2 = "/home/user/project2/file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("project1/file.txt", result1);
		Assert.AreEqual("project2/file.txt", result2);
	}

	/// <summary>
	/// Tests that multiple matching internal components are properly ellipsized.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_MultipleMatchingInternalComponents_EllipsizesCorrectly()
	{
		// Arrange
		string path1 = @"C:\dev\project\src\common\services\data\file.txt";
		string path2 = @"C:\dev\project\tests\common\services\mocks\file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		Assert.AreEqual("src/.../data/file.txt", result1);
		Assert.AreEqual("tests/.../mocks/file.txt", result2);
	}

	/// <summary>
	/// Tests that empty or null paths are handled gracefully.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_EmptyPaths_HandleGracefully()
	{
		// Arrange & Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			FileDisplayService.MakeDistinguishedPaths(null!, "test"));

		Assert.ThrowsException<ArgumentNullException>(() =>
			FileDisplayService.MakeDistinguishedPaths("test", null!));
	}

	/// <summary>
	/// Tests that relative paths are normalized properly.
	/// </summary>
	[TestMethod]
	public void MakeDistinguishedPaths_RelativePaths_NormalizesCorrectly()
	{
		// Arrange
		string path1 = "project1/src/../lib/file.txt";
		string path2 = "project2/lib/file.txt";

		// Act
		(string result1, string result2) = FileDisplayService.MakeDistinguishedPaths(path1, path2);

		// Assert
		// Both should resolve to their respective lib/file.txt after normalization
		Assert.IsTrue(result1.EndsWith("lib/file.txt"));
		Assert.IsTrue(result2.EndsWith("lib/file.txt"));
		Assert.AreNotEqual(result1, result2); // Should still be distinguishable
	}
}
