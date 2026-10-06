// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using System;
using System.Globalization;
using System.Linq;
using ktsu.BlastMerge.Cli.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="UserInteractionService"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class UserInteractionServiceTests : ConsoleTestBase
{
	/// <summary>
	/// Answering yes to the continue prompt continues.
	/// </summary>
	[TestMethod]
	public void ConfirmContinueMerge_Yes_ReturnsTrue()
	{
		Input.PushTextWithEnter("y");

		Assert.IsTrue(UserInteractionService.ConfirmContinueMerge());
		StringAssert.Contains(Output, "Continue with next merge?");
	}

	/// <summary>
	/// Answering no to the continue prompt stops.
	/// </summary>
	[TestMethod]
	public void ConfirmContinueMerge_No_ReturnsFalse()
	{
		Input.PushTextWithEnter("n");

		Assert.IsFalse(UserInteractionService.ConfirmContinueMerge());
	}

	/// <summary>
	/// The continue prompt shows the default message and consumes one key.
	/// </summary>
	[TestMethod]
	public void PressAnyKeyToContinue_DefaultMessage_ConsumesOneKey()
	{
		PressAnyKey();

		UserInteractionService.PressAnyKeyToContinue();

		StringAssert.Contains(Output, "Press any key to continue...");
		Assert.ThrowsExactly<InvalidOperationException>(() => UserInteractionService.PressAnyKeyToContinue());
	}

	/// <summary>
	/// A custom continue message is written verbatim, markup and all.
	/// </summary>
	[TestMethod]
	public void PressAnyKeyToContinue_CustomMessage_WrittenVerbatim()
	{
		PressAnyKey();

		UserInteractionService.PressAnyKeyToContinue("[red]Hit it[/]");

		StringAssert.Contains(Output, "[red]Hit it[/]");
	}

	/// <summary>
	/// A null continue message is rejected.
	/// </summary>
	[TestMethod]
	public void PressAnyKeyToContinue_NullMessage_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => UserInteractionService.PressAnyKeyToContinue(null!));

	/// <summary>
	/// A confirmation returns the user's answer and shows the prompt.
	/// </summary>
	[TestMethod]
	public void Confirm_ReturnsAnswer()
	{
		Input.PushTextWithEnter("y");
		Input.PushTextWithEnter("n");

		Assert.IsTrue(UserInteractionService.Confirm("Really do it?"));
		Assert.IsFalse(UserInteractionService.Confirm("Really do it?"));
		StringAssert.Contains(Output, "Really do it?");
	}

	/// <summary>
	/// A null confirmation prompt is rejected.
	/// </summary>
	[TestMethod]
	public void Confirm_NullPrompt_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => UserInteractionService.Confirm(null!));

	/// <summary>
	/// The selection prompt returns the choice the user moved to.
	/// </summary>
	[TestMethod]
	public void ShowSelectionPrompt_ReturnsSelectedChoice()
	{
		SelectIndex(1);

		string result = UserInteractionService.ShowSelectionPrompt("Pick one", ["alpha", "beta", "gamma"]);

		Assert.AreEqual("beta", result);
		StringAssert.Contains(Output, "Pick one");
	}

	/// <summary>
	/// A list longer than one page can still be scrolled to its last choice.
	/// </summary>
	[TestMethod]
	public void ShowSelectionPrompt_LongList_ScrollsToLastChoice()
	{
		string[] choices = [.. Enumerable.Range(1, 15).Select(i => "choice " + i.ToString(CultureInfo.InvariantCulture))];
		SelectIndex(14);

		string result = UserInteractionService.ShowSelectionPrompt("Pick one", choices);

		Assert.AreEqual("choice 15", result);
		StringAssert.Contains(Output, "Move up and down to reveal more options");
	}

	/// <summary>
	/// A single choice is still selectable even though the page size has a floor of three.
	/// </summary>
	[TestMethod]
	public void ShowSelectionPrompt_SingleChoice_ReturnsIt()
	{
		SelectIndex(0);

		Assert.AreEqual("only", UserInteractionService.ShowSelectionPrompt("Pick one", ["only"]));
	}

	/// <summary>
	/// A null title or choice list is rejected.
	/// </summary>
	[TestMethod]
	public void ShowSelectionPrompt_NullArguments_Throw()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => UserInteractionService.ShowSelectionPrompt(null!, ["a"]));
		Assert.ThrowsExactly<ArgumentNullException>(() => UserInteractionService.ShowSelectionPrompt("title", null!));
	}
}
