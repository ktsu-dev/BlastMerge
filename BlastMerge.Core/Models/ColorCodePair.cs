// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.BlastMerge.Core.Models;

/// <summary>
/// Represents a pair of color codes for formatting text
/// </summary>
public record ColorCodePair
{
	/// <summary>
	/// Gets the prefix color code (e.g., ANSI escape sequence)
	/// </summary>
	public required string Prefix { get; init; }

	/// <summary>
	/// Gets the suffix color code (e.g., reset sequence)
	/// </summary>
	public required string Suffix { get; init; }

	/// <summary>
	/// Creates a color code pair with no formatting
	/// </summary>
	public static ColorCodePair None => new() { Prefix = string.Empty, Suffix = string.Empty };

	/// <summary>
	/// Creates a color code pair with ANSI color formatting
	/// </summary>
	/// <param name="ansiColorCode">The ANSI color code</param>
	/// <param name="resetCode">The reset code (defaults to ANSI reset)</param>
	/// <returns>A color code pair</returns>
	public static ColorCodePair CreateAnsi(string ansiColorCode, string resetCode = "\u001b[0m") =>
		new() { Prefix = ansiColorCode, Suffix = resetCode };
}
