// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System.Collections.ObjectModel;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ResolutionItemTests
{
	[TestMethod]
	public void ResolutionItem_WithSingleFileGroup_CalculatesCorrectTotals()
	{
		// Arrange
		FileGroup fileGroup = new(["file1.txt", "file2.txt"]) { Hash = "hash1" };
		ReadOnlyCollection<FileGroup> fileGroups = new List<FileGroup> { fileGroup }.AsReadOnly();

		// Act
		ResolutionItem item = new()
		{
			Pattern = "*.txt",
			FileName = "test.txt",
			FileGroups = fileGroups,
			ResolutionType = ResolutionType.Identical
		};

		// Assert
		Assert.AreEqual("*.txt", item.Pattern);
		Assert.AreEqual("test.txt", item.FileName);
		Assert.AreEqual(ResolutionType.Identical, item.ResolutionType);
		Assert.AreEqual(2, item.TotalFiles);
		Assert.AreEqual(1, item.UniqueVersions);
	}

	[TestMethod]
	public void ResolutionItem_WithMultipleFileGroups_CalculatesCorrectTotals()
	{
		// Arrange
		FileGroup group1 = new(["file1.txt", "file2.txt"]) { Hash = "hash1" };
		FileGroup group2 = new(["file3.txt"]) { Hash = "hash2" };
		FileGroup group3 = new(["file4.txt", "file5.txt", "file6.txt"]) { Hash = "hash3" };
		ReadOnlyCollection<FileGroup> fileGroups = new List<FileGroup> { group1, group2, group3 }.AsReadOnly();

		// Act
		ResolutionItem item = new()
		{
			Pattern = "*.txt",
			FileName = "test.txt",
			FileGroups = fileGroups,
			ResolutionType = ResolutionType.Merge
		};

		// Assert
		Assert.AreEqual(6, item.TotalFiles); // 2 + 1 + 3
		Assert.AreEqual(3, item.UniqueVersions); // 3 groups
		Assert.AreEqual(ResolutionType.Merge, item.ResolutionType);
	}

	[TestMethod]
	public void ResolutionItem_WithEmptyFileGroups_ReturnsZeroTotals()
	{
		// Arrange
		ReadOnlyCollection<FileGroup> fileGroups = new List<FileGroup>().AsReadOnly();

		// Act
		ResolutionItem item = new()
		{
			Pattern = "*.txt",
			FileName = "test.txt",
			FileGroups = fileGroups,
			ResolutionType = ResolutionType.Empty
		};

		// Assert
		Assert.AreEqual(0, item.TotalFiles);
		Assert.AreEqual(0, item.UniqueVersions);
		Assert.AreEqual(ResolutionType.Empty, item.ResolutionType);
	}

	[TestMethod]
	public void ResolutionItem_WithSingleFileInSingleGroup_CalculatesCorrectly()
	{
		// Arrange
		FileGroup fileGroup = new(["single.txt"]) { Hash = "hash1" };
		ReadOnlyCollection<FileGroup> fileGroups = new List<FileGroup> { fileGroup }.AsReadOnly();

		// Act
		ResolutionItem item = new()
		{
			Pattern = "single.txt",
			FileName = "single.txt",
			FileGroups = fileGroups,
			ResolutionType = ResolutionType.SingleFile
		};

		// Assert
		Assert.AreEqual(1, item.TotalFiles);
		Assert.AreEqual(1, item.UniqueVersions);
		Assert.AreEqual(ResolutionType.SingleFile, item.ResolutionType);
	}

	[TestMethod]
	public void ResolutionType_AllEnumValues_AreValid()
	{
		// Act & Assert - Ensure all enum values are accessible
		Assert.AreEqual(ResolutionType.Merge, ResolutionType.Merge);
		Assert.AreEqual(ResolutionType.Identical, ResolutionType.Identical);
		Assert.AreEqual(ResolutionType.SingleFile, ResolutionType.SingleFile);
		Assert.AreEqual(ResolutionType.Empty, ResolutionType.Empty);
	}

	[TestMethod]
	public void ResolutionItem_WithGlobPattern_StoresPatternCorrectly()
	{
		// Arrange
		FileGroup fileGroup = new(["config.txt", "app.config"]) { Hash = "hash1" };
		ReadOnlyCollection<FileGroup> fileGroups = new List<FileGroup> { fileGroup }.AsReadOnly();

		// Act
		ResolutionItem item = new()
		{
			Pattern = "*.config",
			FileName = "config.txt",
			FileGroups = fileGroups,
			ResolutionType = ResolutionType.Identical
		};

		// Assert
		Assert.AreEqual("*.config", item.Pattern);
		Assert.AreEqual("config.txt", item.FileName);
		Assert.AreEqual(2, item.TotalFiles);
	}

	[TestMethod]
	public void ResolutionItem_WithMixedFileSizes_CalculatesTotalFiles()
	{
		// Arrange - Groups with different numbers of files
		FileGroup smallGroup = new(["a.txt"]) { Hash = "hash1" };
		FileGroup mediumGroup = new(["b.txt", "c.txt", "d.txt"]) { Hash = "hash2" };
		FileGroup largeGroup = new(["e.txt", "f.txt", "g.txt", "h.txt", "i.txt"]) { Hash = "hash3" };
		ReadOnlyCollection<FileGroup> fileGroups = new List<FileGroup> { smallGroup, mediumGroup, largeGroup }.AsReadOnly();

		// Act
		ResolutionItem item = new()
		{
			Pattern = "*.txt",
			FileName = "test.txt",
			FileGroups = fileGroups,
			ResolutionType = ResolutionType.Merge
		};

		// Assert
		Assert.AreEqual(9, item.TotalFiles); // 1 + 3 + 5
		Assert.AreEqual(3, item.UniqueVersions);
	}

	[TestMethod]
	public void ResolutionItem_RecordEquality_WorksCorrectly()
	{
		// Arrange
		FileGroup fileGroup = new(["file.txt"]) { Hash = "hash1" };
		ReadOnlyCollection<FileGroup> fileGroups = new List<FileGroup> { fileGroup }.AsReadOnly();

		ResolutionItem item1 = new()
		{
			Pattern = "*.txt",
			FileName = "test.txt",
			FileGroups = fileGroups,
			ResolutionType = ResolutionType.SingleFile
		};

		ResolutionItem item2 = new()
		{
			Pattern = "*.txt",
			FileName = "test.txt",
			FileGroups = fileGroups,
			ResolutionType = ResolutionType.SingleFile
		};

		// Assert
		Assert.AreEqual(item1, item2); // Records should be equal with same values
		Assert.AreEqual(item1.GetHashCode(), item2.GetHashCode());
	}
}
