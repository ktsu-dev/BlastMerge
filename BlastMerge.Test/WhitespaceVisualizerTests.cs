// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for WhitespaceVisualizer using dependency injection
/// </summary>
[TestClass]
public class WhitespaceVisualizerTests : DependencyInjectionTestBase
{
	private WhitespaceVisualizer _visualizer = null!;

	protected override void InitializeTestData()
	{
		_visualizer = GetService<WhitespaceVisualizer>();
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithTabsAndSpaces_ShowsCorrectSymbols()
	{
		// Arrange
		string input = "Hello\tWorld    End";

		// Act
		string result = _visualizer.MakeWhitespaceVisible(input, showSpaces: true, showTabs: true, showLineEndings: true);

		// Assert
		Assert.IsTrue(result.Contains("→"));
		Assert.IsTrue(result.Contains("·"));
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithNewlines_ShowsCorrectSymbols()
	{
		// Arrange
		string input = "Line1\nLine2\r\nLine3";

		// Act
		string result = _visualizer.MakeWhitespaceVisible(input, showSpaces: true, showTabs: true, showLineEndings: true);

		// Assert
		Assert.IsTrue(result.Contains("¶"));
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithMixedWhitespace_ShowsAllSymbols()
	{
		// Arrange
		string input = "Text\t    \r\nMore";

		// Act
		string result = _visualizer.MakeWhitespaceVisible(input, showSpaces: true, showTabs: true, showLineEndings: true);

		// Assert
		Assert.IsTrue(result.Contains("→"));
		Assert.IsTrue(result.Contains("·"));
		Assert.IsTrue(result.Contains("¶"));
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithTabsDisabled_DoesNotShowTabs()
	{
		// Arrange
		string input = "Hello\tWorld";

		// Act
		string result = _visualizer.MakeWhitespaceVisible(input, showSpaces: true, showTabs: false, showLineEndings: true);

		// Assert
		Assert.IsFalse(result.Contains("→"));
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithSpacesDisabled_DoesNotShowSpaces()
	{
		// Arrange
		string input = "Hello World";

		// Act
		string result = _visualizer.MakeWhitespaceVisible(input, showSpaces: false, showTabs: true, showLineEndings: true);

		// Assert
		Assert.IsFalse(result.Contains("·"));
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithNewlinesDisabled_DoesNotShowNewlines()
	{
		// Arrange
		string input = "Line1\nLine2";

		// Act
		string result = _visualizer.MakeWhitespaceVisible(input, showSpaces: true, showTabs: true, showLineEndings: false);

		// Assert
		Assert.IsFalse(result.Contains("¶"));
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithEmptyString_ReturnsEmptyString()
	{
		// Arrange
		string input = "";

		// Act
		string result = _visualizer.MakeWhitespaceVisible(input, showSpaces: true, showTabs: true, showLineEndings: true);

		// Assert
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void MakeWhitespaceVisible_WithNullString_ReturnsEmptyString()
	{
		// Arrange
		string? input = null;

		// Act
		string result = _visualizer.MakeWhitespaceVisible(input, showSpaces: true, showTabs: true, showLineEndings: true);

		// Assert
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithTrailingSpaces_HighlightsCorrectly()
	{
		// Arrange
		string input = "Hello World    ";

		// Act
		string result = _visualizer.HighlightTrailingWhitespace(input);

		// Assert
		Assert.IsTrue(result.Contains("    "));
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithTrailingTabs_HighlightsCorrectly()
	{
		// Arrange
		string input = "Hello World\t\t";

		// Act
		string result = _visualizer.HighlightTrailingWhitespace(input);

		// Assert
		Assert.IsTrue(result.Contains("\t\t"));
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithNoTrailingWhitespace_ReturnsOriginal()
	{
		// Arrange
		string input = "Hello World";

		// Act
		string result = _visualizer.HighlightTrailingWhitespace(input);

		// Assert
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithMixedTrailingWhitespace_HighlightsAll()
	{
		// Arrange
		string input = "Hello World \t ";

		// Act
		string result = _visualizer.HighlightTrailingWhitespace(input);

		// Assert
		Assert.IsTrue(result.Contains(" \t "));
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithOnlyWhitespace_HighlightsAll()
	{
		// Arrange
		string input = "   \t  ";

		// Act
		string result = _visualizer.HighlightTrailingWhitespace(input);

		// Assert
		Assert.IsTrue(result.Contains("   \t  "));
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithEmptyString_ReturnsEmptyString()
	{
		// Arrange
		string input = "";

		// Act
		string result = _visualizer.HighlightTrailingWhitespace(input);

		// Assert
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void HighlightTrailingWhitespace_WithNullString_ReturnsEmptyString()
	{
		// Arrange
		string? input = null;

		// Act
		string result = _visualizer.HighlightTrailingWhitespace(input);

		// Assert
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void CreateWhitespaceLegend_ReturnsValidLegend()
	{
		// Act
		string legend = _visualizer.CreateWhitespaceLegend();

		// Assert
		Assert.IsNotNull(legend);
		Assert.IsTrue(legend.Contains("→"));
		Assert.IsTrue(legend.Contains("·"));
		Assert.IsTrue(legend.Contains("¶"));
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithWhitespace_ProcessesCorrectly()
	{
		// Arrange
		string input = "Hello\tWorld    ";

		// Act
		string result = _visualizer.ProcessLineForDisplay(input, showWhitespace: true, highlightTrailing: false);

		// Assert
		Assert.IsTrue(result.Contains("→"));
		Assert.IsTrue(result.Contains("·"));
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithNullInput_ReturnsEmptyString()
	{
		// Arrange
		string? input = null;

		// Act
		string result = _visualizer.ProcessLineForDisplay(input, showWhitespace: true, highlightTrailing: false);

		// Assert
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithEmptyInput_ReturnsEmptyString()
	{
		// Arrange
		string input = "";

		// Act
		string result = _visualizer.ProcessLineForDisplay(input, showWhitespace: true, highlightTrailing: false);

		// Assert
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithWhitespaceDisabled_DoesNotShowWhitespace()
	{
		// Arrange
		string input = "Hello\tWorld";

		// Act
		string result = _visualizer.ProcessLineForDisplay(input, showWhitespace: false, highlightTrailing: false);

		// Assert
		Assert.IsFalse(result.Contains("→"));
		Assert.AreEqual(input, result);
	}

	[TestMethod]
	public void ProcessLineForDisplay_WithTrailingHighlight_HighlightsTrailing()
	{
		// Arrange
		string input = "Hello World  ";

		// Act
		string result = _visualizer.ProcessLineForDisplay(input, showWhitespace: false, highlightTrailing: true);

		// Assert
		Assert.IsTrue(result.Contains("Hello World"));
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithWhitespace_ProcessesCorrectly()
	{
		// Arrange
		string input = "Hello\tWorld    ";

		// Act
		string result = _visualizer.ProcessLineForMarkupDisplay(input, showWhitespace: true, highlightTrailing: false);

		// Assert
		Assert.IsTrue(result.Contains("→"));
		Assert.IsTrue(result.Contains("·"));
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithNullInput_ReturnsEmptyString()
	{
		// Arrange
		string? input = null;

		// Act
		string result = _visualizer.ProcessLineForMarkupDisplay(input, showWhitespace: true, highlightTrailing: false);

		// Assert
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithEmptyInput_ReturnsEmptyString()
	{
		// Arrange
		string input = "";

		// Act
		string result = _visualizer.ProcessLineForMarkupDisplay(input, showWhitespace: true, highlightTrailing: false);

		// Assert
		Assert.AreEqual("", result);
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithWhitespaceDisabled_DoesNotShowWhitespace()
	{
		// Arrange
		string input = "Hello\tWorld";

		// Act
		string result = _visualizer.ProcessLineForMarkupDisplay(input, showWhitespace: false, highlightTrailing: false);

		// Assert
		Assert.IsFalse(result.Contains("→"));
	}

	[TestMethod]
	public void ProcessLineForMarkupDisplay_WithTrailingHighlight_HighlightsTrailing()
	{
		// Arrange
		string input = "Hello World  ";

		// Act
		string result = _visualizer.ProcessLineForMarkupDisplay(input, showWhitespace: false, highlightTrailing: true);

		// Assert
		Assert.IsTrue(result.Contains("Hello World"));
	}
}
