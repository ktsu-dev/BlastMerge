// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Contracts;

/// <summary>
/// Interface for visualizing whitespace characters in text
/// </summary>
public interface IWhitespaceVisualizer
{
	/// <summary>
	/// Makes whitespace characters visible in text by replacing them with visible symbols
	/// </summary>
	/// <param name="text">The text to process</param>
	/// <param name="showSpaces">Whether to show spaces as visible characters</param>
	/// <param name="showTabs">Whether to show tabs as visible characters</param>
	/// <param name="showLineEndings">Whether to show line endings as visible characters</param>
	/// <returns>Text with whitespace characters made visible</returns>
	public string MakeWhitespaceVisible(string? text, bool showSpaces = true, bool showTabs = true, bool showLineEndings = true);

	/// <summary>
	/// Makes trailing whitespace visible with a special highlight
	/// </summary>
	/// <param name="text">The text to process</param>
	/// <returns>Text with trailing whitespace highlighted</returns>
	public string HighlightTrailingWhitespace(string? text);

	/// <summary>
	/// Creates a legend showing what the whitespace symbols mean
	/// </summary>
	/// <returns>A formatted legend string</returns>
	public string CreateWhitespaceLegend();

	/// <summary>
	/// Processes a line for diff display with whitespace visualization
	/// </summary>
	/// <param name="line">The line to process</param>
	/// <param name="showWhitespace">Whether to show whitespace characters</param>
	/// <param name="highlightTrailing">Whether to highlight trailing whitespace</param>
	/// <returns>Processed line ready for display</returns>
	public string ProcessLineForDisplay(string? line, bool showWhitespace = true, bool highlightTrailing = true);

	/// <summary>
	/// Processes a line for display with proper markup handling.
	/// This method ensures that markup escaping happens before whitespace markup is added.
	/// </summary>
	/// <param name="line">The line to process</param>
	/// <param name="showWhitespace">Whether to show whitespace characters</param>
	/// <param name="highlightTrailing">Whether to highlight trailing whitespace</param>
	/// <returns>Processed line ready for markup display</returns>
	public string ProcessLineForMarkupDisplay(string? line, bool showWhitespace = true, bool highlightTrailing = true);
}
