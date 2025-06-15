// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Services;

using System;
using System.Text;
using Spectre.Console;

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
			if (text[i] is not (' ' or '\t'))
			{
				break;
			}
			trailingStart = i;
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

		// If we reach here, highlightTrailing is false and showWhitespace must be true
		return MakeWhitespaceVisible(line);
	}

	/// <summary>
	/// Processes a line for display with proper markup handling.
	/// This method ensures that markup escaping happens before whitespace markup is added.
	/// </summary>
	/// <param name="line">The line to process</param>
	/// <param name="showWhitespace">Whether to show whitespace characters</param>
	/// <param name="highlightTrailing">Whether to highlight trailing whitespace</param>
	/// <returns>Processed line ready for markup display</returns>
	public static string ProcessLineForMarkupDisplay(string? line, bool showWhitespace = true, bool highlightTrailing = true)
	{
		if (string.IsNullOrEmpty(line))
		{
			return string.Empty;
		}

		if (!showWhitespace && !highlightTrailing)
		{
			return Markup.Escape(line);
		}

		if (highlightTrailing)
		{
			return ProcessLineWithTrailingHighlight(line, showWhitespace);
		}

		return ProcessLineWithWhitespace(line);
	}

	/// <summary>
	/// Processes a line with trailing whitespace highlighting
	/// </summary>
	private static string ProcessLineWithTrailingHighlight(string line, bool showWhitespace)
	{
		int trailingStart = FindTrailingWhitespaceStart(line);

		return trailingStart >= line.Length ?
			ProcessLineWithWhitespace(line, showWhitespace) :
			BuildLineWithTrailingHighlight(line, trailingStart, showWhitespace);
	}

	/// <summary>
	/// Processes a line with whitespace visualization only
	/// </summary>
	private static string ProcessLineWithWhitespace(string line, bool showWhitespace = true)
	{
		string processed = showWhitespace ? MakeWhitespaceVisible(line) : line;
		return Markup.Escape(processed);
	}

	/// <summary>
	/// Finds the start position of trailing whitespace
	/// </summary>
	private static int FindTrailingWhitespaceStart(string line)
	{
		int trailingStart = line.Length;
		for (int i = line.Length - 1; i >= 0; i--)
		{
			if (line[i] is ' ' or '\t')
			{
				trailingStart = i;
			}
			else
			{
				break;
			}
		}
		return trailingStart;
	}

	/// <summary>
	/// Builds a line with trailing whitespace highlighted
	/// </summary>
	private static string BuildLineWithTrailingHighlight(string line, int trailingStart, bool showWhitespace)
	{
		StringBuilder result = new();

		// Add non-trailing part (escaped and with whitespace visualization if needed)
		if (trailingStart > 0)
		{
			string beforeTrailing = line[..trailingStart];
			string processedBefore = showWhitespace ? MakeWhitespaceVisible(beforeTrailing) : beforeTrailing;
			result.Append(Markup.Escape(processedBefore));
		}

		// Add highlighted trailing whitespace (escape the content, then add markup)
		string trailingPart = line[trailingStart..];
		string visibleTrailing = MakeWhitespaceVisible(trailingPart);
		result.Append($"[on red]{Markup.Escape(visibleTrailing)}[/]");

		return result.ToString();
	}
}
