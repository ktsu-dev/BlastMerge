// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Services;

using System;
using System.Text;

/// <summary>
/// Provides functionality to visualize whitespace characters in text
/// </summary>
public static class WhitespaceVisualizer
{
	/// <summary>
	/// Makes whitespace characters visible in text by replacing them with visible symbols
	/// </summary>
	/// <param name="text">The text to process</param>
	/// <param name="showSpaces">Whether to show spaces as visible characters</param>
	/// <param name="showTabs">Whether to show tabs as visible characters</param>
	/// <param name="showLineEndings">Whether to show line endings as visible characters</param>
	/// <returns>Text with whitespace characters made visible</returns>
	public static string MakeWhitespaceVisible(string? text, bool showSpaces = true, bool showTabs = true, bool showLineEndings = true)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		StringBuilder result = new(text.Length * 2); // Allocate extra space for symbols

		foreach (char c in text)
		{
			switch (c)
			{
				case ' ' when showSpaces:
					result.Append('·'); // Middle dot for spaces
					break;
				case '\t' when showTabs:
					result.Append('→'); // Right arrow for tabs
					break;
				case '\r' when showLineEndings:
					result.Append('↵'); // Return symbol
					break;
				case '\n' when showLineEndings:
					result.Append('¶'); // Pilcrow for newlines
					break;
				default:
					result.Append(c);
					break;
			}
		}

		return result.ToString();
	}

	/// <summary>
	/// Makes trailing whitespace visible with a special highlight
	/// </summary>
	/// <param name="text">The text to process</param>
	/// <returns>Text with trailing whitespace highlighted</returns>
	public static string HighlightTrailingWhitespace(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}

		// Find trailing whitespace
		int trailingStart = text.Length;
		for (int i = text.Length - 1; i >= 0; i--)
		{
			if (text[i] is ' ' or '\t')
			{
				trailingStart = i;
			}
			else
			{
				break;
			}
		}

		// If no trailing whitespace, return as-is
		if (trailingStart >= text.Length)
		{
			return text;
		}

		// Build result with highlighted trailing whitespace
		StringBuilder result = new();

		// Add non-trailing part
		if (trailingStart > 0)
		{
			result.Append(text[..trailingStart]);
		}

		// Add highlighted trailing whitespace
		string trailingPart = text[trailingStart..];
		string visibleTrailing = MakeWhitespaceVisible(trailingPart);
		result.Append($"[on red]{visibleTrailing}[/]"); // Red background for trailing whitespace

		return result.ToString();
	}

	/// <summary>
	/// Creates a legend showing what the whitespace symbols mean
	/// </summary>
	/// <returns>A formatted legend string</returns>
	public static string CreateWhitespaceLegend() =>
		"[dim]Whitespace: [/]" +
		"[dim]· = space  → = tab  ↵ = return  ¶ = newline  [/]" +
		"[on red dim]red background = trailing whitespace[/]";

	/// <summary>
	/// Processes a line for diff display with whitespace visualization
	/// </summary>
	/// <param name="line">The line to process</param>
	/// <param name="showWhitespace">Whether to show whitespace characters</param>
	/// <param name="highlightTrailing">Whether to highlight trailing whitespace</param>
	/// <returns>Processed line ready for display</returns>
	public static string ProcessLineForDisplay(string? line, bool showWhitespace = true, bool highlightTrailing = true)
	{
		if (string.IsNullOrEmpty(line))
		{
			return string.Empty;
		}

		if (!showWhitespace && !highlightTrailing)
		{
			return line;
		}

		if (highlightTrailing)
		{
			string highlighted = HighlightTrailingWhitespace(line);
			if (showWhitespace)
			{
				// Apply whitespace visualization to the non-highlighted part
				int redBackgroundStart = highlighted.IndexOf("[on red]", StringComparison.Ordinal);
				if (redBackgroundStart >= 0)
				{
					string beforeTrailing = highlighted[..redBackgroundStart];
					string trailingPart = highlighted[redBackgroundStart..];
					return MakeWhitespaceVisible(beforeTrailing) + trailingPart;
				}
				else
				{
					return MakeWhitespaceVisible(highlighted);
				}
			}
			return highlighted;
		}
		else if (showWhitespace)
		{
			return MakeWhitespaceVisible(line);
		}

		return line;
	}
}
