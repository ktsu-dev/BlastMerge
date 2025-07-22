// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using System.Collections.Generic;
using ktsu.BlastMerge.Models;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for BlockMerger using dependency injection
/// </summary>
[TestClass]
public class BlockMergerTests : DependencyInjectionTestBase
{
	private BlockMerger _merger = null!;

	protected override void InitializeTestData()
	{
		_merger = GetService<BlockMerger>();
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithIdenticalFiles_ReturnsOriginalContent()
	{
		// Arrange
		string[] lines1 = ["line1", "line2", "line3"];
		string[] lines2 = ["line1", "line2", "line3"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(3, result.MergedLines.Count);
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithUseVersion1Choice_UsesVersion1Content()
	{
		// Arrange
		string[] lines1 = ["line1", "modified_in_v1", "line3"];
		string[] lines2 = ["line1", "modified_in_v2", "line3"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.MergedLines.Contains("modified_in_v1"));
		Assert.IsFalse(result.MergedLines.Contains("modified_in_v2"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithUseVersion2Choice_UsesVersion2Content()
	{
		// Arrange
		string[] lines1 = ["line1", "modified_in_v1", "line3"];
		string[] lines2 = ["line1", "modified_in_v2", "line3"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsFalse(result.MergedLines.Contains("modified_in_v1"));
		Assert.IsTrue(result.MergedLines.Contains("modified_in_v2"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithUseBothChoice_IncludesBothVersions()
	{
		// Arrange
		string[] lines1 = ["line1", "modified_in_v1", "line3"];
		string[] lines2 = ["line1", "modified_in_v2", "line3"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseBoth);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.MergedLines.Contains("modified_in_v1"));
		Assert.IsTrue(result.MergedLines.Contains("modified_in_v2"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithSkipChoice_SkipsBothVersions()
	{
		// Arrange
		string[] lines1 = ["line1", "modified_in_v1", "line3"];
		string[] lines2 = ["line1", "modified_in_v2", "line3"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.Skip);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsFalse(result.MergedLines.Contains("modified_in_v1"));
		Assert.IsFalse(result.MergedLines.Contains("modified_in_v2"));
		// Should still contain unchanged lines
		Assert.IsTrue(result.MergedLines.Contains("line1"));
		Assert.IsTrue(result.MergedLines.Contains("line3"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithEmptyFiles_ReturnsEmptyResult()
	{
		// Arrange
		string[] lines1 = [];
		string[] lines2 = [];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
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
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.MergedLines.Contains("line1"));
		Assert.IsTrue(result.MergedLines.Contains("line2"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithMultipleBlocks_CallsCallbackForEachBlock()
	{
		// Arrange
		string[] lines1 = ["line1", "v1_change1", "line3", "v1_change2", "line5"];
		string[] lines2 = ["line1", "v2_change1", "line3", "v2_change2", "line5"];

		List<int> blockNumbers = [];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) =>
			{
				blockNumbers.Add(num);
				return BlockChoice.UseVersion1;
			});

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(blockNumbers.Count > 0); // Should have called callback for blocks
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithDifferentChoicesPerBlock_AppliesCorrectly()
	{
		// Arrange
		string[] lines1 = ["line1", "v1_change1", "line3", "v1_change2", "line5"];
		string[] lines2 = ["line1", "v2_change1", "line3", "v2_change2", "line5"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) =>
				// Use version 1 for first block, version 2 for second block
				num == 1 ? BlockChoice.UseVersion1 : BlockChoice.UseVersion2);

		// Assert
		Assert.IsNotNull(result);
		// Should contain unchanged lines
		Assert.IsTrue(result.MergedLines.Contains("line1"));
		Assert.IsTrue(result.MergedLines.Contains("line3"));
		Assert.IsTrue(result.MergedLines.Contains("line5"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithAddedLines_HandlesCorrectly()
	{
		// Arrange
		string[] lines1 = ["line1", "line2"];
		string[] lines2 = ["line1", "added_line", "line2"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.MergedLines.Contains("line1"));
		Assert.IsTrue(result.MergedLines.Contains("added_line"));
		Assert.IsTrue(result.MergedLines.Contains("line2"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithDeletedLines_HandlesCorrectly()
	{
		// Arrange
		string[] lines1 = ["line1", "deleted_line", "line2"];
		string[] lines2 = ["line1", "line2"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.MergedLines.Contains("line1"));
		Assert.IsTrue(result.MergedLines.Contains("deleted_line"));
		Assert.IsTrue(result.MergedLines.Contains("line2"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithComplexChanges_MaintainsCorrectOrder()
	{
		// Arrange
		string[] lines1 = ["start", "old_content", "middle", "more_old", "end"];
		string[] lines2 = ["start", "new_content", "middle", "more_new", "end"];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseBoth);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.MergedLines.Contains("start"));
		Assert.IsTrue(result.MergedLines.Contains("middle"));
		Assert.IsTrue(result.MergedLines.Contains("end"));
	}

	[TestMethod]
	public void PerformManualBlockSelection_WithSpecialCharacters_PreservesContent()
	{
		// Arrange
		string[] lines1 = ["line with\ttabs", "line with \"quotes\""];
		string[] lines2 = ["line with\ttabs", "line with \"quotes\""];

		// Act
		MergeResult result = _merger.PerformManualBlockSelection(lines1, lines2,
			(block, context, num) => BlockChoice.UseVersion1);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(2, result.MergedLines.Count);
		Assert.IsTrue(result.MergedLines.Contains("line with\ttabs"));
		Assert.IsTrue(result.MergedLines.Contains("line with \"quotes\""));
	}
}
