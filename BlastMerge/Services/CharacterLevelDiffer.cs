// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DiffPlex.Model;
using Spectre.Console;

/// <summary>
/// Provides character-level diff visualization with background highlighting
/// </summary>
public static class CharacterLevelDiffer
{
	/// <summary>
	/// Creates a character-level diff visualization of two strings with background highlighting
	/// </summary>
	/// <param name="oldText">The original text</param>
	/// <param name="newText">The modified text</param>
	/// <returns>A tuple containing the highlighted old and new text with markup</returns>
	public static (string highlightedOld, string highlightedNew) CreateCharacterLevelDiff(string oldText, string newText)
	{
		Ensure.NotNull(oldText);
		Ensure.NotNull(newText);

		// Diff whole text elements rather than UTF-16 code units. Emoji in one block share a high
		// surrogate, so a code-unit diff marks only the low surrogate as changed and the pair is
		// split across two markup spans, each half then rendering as U+FFFD.
		string[] oldElements = SplitTextElements(oldText);
		string[] newElements = SplitTextElements(newText);
		DiffResult elementDiff = DiffPlexHelper.CreateLineDiffsFromLines(oldElements, newElements);

		StringBuilder oldHighlighted = new();
		StringBuilder newHighlighted = new();

		int oldIndex = 0;
		int newIndex = 0;

		foreach (DiffBlock block in elementDiff.DiffBlocks)
		{
			// Add unchanged elements before this block
			oldIndex = AppendElements(oldElements, oldHighlighted, oldIndex, block.DeleteStartA - oldIndex, "dim white");
			newIndex = AppendElements(newElements, newHighlighted, newIndex, block.InsertStartB - newIndex, "dim white");

			// Handle deleted and inserted elements
			oldIndex = AppendElements(oldElements, oldHighlighted, oldIndex, block.DeleteCountA, "red");
			newIndex = AppendElements(newElements, newHighlighted, newIndex, block.InsertCountB, "green");
		}

		// Add any remaining unchanged elements
		AppendElements(oldElements, oldHighlighted, oldIndex, oldElements.Length - oldIndex, "dim white");
		AppendElements(newElements, newHighlighted, newIndex, newElements.Length - newIndex, "dim white");

		return (oldHighlighted.ToString(), newHighlighted.ToString());
	}

	/// <summary>
	/// Splits text into text elements (grapheme clusters), so a surrogate pair, or a base
	/// character with its combining marks, is never divided between two markup spans
	/// </summary>
	private static string[] SplitTextElements(string text)
	{
		List<string> elements = [];
		TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
		while (enumerator.MoveNext())
		{
			elements.Add(enumerator.GetTextElement());
		}

		return [.. elements];
	}

	/// <summary>
	/// Appends up to <paramref name="count"/> text elements, each wrapped in the given style
	/// </summary>
	private static int AppendElements(string[] elements, StringBuilder highlighted, int currentIndex, int count, string style)
	{
		int endIndex = Math.Min(elements.Length, currentIndex + Math.Max(0, count));
		while (currentIndex < endIndex)
		{
			highlighted.Append($"[{style}]{EscapeForMarkup(elements[currentIndex])}[/]");
			currentIndex++;
		}

		return currentIndex;
	}

	/// <summary>
	/// Creates a character-level inline diff with background highlighting for similar lines
	/// </summary>
	/// <param name="oldLine">The original line</param>
	/// <param name="newLine">The modified line</param>
	/// <returns>A single string showing both old and new with character-level highlighting</returns>
	public static string CreateInlineCharacterDiff(string oldLine, string newLine)
	{
		Ensure.NotNull(oldLine);
		Ensure.NotNull(newLine);

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
		Ensure.NotNull(line1);
		Ensure.NotNull(line2);

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
		Ensure.NotNull(oldLine);
		Ensure.NotNull(newLine);

		(string highlightedOld, string highlightedNew) = CreateCharacterLevelDiff(oldLine, newLine);

		// Apply whitespace visualization to the original lines before character-level diffing
		// to preserve the markup from character-level diffing
		string leftSide = ApplyWhitespaceVisualizationToMarkup(highlightedOld);
		string rightSide = ApplyWhitespaceVisualizationToMarkup(highlightedNew);

		return (leftSide, rightSide);
	}
}
