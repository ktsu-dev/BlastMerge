// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="MergeResult.IsFullyResolved"/>.
/// </summary>
[TestClass]
public class MergeResultTests
{
	/// <summary>
	/// A merge with no conflicts is fully resolved, and the two-argument constructor uses the platform line ending.
	/// </summary>
	[TestMethod]
	public void NoConflicts_IsFullyResolved()
	{
		MergeResult result = new(["a"], []);

		Assert.IsTrue(result.IsFullyResolved);
		Assert.AreEqual(Environment.NewLine, result.LineEnding);
	}

	/// <summary>
	/// An unresolved conflict means the merge is not fully resolved, until it is resolved.
	/// </summary>
	[TestMethod]
	public void UnresolvedConflict_IsNotFullyResolvedUntilResolved()
	{
		MergeConflict conflict = new(1, "left", "right", null, false);
		MergeConflict resolved = new(2, "x", "y", "x", true);
		MergeResult result = new(["a"], [resolved, conflict]);

		Assert.IsFalse(result.IsFullyResolved);

		conflict.IsResolved = true;
		conflict.ResolvedContent = "left";

		Assert.IsTrue(result.IsFullyResolved);
	}
}
