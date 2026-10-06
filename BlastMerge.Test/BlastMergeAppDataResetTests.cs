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
	/// After a reset the next <c>Get</c> loads a new instance rather than returning the cached one.
	/// </summary>
	[TestMethod]
	public void ResetForTesting_DiscardsTheCachedInstance()
	{
		BlastMergeAppData before = BlastMergeAppData.Get();
		before.InputHistory["prompt"] = ["unsaved"];

		BlastMergeAppData.ResetForTesting();
		BlastMergeAppData after = BlastMergeAppData.Get();

		Assert.AreNotSame(before, after);
		Assert.IsFalse(after.InputHistory.ContainsKey("prompt"));
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
