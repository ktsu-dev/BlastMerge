// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the markup-safe display path of <see cref="WhitespaceVisualizer"/>, and the plain display
/// path for a line with nothing trailing.
/// </summary>
[TestClass]
public class WhitespaceVisualizerCoverageTests
{
	/// <summary>
	/// With both options on, a line with no trailing whitespace only has its inner whitespace made visible.
	/// </summary>
	[TestMethod]
	public void ProcessLineForDisplay_NoTrailingWhitespace_OnlyShowsInnerWhitespace()
	{
		string result = WhitespaceVisualizer.ProcessLineForDisplay("a b\tc", showWhitespace: true, highlightTrailing: true);

		Assert.AreEqual("a·b→c", result);
	}

	/// <summary>
	/// The markup path escapes the line and highlights trailing whitespace with escaped content.
	/// </summary>
	[TestMethod]
	public void ProcessLineForMarkupDisplay_BothOptions_EscapesAndHighlightsTrailing()
	{
		string result = WhitespaceVisualizer.ProcessLineForMarkupDisplay("[a] b \t", showWhitespace: true, highlightTrailing: true);

		Assert.AreEqual("[[a]]·b[on red]·→[/]", result);
	}

	/// <summary>
	/// Highlighting without visible whitespace leaves the leading part's spaces alone but still marks the trailing part.
	/// </summary>
	[TestMethod]
	public void ProcessLineForMarkupDisplay_HighlightOnly_LeavesInnerSpacesPlain()
	{
		string result = WhitespaceVisualizer.ProcessLineForMarkupDisplay("a b  ", showWhitespace: false, highlightTrailing: true);

		Assert.AreEqual("a b[on red]··[/]", result);
	}

	/// <summary>
	/// A line made only of whitespace is highlighted in full, with nothing before it.
	/// </summary>
	[TestMethod]
	public void ProcessLineForMarkupDisplay_WhitespaceOnlyLine_IsEntirelyHighlighted()
	{
		string result = WhitespaceVisualizer.ProcessLineForMarkupDisplay(" \t ", showWhitespace: true, highlightTrailing: true);

		Assert.AreEqual("[on red]·→·[/]", result);
	}

	/// <summary>
	/// A line with no trailing whitespace is escaped and, when asked, its whitespace made visible.
	/// </summary>
	[TestMethod]
	public void ProcessLineForMarkupDisplay_NoTrailingWhitespace_IsEscapedWithoutHighlight()
	{
		Assert.AreEqual("x·[[y]]", WhitespaceVisualizer.ProcessLineForMarkupDisplay("x [y]", showWhitespace: true, highlightTrailing: true));
		Assert.AreEqual("x [[y]]", WhitespaceVisualizer.ProcessLineForMarkupDisplay("x [y]", showWhitespace: false, highlightTrailing: true));
	}

	/// <summary>
	/// Without trailing highlighting every whitespace character is made visible and the result is escaped.
	/// </summary>
	[TestMethod]
	public void ProcessLineForMarkupDisplay_WhitespaceOnly_MakesAllWhitespaceVisible()
	{
		string result = WhitespaceVisualizer.ProcessLineForMarkupDisplay("[b] c ", showWhitespace: true, highlightTrailing: false);

		Assert.AreEqual("[[b]]·c·", result);
	}
}
