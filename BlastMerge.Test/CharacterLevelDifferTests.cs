// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class CharacterLevelDifferTests
{
	[TestMethod]
	public void CreateCharacterLevelDiff_WithNullOldText_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			CharacterLevelDiffer.CreateCharacterLevelDiff(null!, "new text"));
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithNullNewText_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			CharacterLevelDiffer.CreateCharacterLevelDiff("old text", null!));
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithIdenticalText_ReturnsUnchangedText()
	{
		// Arrange
		string text = "hello world";

		// Act
		(string highlightedOld, string highlightedNew) = CharacterLevelDiffer.CreateCharacterLevelDiff(text, text);

		// Assert
		Assert.IsTrue(highlightedOld.Contains("[dim white]"), "Highlighted old text should contain dim white markup for unchanged text");
		Assert.IsTrue(highlightedNew.Contains("[dim white]"), "Highlighted new text should contain dim white markup for unchanged text");
		// Each character is individually wrapped in markup tags, so check for individual characters
		Assert.IsTrue(highlightedOld.Contains('h'), "Highlighted old text should contain character 'h'");
		Assert.IsTrue(highlightedOld.Contains('e'), "Highlighted old text should contain character 'e'");
		Assert.IsTrue(highlightedOld.Contains('l'), "Highlighted old text should contain character 'l'");
		Assert.IsTrue(highlightedOld.Contains('o'), "Highlighted old text should contain character 'o'");
		Assert.IsTrue(highlightedNew.Contains('h'), "Highlighted new text should contain character 'h'");
		Assert.IsTrue(highlightedNew.Contains('e'), "Highlighted new text should contain character 'e'");
		Assert.IsTrue(highlightedNew.Contains('l'), "Highlighted new text should contain character 'l'");
		Assert.IsTrue(highlightedNew.Contains('o'), "Highlighted new text should contain character 'o'");
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithCompletelyDifferentText_HighlightsAllChanges()
	{
		// Arrange
		string oldText = "abc";
		string newText = "xyz";

		// Act
		(string highlightedOld, string highlightedNew) = CharacterLevelDiffer.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		Assert.IsTrue(highlightedOld.Contains("[red]"), "Highlighted old text should contain red markup for deleted characters");
		Assert.IsTrue(highlightedNew.Contains("[green]"), "Highlighted new text should contain green markup for added characters");
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithPartialChanges_HighlightsOnlyChangedParts()
	{
		// Arrange
		string oldText = "hello world";
		string newText = "hello earth";

		// Act
		(string highlightedOld, string highlightedNew) = CharacterLevelDiffer.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		// Should have unchanged "hello " at the beginning
		Assert.IsTrue(highlightedOld.Contains("[dim white]h[/]"), "Highlighted old text should contain unchanged 'h' with dim white markup");
		Assert.IsTrue(highlightedNew.Contains("[dim white]h[/]"), "Highlighted new text should contain unchanged 'h' with dim white markup");

		// Should have changes for "world" vs "earth"
		Assert.IsTrue(highlightedOld.Contains("[red]"), "Highlighted old text should contain red markup for changed portion");
		Assert.IsTrue(highlightedNew.Contains("[green]"), "Highlighted new text should contain green markup for changed portion");
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithEmptyStrings_ReturnsEmptyResults()
	{
		// Act
		(string highlightedOld, string highlightedNew) = CharacterLevelDiffer.CreateCharacterLevelDiff("", "");

		// Assert
		Assert.AreEqual("", highlightedOld);
		Assert.AreEqual("", highlightedNew);
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithOneEmptyString_HighlightsAppropriately()
	{
		// Arrange
		string oldText = "";
		string newText = "hello";

		// Act
		(string highlightedOld, string highlightedNew) = CharacterLevelDiffer.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		Assert.AreEqual("", highlightedOld);
		Assert.IsTrue(highlightedNew.Contains("[green]"), "Highlighted new text should contain green markup for all added characters");
	}

	[TestMethod]
	public void CreateInlineCharacterDiff_WithNullOldLine_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			CharacterLevelDiffer.CreateInlineCharacterDiff(null!, "new line"));
	}

	[TestMethod]
	public void CreateInlineCharacterDiff_WithNullNewLine_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			CharacterLevelDiffer.CreateInlineCharacterDiff("old line", null!));
	}

	[TestMethod]
	public void CreateInlineCharacterDiff_WithDifferentLines_ReturnsFormattedDiff()
	{
		// Arrange
		string oldLine = "hello world";
		string newLine = "hello earth";

		// Act
		string result = CharacterLevelDiffer.CreateInlineCharacterDiff(oldLine, newLine);

		// Assert
		Assert.IsTrue(result.Contains("[red]- "), "Inline diff result should contain red deletion prefix");
		Assert.IsTrue(result.Contains("[green]+ "), "Inline diff result should contain green addition prefix");
		// Each character is individually wrapped in markup tags, so check for individual characters
		Assert.IsTrue(result.Contains('h'), "Inline diff result should contain character 'h'");
		Assert.IsTrue(result.Contains('e'), "Inline diff result should contain character 'e'");
		Assert.IsTrue(result.Contains('l'), "Inline diff result should contain character 'l'");
		Assert.IsTrue(result.Contains('o'), "Inline diff result should contain character 'o'");
	}

	[TestMethod]
	public void AreLinesSimilar_WithNullLine1_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			CharacterLevelDiffer.AreLinesSimilar(null!, "line2"));
	}

	[TestMethod]
	public void AreLinesSimilar_WithNullLine2_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.ThrowsException<ArgumentNullException>(() =>
			CharacterLevelDiffer.AreLinesSimilar("line1", null!));
	}

	[TestMethod]
	public void AreLinesSimilar_WithEmptyLines_ReturnsFalse()
	{
		// Act
		bool result = CharacterLevelDiffer.AreLinesSimilar("", "");

		// Assert
		Assert.IsFalse(result, "Empty lines should not be considered similar");
	}

	[TestMethod]
	public void AreLinesSimilar_WithOneEmptyLine_ReturnsFalse()
	{
		// Act
		bool result1 = CharacterLevelDiffer.AreLinesSimilar("", "hello");
		bool result2 = CharacterLevelDiffer.AreLinesSimilar("hello", "");

		// Assert
		Assert.IsFalse(result1, "Empty line compared to non-empty line should not be similar");
		Assert.IsFalse(result2, "Non-empty line compared to empty line should not be similar");
	}

	[TestMethod]
	public void AreLinesSimilar_WithIdenticalLines_ReturnsTrue()
	{
		// Arrange
		string line = "hello world";

		// Act
		bool result = CharacterLevelDiffer.AreLinesSimilar(line, line);

		// Assert
		Assert.IsTrue(result, "Identical lines should be considered similar");
	}

	[TestMethod]
	public void AreLinesSimilar_WithSimilarLines_ReturnsTrue()
	{
		// Arrange
		string line1 = "hello world";
		string line2 = "hello earth";

		// Act
		bool result = CharacterLevelDiffer.AreLinesSimilar(line1, line2);

		// Assert
		Assert.IsTrue(result, "Lines with common prefix 'hello ' should be considered similar");
	}

	[TestMethod]
	public void AreLinesSimilar_WithVeryDifferentLines_ReturnsFalse()
	{
		// Arrange
		string line1 = "hello world";
		string line2 = "xyz abc def";

		// Act
		bool result = CharacterLevelDiffer.AreLinesSimilar(line1, line2);

		// Assert
		Assert.IsFalse(result, "Lines with no common content should not be considered similar");
	}

	[TestMethod]
	public void AreLinesSimilar_WithVeryDifferentLengths_ReturnsFalse()
	{
		// Arrange
		string line1 = "hi";
		string line2 = "this is a very long line that is much longer than the first";

		// Act
		bool result = CharacterLevelDiffer.AreLinesSimilar(line1, line2);

		// Assert
		Assert.IsFalse(result, "Lines with very different lengths should not be considered similar");
	}

	[TestMethod]
	public void AreLinesSimilar_WithModeratelyDifferentLines_ReturnsAppropriateResult()
	{
		// Arrange
		string line1 = "function calculateTotal()";
		string line2 = "function calculateSum()";

		// Act
		bool result = CharacterLevelDiffer.AreLinesSimilar(line1, line2);

		// Assert
		Assert.IsTrue(result, "Lines with common prefix 'function calculate' should be considered similar");
	}

	[TestMethod]
	public void CreateSideBySideCharacterDiff_WithDifferentLines_ReturnsFormattedSides()
	{
		// Arrange
		string oldLine = "hello world";
		string newLine = "hello earth";

		// Act
		(string leftSide, string rightSide) = CharacterLevelDiffer.CreateSideBySideCharacterDiff(oldLine, newLine);

		// Assert
		Assert.IsNotNull(leftSide);
		Assert.IsNotNull(rightSide);

		// The CreateSideBySideCharacterDiff method applies character-level markup and whitespace visualization
		// Each character is individually wrapped in markup tags, so we need to check for individual characters
		Assert.IsTrue(leftSide.Contains('h'), "Left side should contain character 'h'");
		Assert.IsTrue(leftSide.Contains('e'), "Left side should contain character 'e'");
		Assert.IsTrue(leftSide.Contains('l'), "Left side should contain character 'l'");
		Assert.IsTrue(leftSide.Contains('o'), "Left side should contain character 'o'");
		Assert.IsTrue(rightSide.Contains('h'), "Right side should contain character 'h'");
		Assert.IsTrue(rightSide.Contains('e'), "Right side should contain character 'e'");
		Assert.IsTrue(rightSide.Contains('l'), "Right side should contain character 'l'");
		Assert.IsTrue(rightSide.Contains('o'), "Right side should contain character 'o'");

		// Verify whitespace visualization is applied (spaces become middle dots)
		Assert.IsTrue(leftSide.Contains('·') || rightSide.Contains('·'), "Whitespace should be visualized as middle dots in at least one side");

		// Verify markup tags are present
		Assert.IsTrue(leftSide.Contains("[dim white]") || leftSide.Contains("[red]"), "Left side should contain dim white or red markup");
		Assert.IsTrue(rightSide.Contains("[dim white]") || rightSide.Contains("[green]"), "Right side should contain dim white or green markup");
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithSpecialCharacters_EscapesMarkupProperly()
	{
		// Arrange
		string oldText = "hello [world]";
		string newText = "hello [earth]";

		// Act
		(string highlightedOld, string highlightedNew) = CharacterLevelDiffer.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		// Should contain escaped brackets
		Assert.IsTrue(highlightedOld.Contains("[["), "Highlighted old text should contain escaped opening brackets");
		Assert.IsTrue(highlightedNew.Contains("[["), "Highlighted new text should contain escaped opening brackets");
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithWhitespaceChanges_HighlightsWhitespace()
	{
		// Arrange
		string oldText = "hello world";
		string newText = "hello  world"; // Extra space

		// Act
		(string highlightedOld, string highlightedNew) = CharacterLevelDiffer.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		Assert.IsTrue(highlightedOld.Contains("[dim white]"), "Highlighted old text should contain dim white markup for unchanged characters");
		Assert.IsTrue(highlightedNew.Contains("[green]"), "Highlighted new text should contain green markup for the added space");
	}
}
