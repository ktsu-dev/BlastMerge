// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="DiffBlockStatistics"/>.
/// </summary>
[TestClass]
public class DiffBlockStatisticsTests
{
	/// <summary>
	/// The total is the sum of deletions and insertions, and any change counts.
	/// </summary>
	[TestMethod]
	public void TotalChanges_SumsDeletionsAndInsertions()
	{
		DiffBlockStatistics stats = new() { Deletions = 2, Insertions = 3 };

		Assert.AreEqual(5, stats.TotalChanges);
		Assert.IsTrue(stats.HasChanges);
	}

	/// <summary>
	/// Deletions alone or insertions alone still count as changes.
	/// </summary>
	[TestMethod]
	public void HasChanges_TrueForOneSidedChanges()
	{
		Assert.IsTrue(new DiffBlockStatistics { Deletions = 1, Insertions = 0 }.HasChanges);
		Assert.IsTrue(new DiffBlockStatistics { Deletions = 0, Insertions = 1 }.HasChanges);
	}

	/// <summary>
	/// No deletions and no insertions means no changes.
	/// </summary>
	[TestMethod]
	public void HasChanges_FalseWhenNothingChanged()
	{
		DiffBlockStatistics stats = new() { Deletions = 0, Insertions = 0 };

		Assert.AreEqual(0, stats.TotalChanges);
		Assert.IsFalse(stats.HasChanges);
	}
}
