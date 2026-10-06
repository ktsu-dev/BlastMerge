// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="RecentBatchInfo"/>.
/// </summary>
[TestClass]
public class RecentBatchInfoTests
{
	/// <summary>
	/// A new instance has an empty name and the default timestamp.
	/// </summary>
	[TestMethod]
	public void Defaults_AreEmpty()
	{
		RecentBatchInfo info = new();

		Assert.AreEqual(string.Empty, info.BatchName);
		Assert.AreEqual(default, info.LastUsed);
	}

	/// <summary>
	/// The name and timestamp hold what was set.
	/// </summary>
	[TestMethod]
	public void Properties_HoldAssignedValues()
	{
		DateTime when = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

		RecentBatchInfo info = new() { BatchName = "Sync", LastUsed = when };

		Assert.AreEqual("Sync", info.BatchName);
		Assert.AreEqual(when, info.LastUsed);
	}
}
