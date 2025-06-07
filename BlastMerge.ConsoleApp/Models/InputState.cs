// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.ConsoleApp.Models;

/// <summary>
/// Represents the current state of input processing
/// </summary>
/// <param name="initialInput">The initial input text</param>
internal class InputState(string initialInput)
{
	/// <summary>
	/// Gets or sets the current input text
	/// </summary>
	public string Input { get; set; } = initialInput;

	/// <summary>
	/// Gets or sets the cursor position within the input
	/// </summary>
	public int CursorPos { get; set; } = initialInput.Length;

	/// <summary>
	/// Gets or sets the history index (-1 means current input, 0+ means history items)
	/// </summary>
	public int HistoryIndex { get; set; } = -1;
}
