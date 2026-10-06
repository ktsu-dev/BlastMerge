// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="BlastMergeAppData.ResetForTesting"/>, which every console test relies on for isolation.
/// </summary>
[TestClass]
[DoNotParallelize]
public class BlastMergeAppDataResetTests : ConsoleTestBase
{
	/// <summary>
	/// A reset empties the cached instance and restores its default settings.
	/// </summary>
	[TestMethod]
	public void ResetForTesting_EmptiesTheCachedInstance()
	{
		BlastMergeAppData before = BlastMergeAppData.Get();
		before.InputHistory["prompt"] = ["unsaved"];
		before.BatchConfigurations["batch"] = new BatchConfiguration { Name = "batch" };
		before.RecentBatch = new RecentBatchInfo { BatchName = "batch" };
		before.Settings.MaxHistoryEntriesPerPrompt = 3;

		BlastMergeAppData.ResetForTesting();
		BlastMergeAppData after = BlastMergeAppData.Get();

		Assert.IsEmpty(after.InputHistory);
		Assert.IsEmpty(after.BatchConfigurations);
		Assert.IsNull(after.RecentBatch);
		Assert.AreEqual(new ApplicationSettings().MaxHistoryEntriesPerPrompt, after.Settings.MaxHistoryEntriesPerPrompt);
	}

	/// <summary>
	/// The instance loaded after a reset is cached again until the next reset.
	/// </summary>
	[TestMethod]
	public void ResetForTesting_LeavesASingletonBehind()
	{
		BlastMergeAppData.ResetForTesting();

		Assert.AreSame(BlastMergeAppData.Get(), BlastMergeAppData.Get());
	}

	/// <summary>
	/// Resetting before anything has been loaded is harmless.
	/// </summary>
	[TestMethod]
	public void ResetForTesting_IsRepeatable()
	{
		BlastMergeAppData.ResetForTesting();
		BlastMergeAppData.ResetForTesting();

		Assert.IsNotNull(BlastMergeAppData.Get());
	}
}
