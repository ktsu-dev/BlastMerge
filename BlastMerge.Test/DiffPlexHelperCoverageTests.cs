// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers <see cref="DiffPlexHelper.FormatDiffStatistics"/>.
/// </summary>
[TestClass]
public class DiffPlexHelperCoverageTests
{
	/// <summary>
	/// All three counts are shown in order with their signs.
	/// </summary>
	[TestMethod]
	public void FormatDiffStatistics_AllCounts_AreJoinedInOrder()
	{
		Assert.AreEqual("+3 -2 ~1", DiffPlexHelper.FormatDiffStatistics(3, 2, 1));
	}

	/// <summary>
	/// Zero counts are left out entirely.
	/// </summary>
	[TestMethod]
	public void FormatDiffStatistics_ZeroCounts_AreOmitted()
	{
		Assert.AreEqual(string.Empty, DiffPlexHelper.FormatDiffStatistics(0, 0, 0));
		Assert.AreEqual("-2", DiffPlexHelper.FormatDiffStatistics(0, 2, 0));
		Assert.AreEqual("+1 ~4", DiffPlexHelper.FormatDiffStatistics(1, 0, 4));
		Assert.AreEqual("~5", DiffPlexHelper.FormatDiffStatistics(0, 0, 5));
	}
}
