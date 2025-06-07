// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using System;
using ktsu.BlastMerge.Core.Services;
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
		Assert.IsTrue(highlightedOld.Contains("[dim white]"));
		Assert.IsTrue(highlightedNew.Contains("[dim white]"));
		Assert.IsTrue(highlightedOld.Contains("hello"));
		Assert.IsTrue(highlightedNew.Contains("hello"));
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
		Assert.IsTrue(highlightedOld.Contains("[red]"));
		Assert.IsTrue(highlightedNew.Contains("[green]"));
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
		Assert.IsTrue(highlightedOld.Contains("[dim white]h[/]"));
		Assert.IsTrue(highlightedNew.Contains("[dim white]h[/]"));

		// Should have changes for "world" vs "earth"
		Assert.IsTrue(highlightedOld.Contains("[red]"));
		Assert.IsTrue(highlightedNew.Contains("[green]"));
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
		Assert.IsTrue(highlightedNew.Contains("[green]"));
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
		Assert.IsTrue(result.Contains("[red]- "));
		Assert.IsTrue(result.Contains("[green]+ "));
		Assert.IsTrue(result.Contains("hello"));
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
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void AreLinesSimilar_WithOneEmptyLine_ReturnsFalse()
	{
		// Act
		bool result1 = CharacterLevelDiffer.AreLinesSimilar("", "hello");
		bool result2 = CharacterLevelDiffer.AreLinesSimilar("hello", "");

		// Assert
		Assert.IsFalse(result1);
		Assert.IsFalse(result2);
	}

	[TestMethod]
	public void AreLinesSimilar_WithIdenticalLines_ReturnsTrue()
	{
		// Arrange
		string line = "hello world";

		// Act
		bool result = CharacterLevelDiffer.AreLinesSimilar(line, line);

		// Assert
		Assert.IsTrue(result);
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
		Assert.IsTrue(result); // Should be similar due to common "hello " prefix
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
		Assert.IsFalse(result); // Should not be similar
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
		Assert.IsFalse(result); // Length difference is too large
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
		Assert.IsTrue(result); // Should be similar due to common prefix
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
		Assert.IsTrue(leftSide.Contains("hello"));
		Assert.IsTrue(rightSide.Contains("hello"));
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
		Assert.IsTrue(highlightedOld.Contains("[["));
		Assert.IsTrue(highlightedNew.Contains("[["));
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
		Assert.IsTrue(highlightedOld.Contains("[dim white]"));
		Assert.IsTrue(highlightedNew.Contains("[green]")); // Should highlight the added space
	}
}
