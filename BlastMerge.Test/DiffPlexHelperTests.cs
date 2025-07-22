// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using DiffPlex.Model;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for DiffPlexHelper using dependency injection
/// </summary>
[TestClass]
public class DiffPlexHelperTests : DependencyInjectionTestBase
{
	private DiffPlexHelper _helper = null!;

	protected override void InitializeTestData()
	{
		_helper = GetService<DiffPlexHelper>();
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithIdenticalContent_ReturnsNoBlocks()
	{
		// Arrange
		string content1 = "line1\nline2\nline3";
		string content2 = "line1\nline2\nline3";

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.DiffBlocks.Count);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithDifferentContent_ReturnsDiffBlocks()
	{
		// Arrange
		string content1 = "line1\nline2\nline3";
		string content2 = "line1\nmodified\nline3";

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithEmptyContent_HandlesCorrectly()
	{
		// Arrange
		string content1 = "";
		string content2 = "";

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithOneEmptyContent_HandlesCorrectly()
	{
		// Arrange
		string content1 = "";
		string content2 = "line1\nline2";

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithAddedLines_DetectsAdditions()
	{
		// Arrange
		string content1 = "line1\nline3";
		string content2 = "line1\nline2\nline3";

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithDeletedLines_DetectsDeletions()
	{
		// Arrange
		string content1 = "line1\nline2\nline3";
		string content2 = "line1\nline3";

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithModifiedLines_DetectsModifications()
	{
		// Arrange
		string content1 = "line1\noriginal line\nline3";
		string content2 = "line1\nmodified line\nline3";

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithComplexChanges_HandlesCorrectly()
	{
		// Arrange
		string content1 = "line1\nline2\nline3\nline4";
		string content2 = "line1\nmodified2\nline3\nadded\nline4";

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithSpecialCharacters_PreservesContent()
	{
		// Arrange
		string content1 = "line with\ttabs\nline with \"quotes\"";
		string content2 = "line with\ttabs\nline with \"quotes\"";

		// Act
		var result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(0, result.DiffBlocks.Count); // Should be identical
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithUnicodeCharacters_HandlesCorrectly()
	{
		// Arrange
		string content1 = "héllo wörld\nline2";
		string content2 = "hello world\nline2";

		// Act
		var result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithLargeContent_PerformsEfficiently()
	{
		// Arrange
		string[] lines1 = new string[1000];
		string[] lines2 = new string[1000];

		for (int i = 0; i < 1000; i++)
		{
			lines1[i] = $"line {i}";
			lines2[i] = i == 500 ? "modified line" : $"line {i}"; // Change one line
		}

		string content1 = string.Join("\n", lines1);
		string content2 = string.Join("\n", lines2);

		// Act
		DiffResult result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithWhitespaceOnlyDifferences_DetectsChanges()
	{
		// Arrange
		string content1 = "line1\nline2";
		string content2 = "line1 \nline2"; // Extra space

		// Act
		var result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithDifferentLineEndings_HandlesCorrectly()
	{
		// Arrange
		string content1 = "line1\r\nline2\r\nline3";
		string content2 = "line1\nline2\nline3";

		// Act
		var result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		// Line endings are normalized, so should be no differences
		Assert.AreEqual(0, result.DiffBlocks.Count);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_WithReorderedLines_DetectsReordering()
	{
		// Arrange
		string content1 = "line1\nline2\nline3";
		string content2 = "line1\nline3\nline2";

		// Act
		var result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsTrue(result.DiffBlocks.Count > 0);
	}

	[TestMethod]
	public void CreateLineDiffsFromContent_ReturnsImmutableResult()
	{
		// Arrange
		string content1 = "line1\nline2";
		string content2 = "line1\nmodified";

		// Act
		var result = _helper.CreateLineDiffsFromContent(content1, content2);

		// Assert
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.DiffBlocks);
		Assert.IsNotNull(result.PiecesOld);
		Assert.IsNotNull(result.PiecesNew);
	}
}
