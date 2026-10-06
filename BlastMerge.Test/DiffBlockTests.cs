// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for the <see cref="DiffBlock"/> model.
/// </summary>
[TestClass]
public class DiffBlockTests
{
	/// <summary>
	/// A new block keeps its type and starts with no lines, so its last line numbers are zero.
	/// </summary>
	[TestMethod]
	public void NewBlock_IsEmptyWithZeroLastLineNumbers()
	{
		DiffBlock block = new(BlockType.Insert);

		Assert.AreEqual(BlockType.Insert, block.Type);
		Assert.IsEmpty(block.Lines1);
		Assert.IsEmpty(block.Lines2);
		Assert.IsEmpty(block.LineNumbers1);
		Assert.IsEmpty(block.LineNumbers2);
		Assert.AreEqual(0, block.LastLineNumber1);
		Assert.AreEqual(0, block.LastLineNumber2);
	}

	/// <summary>
	/// The last line numbers are the last entries added on each side.
	/// </summary>
	[TestMethod]
	public void LastLineNumbers_AreTheLastEntries()
	{
		DiffBlock block = new(BlockType.Replace);
		block.Lines1.Add("old");
		block.Lines2.Add("new");
		block.LineNumbers1.Add(3);
		block.LineNumbers1.Add(4);
		block.LineNumbers2.Add(7);

		Assert.AreEqual(4, block.LastLineNumber1);
		Assert.AreEqual(7, block.LastLineNumber2);
		Assert.AreEqual("old", block.Lines1[0]);
		Assert.AreEqual("new", block.Lines2[0]);
	}
}
