// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Cli.Services.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="NavigationHistory"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class NavigationHistoryTests
{
	/// <summary>
	/// Starts every test from an empty history, since the history is static.
	/// </summary>
	[TestInitialize]
	public void Setup() => NavigationHistory.Clear();

	/// <summary>
	/// Leaves the history empty for whatever runs next.
	/// </summary>
	[TestCleanup]
	public void Cleanup() => NavigationHistory.Clear();

	/// <summary>
	/// Popping and peeking an empty history returns null rather than throwing.
	/// </summary>
	[TestMethod]
	public void EmptyHistory_PopAndPeekReturnNull()
	{
		Assert.IsNull(NavigationHistory.Pop());
		Assert.IsNull(NavigationHistory.Peek());
		Assert.AreEqual(0, NavigationHistory.Count);
	}

	/// <summary>
	/// The history is last in, first out.
	/// </summary>
	[TestMethod]
	public void PushThenPop_ReturnsMenusInReverseOrder()
	{
		NavigationHistory.Push("Main Menu");
		NavigationHistory.Push("Settings");

		Assert.AreEqual(2, NavigationHistory.Count);
		Assert.AreEqual("Settings", NavigationHistory.Peek());
		Assert.AreEqual("Settings", NavigationHistory.Pop());
		Assert.AreEqual("Main Menu", NavigationHistory.Pop());
		Assert.AreEqual(0, NavigationHistory.Count);
	}

	/// <summary>
	/// A null menu name is refused.
	/// </summary>
	[TestMethod]
	public void Push_Null_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => NavigationHistory.Push(null!));

	/// <summary>
	/// With one menu or none, back goes to the main menu.
	/// </summary>
	[TestMethod]
	public void GetBackMenuText_WithAtMostOneEntry_GoesToMainMenu()
	{
		Assert.AreEqual("🔙 Back to Main Menu", NavigationHistory.GetBackMenuText());

		NavigationHistory.Push("Settings");
		Assert.AreEqual("🔙 Back to Main Menu", NavigationHistory.GetBackMenuText());
	}

	/// <summary>
	/// Back names the menu below the current one, not the current one.
	/// </summary>
	[TestMethod]
	public void GetBackMenuText_NamesTheMenuBelowTheCurrentOne()
	{
		NavigationHistory.Push("Main Menu");
		NavigationHistory.Push("Settings");
		NavigationHistory.Push("Help");

		Assert.AreEqual("🔙 Back to Settings", NavigationHistory.GetBackMenuText());
	}

	/// <summary>
	/// The main menu below the current one is named the same way as an empty history.
	/// </summary>
	[TestMethod]
	public void GetBackMenuText_WithMainMenuBelow_GoesToMainMenu()
	{
		NavigationHistory.Push("Main Menu");
		NavigationHistory.Push("Settings");

		Assert.AreEqual("🔙 Back to Main Menu", NavigationHistory.GetBackMenuText());
	}

	/// <summary>
	/// Only an empty history, or the main menu on top, means back goes to the main menu.
	/// </summary>
	[TestMethod]
	public void ShouldGoToMainMenu_ReflectsTheTopOfTheHistory()
	{
		Assert.IsTrue(NavigationHistory.ShouldGoToMainMenu());

		NavigationHistory.Push("Main Menu");
		Assert.IsTrue(NavigationHistory.ShouldGoToMainMenu());

		NavigationHistory.Push("Settings");
		Assert.IsFalse(NavigationHistory.ShouldGoToMainMenu());
	}
}
