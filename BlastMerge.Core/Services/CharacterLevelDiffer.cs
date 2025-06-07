// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Text;
using DiffPlex;
using DiffPlex.Model;

/// <summary>
/// Provides character-level diff visualization with background highlighting
/// </summary>
public static class CharacterLevelDiffer
{
	private static readonly Differ Differ = new();

	/// <summary>
	/// Creates a character-level diff visualization of two strings with background highlighting
	/// </summary>
	/// <param name="oldText">The original text</param>
	/// <param name="newText">The modified text</param>
	/// <returns>A tuple containing the highlighted old and new text with markup</returns>
	public static (string highlightedOld, string highlightedNew) CreateCharacterLevelDiff(string oldText, string newText)
	{
		ArgumentNullException.ThrowIfNull(oldText);
		ArgumentNullException.ThrowIfNull(newText);

		// Use DiffPlex character-level diffing
		DiffResult charDiff = Differ.CreateCharacterDiffs(oldText, newText, ignoreWhitespace: false);

		StringBuilder oldHighlighted = new();
		StringBuilder newHighlighted = new();

		int oldIndex = 0;
		int newIndex = 0;

		foreach (DiffBlock block in charDiff.DiffBlocks)
		{
			// Add unchanged characters before this block
			while (oldIndex < block.DeleteStartA)
			{
				string charText = EscapeForMarkup(oldText[oldIndex].ToString());
				oldHighlighted.Append(charText);
				oldIndex++;
			}

			while (newIndex < block.InsertStartB)
			{
				string charText = EscapeForMarkup(newText[newIndex].ToString());
				newHighlighted.Append(charText);
				newIndex++;
			}

			// Handle deleted characters (red background)
			for (int i = 0; i < block.DeleteCountA; i++)
			{
				if (oldIndex < oldText.Length)
				{
					string charText = EscapeForMarkup(oldText[oldIndex].ToString());
					oldHighlighted.Append($"[on red]{charText}[/]");
					oldIndex++;
				}
			}

			// Handle inserted characters (green background)
			for (int i = 0; i < block.InsertCountB; i++)
			{
				if (newIndex < newText.Length)
				{
					string charText = EscapeForMarkup(newText[newIndex].ToString());
					newHighlighted.Append($"[on green]{charText}[/]");
					newIndex++;
				}
			}
		}

		// Add any remaining unchanged characters
		while (oldIndex < oldText.Length)
		{
			string charText = EscapeForMarkup(oldText[oldIndex].ToString());
			oldHighlighted.Append(charText);
			oldIndex++;
		}

		while (newIndex < newText.Length)
		{
			string charText = EscapeForMarkup(newText[newIndex].ToString());
			newHighlighted.Append(charText);
			newIndex++;
		}

		return (oldHighlighted.ToString(), newHighlighted.ToString());
	}

	/// <summary>
	/// Creates a character-level inline diff with background highlighting for similar lines
	/// </summary>
	/// <param name="oldLine">The original line</param>
	/// <param name="newLine">The modified line</param>
	/// <returns>A single string showing both old and new with character-level highlighting</returns>
	public static string CreateInlineCharacterDiff(string oldLine, string newLine)
	{
		ArgumentNullException.ThrowIfNull(oldLine);
		ArgumentNullException.ThrowIfNull(newLine);

		(string highlightedOld, string highlightedNew) = CreateCharacterLevelDiff(oldLine, newLine);

		StringBuilder result = new();
		result.AppendLine($"[red]- {highlightedOld}[/]");
		result.AppendLine($"[green]+ {highlightedNew}[/]");

		return result.ToString();
	}

	/// <summary>
	/// Determines if two lines are similar enough to benefit from character-level diffing
	/// </summary>
	/// <param name="line1">First line</param>
	/// <param name="line2">Second line</param>
	/// <returns>True if lines are similar enough for character-level diffing</returns>
	public static bool AreLinesSimilar(string line1, string line2)
	{
		ArgumentNullException.ThrowIfNull(line1);
		ArgumentNullException.ThrowIfNull(line2);

		// If either line is empty, they're not similar enough
		if (string.IsNullOrEmpty(line1) || string.IsNullOrEmpty(line2))
		{
			return false;
		}

		// Calculate similarity ratio based on Levenshtein distance concept
		int maxLength = Math.Max(line1.Length, line2.Length);
		int minLength = Math.Min(line1.Length, line2.Length);

		// If length difference is too large, they're probably not similar
		if (maxLength > minLength * 3)
		{
			return false;
		}

		// Count common characters (simple heuristic)
		int commonChars = 0;
		int maxCommonLength = Math.Min(line1.Length, line2.Length);

		for (int i = 0; i < maxCommonLength; i++)
		{
			if (line1[i] == line2[i])
			{
				commonChars++;
			}
		}

		// Consider lines similar if they share at least 30% common prefix
		double similarity = (double)commonChars / maxLength;
		return similarity >= 0.3;
	}

	/// <summary>
	/// Escapes text for safe use in Spectre.Console markup
	/// </summary>
	/// <param name="text">Text to escape</param>
	/// <returns>Escaped text safe for markup</returns>
	private static string EscapeForMarkup(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		// Handle special characters that need escaping for Spectre.Console
		return text.Replace("[", "[[").Replace("]", "]]");
	}

	/// <summary>
	/// Creates a character-level diff for display in side-by-side format
	/// </summary>
	/// <param name="oldLine">Original line</param>
	/// <param name="newLine">Modified line</param>
	/// <returns>Tuple of highlighted old and new text for side-by-side display</returns>
	public static (string leftSide, string rightSide) CreateSideBySideCharacterDiff(string oldLine, string newLine)
	{
		ArgumentNullException.ThrowIfNull(oldLine);
		ArgumentNullException.ThrowIfNull(newLine);

		(string highlightedOld, string highlightedNew) = CreateCharacterLevelDiff(oldLine, newLine);

		// Apply whitespace visualization to the character-level diff results
		string leftSide = WhitespaceVisualizer.ProcessLineForMarkupDisplay(highlightedOld);
		string rightSide = WhitespaceVisualizer.ProcessLineForMarkupDisplay(highlightedNew);

		return (leftSide, rightSide);
	}
}
