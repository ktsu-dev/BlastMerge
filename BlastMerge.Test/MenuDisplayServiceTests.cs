// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Cli.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="MenuDisplayService"/>.
/// </summary>
/// <remarks>
/// Whether the ASCII-art or the plain title is drawn depends on the width of the real console
/// window, which a test cannot set, so these tests assert the content both variants share.
/// </remarks>
[TestClass]
[DoNotParallelize]
public class MenuDisplayServiceTests : ConsoleTestBase
{
	/// <summary>
	/// The welcome screen shows the welcome panel, the feature list and the navigation help.
	/// </summary>
	[TestMethod]
	public void ShowWelcomeScreen_ShowsWelcomePanel()
	{
		MenuDisplayService.ShowWelcomeScreen();

		StringAssert.Contains(Output, "WELCOME TO BLASTMERGE");
		StringAssert.Contains(Output, "CROSS-REPOSITORY FILE SYNCHRONIZATION WEAPON");
		StringAssert.Contains(Output, "Intelligent iterative merging across repositories");
		StringAssert.Contains(Output, "Interactive conflict resolution TUI");
		StringAssert.Contains(Output, "Navigate:");
		Assert.DoesNotContain("DEMOLITION COMPLETE", Output);
	}

	/// <summary>
	/// The welcome screen draws either the ASCII-art title or the plain title rule.
	/// </summary>
	[TestMethod]
	public void ShowWelcomeScreen_DrawsATitle()
	{
		MenuDisplayService.ShowWelcomeScreen();

		Assert.IsTrue(
			Output.Contains("BLAST MERGE", System.StringComparison.Ordinal) || Output.Contains("██████", System.StringComparison.Ordinal),
			$"Expected a title, got: {Output}");
	}

	/// <summary>
	/// The goodbye screen shows the goodbye panel, the project link and the exit message.
	/// </summary>
	[TestMethod]
	public void ShowGoodbyeScreen_ShowsGoodbyePanel()
	{
		MenuDisplayService.ShowGoodbyeScreen();

		StringAssert.Contains(Output, "DEMOLITION COMPLETE");
		StringAssert.Contains(Output, "DETONATION SUCCESSFUL");
		StringAssert.Contains(Output, "https://github.com/ktsu-dev/BlastMerge");
		StringAssert.Contains(Output, "May your merges always go out with a BANG!");
		StringAssert.Contains(Output, "Exiting in 3... 2... 1...");
		Assert.DoesNotContain("WELCOME TO BLASTMERGE", Output);
	}

	/// <summary>
	/// The goodbye screen draws either the ASCII-art title or the plain title rule.
	/// </summary>
	[TestMethod]
	public void ShowGoodbyeScreen_DrawsATitle()
	{
		MenuDisplayService.ShowGoodbyeScreen();

		Assert.IsTrue(
			Output.Contains("GOODBYE", System.StringComparison.Ordinal) || Output.Contains("██████", System.StringComparison.Ordinal),
			$"Expected a title, got: {Output}");
	}
}
