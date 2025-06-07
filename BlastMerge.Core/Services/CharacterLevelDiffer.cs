// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Text;
using DiffPlex;
using DiffPlex.Model;
using Spectre.Console;

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
			oldIndex = AddUnchangedCharactersBefore(oldText, oldHighlighted, oldIndex, block.DeleteStartA);
			newIndex = AddUnchangedCharactersBefore(newText, newHighlighted, newIndex, block.InsertStartB);

			// Handle deleted and inserted characters
			oldIndex = AddDeletedCharacters(oldText, oldHighlighted, oldIndex, block.DeleteCountA);
			newIndex = AddInsertedCharacters(newText, newHighlighted, newIndex, block.InsertCountB);
		}

		// Add any remaining unchanged characters
		AddRemainingUnchangedCharacters(oldText, oldHighlighted, oldIndex);
		AddRemainingUnchangedCharacters(newText, newHighlighted, newIndex);

		return (oldHighlighted.ToString(), newHighlighted.ToString());
	}

	/// <summary>
	/// Adds unchanged characters before a diff block
	/// </summary>
	private static int AddUnchangedCharactersBefore(string text, StringBuilder highlighted, int currentIndex, int endIndex)
	{
		while (currentIndex < endIndex)
		{
			string charText = EscapeForMarkup(text[currentIndex].ToString());
			highlighted.Append($"[dim white]{charText}[/]");
			currentIndex++;
		}
		return currentIndex;
	}

	/// <summary>
	/// Adds deleted characters with red highlighting
	/// </summary>
	private static int AddDeletedCharacters(string text, StringBuilder highlighted, int currentIndex, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (currentIndex < text.Length)
			{
				string charText = EscapeForMarkup(text[currentIndex].ToString());
				highlighted.Append($"[red]{charText}[/]");
				currentIndex++;
			}
		}
		return currentIndex;
	}

	/// <summary>
	/// Adds inserted characters with green highlighting
	/// </summary>
	private static int AddInsertedCharacters(string text, StringBuilder highlighted, int currentIndex, int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (currentIndex < text.Length)
			{
				string charText = EscapeForMarkup(text[currentIndex].ToString());
				highlighted.Append($"[green]{charText}[/]");
				currentIndex++;
			}
		}
		return currentIndex;
	}

	/// <summary>
	/// Adds remaining unchanged characters at the end
	/// </summary>
	private static void AddRemainingUnchangedCharacters(string text, StringBuilder highlighted, int startIndex)
	{
		while (startIndex < text.Length)
		{
			string charText = EscapeForMarkup(text[startIndex].ToString());
			highlighted.Append($"[dim white]{charText}[/]");
			startIndex++;
		}
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

		// Use Spectre.Console's built-in markup escaping
		return Markup.Escape(text);
	}

	/// <summary>
	/// Applies whitespace visualization to text that already contains markup,
	/// preserving the existing markup tags while making whitespace visible
	/// </summary>
	/// <param name="textWithMarkup">Text that already contains markup tags</param>
	/// <returns>Text with whitespace visualization applied while preserving markup</returns>
	private static string ApplyWhitespaceVisualizationToMarkup(string textWithMarkup)
	{
		if (string.IsNullOrEmpty(textWithMarkup))
		{
			return string.Empty;
		}

		StringBuilder result = new();
		bool insideMarkup = false;

		for (int i = 0; i < textWithMarkup.Length; i++)
		{
			char c = textWithMarkup[i];

			// Track if we're inside a markup tag
			if (c == '[' && i + 1 < textWithMarkup.Length && textWithMarkup[i + 1] != '[')
			{
				insideMarkup = true;
				result.Append(c);
			}
			else if (c == ']' && insideMarkup)
			{
				insideMarkup = false;
				result.Append(c);
			}
			else if (insideMarkup)
			{
				// Inside markup tag, don't modify
				result.Append(c);
			}
			else
			{
				// Outside markup, apply whitespace visualization
				switch (c)
				{
					case ' ':
						result.Append('·'); // Middle dot for spaces
						break;
					case '\t':
						result.Append('→'); // Right arrow for tabs
						break;
					case '\r':
						result.Append('↵'); // Return symbol
						break;
					case '\n':
						result.Append('¶'); // Pilcrow for newlines
						break;
					default:
						result.Append(c);
						break;
				}
			}
		}

		return result.ToString();
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

		// Apply whitespace visualization to the original lines before character-level diffing
		// to preserve the markup from character-level diffing
		string leftSide = ApplyWhitespaceVisualizationToMarkup(highlightedOld);
		string rightSide = ApplyWhitespaceVisualizationToMarkup(highlightedNew);

		return (leftSide, rightSide);
	}
}
