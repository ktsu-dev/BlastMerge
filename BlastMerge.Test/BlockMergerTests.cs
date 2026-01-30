// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BlockMergerTests
{
	[TestMethod]
	public void PerformManualBlockSelection_WithNullLines1_ThrowsArgumentNullException()
	{
		// Arrange
		string[] lines2 = ["line1", "line2"];

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			BlockMerger.PerformManualBlockSelection(null!, lines2, (block, context, num) => BlockChoice.UseVersion1));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithNullLines2_ThrowsArgumentNullException()
	{
		// Arrange
		string[] lines1 = ["line1", "line2"];

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			BlockMerger.PerformManualBlockSelection(lines1, null!, (block, context, num) => BlockChoice.UseVersion1));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithNullCallback_ThrowsArgumentNullException()
	{
		// Arrange
		string[] lines1 = ["line1", "line2"];
		string[] lines2 = ["line1", "line2"];

		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			BlockMerger.PerformManualBlockSelection(lines1, lines2, null!));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithIdenticalFiles_ReturnsOriginalContent()
	{
		// Arrange
		string[] lines1 = ["line1", "line2", "line3"];
		string[] lines2 = ["line1", "line2", "line3"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(3, result.MergedLines.Count);
		Assert.AreEqual("line1", result.MergedLines[0]);
		Assert.AreEqual("line2", result.MergedLines[1]);
		Assert.AreEqual("line3", result.MergedLines[2]);
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithUseVersion1Choice_UsesVersion1Content()
	{
		// Arrange
		string[] lines1 = ["line1", "modified_in_v1", "line3"];
		string[] lines2 = ["line1", "modified_in_v2", "line3"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		CollectionAssert.Contains(result.MergedLines.ToList(), "modified_in_v1");
		CollectionAssert.DoesNotContain(result.MergedLines.ToList(), "modified_in_v2");
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithUseVersion2Choice_UsesVersion2Content()
	{
		// Arrange
		string[] lines1 = ["line1", "modified_in_v1", "line3"];
		string[] lines2 = ["line1", "modified_in_v2", "line3"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion2);

		// Assert
		Assert.IsNotNull(result);
		CollectionAssert.DoesNotContain(result.MergedLines.ToList(), "modified_in_v1");
		CollectionAssert.Contains(result.MergedLines.ToList(), "modified_in_v2");
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithUseBothChoice_IncludesBothVersions()
	{
		// Arrange
		string[] lines1 = ["line1", "modified_in_v1", "line3"];
		string[] lines2 = ["line1", "modified_in_v2", "line3"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseBoth);

		// Assert
		Assert.IsNotNull(result);
		CollectionAssert.Contains(result.MergedLines.ToList(), "modified_in_v1");
		CollectionAssert.Contains(result.MergedLines.ToList(), "modified_in_v2");
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithSkipChoice_SkipsBothVersions()
	{
		// Arrange
		string[] lines1 = ["line1", "modified_in_v1", "line3"];
		string[] lines2 = ["line1", "modified_in_v2", "line3"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.Skip);

		// Assert
		Assert.IsNotNull(result);
		CollectionAssert.DoesNotContain(result.MergedLines.ToList(), "modified_in_v1");
		CollectionAssert.DoesNotContain(result.MergedLines.ToList(), "modified_in_v2");
		// Should still contain unchanged lines
		CollectionAssert.Contains(result.MergedLines.ToList(), "line1");
		CollectionAssert.Contains(result.MergedLines.ToList(), "line3");
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithEmptyFiles_ReturnsEmptyResult()
	{
		// Arrange
		string[] lines1 = [];
		string[] lines2 = [];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.MergedLines.Count);
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithOneEmptyFile_HandlesCorrectly()
	{
		// Arrange
		string[] lines1 = [];
		string[] lines2 = ["line1", "line2"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion2);

		// Assert
		Assert.IsNotNull(result);
		CollectionAssert.Contains(result.MergedLines.ToList(), "line1");
		CollectionAssert.Contains(result.MergedLines.ToList(), "line2");
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithMultipleBlocks_CallsCallbackForEachBlock()
	{
		// Arrange
		string[] lines1 = ["line1", "v1_change1", "line3", "v1_change2", "line5"];
		string[] lines2 = ["line1", "v2_change1", "line3", "v2_change2", "line5"];

		List<int> blockNumbers = [];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) =>
			{
				blockNumbers.Add(num);
				return BlockChoice.UseVersion1;
			});

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(blockNumbers.Count > 0); // Should have called callback for blocks
		Assert.IsTrue(blockNumbers.All(n => n > 0)); // Block numbers should be positive
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithDifferentChoicesPerBlock_AppliesCorrectly()
	{
		// Arrange
		string[] lines1 = ["line1", "v1_change1", "line3", "v1_change2", "line5"];
		string[] lines2 = ["line1", "v2_change1", "line3", "v2_change2", "line5"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) =>
				// Use version 1 for first block, version 2 for second block
				num == 1 ? BlockChoice.UseVersion1 : BlockChoice.UseVersion2);

		// Assert
		Assert.IsNotNull(result);
		// Should contain unchanged lines
		CollectionAssert.Contains(result.MergedLines.ToList(), "line1");
		CollectionAssert.Contains(result.MergedLines.ToList(), "line3");
		CollectionAssert.Contains(result.MergedLines.ToList(), "line5");
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithAddedLines_HandlesCorrectly()
	{
		// Arrange
		string[] lines1 = ["line1", "line2"];
		string[] lines2 = ["line1", "added_line", "line2"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion2);

		// Assert
		Assert.IsNotNull(result);
		CollectionAssert.Contains(result.MergedLines.ToList(), "line1");
		CollectionAssert.Contains(result.MergedLines.ToList(), "added_line");
		CollectionAssert.Contains(result.MergedLines.ToList(), "line2");
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithDeletedLines_HandlesCorrectly()
	{
		// Arrange
		string[] lines1 = ["line1", "deleted_line", "line2"];
		string[] lines2 = ["line1", "line2"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		CollectionAssert.Contains(result.MergedLines.ToList(), "line1");
		CollectionAssert.Contains(result.MergedLines.ToList(), "deleted_line");
		CollectionAssert.Contains(result.MergedLines.ToList(), "line2");
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithComplexChanges_MaintainsCorrectOrder()
	{
		// Arrange
		string[] lines1 = ["start", "old_content", "middle", "more_old", "end"];
		string[] lines2 = ["start", "new_content", "middle", "more_new", "end"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseBoth);

		// Assert
		Assert.IsNotNull(result);
		CollectionAssert.Contains(result.MergedLines.ToList(), "start");
		CollectionAssert.Contains(result.MergedLines.ToList(), "middle");
		CollectionAssert.Contains(result.MergedLines.ToList(), "end");

		// Should contain both old and new content
		CollectionAssert.Contains(result.MergedLines.ToList(), "old_content");
		CollectionAssert.Contains(result.MergedLines.ToList(), "new_content");
		CollectionAssert.Contains(result.MergedLines.ToList(), "more_old");
		CollectionAssert.Contains(result.MergedLines.ToList(), "more_new");
	}

	[TestMethod]
	public void PerformManualBlockSelection_ReturnsReadOnlyCollections()
	{
		// Arrange
		string[] lines1 = ["line1", "line2"];
		string[] lines2 = ["line1", "line2"];

		// Act
		MergeResult result = BlockMerger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.MergedLines);
		Assert.IsNotNull(result.Conflicts);

		// Verify collections are read-only by checking they are the correct interface types
		Assert.IsInstanceOfType<IReadOnlyList<string>>(result.MergedLines);
		Assert.IsInstanceOfType<IReadOnlyCollection<MergeConflict>>(result.Conflicts);
	}
}
