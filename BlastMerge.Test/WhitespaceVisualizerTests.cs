// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class WhitespaceVisualizerTests
{
	[TestMethod]
	public void MakeWhitespaceVisible_WithNullText_ReturnsEmptyString()
	{
		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(null);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithEmptyText_ReturnsEmptyString()
	{
		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(string.Empty);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithSpaces_ReplacesSpacesWithMiddleDot()
	{
		// Arrange
		string text = "hello world";

		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(text, showSpaces: true);

		// Assert
		Assert.AreEqual("hello·world", result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithTabs_ReplacesTabsWithRightArrow()
	{
		// Arrange
		string text = "hello\tworld";

		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(text, showTabs: true);

		// Assert
		Assert.AreEqual("hello→world", result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithCarriageReturn_ReplacesWithReturnSymbol()
	{
		// Arrange
		string text = "hello\rworld";

		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(text, showLineEndings: true);

		// Assert
		Assert.AreEqual("hello↵world", result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithNewline_ReplacesWithPilcrow()
	{
		// Arrange
		string text = "hello\nworld";

		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(text, showLineEndings: true);

		// Assert
		Assert.AreEqual("hello¶world", result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithAllWhitespaceTypes_ReplacesAll()
	{
		// Arrange
		string text = "hello \t\r\nworld";

		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(text);

		// Assert
		Assert.AreEqual("hello·→↵¶world", result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithShowSpacesFalse_DoesNotReplaceSpaces()
	{
		// Arrange
		string text = "hello world";

		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(text, showSpaces: false);

		// Assert
		Assert.AreEqual("hello world", result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithShowTabsFalse_DoesNotReplaceTabs()
	{
		// Arrange
		string text = "hello\tworld";

		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(text, showTabs: false);

		// Assert
		Assert.AreEqual("hello\tworld", result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithShowLineEndingsFalse_DoesNotReplaceLineEndings()
	{
		// Arrange
		string text = "hello\r\nworld";

		// Act
		string result = WhitespaceVisualizer.MakeWhitespaceVisible(text, showLineEndings: false);

		// Assert
		Assert.AreEqual("hello\r\nworld", result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithNullText_ReturnsEmptyString()
	{
		// Act
		string result = WhitespaceVisualizer.HighlightTrailingWhitespace(null);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithEmptyText_ReturnsEmptyString()
	{
		// Act
		string result = WhitespaceVisualizer.HighlightTrailingWhitespace(string.Empty);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithNoTrailingWhitespace_ReturnsOriginalText()
	{
		// Arrange
		string text = "hello world";

		// Act
		string result = WhitespaceVisualizer.HighlightTrailingWhitespace(text);

		// Assert
		Assert.AreEqual("hello world", result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithTrailingSpaces_HighlightsTrailing()
	{
		// Arrange
		string text = "hello world  ";

		// Act
		string result = WhitespaceVisualizer.HighlightTrailingWhitespace(text);

		// Assert
		Assert.AreEqual("hello world[on red]··[/]", result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithTrailingTabs_HighlightsTrailing()
	{
		// Arrange
		string text = "hello world\t\t";

		// Act
		string result = WhitespaceVisualizer.HighlightTrailingWhitespace(text);

		// Assert
		Assert.AreEqual("hello world[on red]→→[/]", result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithMixedTrailingWhitespace_HighlightsAll()
	{
		// Arrange
		string text = "hello world \t ";

		// Act
		string result = WhitespaceVisualizer.HighlightTrailingWhitespace(text);

		// Assert
		Assert.AreEqual("hello world[on red]·→·[/]", result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithOnlyWhitespace_HighlightsAll()
	{
		// Arrange
		string text = "   ";

		// Act
		string result = WhitespaceVisualizer.HighlightTrailingWhitespace(text);

		// Assert
		Assert.AreEqual("[on red]···[/]", result);
	}

	[TestMethod]
	public void CreateWhitespaceLegend_ReturnsFormattedLegend()
	{
		// Act
		string result = WhitespaceVisualizer.CreateWhitespaceLegend();

		// Assert
		Assert.IsTrue(result.Contains("· = space"));
		Assert.IsTrue(result.Contains("→ = tab"));
		Assert.IsTrue(result.Contains("↵ = return"));
		Assert.IsTrue(result.Contains("¶ = newline"));
		Assert.IsTrue(result.Contains("red background = trailing whitespace"));
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithNullLine_ReturnsEmptyString()
	{
		// Act
		string result = WhitespaceVisualizer.ProcessLineForDisplay(null);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithEmptyLine_ReturnsEmptyString()
	{
		// Act
		string result = WhitespaceVisualizer.ProcessLineForDisplay(string.Empty);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithNoWhitespaceAndNoHighlighting_ReturnsOriginal()
	{
		// Arrange
		string line = "hello world";

		// Act
		string result = WhitespaceVisualizer.ProcessLineForDisplay(line, showWhitespace: false, highlightTrailing: false);

		// Assert
		Assert.AreEqual("hello world", result);
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithWhitespaceEnabled_ShowsWhitespace()
	{
		// Arrange
		string line = "hello world\t";

		// Act
		string result = WhitespaceVisualizer.ProcessLineForDisplay(line, showWhitespace: true, highlightTrailing: false);

		// Assert
		Assert.AreEqual("hello·world→", result);
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithTrailingHighlightEnabled_HighlightsTrailing()
	{
		// Arrange
		string line = "hello world  ";

		// Act
		string result = WhitespaceVisualizer.ProcessLineForDisplay(line, showWhitespace: false, highlightTrailing: true);

		// Assert
		Assert.AreEqual("hello world[on red]··[/]", result);
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithBothEnabled_ShowsWhitespaceAndHighlightsTrailing()
	{
		// Arrange
		string line = "hello world  ";

		// Act
		string result = WhitespaceVisualizer.ProcessLineForDisplay(line, showWhitespace: true, highlightTrailing: true);

		// Assert
		Assert.IsTrue(result.Contains("hello·world"));
		Assert.IsTrue(result.Contains("[on red]"));
		Assert.IsTrue(result.Contains("[/]"));
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithNullLine_ReturnsEmptyString()
	{
		// Act
		string result = WhitespaceVisualizer.ProcessLineForMarkupDisplay(null);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithEmptyLine_ReturnsEmptyString()
	{
		// Act
		string result = WhitespaceVisualizer.ProcessLineForMarkupDisplay(string.Empty);

		// Assert
		Assert.AreEqual(string.Empty, result);
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithNoWhitespaceAndNoHighlighting_ReturnsEscapedText()
	{
		// Arrange
		string line = "hello [world]";

		// Act
		string result = WhitespaceVisualizer.ProcessLineForMarkupDisplay(line, showWhitespace: false, highlightTrailing: false);

		// Assert
		Assert.AreEqual("hello [[world]]", result); // Markup should be escaped
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithSpecialCharacters_EscapesMarkup()
	{
		// Arrange
		string line = "text with [brackets] and <tags>";

		// Act
		string result = WhitespaceVisualizer.ProcessLineForMarkupDisplay(line, showWhitespace: false, highlightTrailing: false);

		// Assert
		Assert.IsTrue(result.Contains("[[brackets]]")); // Brackets should be escaped
	}
}
