// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.BlastMerge.Services;

using System;
using System.Collections.Generic;

/// <summary>
/// Detects the line-ending style of file content and splits content into lines without losing it.
/// </summary>
/// <remarks>
/// Reading a file with <c>ReadAllLines</c>, or splitting it on a single hardcoded style, discards
/// the line endings the file actually used. Every site that later reassembles those lines then has
/// to guess, and the merge pipeline historically guessed differently in different places —
/// <see cref="Environment.NewLine"/> in some paths, a hardcoded <c>\n</c> in others — so the style
/// of a merged file depended on which command produced it and which OS it ran on. The pipeline now
/// detects the style once at read time and carries it on the <see cref="Models.MergeResult"/>, so
/// there is exactly one decision instead of one per write site.
/// </remarks>
public static class LineEndingDetector
{
	/// <summary>
	/// Windows-style line ending (carriage return followed by line feed).
	/// </summary>
	public const string CarriageReturnLineFeed = "\r\n";

	/// <summary>
	/// Unix-style line ending (line feed).
	/// </summary>
	public const string LineFeed = "\n";

	/// <summary>
	/// Classic Mac-style line ending (carriage return).
	/// </summary>
	public const string CarriageReturn = "\r";

	private static readonly string[] AllLineEndings = [CarriageReturnLineFeed, LineFeed, CarriageReturn];

	/// <summary>
	/// Detects the dominant line-ending style used by the given content.
	/// </summary>
	/// <param name="content">The raw file content to inspect.</param>
	/// <returns>
	/// The most frequently occurring line ending in <paramref name="content"/>. Ties resolve in
	/// favour of <see cref="CarriageReturnLineFeed"/> and then <see cref="LineFeed"/>. Content with
	/// no line ending at all yields <see cref="Environment.NewLine"/>, since there is nothing to
	/// preserve and the platform default is the least surprising choice for newly created lines.
	/// </returns>
	public static string Detect(string content)
	{
		Ensure.NotNull(content);

		(int crlf, int lf, int cr) = Count(content);
		return Resolve(crlf, lf, cr);
	}

	/// <summary>
	/// Detects the dominant line-ending style across both sides of a merge.
	/// </summary>
	/// <param name="content1">The raw content of the first side.</param>
	/// <param name="content2">The raw content of the second side.</param>
	/// <returns>
	/// The most frequently occurring line ending across both inputs, resolved as described on
	/// <see cref="Detect(string)"/>. Counting both sides together means the side that contributes
	/// more lines decides, rather than whichever side happened to be read first.
	/// </returns>
	public static string Detect(string content1, string content2)
	{
		Ensure.NotNull(content1);
		Ensure.NotNull(content2);

		(int crlf1, int lf1, int cr1) = Count(content1);
		(int crlf2, int lf2, int cr2) = Count(content2);
		return Resolve(crlf1 + crlf2, lf1 + lf2, cr1 + cr2);
	}

	/// <summary>
	/// Splits content into lines, accepting any line-ending style.
	/// </summary>
	/// <param name="content">The raw file content to split.</param>
	/// <returns>
	/// The lines of <paramref name="content"/>, matching the semantics of
	/// <c>File.ReadAllLines</c>: empty content yields no lines, and a trailing line ending does not
	/// produce a final empty line.
	/// </returns>
	/// <remarks>
	/// Splitting on <see cref="Environment.NewLine"/> alone leaves a stray carriage return on the
	/// end of every line when CRLF content is read on a non-Windows host, which then travels into
	/// the diff and the merged output.
	/// </remarks>
	public static string[] SplitLines(string content)
	{
		Ensure.NotNull(content);

		if (content.Length == 0)
		{
			return [];
		}

		string[] lines = content.Split(AllLineEndings, StringSplitOptions.None);

		// A trailing line ending produces one empty trailing entry that ReadAllLines would not
		// report, because it terminates the last line rather than starting a new one.
		return lines[^1].Length == 0 ? lines[..^1] : lines;
	}

	/// <summary>
	/// Joins lines using an explicit line-ending style.
	/// </summary>
	/// <param name="lines">The lines to join.</param>
	/// <param name="lineEnding">The line ending to place between them.</param>
	/// <returns>The joined content.</returns>
	public static string Join(IEnumerable<string> lines, string lineEnding)
	{
		Ensure.NotNull(lines);
		Ensure.NotNull(lineEnding);

		return string.Join(lineEnding, lines);
	}

	/// <summary>
	/// Counts each line-ending style in the content, attributing each character only once.
	/// </summary>
	/// <param name="content">The content to scan.</param>
	/// <returns>The number of CRLF, lone LF, and lone CR occurrences.</returns>
	private static (int Crlf, int Lf, int Cr) Count(string content)
	{
		int crlf = 0;
		int lf = 0;
		int cr = 0;

		for (int i = 0; i < content.Length; i++)
		{
			char current = content[i];

			if (current == '\r')
			{
				if (i + 1 < content.Length && content[i + 1] == '\n')
				{
					crlf++;

					// Skip the line feed so a CRLF is not also counted as a lone LF.
					i++;
				}
				else
				{
					cr++;
				}
			}
			else if (current == '\n')
			{
				lf++;
			}
		}

		return (crlf, lf, cr);
	}

	/// <summary>
	/// Picks the winning style from the counted occurrences.
	/// </summary>
	/// <param name="crlf">The number of CRLF occurrences.</param>
	/// <param name="lf">The number of lone LF occurrences.</param>
	/// <param name="cr">The number of lone CR occurrences.</param>
	/// <returns>The dominant line ending, or <see cref="Environment.NewLine"/> if there were none.</returns>
	private static string Resolve(int crlf, int lf, int cr)
	{
		if (crlf == 0 && lf == 0 && cr == 0)
		{
			return Environment.NewLine;
		}

		if (crlf >= lf && crlf >= cr)
		{
			return CarriageReturnLineFeed;
		}

		return lf >= cr ? LineFeed : CarriageReturn;
	}
}
