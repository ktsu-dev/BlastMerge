// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

/// <summary>
/// Interface for character-level diff visualization with background highlighting
/// </summary>
public interface ICharacterLevelDiffer
{
	/// <summary>
	/// Creates a character-level diff visualization of two strings with background highlighting
	/// </summary>
	/// <param name="oldText">The original text</param>
	/// <param name="newText">The modified text</param>
	/// <returns>A tuple containing the highlighted old and new text with markup</returns>
	public (string highlightedOld, string highlightedNew) CreateCharacterLevelDiff(string oldText, string newText);

	/// <summary>
	/// Creates a character-level inline diff with background highlighting for similar lines
	/// </summary>
	/// <param name="oldLine">The original line</param>
	/// <param name="newLine">The modified line</param>
	/// <returns>A single string showing both old and new with character-level highlighting</returns>
	public string CreateInlineCharacterDiff(string oldLine, string newLine);

	/// <summary>
	/// Determines if two lines are similar enough to benefit from character-level diffing
	/// </summary>
	/// <param name="line1">First line</param>
	/// <param name="line2">Second line</param>
	/// <returns>True if lines are similar enough for character-level diffing</returns>
	public bool AreLinesSimilar(string line1, string line2);

	/// <summary>
	/// Creates a character-level diff for display in side-by-side format
	/// </summary>
	/// <param name="oldLine">Original line</param>
	/// <param name="newLine">Modified line</param>
	/// <returns>Tuple of highlighted old and new text for side-by-side display</returns>
	public (string leftSide, string rightSide) CreateSideBySideCharacterDiff(string oldLine, string newLine);
}
