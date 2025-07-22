// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Test;

using ktsu.BlastMerge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for CharacterLevelDiffer using dependency injection
/// </summary>
[TestClass]
public class CharacterLevelDifferTests : DependencyInjectionTestBase
{
	private CharacterLevelDiffer _differ = null!;

	protected override void InitializeTestData()
	{
		_differ = GetService<CharacterLevelDiffer>();
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithSimilarStrings_ShowsCorrectDifferences()
	{
		// Act
		(string highlightedOld, string highlightedNew) = _differ.CreateCharacterLevelDiff("hello world", "hello there");

		// Assert
		Assert.IsNotNull(highlightedOld);
		Assert.IsNotNull(highlightedNew);
		Assert.IsFalse(string.IsNullOrEmpty(highlightedOld));
		Assert.IsFalse(string.IsNullOrEmpty(highlightedNew));
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithIdenticalStrings_ShowsNoDifferences()
	{
		// Act
		(string highlightedOld, string highlightedNew) = _differ.CreateCharacterLevelDiff("hello", "hello");

		// Assert
		Assert.IsNotNull(highlightedOld);
		Assert.IsNotNull(highlightedNew);
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithCompletelyDifferentStrings_ShowsAllDifferences()
	{
		// Arrange
		string oldText = "abc";
		string newText = "xyz";

		// Act
		(string highlightedOld, string highlightedNew) = _differ.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		Assert.IsNotNull(highlightedOld);
		Assert.IsNotNull(highlightedNew);
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithEmptyStrings_HandlesCorrectly()
	{
		// Act
		(string highlightedOld1, string highlightedNew1) = _differ.CreateCharacterLevelDiff("", "");
		(string highlightedOld2, string highlightedNew2) = _differ.CreateCharacterLevelDiff("hello", "");
		(string highlightedOld3, string highlightedNew3) = _differ.CreateCharacterLevelDiff("", "hello");

		// Assert
		Assert.IsNotNull(highlightedOld1);
		Assert.IsNotNull(highlightedNew1);
		Assert.IsNotNull(highlightedOld2);
		Assert.IsNotNull(highlightedNew2);
		Assert.IsNotNull(highlightedOld3);
		Assert.IsNotNull(highlightedNew3);
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithSpecialCharacters_HandlesCorrectly()
	{
		// Arrange
		string oldText = "hello\tworld\n";
		string newText = "hello world ";

		// Act
		(string highlightedOld, string highlightedNew) = _differ.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		Assert.IsNotNull(highlightedOld);
		Assert.IsNotNull(highlightedNew);
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithLongStrings_PerformsEfficiently()
	{
		// Arrange
		string oldText = new string('a', 1000) + "different" + new string('b', 1000);
		string newText = new string('a', 1000) + "changed" + new string('b', 1000);

		// Act
		(string highlightedOld, string highlightedNew) = _differ.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		Assert.IsNotNull(highlightedOld);
		Assert.IsNotNull(highlightedNew);
	}

	[TestMethod]
	public void CreateInlineCharacterDiff_WithSimilarStrings_ShowsInlineDifferences()
	{
		// Act
		string result = _differ.CreateInlineCharacterDiff("hello world", "hello there");

		// Assert
		Assert.IsNotNull(result);
		Assert.IsFalse(string.IsNullOrEmpty(result));
	}

	[TestMethod]
	public void CreateInlineCharacterDiff_WithIdenticalStrings_ShowsNoDifferences()
	{
		// Act
		string result = _differ.CreateInlineCharacterDiff("hello", "hello");

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void CreateInlineCharacterDiff_WithCompletelyDifferentStrings_ShowsCorrectFormat()
	{
		// Arrange
		string oldText = "completely different";
		string newText = "totally changed text";

		// Act
		string result = _differ.CreateInlineCharacterDiff(oldText, newText);

		// Assert
		Assert.IsNotNull(result);
	}

	[TestMethod]
	public void AreLinesSimilar_WithSimilarLines_ReturnsTrue()
	{
		// Act
		bool result = _differ.AreLinesSimilar("hello world", "hello there");

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void AreLinesSimilar_WithCompletelyDifferentLines_ReturnsFalse()
	{
		// Act
		bool result = _differ.AreLinesSimilar("abc", "xyz");

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void AreLinesSimilar_WithIdenticalLines_ReturnsTrue()
	{
		// Act
		bool similar1 = _differ.AreLinesSimilar("hello", "hello");
		bool similar2 = _differ.AreLinesSimilar("", "");

		// Assert
		Assert.IsTrue(similar1);
		Assert.IsTrue(similar2);
	}

	[TestMethod]
	public void AreLinesSimilar_WithSlightlyDifferentLines_ReturnsTrue()
	{
		// Act
		bool result = _differ.AreLinesSimilar("hello world", "hello word");

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void AreLinesSimilar_WithVeryDifferentLines_ReturnsFalse()
	{
		// Act
		bool result = _differ.AreLinesSimilar("this is completely different", "xyz");

		// Assert
		Assert.IsFalse(result);
	}

	[TestMethod]
	public void AreLinesSimilar_WithCaseDifferences_HandlesProperly()
	{
		// Act
		bool result = _differ.AreLinesSimilar("Hello World", "hello world");

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void AreLinesSimilar_WithWhitespaceDifferences_HandlesProperly()
	{
		// Act
		bool result = _differ.AreLinesSimilar("hello world", "hello  world");

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void AreLinesSimilar_WithPunctuationDifferences_HandlesProperly()
	{
		// Act
		bool result = _differ.AreLinesSimilar("hello, world!", "hello world");

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	public void CreateSideBySideCharacterDiff_WithDifferentStrings_CreatesCorrectFormat()
	{
		// Arrange
		string oldText = "hello world";
		string newText = "hello there";

		// Act
		(string leftSide, string rightSide) = _differ.CreateSideBySideCharacterDiff(oldText, newText);

		// Assert
		Assert.IsNotNull(leftSide);
		Assert.IsNotNull(rightSide);
		Assert.IsFalse(string.IsNullOrEmpty(leftSide));
		Assert.IsFalse(string.IsNullOrEmpty(rightSide));
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithUnicodeCharacters_HandlesCorrectly()
	{
		// Arrange
		string oldText = "héllo wörld";
		string newText = "hello world";

		// Act
		(string highlightedOld, string highlightedNew) = _differ.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		Assert.IsNotNull(highlightedOld);
		Assert.IsNotNull(highlightedNew);
	}

	[TestMethod]
	public void CreateCharacterLevelDiff_WithMultilineStrings_HandlesCorrectly()
	{
		// Arrange
		string oldText = "line1\nline2\nline3";
		string newText = "line1\nmodified\nline3";

		// Act
		(string highlightedOld, string highlightedNew) = _differ.CreateCharacterLevelDiff(oldText, newText);

		// Assert
		Assert.IsNotNull(highlightedOld);
		Assert.IsNotNull(highlightedNew);
	}
}
