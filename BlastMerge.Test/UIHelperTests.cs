// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Cli.Services.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="UIHelper"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class UIHelperTests : ConsoleTestBase
{
	/// <summary>
	/// Each message helper writes its text.
	/// </summary>
	[TestMethod]
	public void MessageHelpers_WriteTheirText()
	{
		UIHelper.ShowError("an error");
		UIHelper.ShowWarning("a warning");
		UIHelper.ShowSuccess("a success");
		UIHelper.ShowInfo("some info");

		StringAssert.Contains(Output, "an error");
		StringAssert.Contains(Output, "a warning");
		StringAssert.Contains(Output, "a success");
		StringAssert.Contains(Output, "some info");
	}

	/// <summary>
	/// Text that looks like markup is printed literally rather than interpreted.
	/// </summary>
	[TestMethod]
	public void MessageHelpers_EscapeMarkup()
	{
		UIHelper.ShowError("[not markup]");

		StringAssert.Contains(Output, "[not markup]");
	}

	/// <summary>
	/// Waiting for a key consumes exactly one key and shows the prompt.
	/// </summary>
	[TestMethod]
	public void WaitForKeyPress_ConsumesOneKey()
	{
		PressAnyKey();

		UIHelper.WaitForKeyPress("Hit a key");

		StringAssert.Contains(Output, "Hit a key");
	}

	/// <summary>
	/// The "and wait" helpers show their message and then wait for a key.
	/// </summary>
	[TestMethod]
	public void AndWaitHelpers_ShowMessageThenWait()
	{
		PressAnyKey();
		PressAnyKey();
		PressAnyKey();

		UIHelper.ShowErrorAndWait("bad thing");
		UIHelper.ShowWarningAndWait("odd thing");
		UIHelper.ShowSuccessAndWait("good thing");

		StringAssert.Contains(Output, "bad thing");
		StringAssert.Contains(Output, "odd thing");
		StringAssert.Contains(Output, "good thing");
		StringAssert.Contains(Output, "Press any key to continue...");
	}

	/// <summary>
	/// A wait with no key queued fails rather than blocking the test run.
	/// </summary>
	[TestMethod]
	public void WaitForKeyPress_WithNoInput_Throws() =>
		Assert.ThrowsExactly<InvalidOperationException>(() => UIHelper.WaitForKeyPress());
}
